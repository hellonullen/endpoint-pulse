namespace EndpointPulse.Cli;

using System.Text.Json;
using EndpointPulse.Cli.UI;
using EndpointPulse.Core.Configuration;
using EndpointPulse.Core.Engine;
using EndpointPulse.Core.Models;

public static class Program
{
    private const string Version = "1.0.0";

    public static async Task<int> Main(string[] args)
    {
        ConsoleRenderer.PrintBanner();

        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            PrintUsage();
            return 0;
        }

        string command = args[0].ToLowerInvariant();
        try
        {
            return command switch
            {
                "check" => await HandleCheckAsync(args.Skip(1).ToArray()),
                "scan" => await HandleScanAsync(args.Skip(1).ToArray()),
                "watch" => await HandleWatchAsync(args.Skip(1).ToArray()),
                "init" => HandleInit(args.Skip(1).ToArray()),
                _ => await HandleDirectCheckOrHelp(args)
            };
        }
        catch (Exception ex)
        {
            ConsoleRenderer.WriteColor(ConsoleColor.Red, $"[FATAL ERROR] {ex.Message}");
            return 1;
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("USAGE:");
        Console.WriteLine("  pulse check <url_or_host:port> [options]");
        Console.WriteLine("  pulse scan [-c targets.json] [options]");
        Console.WriteLine("  pulse watch [-c targets.json] [-i 30] [options]");
        Console.WriteLine("  pulse init [-o targets.json]\n");

        Console.WriteLine("COMMANDS:");
        Console.WriteLine("  check    Fast one-shot health and SSL check for a single endpoint.");
        Console.WriteLine("  scan     Scan a batch of targets defined in a JSON config file.");
        Console.WriteLine("  watch    Continuously monitor targets in a live loop and dispatch alerts.");
        Console.WriteLine("  init     Generate a sample targets.json configuration template.\n");

        Console.WriteLine("OPTIONS:");
        Console.WriteLine("  -c, --config <file>     Path to JSON target config file (default: './targets.json').");
        Console.WriteLine("  -i, --interval <sec>    Watch loop interval in seconds (default: 30).");
        Console.WriteLine("  -t, --timeout <ms>      Request timeout in milliseconds (default: 5000).");
        Console.WriteLine("  --json                  Output scan results as raw JSON instead of table.\n");

        Console.WriteLine("EXAMPLES:");
        Console.WriteLine("  # Quick single check");
        Console.WriteLine("  pulse check https://google.com");
        Console.WriteLine("  pulse check tcp://1.1.1.1:53");
        Console.WriteLine();
        Console.WriteLine("  # Batch scan with configuration");
        Console.WriteLine("  pulse scan -c targets.json");
        Console.WriteLine();
        Console.WriteLine("  # Live continuous watcher with Discord/Telegram alerts");
        Console.WriteLine("  pulse watch -c targets.json -i 60\n");
    }

    private static async Task<int> HandleDirectCheckOrHelp(string[] args)
    {
        if (args[0].StartsWith("http://") || args[0].StartsWith("https://") || args[0].Contains(':'))
        {
            return await HandleCheckAsync(args);
        }

        ConsoleRenderer.WriteColor(ConsoleColor.Red, $"[ERROR] Unknown command or invalid target: {args[0]}\n");
        PrintUsage();
        return 1;
    }

    private static async Task<int> HandleCheckAsync(string[] args)
    {
        if (args.Length == 0)
        {
            ConsoleRenderer.WriteColor(ConsoleColor.Red, "[ERROR] Missing target URL or host:port.");
            PrintUsage();
            return 1;
        }

        string targetUrl = args[0];
        int timeoutMs = 5000;

        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] is "-t" or "--timeout" && i + 1 < args.Length && int.TryParse(args[i + 1], out int parsed))
            {
                timeoutMs = parsed;
                i++;
            }
        }

        var target = new EndpointTarget
        {
            Name = targetUrl,
            Url = targetUrl,
            TimeoutMs = timeoutMs
        };

        var runner = new PulseRunner();
        Console.WriteLine($"[INFO] Probing: {targetUrl} (Timeout: {timeoutMs}ms)...\n");

        var result = await runner.CheckSingleAsync(target);
        ConsoleRenderer.PrintResultRow(result);

        if (result.SslInfo is not null)
        {
            Console.WriteLine("\n  SSL Certificate Details:");
            Console.WriteLine($"    Subject   : {result.SslInfo.Subject}");
            Console.WriteLine($"    Issuer    : {result.SslInfo.Issuer}");
            Console.WriteLine($"    Valid From: {result.SslInfo.ValidFrom:yyyy-MM-dd}");
            Console.WriteLine($"    Valid To  : {result.SslInfo.ValidTo:yyyy-MM-dd} ({result.SslInfo.DaysRemaining} days remaining)");
        }

        Console.WriteLine();
        return result.IsSuccess ? 0 : 1;
    }

    private static async Task<int> HandleScanAsync(string[] args)
    {
        string configFile = "targets.json";
        bool jsonOutput = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "-c" or "--config" when i + 1 < args.Length:
                    configFile = args[++i];
                    break;
                case "--json":
                    jsonOutput = true;
                    break;
            }
        }

        if (!File.Exists(configFile))
        {
            ConsoleRenderer.WriteColor(ConsoleColor.Red, $"[ERROR] Config file not found: {configFile}");
            Console.WriteLine("Tip: Run 'pulse init' to generate a sample configuration file.");
            return 1;
        }

        var config = ConfigLoader.LoadFromFile(configFile);
        var runner = new PulseRunner();

        if (!jsonOutput)
        {
            Console.WriteLine($"[INFO] Scanning {config.Targets.Count} endpoint targets from '{configFile}'...\n");
        }

        var (results, summary) = await runner.ScanAsync(config, onResultAvailable: r =>
        {
            if (!jsonOutput)
            {
                ConsoleRenderer.PrintResultRow(r);
            }
        });

        if (jsonOutput)
        {
            Console.WriteLine(JsonSerializer.Serialize(new { summary, results }, new JsonSerializerOptions { WriteIndented = true }));
            return summary.UnhealthyCount > 0 ? 1 : 0;
        }

        ConsoleRenderer.PrintSummary(summary);
        return summary.UnhealthyCount > 0 ? 1 : 0;
    }

    private static async Task<int> HandleWatchAsync(string[] args)
    {
        string configFile = "targets.json";
        int intervalSeconds = 30;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "-c" or "--config" when i + 1 < args.Length:
                    configFile = args[++i];
                    break;
                case "-i" or "--interval" when i + 1 < args.Length && int.TryParse(args[i + 1], out int parsed):
                    intervalSeconds = Math.Max(5, parsed);
                    i++;
                    break;
            }
        }

        if (!File.Exists(configFile))
        {
            ConsoleRenderer.WriteColor(ConsoleColor.Red, $"[ERROR] Config file not found: {configFile}");
            return 1;
        }

        var config = ConfigLoader.LoadFromFile(configFile);
        var runner = new PulseRunner();

        ConsoleRenderer.WriteColor(ConsoleColor.Cyan, $"[WATCH MODE] Monitoring {config.Targets.Count} endpoints every {intervalSeconds}s. Press Ctrl+C to stop.\n");

        if (config.Webhook.HasAnyWebhook)
        {
            Console.WriteLine("  Alerting Webhooks:");
            if (!string.IsNullOrEmpty(config.Webhook.DiscordWebhookUrl)) Console.WriteLine("    - Discord Webhook: Active");
            if (!string.IsNullOrEmpty(config.Webhook.TelegramBotToken)) Console.WriteLine("    - Telegram Bot: Active");
            Console.WriteLine();
        }

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        while (!cts.Token.IsCancellationRequested)
        {
            Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss} UTC] Running health check pulse...");
            var (_, summary) = await runner.ScanAsync(config, onResultAvailable: ConsoleRenderer.PrintResultRow, ct: cts.Token);
            ConsoleRenderer.PrintSummary(summary);

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        Console.WriteLine("\n[INFO] Monitoring stopped cleanly.");
        return 0;
    }

    private static int HandleInit(string[] args)
    {
        string outputPath = args.Length > 1 && args[0] is "-o" or "--output" ? args[1] : "targets.json";

        var template = new MonitorConfig
        {
            DefaultIntervalSeconds = 30,
            Webhook = new WebhookConfig
            {
                DiscordWebhookUrl = "https://discord.com/api/webhooks/YOUR_WEBHOOK_URL",
                TelegramBotToken = "",
                TelegramChatId = "",
                AlertOnUnhealthy = true,
                AlertOnDegraded = true,
                AlertOnSslExpiring = true
            },
            Targets = new List<EndpointTarget>
            {
                new() { Name = "Google Public DNS", Url = "tcp://8.8.8.8:53", TimeoutMs = 3000 },
                new() { Name = "Cloudflare Web", Url = "https://cloudflare.com", TimeoutMs = 5000, SslWarningDays = 14 },
                new() { Name = "GitHub API", Url = "https://api.github.com", TimeoutMs = 5000, LatencyWarningMs = 800 },
                new() { Name = "Example Backend API", Url = "https://httpbin.org/status/200", ExpectedStatusCode = 200 }
            }
        };

        string json = JsonSerializer.Serialize(template, new JsonSerializerOptions { WriteIndented = true });
        string? dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(outputPath, json);

        ConsoleRenderer.WriteColor(ConsoleColor.Green, $"[SUCCESS] Created sample configuration at: {Path.GetFullPath(outputPath)}");
        Console.WriteLine("Edit this file to include your target servers, APIs, or database ports, then run:");
        Console.WriteLine($"  pulse scan -c {outputPath}");
        return 0;
    }
}
