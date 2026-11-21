namespace EndpointPulse.Core.Alerting;

using System.Text;
using System.Text.Json;
using EndpointPulse.Core.Configuration;
using EndpointPulse.Core.Models;

/// <summary>
/// Formats and dispatches rich visual embeds to Discord webhooks.
/// </summary>
public sealed class DiscordWebhookDispatcher : IAlertDispatcher
{
    private static readonly HttpClient HttpClient = new();

    public async Task DispatchAsync(CheckResult result, WebhookConfig config, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(config.DiscordWebhookUrl))
        {
            return;
        }

        int embedColor = result.Status switch
        {
            HealthStatus.Healthy => 0x2ECC71,       // Green
            HealthStatus.Degraded => 0xF1C40F,      // Yellow
            HealthStatus.SslExpiringSoon => 0xE67E22,// Orange
            _ => 0xE74C3C                           // Red
        };

        string statusEmoji = result.Status switch
        {
            HealthStatus.Healthy => "🟢",
            HealthStatus.Degraded => "🟡",
            HealthStatus.SslExpiringSoon => "⏳",
            _ => "🔴"
        };

        var fields = new List<object>
        {
            new { name = "Target URL", value = $"`{result.Target}`", @inline = true },
            new { name = "Latency", value = $"`{result.LatencyMs} ms`", @inline = true }
        };

        if (result.HttpStatusCode.HasValue)
        {
            fields.Add(new { name = "HTTP Status", value = $"`{result.HttpStatusCode} {result.HttpStatusDescription}`", @inline = true });
        }

        if (result.SslInfo is not null)
        {
            fields.Add(new { name = "SSL Certificate", value = $"`{result.SslInfo.DaysRemaining} days remaining`", @inline = true });
        }

        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            fields.Add(new { name = "Error Detail", value = $"```{result.ErrorMessage}```", @inline = false });
        }

        var payload = new
        {
            username = "EndpointPulse",
            avatar_url = "https://raw.githubusercontent.com/hellonullen/endpoint-pulse/main/assets/pulse_logo.png",
            embeds = new[]
            {
                new
                {
                    title = $"{statusEmoji} Endpoint Alert: {result.Name} is {result.Status}",
                    description = $"Automated health check notice triggered at {result.Timestamp:yyyy-MM-dd HH:mm:ss} UTC.",
                    color = embedColor,
                    fields = fields.ToArray(),
                    footer = new { text = "EndpointPulse CLI Monitor • https://github.com/hellonullen/endpoint-pulse" }
                }
            }
        };

        string json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            await HttpClient.PostAsync(config.DiscordWebhookUrl, content, ct).ConfigureAwait(false);
        }
        catch
        {
            // Suppress network dispatch errors to avoid crashing monitor loop
        }
    }
}
