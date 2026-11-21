namespace EndpointPulse.Core.Models;

/// <summary>
/// Health evaluation status of a monitored endpoint.
/// </summary>
public enum HealthStatus
{
    /// <summary>Endpoint responded successfully within expected thresholds.</summary>
    Healthy = 0,

    /// <summary>Endpoint is reachable but responded with high latency or minor anomaly.</summary>
    Degraded = 1,

    /// <summary>Endpoint failed to respond, connection refused, timeout, or 5xx error.</summary>
    Unhealthy = 2,

    /// <summary>SSL/TLS certificate will expire within warning threshold.</summary>
    SslExpiringSoon = 3,

    /// <summary>SSL/TLS certificate is already expired or invalid.</summary>
    SslExpired = 4
}

/// <summary>
/// Protocol type of target endpoint.
/// </summary>
public enum EndpointType
{
    Http,
    Https,
    Tcp
}
