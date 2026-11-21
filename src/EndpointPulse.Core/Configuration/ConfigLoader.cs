namespace EndpointPulse.Core.Configuration;

using System.Text.Json;

/// <summary>
/// Loads and validates JSON configuration files for endpoints and alert webhooks.
/// </summary>
public static class ConfigLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static MonitorConfig LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Configuration file not found: {filePath}", filePath);
        }

        string json = File.ReadAllText(filePath);
        return LoadFromJson(json);
    }

    public static MonitorConfig LoadFromJson(string json)
    {
        var config = JsonSerializer.Deserialize<MonitorConfig>(json, JsonOptions);
        if (config is null)
        {
            throw new InvalidOperationException("Failed to deserialize monitor configuration.");
        }

        // Sanitize defaults
        foreach (var target in config.Targets)
        {
            if (string.IsNullOrWhiteSpace(target.Name))
            {
                target.Name = target.Url;
            }

            if (target.TimeoutMs <= 0) target.TimeoutMs = 5000;
            if (target.LatencyWarningMs <= 0) target.LatencyWarningMs = 1000;
            if (target.SslWarningDays <= 0) target.SslWarningDays = 14;
        }

        return config;
    }
}
