namespace EndpointPulse.Tests;

using EndpointPulse.Core.Checkers;
using EndpointPulse.Core.Configuration;
using EndpointPulse.Core.Models;
using Xunit;

public class TcpCheckerTests
{
    private readonly TcpEndpointChecker _checker = new();

    [Theory]
    [InlineData("tcp://127.0.0.1:8080", true)]
    [InlineData("localhost:5432", true)]
    [InlineData("10.0.0.1:3306", true)]
    [InlineData("https://example.com", false)]
    [InlineData("http://localhost:5000", false)]
    public void CanHandle_MatchesAppropriateProtocols(string url, bool expected)
    {
        var target = new EndpointTarget { Url = url };
        Assert.Equal(expected, _checker.CanHandle(target));
    }

    [Fact]
    public async Task CheckAsync_InvalidHostPort_ReturnsUnhealthyWithDetail()
    {
        var target = new EndpointTarget
        {
            Name = "Bad Target",
            Url = "tcp://invalid-port-format"
        };

        var result = await _checker.CheckAsync(target);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("Invalid host:port", result.ErrorMessage);
    }
}
