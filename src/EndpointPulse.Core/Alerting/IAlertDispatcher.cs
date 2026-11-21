namespace EndpointPulse.Core.Alerting;

using EndpointPulse.Core.Configuration;
using EndpointPulse.Core.Models;

/// <summary>
/// Dispatcher interface for sending notification alerts to external webhooks.
/// </summary>
public interface IAlertDispatcher
{
    Task DispatchAsync(CheckResult result, WebhookConfig config, CancellationToken ct = default);
}
