# EndpointPulse

A fast and lightweight CLI utility written in C# (.NET 9) for monitoring HTTP, HTTPS, and TCP endpoint availability, latency, and SSL certificate expiration.

## Capabilities

- **Multi-Protocol Probing:** Supports HTTP/HTTPS web endpoints and raw TCP ports (databases, game servers, mail servers).
- **SSL Certificate Expiration:** Measures days remaining until TLS certificate expiry directly during handshake.
- **Alert Dispatching:** Sends notification alerts to Discord webhooks or Telegram channels on state changes (service down, latency degradation, or SSL expiring).
- **Batch Scanning:** Evaluates multiple targets concurrently with configurable timeouts and latency warning thresholds.
- **Continuous Monitoring:** Built-in watch mode for polling targets at defined intervals.

## Getting Started

### Build from source
```bash
dotnet build EndpointPulse.sln
```

### Run Tests
```bash
dotnet test EndpointPulse.sln
```

## Command Line Usage

### 1. Single Endpoint Check
```bash
dotnet run --project src/EndpointPulse.Cli -- check https://github.com
```

### 2. Batch Scan
Generate a sample configuration:
```bash
dotnet run --project src/EndpointPulse.Cli -- init -o targets.json
```

Scan endpoints:
```bash
dotnet run --project src/EndpointPulse.Cli -- scan -c targets.json
```

### 3. Continuous Watch
```bash
dotnet run --project src/EndpointPulse.Cli -- watch -c targets.json -i 60
```

## License

Released under the MIT License.
