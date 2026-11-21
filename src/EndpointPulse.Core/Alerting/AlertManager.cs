namespace EndpointPulse.Core.Alerting;

using System.Collections.Concurrent;
using EndpointPulse.Core.Configuration;
using EndpointPulse.Core.Models;

/// <summary>
/// Coordinates multiple alert dispatchers and manages state transition deduplication.
/// </summary>
public sealed class AlertManager
{
    private readonly List<IAlertDispatcher> _dispatchers = new();
    private readonly ConcurrentDictionary<string, HealthStatus> _lastKnownState = new();

    public AlertManager()
    {
        _dispatchers.Add(new DiscordWebhookDispatcher());
        _dispatchers.Add(new TelegramAlertDispatcher());
    }

    public async Task ProcessResultAsync(CheckResult result, WebhookConfig config, CancellationToken ct = default)
    {
        if (!config.HasAnyWebhook)
        {
            return;
        }

        bool shouldAlert = false;
        _lastKnownState.TryGetValue(result.Target, out var lastState);

        // State change detection (e.g. Healthy -> Unhealthy or Recovery Unhealthy -> Healthy)
        bool stateChanged = lastState != result.Status;

        if (stateChanged)
        {
            if (result.Status == HealthStatus.Unhealthy && config.AlertOnUnhealthy) shouldAlert = true;
            if (result.Status == HealthStatus.Degraded && config.AlertOnDegraded) shouldAlert = true;
            if (result.Status == HealthStatus.SslExpiringSoon && config.AlertOnSslExpiring) shouldAlert = true;
            if (result.Status == HealthStatus.Healthy && lastState == HealthStatus.Unhealthy) shouldAlert = true; // Recovery
        }

        _lastKnownState[result.Target] = result.Status;

        if (!shouldAlert)
        {
            return;
        }

        var tasks = _dispatchers.Select(d => d.DispatchAsync(result, config, ct));
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}
