// <copyright file="ModelsEndpoint.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Endpoints.OpenAI
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
    /// Endpoints for OpenAI-compatible models API.
    /// </summary>
    public static class ModelsEndpoint
    {
        /// <summary>
        /// Maps model endpoints to the application.
        /// </summary>
        /// <param name="app">The endpoint route builder.</param>
        public static void MapModels(this IEndpointRouteBuilder app)
        {
            app.MapGet("/v1/models", (IOptions<SynaxisConfiguration> config) =>
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var models = new List<ModelDto>();

                foreach (var cm in config.Value.CanonicalModels)
                {
                    var providerConfig = config.Value.Providers.TryGetValue(cm.Provider, out var providerConfigValue) ? providerConfigValue : null;
                    models.Add(new ModelDto
                    {
                        Id = cm.Id,
                        Object = "model",
                        Created = now,
                        OwnedBy = cm.Provider,
                        Provider = cm.Provider,
                        ModelPath = cm.ModelPath,
                        Capabilities = new ModelCapabilitiesDto
                        {
                            Streaming = cm.Streaming,
                            Tools = cm.Tools,
                            Vision = cm.Vision,
                            StructuredOutput = cm.StructuredOutput,
                            LogProbs = cm.LogProbs
                        }
                    });
                }

                foreach (var alias in config.Value.Aliases)
                {
                    models.Add(new ModelDto
                    {
                        Id = alias.Key,
                        Object = "model",
                        Created = now,
                        OwnedBy = "synaxis",
                        Provider = "synaxis",
                        ModelPath = alias.Key,
                        Capabilities = new ModelCapabilitiesDto()
                    });
                }

                var response = new ModelsListResponseDto
                {
                    Object = "list",
                    Data = models,
                    Providers = config.Value.Providers.Select(p => new ProviderSummaryDto
                    {
                        Id = p.Key,
                        Type = p.Value.Type,
                        Enabled = p.Value.Enabled,
                        Tier = p.Value.Tier
                    }).ToList()
                };

                return Results.Json(response, ModelJsonContext.Options);
            })
            .WithTags("Models")
            .WithSummary("List models with capabilities and provider information")
            .WithDescription("Returns all available models grouped by provider with their capabilities (streaming, tools, vision, etc.)");

            app.MapGet("/v1/models/{**id}", (string id, IOptions<SynaxisConfiguration> config) =>
            {
                var cm = config.Value.CanonicalModels.FirstOrDefault(x => x.Id == id);
                if (cm != null)
                {
                    return Results.Json(new ModelDto
                    {
                        Id = cm.Id,
                        Object = "model",
                        Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        OwnedBy = cm.Provider,
                        Provider = cm.Provider,
                        ModelPath = cm.ModelPath,
                        Capabilities = new ModelCapabilitiesDto
                        {
                            Streaming = cm.Streaming,
                            Tools = cm.Tools,
                            Vision = cm.Vision,
                            StructuredOutput = cm.StructuredOutput,
                            LogProbs = cm.LogProbs
                        }
                    }, ModelJsonContext.Options);
                }

                if (config.Value.Aliases.ContainsKey(id))
                {
                    return Results.Json(new ModelDto
                    {
                        Id = id,
                        Object = "model",
                        Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        OwnedBy = "synaxis",
                        Provider = "synaxis",
                        ModelPath = id,
                        Capabilities = new ModelCapabilitiesDto()
                    }, ModelJsonContext.Options);
                }

                return Results.Json(new
                {
                    error = new
                    {
                        message = $"The model '{id}' does not exist",
                        type = "invalid_request_error",
                        param = "model",
                        code = "model_not_found"
                    }
                }, ModelJsonContext.Options, statusCode: 404);
            })
            .WithTags("Models")
            .WithSummary("Retrieve model with capabilities")
            .WithDescription("Returns detailed information about a specific model including its capabilities");
        }
    }

    /// <summary>
    /// DTO for model information.
    /// </summary>
    public class ModelDto
    {
        /// <summary>
        /// Gets or sets the model ID.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the object type.
        /// </summary>
        public string Object { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public long Created { get; set; }

        /// <summary>
        /// Gets or sets the model owner.
        /// </summary>
        public string OwnedBy { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the provider name.
        /// </summary>
        public string Provider { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the model path.
        /// </summary>
        public string ModelPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the model capabilities.
        /// </summary>
        public ModelCapabilitiesDto Capabilities { get; set; } = new ModelCapabilitiesDto();
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
    /// Response DTO for models list.
    /// </summary>
    public class ModelsListResponseDto
    {
        /// <summary>
        /// Gets or sets the object type.
        /// </summary>
        public string Object { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of models.
        /// </summary>
        public List<ModelDto> Data { get; set; } = new List<ModelDto>();

        /// <summary>
        /// Gets or sets the list of provider summaries.
        /// </summary>
        public List<ProviderSummaryDto> Providers { get; set; } = new List<ProviderSummaryDto>();
    }

    /// <summary>
    /// DTO for provider summary information.
    /// </summary>
    public class ProviderSummaryDto
    {
        /// <summary>
        /// Gets or sets the provider ID.
        /// </summary>
        public string Id { get; set; } = string.Empty;

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
    }

    /// <summary>
    /// JSON serialization context for model DTOs.
    /// </summary>
    public static class ModelJsonContext
    {
        /// <summary>
        /// Gets the JSON serializer options for model DTOs.
        /// </summary>
        public static readonly System.Text.Json.JsonSerializerOptions Options = new ()
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };
    }
}
