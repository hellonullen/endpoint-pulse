namespace EndpointPulse.Core.Models;

/// <summary>
/// Detailed result of a single endpoint probe.
/// </summary>
public sealed record CheckResult
{
    public string Target { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public EndpointType Type { get; init; }
    public HealthStatus Status { get; init; }
    public long LatencyMs { get; init; }
    public int? HttpStatusCode { get; init; }
    public string? HttpStatusDescription { get; init; }
    public SslCertificateInfo? SslInfo { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public bool IsSuccess => Status is HealthStatus.Healthy or HealthStatus.Degraded or HealthStatus.SslExpiringSoon;
}

/// <summary>
/// Aggregated statistics of an endpoint batch scan.
/// </summary>
public sealed record ScanSummary
{
    public int TotalTargets { get; init; }
    public int HealthyCount { get; init; }
    public int DegradedCount { get; init; }
    public int UnhealthyCount { get; init; }
    public int SslWarningCount { get; init; }
    public double AverageLatencyMs { get; init; }
    public TimeSpan Duration { get; init; }
}
