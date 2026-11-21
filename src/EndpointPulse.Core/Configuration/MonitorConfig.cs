namespace EndpointPulse.Core.Configuration;

/// <summary>
/// Target endpoint configuration item.
/// </summary>
public sealed class EndpointTarget
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int TimeoutMs { get; set; } = 5000;
    public int ExpectedStatusCode { get; set; } = 0; // 0 = Any 2xx
    public int LatencyWarningMs { get; set; } = 1000;
    public int SslWarningDays { get; set; } = 14;

    public override string ToString() => string.IsNullOrEmpty(Name) ? Url : $"{Name} ({Url})";
}

/// <summary>
/// Webhook notification destinations and alerting triggers.
/// </summary>
public sealed class WebhookConfig
{
    public string? DiscordWebhookUrl { get; set; }
    public string? TelegramBotToken { get; set; }
    public string? TelegramChatId { get; set; }
    public bool AlertOnDegraded { get; set; } = true;
    public bool AlertOnUnhealthy { get; set; } = true;
    public bool AlertOnSslExpiring { get; set; } = true;

    public bool HasAnyWebhook => !string.IsNullOrWhiteSpace(DiscordWebhookUrl) ||
                                (!string.IsNullOrWhiteSpace(TelegramBotToken) && !string.IsNullOrWhiteSpace(TelegramChatId));
}

/// <summary>
/// Root monitor configuration structure.
/// </summary>
public sealed class MonitorConfig
{
    public List<EndpointTarget> Targets { get; set; } = new();
    public WebhookConfig Webhook { get; set; } = new();
    public int DefaultIntervalSeconds { get; set; } = 30;
}
