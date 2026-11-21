namespace EndpointPulse.Core.Checkers;

using System.Diagnostics;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using EndpointPulse.Core.Models;

/// <summary>
/// Direct TLS handshake probe to retrieve SSL certificate details without HTTP layer.
/// </summary>
public static class SslCertificateChecker
{
    public static async Task<SslCertificateInfo?> ProbeCertificateAsync(string host, int port = 443, int timeoutMs = 5000, CancellationToken ct = default)
    {
        using var tcpClient = new TcpClient();
        using var cts = new CancellationTokenSource(timeoutMs);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, cts.Token);

        try
        {
            await tcpClient.ConnectAsync(host, port, linkedCts.Token).ConfigureAwait(false);
            using var sslStream = new SslStream(tcpClient.GetStream(), false, (sender, cert, chain, errors) => true);

            await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
            {
                TargetHost = host
            }, linkedCts.Token).ConfigureAwait(false);

            if (sslStream.RemoteCertificate is null)
            {
                return null;
            }

            var x509 = new X509Certificate2(sslStream.RemoteCertificate);
            return new SslCertificateInfo
            {
                Subject = x509.Subject,
                Issuer = x509.Issuer,
                ValidFrom = x509.NotBefore.ToUniversalTime(),
                ValidTo = x509.NotAfter.ToUniversalTime()
            };
        }
        catch
        {
            return null;
        }
    }
}
