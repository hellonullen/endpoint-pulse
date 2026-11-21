namespace EndpointPulse.Core.Engine;

using System.Diagnostics;
using EndpointPulse.Core.Alerting;
using EndpointPulse.Core.Checkers;
using EndpointPulse.Core.Configuration;
using EndpointPulse.Core.Models;

/// <summary>
/// Core execution engine orchestrating endpoint checks, parallel execution, and alert management.
/// </summary>
public sealed class PulseRunner
{
    private readonly List<IEndpointChecker> _checkers = new();
    private readonly AlertManager _alertManager = new();

    public PulseRunner()
    {
        _checkers.Add(new HttpEndpointChecker());
        _checkers.Add(new TcpEndpointChecker());
    }

    /// <summary>
    /// Checks a single endpoint target.
    /// </summary>
    public async Task<CheckResult> CheckSingleAsync(EndpointTarget target, CancellationToken ct = default)
    {
        var checker = _checkers.FirstOrDefault(c => c.CanHandle(target))
                      ?? throw new NotSupportedException($"No suitable checker found for target: {target.Url}");

        return await checker.CheckAsync(target, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Checks a collection of targets concurrently, returning individual results and summary statistics.
    /// </summary>
    public async Task<(IReadOnlyList<CheckResult> Results, ScanSummary Summary)> ScanAsync(
        MonitorConfig config,
        Action<CheckResult>? onResultAvailable = null,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var results = new List<CheckResult>();

        // Run checks in parallel with concurrency throttle (up to 10 concurrent)
        using var semaphore = new SemaphoreSlim(10);
        var tasks = config.Targets.Select(async target =>
        {
            await semaphore.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var result = await CheckSingleAsync(target, ct).ConfigureAwait(false);
                lock (results)
                {
                    results.Add(result);
                }

                onResultAvailable?.Invoke(result);

                // Dispatch alerts if configured
                await _alertManager.ProcessResultAsync(result, config.Webhook, ct).ConfigureAwait(false);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);
        stopwatch.Stop();

        var summary = new ScanSummary
        {
            TotalTargets = results.Count,
            HealthyCount = results.Count(r => r.Status == HealthStatus.Healthy),
            DegradedCount = results.Count(r => r.Status == HealthStatus.Degraded),
            UnhealthyCount = results.Count(r => r.Status == HealthStatus.Unhealthy || r.Status == HealthStatus.SslExpired),
            SslWarningCount = results.Count(r => r.Status == HealthStatus.SslExpiringSoon),
            AverageLatencyMs = results.Count > 0 ? results.Average(r => r.LatencyMs) : 0,
            Duration = stopwatch.Elapsed
        };

        return (results, summary);
    }
}
