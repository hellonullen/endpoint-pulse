namespace EndpointPulse.Core.Checkers;

using System.Diagnostics;
using System.Net.Sockets;
using EndpointPulse.Core.Configuration;
using EndpointPulse.Core.Models;

/// <summary>
/// Probes raw TCP ports (e.g. Postgres 5432, Redis 6379, MySQL 3306, Game server ports).
/// </summary>
public sealed class TcpEndpointChecker : IEndpointChecker
{
    public bool CanHandle(EndpointTarget target)
    {
        return target.Url.StartsWith("tcp://", StringComparison.OrdinalIgnoreCase) ||
               (!target.Url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !target.Url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                target.Url.Contains(':'));
    }

    public async Task<CheckResult> CheckAsync(EndpointTarget target, CancellationToken ct = default)
    {
        string raw = target.Url;
        if (raw.StartsWith("tcp://", StringComparison.OrdinalIgnoreCase))
        {
            raw = raw[6..];
        }

        string[] parts = raw.Split(':');
        if (parts.Length != 2 || !int.TryParse(parts[1], out int port) || port is < 1 or > 65535)
        {
            return new CheckResult
            {
                Target = target.Url,
                Name = target.Name,
                Type = EndpointType.Tcp,
                Status = HealthStatus.Unhealthy,
                ErrorMessage = $"Invalid host:port format: {target.Url}",
                Timestamp = DateTime.UtcNow
            };
        }

        string host = parts[0];
        using var client = new TcpClient();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var timeoutCts = new CancellationTokenSource(target.TimeoutMs);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            await client.ConnectAsync(host, port, linkedCts.Token).ConfigureAwait(false);
            stopwatch.Stop();
            long latencyMs = stopwatch.ElapsedMilliseconds;

            HealthStatus status = latencyMs > target.LatencyWarningMs
                ? HealthStatus.Degraded
                : HealthStatus.Healthy;

            return new CheckResult
            {
                Target = target.Url,
                Name = target.Name,
                Type = EndpointType.Tcp,
                Status = status,
                LatencyMs = latencyMs,
                HttpStatusDescription = "TCP Port Open",
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new CheckResult
            {
                Target = target.Url,
                Name = target.Name,
                Type = EndpointType.Tcp,
                Status = HealthStatus.Unhealthy,
                LatencyMs = stopwatch.ElapsedMilliseconds,
                ErrorMessage = ex.InnerException?.Message ?? ex.Message,
                HttpStatusDescription = "TCP Port Closed / Unreachable",
                Timestamp = DateTime.UtcNow
            };
        }
    }
}
