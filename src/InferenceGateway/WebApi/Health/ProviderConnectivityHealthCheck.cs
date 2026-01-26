using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Synaxis.InferenceGateway.Application.Configuration;

namespace Synaxis.InferenceGateway.WebApi.Health;

public class ProviderConnectivityHealthCheck : IHealthCheck
{
    private readonly SynaxisConfiguration _config;
    private readonly ILogger<ProviderConnectivityHealthCheck> _logger;

    public ProviderConnectivityHealthCheck(IOptions<SynaxisConfiguration> config, ILogger<ProviderConnectivityHealthCheck> logger)
    {
        _config = config.Value;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var failures = new List<string>();

        foreach (var entry in _config.Providers)
        {
            var name = entry.Key;
            var provider = entry.Value;

            if (!provider.Enabled) continue;

            var endpoints = new List<string>();
            if (!string.IsNullOrWhiteSpace(provider.Endpoint)) endpoints.Add(provider.Endpoint);
            if (!string.IsNullOrWhiteSpace(provider.FallbackEndpoint)) endpoints.Add(provider.FallbackEndpoint);

            if (endpoints.Count == 0)
            {
                var defaultEndpoint = GetDefaultEndpoint(provider.Type);
                if (!string.IsNullOrWhiteSpace(defaultEndpoint))
                {
                    endpoints.Add(defaultEndpoint);
                }
            }

            if (endpoints.Count == 0)
            {
                _logger.LogWarning("Provider {Provider} is enabled but has no endpoint and no default for type {Type}.", name, provider.Type);
                _logger.LogError("Provider {Provider} health check failed due to missing endpoint.", name);
                failures.Add($"{name}: Missing endpoint");
                continue;
            }

            Exception? lastError = null;
            var reachable = false;

            foreach (var endpoint in endpoints)
            {
                try
                {
                    await CheckConnectivityAsync(name, endpoint, cancellationToken);
                    reachable = true;
                    break;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    _logger.LogWarning(ex, "Provider {Provider} connectivity check failed.", name);
                    _logger.LogError(ex, "Connectivity check failed for provider {Provider} at {Endpoint}", name, endpoint);
                }
            }

            if (!reachable)
            {
                failures.Add($"{name}: {lastError?.Message ?? "Connectivity check failed"}");
            }
        }

        if (failures.Count > 0)
        {
            return HealthCheckResult.Unhealthy($"Provider connectivity failures: {string.Join(", ", failures)}");
        }

        return HealthCheckResult.Healthy("All enabled providers are reachable.");
    }

    private async Task CheckConnectivityAsync(string name, string endpoint, CancellationToken ct)
    {
        var normalized = endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? endpoint
            : $"https://{endpoint}";
        var uri = new Uri(normalized);
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : (uri.Scheme == "https" ? 443 : 80);

        // 1. DNS (500ms)
        using var dnsCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        dnsCts.CancelAfter(TimeSpan.FromMilliseconds(500));
        var addresses = await Dns.GetHostAddressesAsync(host, dnsCts.Token);
        var ip = addresses.First(a => a.AddressFamily == AddressFamily.InterNetwork || a.AddressFamily == AddressFamily.InterNetworkV6);

        // 2. TCP (800ms)
        using var tcpClient = new TcpClient();
        using var tcpCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        tcpCts.CancelAfter(TimeSpan.FromMilliseconds(800));
        await tcpClient.ConnectAsync(ip, port, tcpCts.Token);

        // 3. TLS (1200ms) - only if https
        if (uri.Scheme == "https")
        {
            using var tlsCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            tlsCts.CancelAfter(TimeSpan.FromMilliseconds(1200));
            using var sslStream = new SslStream(tcpClient.GetStream(), false);
            await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions { TargetHost = host }, tlsCts.Token);
        }

        // 4. HTTP HEAD (500ms)
        using var httpCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        httpCts.CancelAfter(TimeSpan.FromMilliseconds(500));
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromMilliseconds(500) };
        var request = new HttpRequestMessage(HttpMethod.Head, uri);
        var response = await httpClient.SendAsync(request, httpCts.Token);
        // We don't strictly require 200 OK, just that the server responded. 
        // Many APIs return 401/404 on HEAD without tokens, which is fine for connectivity.
    }

    private string? GetDefaultEndpoint(string type) => type?.ToLowerInvariant() switch
    {
        "openai" => "https://api.openai.com/v1",
        "groq" => "https://api.groq.com/openai/v1",
        "cohere" => "https://api.cohere.ai/v1",
        "cloudflare" => "https://api.cloudflare.com/client/v4",
        "gemini" => "https://generativelanguage.googleapis.com",
        "antigravity" => "https://cloudcode-pa.googleapis.com",
        "openrouter" => "https://openrouter.ai/api/v1",
        "nvidia" => "https://integrate.api.nvidia.com/v1",
        "huggingface" => "https://api-inference.huggingface.co",
        "pollinations" => "https://pollinations.ai",
        _ => null
    };
}
