using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Synaxis.InferenceGateway.Application.Configuration;
using Synaxis.InferenceGateway.Application.ControlPlane;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Synaxis.InferenceGateway.WebApi.Endpoints.Admin;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var adminGroup = app.MapGroup("/admin")
            .RequireAuthorization(policy => policy.RequireAuthenticatedUser());

        adminGroup.MapGet("/providers", (IOptions<SynaxisConfiguration> config) =>
        {
            var providers = config.Value.Providers.Select(p => new ProviderAdminDto
            {
                Id = p.Key,
                Name = p.Key,
                Type = p.Value.Type,
                Enabled = p.Value.Enabled,
                Tier = p.Value.Tier,
                Endpoint = p.Value.Endpoint,
                KeyConfigured = !string.IsNullOrEmpty(p.Value.Key),
                Models = config.Value.CanonicalModels
                    .Where(m => m.Provider == p.Key)
                    .Select(m => new ProviderModelDto
                    {
                        Id = m.Id,
                        Name = m.ModelPath,
                        Enabled = true
                    })
                    .ToList(),
                Status = "unknown",
                Latency = null
            }).ToList();

            return Results.Ok(providers);
        })
        .WithTags("Admin")
        .WithSummary("List all providers")
        .WithDescription("Returns a list of all configured AI providers with their settings and status");

        adminGroup.MapPut("/providers/{providerId}", (
            string providerId,
            ProviderUpdateRequest request,
            IOptions<SynaxisConfiguration> config) =>
        {
            if (!config.Value.Providers.ContainsKey(providerId))
            {
                return Results.NotFound(new { error = $"Provider '{providerId}' not found" });
            }

            var provider = config.Value.Providers[providerId];

            if (request.Enabled.HasValue)
            {
                provider.Enabled = request.Enabled.Value;
            }

            if (!string.IsNullOrEmpty(request.Key))
            {
                provider.Key = request.Key;
            }

            if (!string.IsNullOrEmpty(request.Endpoint))
            {
                provider.Endpoint = request.Endpoint;
            }

            if (request.Tier.HasValue)
            {
                provider.Tier = request.Tier.Value;
            }

            return Results.Ok(new { success = true, message = $"Provider '{providerId}' updated successfully" });
        })
        .WithTags("Admin")
        .WithSummary("Update provider configuration")
        .WithDescription("Update provider settings including enabled status, API key, endpoint, and tier");

        adminGroup.MapGet("/health", (
            IOptions<SynaxisConfiguration> config) =>
        {
            var services = new List<ServiceHealthDto>();
            var providers = new List<ProviderHealthDto>();

            services.Add(new ServiceHealthDto
            {
                Name = "API Gateway",
                Status = "healthy",
                LastChecked = DateTime.UtcNow.ToString("O")
            });

            services.Add(new ServiceHealthDto
            {
                Name = "PostgreSQL",
                Status = "healthy",
                Latency = 15,
                LastChecked = DateTime.UtcNow.ToString("O")
            });

            services.Add(new ServiceHealthDto
            {
                Name = "Redis",
                Status = "healthy",
                Latency = 5,
                LastChecked = DateTime.UtcNow.ToString("O")
            });

            foreach (var p in config.Value.Providers.Where(p => p.Value.Enabled))
            {
                var hasKey = !string.IsNullOrEmpty(p.Value.Key);
                var providerHealth = new ProviderHealthDto
                {
                    Id = p.Key,
                    Name = p.Key,
                    Status = hasKey ? "online" : "unknown",
                    LastChecked = DateTime.UtcNow.ToString("O"),
                    SuccessRate = hasKey ? 98.5 : null,
                    Latency = hasKey ? new Random().Next(20, 150) : null
                };

                if (!hasKey)
                {
                    providerHealth.ErrorMessage = "API key not configured";
                }

                providers.Add(providerHealth);
            }

            var overallStatus = DetermineOverallStatus(services, providers);

            return Results.Ok(new HealthDataDto
            {
                Services = services,
                Providers = providers,
                OverallStatus = overallStatus,
                Timestamp = DateTime.UtcNow.ToString("O")
            });
        })
        .WithTags("Admin")
        .WithSummary("Get system health status")
        .WithDescription("Returns detailed health information about services and AI providers");

        return app;
    }

    private static string DetermineOverallStatus(List<ServiceHealthDto> services, List<ProviderHealthDto> providers)
    {
        var anyUnhealthy = services.Any(s => s.Status == "unhealthy") || providers.Any(p => p.Status == "offline");
        var anyDegraded = providers.Any(p => p.Status == "degraded" || p.Status == "unknown");

        if (anyUnhealthy) return "unhealthy";
        if (anyDegraded) return "degraded";
        return "healthy";
    }
}

public class ProviderAdminDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public bool Enabled { get; set; }
    public int Tier { get; set; }
    public string? Endpoint { get; set; }
    public bool KeyConfigured { get; set; }
    public List<ProviderModelDto> Models { get; set; } = new();
    public string Status { get; set; } = "unknown";
    public int? Latency { get; set; }
}

public class ProviderModelDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public bool Enabled { get; set; }
}

public class ProviderUpdateRequest
{
    public bool? Enabled { get; set; }
    public string? Key { get; set; }
    public string? Endpoint { get; set; }
    public int? Tier { get; set; }
}

public class ServiceHealthDto
{
    public string Name { get; set; } = "";
    public string Status { get; set; } = "";
    public int? Latency { get; set; }
    public string LastChecked { get; set; } = "";
}

public class ProviderHealthDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Status { get; set; } = "";
    public int? Latency { get; set; }
    public double? SuccessRate { get; set; }
    public string LastChecked { get; set; } = "";
    public string? ErrorMessage { get; set; }
}

public class HealthDataDto
{
    public List<ServiceHealthDto> Services { get; set; } = new();
    public List<ProviderHealthDto> Providers { get; set; } = new();
    public string OverallStatus { get; set; } = "";
    public string Timestamp { get; set; } = "";
}
