namespace EndpointPulse.Cli.UI;

using EndpointPulse.Core.Models;

/// <summary>
/// Handles colorized console formatting and tabular result rendering.
/// </summary>
public static class ConsoleRenderer
{
    public static void PrintBanner()
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine(@"  _____           _             _       _     ____        _           ");
        Console.WriteLine(@" | ____|_ __   __| |_ __   ___ (_)_ __ | |_  |  _ \ _   _| |___  ___  ");
        Console.WriteLine(@" |  _| | '_ \ / _` | '_ \ / _ \| | '_ \| __| | |_) | | | | / __|/ _ \ ");
        Console.WriteLine(@" | |___| | | | (_| | |_) | (_) | | | | | |_  |  __/| |_| | \__ \  __/ ");
        Console.WriteLine(@" |_____|_| |_|\__,_| .__/ \___/|_|_| |_|\__| |_|    \__,_|_|___/\___| ");
        Console.WriteLine(@"                   |_|                                                ");
        Console.ResetColor();
        Console.WriteLine("  EndpointPulse - Fast Endpoint Health & SSL Certificate Monitor");
        Console.WriteLine("  Repository: https://github.com/hellonullen/endpoint-pulse");
        Console.WriteLine("  Author: hellonullen | License: MIT\n");
    }

    public static void PrintResultRow(CheckResult r)
    {
        var (statusText, color) = r.Status switch
        {
            HealthStatus.Healthy => ("[HEALTHY]", ConsoleColor.Green),
            HealthStatus.Degraded => ("[DEGRADED]", ConsoleColor.Yellow),
            HealthStatus.SslExpiringSoon => ("[SSL-WARN]", ConsoleColor.DarkYellow),
            HealthStatus.SslExpired => ("[EXPIRED]", ConsoleColor.Red),
            _ => ("[DOWN]", ConsoleColor.Red)
        };

        string statusCol = $"{statusText,-11}";
        string nameCol = $"{(r.Name.Length > 20 ? r.Name[..17] + "..." : r.Name),-20}";
        string typeCol = $"[{r.Type,-5}]";
        string latencyCol = $"{r.LatencyMs,5} ms";

        string detailCol = r.HttpStatusCode.HasValue
            ? $"HTTP {r.HttpStatusCode} ({r.HttpStatusDescription})"
            : (!string.IsNullOrEmpty(r.ErrorMessage) ? r.ErrorMessage : r.HttpStatusDescription ?? "OK");

        if (detailCol.Length > 35) detailCol = detailCol[..32] + "...";

        string sslCol = r.SslInfo is not null
            ? (r.SslInfo.IsExpired ? "SSL EXPIRED!" : $"SSL: {r.SslInfo.DaysRemaining}d left")
            : "-";

        Console.ForegroundColor = color;
        Console.Write($"  {statusCol} ");
        Console.ResetColor();
        Console.WriteLine($"{nameCol} {typeCol}  {latencyCol}  {detailCol,-35}  {sslCol}");
    }

    public static void PrintSummary(ScanSummary s)
    {
        Console.WriteLine("\n" + new string('-', 85));
        Console.Write("  Scan Completed in ");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write($"{s.Duration.TotalSeconds:F2}s");
        Console.ResetColor();
        Console.Write(" | Total: ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write(s.TotalTargets);
        Console.ResetColor();
        Console.Write(" | Healthy: ");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write(s.HealthyCount);
        Console.ResetColor();
        Console.Write(" | Degraded: ");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write(s.DegradedCount);
        Console.ResetColor();
        Console.Write(" | Down: ");
        Console.ForegroundColor = s.UnhealthyCount > 0 ? ConsoleColor.Red : ConsoleColor.DarkGray;
        Console.Write(s.UnhealthyCount);
        Console.ResetColor();

        if (s.SslWarningCount > 0)
        {
            Console.Write(" | SSL Warnings: ");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write(s.SslWarningCount);
            Console.ResetColor();
        }

        Console.Write(" | Avg Latency: ");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"{s.AverageLatencyMs:F1} ms");
        Console.ResetColor();
        Console.WriteLine(new string('-', 85) + "\n");
    }

    public static void WriteColor(ConsoleColor color, string message)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}
