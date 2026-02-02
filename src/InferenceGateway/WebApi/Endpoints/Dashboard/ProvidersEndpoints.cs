using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Synaxis.InferenceGateway.Application.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Synaxis.InferenceGateway.WebApi.Endpoints.Dashboard;

public static class ProvidersEndpoints
{
    public static IEndpointRouteBuilder MapProvidersEndpoints(this IEndpointRouteBuilder app)
    {
        var dashboardGroup = app.MapGroup("/api/providers")
            .RequireCors("WebApp");

        // GET /api/providers - List all providers
        dashboardGroup.MapGet("", (
            IOptions<SynaxisConfiguration> config,
            IConnectionMultiplexer redis,
            CancellationToken ct) =>
        {
            var db = redis.GetDatabase();
            var providers = new List<ProviderDto>();

            foreach (var p in config.Value.Providers)
            {
                var providerKey = p.Key;
                var providerConfig = p.Value;

                // Get usage stats from Redis (if available)
                var totalTokens = 0;
                var requests = 0;

                try
                {
                    var tokensKey = $"provider:{providerKey}:tokens";
                    var requestsKey = $"provider:{providerKey}:requests";
                    
                    var tokensValue = db.StringGet(tokensKey);
                    var requestsValue = db.StringGet(requestsKey);

                    if (tokensValue.HasValue) int.TryParse(tokensValue.ToString(), out totalTokens);
                    if (requestsValue.HasValue) int.TryParse(requestsValue.ToString(), out requests);
                }
                catch
                {
                    // Redis unavailable, use defaults
                }

                // Get models for this provider
                var models = config.Value.CanonicalModels
                    .Where(m => m.Provider == providerKey)
                    .Select(m => m.Id)
                    .ToList();

                // Determine status based on configuration
                var status = providerConfig.Enabled && !string.IsNullOrEmpty(providerConfig.Key) 
                    ? "healthy" 
                    : "unhealthy";

                providers.Add(new ProviderDto
                {
                    Id = providerKey,
                    Name = providerKey,
                    Status = status,
                    Tier = providerConfig.Tier,
                    Models = models,
                    Usage = new ProviderStatsDto
                    {
                        TotalTokens = totalTokens,
                        Requests = requests
                    }
                });
            }

            return Results.Json(new ProvidersListResponse
            {
                Providers = providers
            });
        })
        .WithTags("Dashboard")
        .WithSummary("List all providers")
        .WithDescription("Returns a list of all configured providers with their status, tier, models, and usage statistics");

        // GET /api/providers/{id}/status - Get provider health status
        dashboardGroup.MapGet("/{id}/status", async (
            string id,
            IOptions<SynaxisConfiguration> config,
            IHttpClientFactory httpClientFactory,
            CancellationToken ct) =>
        {
            if (!config.Value.Providers.ContainsKey(id))
            {
                return Results.NotFound(new { error = $"Provider '{id}' not found" });
            }

            var provider = config.Value.Providers[id];
            var status = "unhealthy";
            var lastChecked = DateTime.UtcNow;

            if (provider.Enabled && !string.IsNullOrEmpty(provider.Key))
            {
                // Perform a lightweight health check
                try
                {
                    var endpoint = GetProviderEndpoint(provider);
                    if (!string.IsNullOrEmpty(endpoint))
                    {
                        using var httpClient = httpClientFactory.CreateClient();
                        httpClient.Timeout = TimeSpan.FromSeconds(5);
                        
                        var request = new HttpRequestMessage(HttpMethod.Head, endpoint);
                        var response = await httpClient.SendAsync(request, ct);
                        
                        // Accept any response (including 401, 404) as "reachable"
                        status = "healthy";
                    }
                }
                catch
                {
                    status = "unhealthy";
                }
            }

            return Results.Json(new ProviderStatusResponse
            {
                Status = status,
                LastChecked = lastChecked.ToString("O")
            });
        })
        .WithTags("Dashboard")
        .WithSummary("Get provider health status")
        .WithDescription("Returns the current health status of a specific provider");

        // PUT /api/providers/{id}/config - Update provider configuration
        dashboardGroup.MapPut("/{id}/config", (
            string id,
            ProviderConfigUpdateRequest request,
            IOptions<SynaxisConfiguration> config) =>
        {
            if (!config.Value.Providers.ContainsKey(id))
            {
                return Results.NotFound(new { error = $"Provider '{id}' not found" });
            }

            var provider = config.Value.Providers[id];

            if (request.Enabled.HasValue)
            {
                provider.Enabled = request.Enabled.Value;
            }

            if (request.Tier.HasValue)
            {
                provider.Tier = request.Tier.Value;
            }

            return Results.Json(new ProviderConfigUpdateResponse
            {
                Success = true
            });
        })
        .WithTags("Dashboard")
        .WithSummary("Update provider configuration")
        .WithDescription("Updates provider configuration including enabled status and tier priority");

        return app;
    }

    private static string? GetProviderEndpoint(ProviderConfig provider)
    {
        if (!string.IsNullOrEmpty(provider.Endpoint))
        {
            return provider.Endpoint;
        }

        // Return default endpoints based on provider type
        return provider.Type?.ToLowerInvariant() switch
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
            _ => null
        };
    }
}

// DTOs for provider management
public class ProvidersListResponse
{
    public List<ProviderDto> Providers { get; set; } = new();
}

public class ProviderDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Status { get; set; } = "";
    public int Tier { get; set; }
    public List<string> Models { get; set; } = new();
    public ProviderStatsDto Usage { get; set; } = new();
}

public class ProviderStatsDto
{
    public int TotalTokens { get; set; }
    public int Requests { get; set; }
}

public class ProviderStatusResponse
{
    public string Status { get; set; } = "";
    public string LastChecked { get; set; } = "";
}

public class ProviderConfigUpdateRequest
{
    public bool? Enabled { get; set; }
    public int? Tier { get; set; }
}

public class ProviderConfigUpdateResponse
{
    public bool Success { get; set; }
}
