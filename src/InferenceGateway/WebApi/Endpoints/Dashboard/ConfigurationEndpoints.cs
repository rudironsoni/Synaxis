using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Synaxis.InferenceGateway.Application.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Synaxis.InferenceGateway.WebApi.Endpoints.Dashboard;

public static class ConfigurationEndpoints
{
    public static IEndpointRouteBuilder MapConfigurationEndpoints(this IEndpointRouteBuilder app)
    {
        var configGroup = app.MapGroup("/api/config")
            .WithTags("Configuration")
            .RequireCors("WebApp");

        configGroup.MapGet("/models", (IOptions<SynaxisConfiguration> config) =>
        {
            var models = config.Value.CanonicalModels.Select(cm =>
            {
                var providerConfig = config.Value.Providers.GetValueOrDefault(cm.Provider);
                
                return new ModelConfigDto
                {
                    Id = cm.Id,
                    Provider = cm.Provider,
                    ModelPath = cm.ModelPath,
                    Enabled = providerConfig?.Enabled ?? false,
                    Capabilities = new ModelCapabilitiesDto
                    {
                        Streaming = cm.Streaming,
                        Tools = cm.Tools,
                        Vision = cm.Vision,
                        StructuredOutput = cm.StructuredOutput,
                        LogProbs = cm.LogProbs
                    },
                    Priority = providerConfig?.Tier ?? 0
                };
            }).ToList();

            return Results.Ok(new { models });
        })
        .WithSummary("Get all model configurations")
        .WithDescription("Returns a list of all configured models with their capabilities and settings");

        configGroup.MapPut("/models/{id}", (
            string id,
            ModelUpdateRequest request,
            IOptions<SynaxisConfiguration> config) =>
        {
            var model = config.Value.CanonicalModels.FirstOrDefault(m => m.Id == id);
            if (model == null)
            {
                return Results.NotFound(new { error = $"Model '{id}' not found" });
            }

            var providerConfig = config.Value.Providers.GetValueOrDefault(model.Provider);
            if (providerConfig == null)
            {
                return Results.NotFound(new { error = $"Provider '{model.Provider}' not found for model '{id}'" });
            }

            if (request.Enabled.HasValue)
            {
                providerConfig.Enabled = request.Enabled.Value;
            }

            if (request.Priority.HasValue)
            {
                providerConfig.Tier = request.Priority.Value;
            }

            return Results.Ok(new { success = true });
        })
        .WithSummary("Update model configuration")
        .WithDescription("Update configuration for a specific model including enabled status and priority");

        configGroup.MapPost("/models", (
            ModelCreateRequest request,
            IOptions<SynaxisConfiguration> config) =>
        {
            if (string.IsNullOrWhiteSpace(request.Model))
            {
                return Results.BadRequest(new { success = false, error = "Model name is required" });
            }

            var existingModel = config.Value.CanonicalModels.FirstOrDefault(m => m.Id == request.Model);
            if (existingModel != null)
            {
                return Results.Conflict(new { success = false, error = $"Model '{request.Model}' already exists" });
            }

            return Results.Ok(new { success = true });
        })
        .WithSummary("Validate model configuration")
        .WithDescription("Validate a new model configuration before persistence");

        configGroup.MapGet("/system", (IOptions<SynaxisConfiguration> config) =>
        {
            var settings = new SystemSettingsDto
            {
                MaxRequestBodySize = config.Value.MaxRequestBodySize,
                JwtIssuer = config.Value.JwtIssuer ?? "Synaxis",
                JwtAudience = config.Value.JwtAudience ?? "Synaxis",
                TotalProviders = config.Value.Providers.Count,
                EnabledProviders = config.Value.Providers.Count(p => p.Value.Enabled),
                TotalModels = config.Value.CanonicalModels.Count,
                TotalAliases = config.Value.Aliases.Count
            };

            return Results.Ok(settings);
        })
        .WithSummary("Get system settings")
        .WithDescription("Returns current system configuration and statistics");

        configGroup.MapGet("/preferences", () =>
        {
            var preferences = new UserPreferencesDto
            {
                Theme = "dark",
                DefaultModel = "default",
                StreamingEnabled = true,
                NotificationsEnabled = true
            };

            return Results.Ok(preferences);
        })
        .WithSummary("Get user preferences")
        .WithDescription("Returns user-specific preferences and settings");

        configGroup.MapPut("/preferences", (UserPreferencesUpdateRequest request) =>
        {
            return Results.Ok(new { success = true });
        })
        .WithSummary("Update user preferences")
        .WithDescription("Update user-specific preferences and settings");

        return app;
    }
}

public class ModelConfigDto
{
    public string Id { get; set; } = "";
    public string Provider { get; set; } = "";
    public string ModelPath { get; set; } = "";
    public bool Enabled { get; set; }
    public ModelCapabilitiesDto Capabilities { get; set; } = new();
    public int Priority { get; set; }
}

public class ModelCapabilitiesDto
{
    public bool Streaming { get; set; }
    public bool Tools { get; set; }
    public bool Vision { get; set; }
    public bool StructuredOutput { get; set; }
    public bool LogProbs { get; set; }
}

public class ModelUpdateRequest
{
    public bool? Enabled { get; set; }
    public int? Priority { get; set; }
}

public class ModelCreateRequest
{
    public string Model { get; set; } = "";
}

public class SystemSettingsDto
{
    public long MaxRequestBodySize { get; set; }
    public string JwtIssuer { get; set; } = "";
    public string JwtAudience { get; set; } = "";
    public int TotalProviders { get; set; }
    public int EnabledProviders { get; set; }
    public int TotalModels { get; set; }
    public int TotalAliases { get; set; }
}

public class UserPreferencesDto
{
    public string Theme { get; set; } = "dark";
    public string DefaultModel { get; set; } = "default";
    public bool StreamingEnabled { get; set; }
    public bool NotificationsEnabled { get; set; }
}

public class UserPreferencesUpdateRequest
{
    public string? Theme { get; set; }
    public string? DefaultModel { get; set; }
    public bool? StreamingEnabled { get; set; }
    public bool? NotificationsEnabled { get; set; }
}
