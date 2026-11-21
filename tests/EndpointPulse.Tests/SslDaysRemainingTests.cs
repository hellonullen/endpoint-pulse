namespace EndpointPulse.Tests;

using EndpointPulse.Core.Models;
using Xunit;

public class SslDaysRemainingTests
{
    [Fact]
    public void DaysRemaining_FutureDate_CalculatesAccurately()
    {
        var ssl = new SslCertificateInfo
        {
            Subject = "CN=test.com",
            Issuer = "CN=Let's Encrypt",
            ValidFrom = DateTime.UtcNow.AddDays(-30),
            ValidTo = DateTime.UtcNow.AddDays(25)
        };

        Assert.False(ssl.IsExpired);
        Assert.InRange(ssl.DaysRemaining, 24, 25);
        Assert.True(ssl.IsExpiringSoon(30));
        Assert.False(ssl.IsExpiringSoon(10));
    }

    [Fact]
    public void DaysRemaining_ExpiredDate_FlagsExpiredCorrectly()
    {
        var ssl = new SslCertificateInfo
        {
            Subject = "CN=expired.com",
            ValidFrom = DateTime.UtcNow.AddDays(-100),
            ValidTo = DateTime.UtcNow.AddDays(-2)
        };

        Assert.True(ssl.IsExpired);
        Assert.Equal(0, ssl.DaysRemaining);
    }
}
