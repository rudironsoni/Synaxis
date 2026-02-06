// <copyright file="ConfigurationEndpoints.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Endpoints.Dashboard
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.Extensions.Options;
    using Synaxis.InferenceGateway.Application.Configuration;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Endpoints for configuration management.
    /// </summary>
    public static class ConfigurationEndpoints
    {
    /// <summary>
    /// Maps configuration endpoints to the application.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The endpoint route builder.</returns>
    public static IEndpointRouteBuilder MapConfigurationEndpoints(this IEndpointRouteBuilder app)
    {
        var configGroup = app.MapGroup("/api/config")
            .WithTags("Configuration")
            .RequireCors("WebApp");

        configGroup.MapGet("/models", (IOptions<SynaxisConfiguration> config) =>
        {
            var models = config.Value.CanonicalModels.Select(cm =>
            {
                var providerConfig = config.Value.Providers.TryGetValue(cm.Provider, out var providerConfigValue) ? providerConfigValue : null;

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
                        LogProbs = cm.LogProbs,
                    },
                    Priority = providerConfig?.Tier ?? 0,
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
            var model = config.Value.CanonicalModels.FirstOrDefault(m => string.Equals(m.Id, id, StringComparison.Ordinal));
            if (model == null)
            {
                return Results.NotFound(new { error = $"Model '{id}' not found" });
            }

            var providerConfig = config.Value.Providers.TryGetValue(model.Provider, out var providerConfigValue) ? providerConfigValue : null;
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

            var existingModel = config.Value.CanonicalModels.FirstOrDefault(m => string.Equals(m.Id, request.Model, StringComparison.Ordinal));
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

    /// <summary>
    /// DTO for model configuration.
    /// </summary>
    public class ModelConfigDto
    {
    /// <summary>
    /// Gets or sets the model ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the provider name.
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model path.
    /// </summary>
    public string ModelPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the model is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the model capabilities.
    /// </summary>
    public ModelCapabilitiesDto Capabilities { get; set; } = new ModelCapabilitiesDto();

    /// <summary>
    /// Gets or sets the model priority.
    /// </summary>
    public int Priority { get; set; }
    }

    /// <summary>
    /// DTO for model capabilities.
    /// </summary>
    public class ModelCapabilitiesDto
    {
    /// <summary>
    /// Gets or sets a value indicating whether streaming is supported.
    /// </summary>
    public bool Streaming { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether tools are supported.
    /// </summary>
    public bool Tools { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether vision is supported.
    /// </summary>
    public bool Vision { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether structured output is supported.
    /// </summary>
    public bool StructuredOutput { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether log probabilities are supported.
    /// </summary>
    public bool LogProbs { get; set; }
    }

    /// <summary>
    /// Request to update model configuration.
    /// </summary>
    public class ModelUpdateRequest
    {
    /// <summary>
    /// Gets or sets a value indicating whether the model is enabled.
    /// </summary>
    public bool? Enabled { get; set; }

    /// <summary>
    /// Gets or sets the model priority.
    /// </summary>
    public int? Priority { get; set; }
    }

    /// <summary>
    /// Request to create a new model.
    /// </summary>
    public class ModelCreateRequest
    {
    /// <summary>
    /// Gets or sets the model name.
    /// </summary>
    public string Model { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for system settings.
    /// </summary>
    public class SystemSettingsDto
    {
    /// <summary>
    /// Gets or sets the maximum request body size.
    /// </summary>
    public long MaxRequestBodySize { get; set; }

    /// <summary>
    /// Gets or sets the JWT issuer.
    /// </summary>
    public string JwtIssuer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JWT audience.
    /// </summary>
    public string JwtAudience { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total number of providers.
    /// </summary>
    public int TotalProviders { get; set; }

    /// <summary>
    /// Gets or sets the number of enabled providers.
    /// </summary>
    public int EnabledProviders { get; set; }

    /// <summary>
    /// Gets or sets the total number of models.
    /// </summary>
    public int TotalModels { get; set; }

    /// <summary>
    /// Gets or sets the total number of aliases.
    /// </summary>
    public int TotalAliases { get; set; }
    }

    /// <summary>
    /// DTO for user preferences.
    /// </summary>
    public class UserPreferencesDto
    {
    /// <summary>
    /// Gets or sets the UI theme.
    /// </summary>
    public string Theme { get; set; } = "dark";

    /// <summary>
    /// Gets or sets the default model.
    /// </summary>
    public string DefaultModel { get; set; } = "default";

    /// <summary>
    /// Gets or sets a value indicating whether streaming is enabled.
    /// </summary>
    public bool StreamingEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether notifications are enabled.
    /// </summary>
    public bool NotificationsEnabled { get; set; }
    }

    /// <summary>
    /// Request to update user preferences.
    /// </summary>
    public class UserPreferencesUpdateRequest
    {
    /// <summary>
    /// Gets or sets the UI theme.
    /// </summary>
    public string? Theme { get; set; }

    /// <summary>
    /// Gets or sets the default model.
    /// </summary>
    public string? DefaultModel { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether streaming is enabled.
    /// </summary>
    public bool? StreamingEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether notifications are enabled.
    /// </summary>
    public bool? NotificationsEnabled { get; set; }
    }
}