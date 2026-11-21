namespace EndpointPulse.Core.Models;

/// <summary>
/// Extracted SSL/TLS certificate metadata and expiration countdown.
/// </summary>
public sealed record SslCertificateInfo
{
    public string Subject { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public DateTime ValidFrom { get; init; }
    public DateTime ValidTo { get; init; }
    public int DaysRemaining => Math.Max(0, (int)Math.Floor((ValidTo - DateTime.UtcNow).TotalDays));
    public bool IsExpired => DateTime.UtcNow > ValidTo;

    public bool IsExpiringSoon(int thresholdDays) => !IsExpired && DaysRemaining <= thresholdDays;

    public override string ToString()
    {
        return IsExpired
            ? $"EXPIRED ({ValidTo:yyyy-MM-dd})"
            : $"{DaysRemaining} days remaining (Expires: {ValidTo:yyyy-MM-dd})";
    }
}
