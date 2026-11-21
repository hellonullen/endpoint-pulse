namespace EndpointPulse.Core.Checkers;

using System.Diagnostics;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using EndpointPulse.Core.Configuration;
using EndpointPulse.Core.Models;

/// <summary>
/// Probes HTTP and HTTPS endpoints, measuring status code, latency, and extracting SSL certificate expiration.
/// </summary>
public sealed class HttpEndpointChecker : IEndpointChecker
{
    public bool CanHandle(EndpointTarget target)
    {
        return target.Url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
               target.Url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<CheckResult> CheckAsync(EndpointTarget target, CancellationToken ct = default)
    {
        bool isHttps = target.Url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        SslCertificateInfo? sslInfo = null;

        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5,
            ConnectTimeout = TimeSpan.FromMilliseconds(target.TimeoutMs),
            SslOptions = new SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = (sender, cert, chain, errors) =>
                {
                    if (cert is not null)
                    {
                        var x509 = new X509Certificate2(cert);
                        sslInfo = new SslCertificateInfo
                        {
                            Subject = x509.Subject,
                            Issuer = x509.Issuer,
                            ValidFrom = x509.NotBefore.ToUniversalTime(),
                            ValidTo = x509.NotAfter.ToUniversalTime()
                        };
                    }
                    return true; // We don't fail handshake to allow capturing expired cert diagnostics
                }
            }
        };

        using var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMilliseconds(target.TimeoutMs)
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("EndpointPulse/1.0 (+https://github.com/hellonullen/endpoint-pulse)");

        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var response = await client.GetAsync(target.Url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            stopwatch.Stop();
            long latencyMs = stopwatch.ElapsedMilliseconds;

            int statusCode = (int)response.StatusCode;
            string statusDesc = response.StatusCode.ToString();

            // Evaluate health
            HealthStatus status;
            bool statusMatches = target.ExpectedStatusCode > 0
                ? statusCode == target.ExpectedStatusCode
                : response.IsSuccessStatusCode;

            if (!statusMatches)
            {
                status = HealthStatus.Unhealthy;
            }
            else if (sslInfo is not null && sslInfo.IsExpired)
            {
                status = HealthStatus.SslExpired;
            }
            else if (sslInfo is not null && sslInfo.IsExpiringSoon(target.SslWarningDays))
            {
                status = HealthStatus.SslExpiringSoon;
            }
            else if (latencyMs > target.LatencyWarningMs)
            {
                status = HealthStatus.Degraded;
            }
            else
            {
                status = HealthStatus.Healthy;
            }

            return new CheckResult
            {
                Target = target.Url,
                Name = target.Name,
                Type = isHttps ? EndpointType.Https : EndpointType.Http,
                Status = status,
                LatencyMs = latencyMs,
                HttpStatusCode = statusCode,
                HttpStatusDescription = statusDesc,
                SslInfo = sslInfo,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new CheckResult
            {
                Target = target.Url,
                Name = target.Name,
                Type = isHttps ? EndpointType.Https : EndpointType.Http,
                Status = HealthStatus.Unhealthy,
                LatencyMs = stopwatch.ElapsedMilliseconds,
                ErrorMessage = ex.InnerException?.Message ?? ex.Message,
                SslInfo = sslInfo,
                Timestamp = DateTime.UtcNow
            };
        }
    }
}
