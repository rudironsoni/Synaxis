// <copyright file="ProviderConnectivityHealthCheck.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Health
{
    using System.Net;
    using System.Net.Security;
    using System.Net.Sockets;
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using Microsoft.Extensions.Options;
    using Synaxis.InferenceGateway.Application.Configuration;

    /// <summary>
    /// Health check for provider connectivity.
    /// </summary>
    public class ProviderConnectivityHealthCheck : IHealthCheck
    {
        private static readonly TimeSpan DnsTimeout = TimeSpan.FromMilliseconds(500);
        private static readonly TimeSpan TcpTimeout = TimeSpan.FromMilliseconds(800);
        private static readonly TimeSpan TlsTimeout = TimeSpan.FromMilliseconds(1200);
        private static readonly TimeSpan HttpTimeout = TimeSpan.FromMilliseconds(500);
        private readonly SynaxisConfiguration _config;
        private readonly ILogger<ProviderConnectivityHealthCheck> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProviderConnectivityHealthCheck"/> class.
        /// </summary>
        /// <param name="config">The configuration options.</param>
        /// <param name="logger">The logger.</param>
        public ProviderConnectivityHealthCheck(IOptions<SynaxisConfiguration> config, ILogger<ProviderConnectivityHealthCheck> logger)
        {
            this._config = config.Value;
            this._logger = logger;
        }

        /// <summary>
        /// Checks the health of provider connectivity.
        /// </summary>
        /// <param name="context">The health check context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The health check result.</returns>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var failures = new List<string>();

            foreach (var entry in this._config.Providers)
            {
                var name = entry.Key;
                var provider = entry.Value;

                if (!provider.Enabled)
                {
                    continue;
                }

                var endpoints = BuildEndpoints(provider);

                if (endpoints.Count == 0)
                {
                    this._logger.LogWarning("Provider {Provider} is enabled but has no endpoint and no default for type {Type}.", name, provider.Type);
                    this._logger.LogError("Provider {Provider} health check failed due to missing endpoint.", name);
                    failures.Add($"{name}: Missing endpoint");
                    continue;
                }

                var (reachable, lastError) = await this.CheckEndpointsAsync(name, endpoints, cancellationToken).ConfigureAwait(false);

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

        private async Task CheckConnectivityAsync(string endpoint, CancellationToken ct)
        {
            var uri = NormalizeEndpoint(endpoint);
            var host = uri.Host;
            var port = ResolvePort(uri);

            // 1. DNS (500ms)
            using var dnsCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            dnsCts.CancelAfter(DnsTimeout);
            var ip = await ResolveHostAsync(host, dnsCts.Token).ConfigureAwait(false);

            // 2. TCP (800ms)
            using var tcpClient = new TcpClient();
            using var tcpCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            tcpCts.CancelAfter(TcpTimeout);
            await tcpClient.ConnectAsync(ip, port, tcpCts.Token).ConfigureAwait(false);

            // 3. TLS (1200ms) - only if https
            if (string.Equals(uri.Scheme, "https", StringComparison.Ordinal))
            {
                using var tlsCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                tlsCts.CancelAfter(TlsTimeout);
                using var networkStream = tcpClient.GetStream();
                using var sslStream = new SslStream(networkStream, leaveInnerStreamOpen: false);
                var authOptions = new SslClientAuthenticationOptions { TargetHost = host };
                await sslStream.AuthenticateAsClientAsync(authOptions, tlsCts.Token).ConfigureAwait(false);
            }

            // 4. HTTP HEAD (500ms)
            using var httpCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            using var httpClient = new HttpClient { Timeout = HttpTimeout };
            httpCts.CancelAfter(HttpTimeout);
            using var request = new HttpRequestMessage(HttpMethod.Head, uri);
            using var response = await httpClient.SendAsync(request, httpCts.Token).ConfigureAwait(false);
            _ = response.StatusCode;

            // We don't strictly require 200 OK, just that the server responded.
            // Many APIs return 401/404 on HEAD without tokens, which is fine for connectivity.
        }

        private static string? GetDefaultEndpoint(string type) => type?.ToLowerInvariant() switch
        {
            "openai" => "https://api.openai.com/v1",
            "groq" => "https://api.groq.com/openai/v1",
            "cohere" => "https://api.cohere.ai/v1",
            "cloudflare" => "https://api.cloudflare.com/client/v4",
            "gemini" => "https://generativelanguage.googleapis.com",
            "antigravity" => "https://cloudcode-pa.googleapis.com",
            "openrouter" => "https://openrouter.ai/api/v1",
            "nvidia" => "https://integrate.api.nvidia.com/v1",
            "huggingface" => "https://router.huggingface.co",
            "pollinations" => "https://pollinations.ai",
            _ => null,
        };

        private static Uri NormalizeEndpoint(string endpoint)
        {
            var normalized = endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                ? endpoint
                : $"https://{endpoint}";
            return new Uri(normalized);
        }

        private static int ResolvePort(Uri uri)
        {
            if (uri.Port > 0)
            {
                return uri.Port;
            }

            return string.Equals(uri.Scheme, "https", StringComparison.Ordinal) ? 443 : 80;
        }

        private static async Task<IPAddress> ResolveHostAsync(string host, CancellationToken ct)
        {
            var addresses = await Dns.GetHostAddressesAsync(host, ct).ConfigureAwait(false);
            return addresses.First(a => a.AddressFamily == AddressFamily.InterNetwork || a.AddressFamily == AddressFamily.InterNetworkV6);
        }

        private static List<string> BuildEndpoints(ProviderConfig provider)
        {
            var endpoints = new List<string>();

            if (!string.IsNullOrWhiteSpace(provider.Endpoint))
            {
                endpoints.Add(provider.Endpoint);
            }

            if (!string.IsNullOrWhiteSpace(provider.FallbackEndpoint))
            {
                endpoints.Add(provider.FallbackEndpoint);
            }

            if (endpoints.Count == 0)
            {
                var defaultEndpoint = GetDefaultEndpoint(provider.Type);
                if (!string.IsNullOrWhiteSpace(defaultEndpoint))
                {
                    endpoints.Add(defaultEndpoint);
                }
            }

            return endpoints;
        }

        private async Task<(bool Reachable, Exception? LastError)> CheckEndpointsAsync(
            string providerName,
            IEnumerable<string> endpoints,
            CancellationToken cancellationToken)
        {
            Exception? lastError = null;

            foreach (var endpoint in endpoints)
            {
                try
                {
                    await this.CheckConnectivityAsync(endpoint, cancellationToken).ConfigureAwait(false);
                    return (true, null);
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    this._logger.LogWarning(ex, "Provider {Provider} connectivity check failed.", providerName);
                    this._logger.LogError(ex, "Connectivity check failed for provider {Provider} at {Endpoint}", providerName, endpoint);
                }
            }

            return (false, lastError);
        }
    }
}
