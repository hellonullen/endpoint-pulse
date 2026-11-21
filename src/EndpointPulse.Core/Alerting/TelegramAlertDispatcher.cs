namespace EndpointPulse.Core.Alerting;

using System.Text;
using System.Text.Json;
using EndpointPulse.Core.Configuration;
using EndpointPulse.Core.Models;

/// <summary>
/// Dispatches formatted Markdown notification alerts to Telegram chats via Bot API.
/// </summary>
public sealed class TelegramAlertDispatcher : IAlertDispatcher
{
    private static readonly HttpClient HttpClient = new();

    public async Task DispatchAsync(CheckResult result, WebhookConfig config, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(config.TelegramBotToken) || string.IsNullOrWhiteSpace(config.TelegramChatId))
        {
            return;
        }

        string statusEmoji = result.Status switch
        {
            HealthStatus.Healthy => "🟢",
            HealthStatus.Degraded => "🟡",
            HealthStatus.SslExpiringSoon => "⏳",
            _ => "🔴"
        };

        var sb = new StringBuilder();
        sb.AppendLine($"{statusEmoji} *EndpointPulse Alert*");
        sb.AppendLine($"*Target:* `{result.Name}`");
        sb.AppendLine($"*URL:* `{result.Target}`");
        sb.AppendLine($"*Status:* `{result.Status}`");
        sb.AppendLine($"*Latency:* `{result.LatencyMs} ms`");

        if (result.HttpStatusCode.HasValue)
        {
            sb.AppendLine($"*HTTP Code:* `{result.HttpStatusCode} {result.HttpStatusDescription}`");
        }

        if (result.SslInfo is not null)
        {
            sb.AppendLine($"*SSL Expiry:* `{result.SslInfo.DaysRemaining} days remaining`");
        }

        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            sb.AppendLine($"*Error:* `{result.ErrorMessage}`");
        }

        sb.AppendLine($"_Timestamp: {result.Timestamp:yyyy-MM-dd HH:mm:ss} UTC_");

        string url = $"https://api.telegram.org/bot{config.TelegramBotToken}/sendMessage";
        var payload = new
        {
            chat_id = config.TelegramChatId,
            text = sb.ToString(),
            parse_mode = "Markdown"
        };

        string json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            await HttpClient.PostAsync(url, content, ct).ConfigureAwait(false);
        }
        catch
        {
            // Suppress network dispatch errors
        }
    }
}
