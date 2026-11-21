namespace EndpointPulse.Core.Checkers;

using EndpointPulse.Core.Configuration;
using EndpointPulse.Core.Models;

/// <summary>
/// Probing engine interface for checking health and latency of an endpoint target.
/// </summary>
public interface IEndpointChecker
{
    /// <summary>
    /// Checks if this checker supports the target URL/protocol.
    /// </summary>
    bool CanHandle(EndpointTarget target);

    /// <summary>
    /// Executes the probe against the target.
    /// </summary>
    Task<CheckResult> CheckAsync(EndpointTarget target, CancellationToken ct = default);
}
