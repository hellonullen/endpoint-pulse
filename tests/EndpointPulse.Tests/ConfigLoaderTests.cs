namespace EndpointPulse.Tests;

using EndpointPulse.Core.Configuration;
using Xunit;

public class ConfigLoaderTests
{
    [Fact]
    public void LoadFromJson_ValidJson_ShouldPopulateTargetsAndDefaults()
    {
        string json = """
        {
          "defaultIntervalSeconds": 45,
          "webhook": {
            "discordWebhookUrl": "https://discord.com/api/webhooks/test",
            "alertOnUnhealthy": true
          },
          "targets": [
            {
              "name": "Production API",
              "url": "https://api.example.com",
              "timeoutMs": 4000,
              "latencyWarningMs": 500
            },
            {
              "url": "tcp://db.local:5432"
            }
          ]
        }
        """;

        var config = ConfigLoader.LoadFromJson(json);

        Assert.Equal(45, config.DefaultIntervalSeconds);
        Assert.True(config.Webhook.HasAnyWebhook);
        Assert.Equal(2, config.Targets.Count);

        var target1 = config.Targets[0];
        Assert.Equal("Production API", target1.Name);
        Assert.Equal(4000, target1.TimeoutMs);
        Assert.Equal(500, target1.LatencyWarningMs);

        // Target 2 should inherit defaults and use URL as Name
        var target2 = config.Targets[1];
        Assert.Equal("tcp://db.local:5432", target2.Name);
        Assert.Equal(5000, target2.TimeoutMs);
        Assert.Equal(1000, target2.LatencyWarningMs);
    }
}
