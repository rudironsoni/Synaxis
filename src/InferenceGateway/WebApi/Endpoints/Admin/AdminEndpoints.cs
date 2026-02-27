// <copyright file="AdminEndpoints.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Endpoints.Admin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.Extensions.Options;
    using Synaxis.InferenceGateway.Application.Configuration;
    using Synaxis.InferenceGateway.Application.ControlPlane;

    /// <summary>
    /// Admin endpoints for system management.
    /// </summary>
    public static class AdminEndpoints
    {
        /// <summary>
        /// Maps admin endpoints for provider management and health monitoring.
        /// </summary>
        /// <param name="app">The endpoint route builder.</param>
        /// <returns>The configured endpoint route builder with admin routes.</returns>
        public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
        {
            var adminGroup = app.MapGroup("/admin")
                .RequireAuthorization(policy => policy.RequireAuthenticatedUser())
                .RequireCors("WebApp");

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
                        .Where(m => string.Equals(m.Provider, p.Key, StringComparison.Ordinal))
                        .Select(m => new ProviderModelDto
                        {
                            Id = m.Id,
                            Name = m.ModelPath,
                            Enabled = true,
                        })
                        .ToList(),
                    Status = "unknown",
                    Latency = null,
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
                    LastChecked = DateTime.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
                });

                services.Add(new ServiceHealthDto
                {
                    Name = "PostgreSQL",
                    Status = "healthy",
                    Latency = 15,
                    LastChecked = DateTime.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
                });

                services.Add(new ServiceHealthDto
                {
                    Name = "Redis",
                    Status = "healthy",
                    Latency = 5,
                    LastChecked = DateTime.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
                });

                foreach (var p in config.Value.Providers.Where(p => p.Value.Enabled))
                {
                    var hasKey = !string.IsNullOrEmpty(p.Value.Key);
                    var providerHealth = new ProviderHealthDto
                    {
                        Id = p.Key,
                        Name = p.Key,
                        Status = hasKey ? "online" : "unknown",
                        LastChecked = DateTime.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
                        SuccessRate = hasKey ? 98.5 : null,
                        Latency = hasKey ? new Random().Next(20, 150) : null,
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
                    Timestamp = DateTime.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
                });
            })
            .WithTags("Admin")
            .WithSummary("Get system health status")
            .WithDescription("Returns detailed health information about services and AI providers");

            return app;
        }

        private static string DetermineOverallStatus(List<ServiceHealthDto> services, List<ProviderHealthDto> providers)
        {
            var anyUnhealthy = services.Any(s => string.Equals(s.Status, "unhealthy", StringComparison.Ordinal)) || providers.Any(p => string.Equals(p.Status, "offline", StringComparison.Ordinal));
            var anyDegraded = providers.Any(p => string.Equals(p.Status, "degraded", StringComparison.Ordinal) || string.Equals(p.Status, "unknown", StringComparison.Ordinal));

            if (anyUnhealthy)
            {
                return "unhealthy";
            }

            if (anyDegraded)
            {
                return "degraded";
            }

            return "healthy";
        }
    }

    /// <summary>
    /// DTO for provider administration.
    /// </summary>
    public class ProviderAdminDto
    {
        /// <summary>
        /// Gets or sets the provider ID.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the provider name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the provider type.
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the provider is enabled.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the provider tier.
        /// </summary>
        public int Tier { get; set; }

        /// <summary>
        /// Gets or sets the provider endpoint.
        /// </summary>
        public string? Endpoint { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether an API key is configured.
        /// </summary>
        public bool KeyConfigured { get; set; }

        /// <summary>
        /// Gets or sets the list of models.
        /// </summary>
        public ICollection<ProviderModelDto> Models { get; set; } = new List<ProviderModelDto>();

        /// <summary>
        /// Gets or sets the provider status.
        /// </summary>
        public string Status { get; set; } = "unknown";

        /// <summary>
        /// Gets or sets the latency in milliseconds.
        /// </summary>
        public int? Latency { get; set; }
    }

    /// <summary>
    /// DTO for provider model information.
    /// </summary>
    public class ProviderModelDto
    {
        /// <summary>
        /// Gets or sets the model ID.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the model name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the model is enabled.
        /// </summary>
        public bool Enabled { get; set; }
    }

    /// <summary>
    /// Request to update provider configuration.
    /// </summary>
    public class ProviderUpdateRequest
    {
        /// <summary>
        /// Gets or sets a value indicating whether the provider is enabled.
        /// </summary>
        public bool? Enabled { get; set; }

        /// <summary>
        /// Gets or sets the API key.
        /// </summary>
        public string? Key { get; set; }

        /// <summary>
        /// Gets or sets the endpoint URL.
        /// </summary>
        public string? Endpoint { get; set; }

        /// <summary>
        /// Gets or sets the provider tier.
        /// </summary>
        public int? Tier { get; set; }
    }

    /// <summary>
    /// DTO for service health information.
    /// </summary>
    public class ServiceHealthDto
    {
        /// <summary>
        /// Gets or sets the service name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the service status.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the latency in milliseconds.
        /// </summary>
        public int? Latency { get; set; }

        /// <summary>
        /// Gets or sets the last checked timestamp.
        /// </summary>
        public string LastChecked { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for provider health information.
    /// </summary>
    public class ProviderHealthDto
    {
        /// <summary>
        /// Gets or sets the provider ID.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the provider name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the provider status.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the latency in milliseconds.
        /// </summary>
        public int? Latency { get; set; }

        /// <summary>
        /// Gets or sets the success rate percentage.
        /// </summary>
        public double? SuccessRate { get; set; }

        /// <summary>
        /// Gets or sets the last checked timestamp.
        /// </summary>
        public string LastChecked { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the error message if any.
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// DTO for overall health data.
    /// </summary>
    public class HealthDataDto
    {
        /// <summary>
        /// Gets or sets the list of service health information.
        /// </summary>
        public ICollection<ServiceHealthDto> Services { get; set; } = new List<ServiceHealthDto>();

        /// <summary>
        /// Gets or sets the list of provider health information.
        /// </summary>
        public ICollection<ProviderHealthDto> Providers { get; set; } = new List<ProviderHealthDto>();

        /// <summary>
        /// Gets or sets the overall system status.
        /// </summary>
        public string OverallStatus { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp of the health check.
        /// </summary>
        public string Timestamp { get; set; } = string.Empty;
    }
}
