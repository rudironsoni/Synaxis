// <copyright file="ModelsEndpoint.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Endpoints.OpenAI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.Extensions.Options;
    using Synaxis.InferenceGateway.Application.Configuration;

    /// <summary>
    /// Endpoints for OpenAI-compatible models API.
    /// </summary>
    public static class ModelsEndpoint
    {
        private const string SynaxisProvider = "synaxis";

        /// <summary>
        /// Maps model endpoints to the application.
        /// </summary>
        /// <param name="app">The endpoint route builder.</param>
        public static void MapModels(this IEndpointRouteBuilder app)
        {
            app.MapGet("/v1/models", (IOptions<SynaxisConfiguration> config) =>
            {
                var response = BuildModelsListResponse(config.Value);
                return Results.Json(response, ModelJsonContext.Options);
            })
            .WithTags("Models")
            .WithSummary("List models with capabilities and provider information")
            .WithDescription("Returns all available models grouped by provider with their capabilities (streaming, tools, vision, etc.)");

            app.MapGet("/v1/models/{**id}", (string id, IOptions<SynaxisConfiguration> config) =>
            {
                var model = TryResolveModel(id, config.Value);
                if (model != null)
                {
                    return Results.Json(model, ModelJsonContext.Options);
                }

                return Results.Json(
                    new
                    {
                        error = new
                        {
                            message = $"The model '{id}' does not exist",
                            type = "invalid_request_error",
                            param = "model",
                            code = "model_not_found",
                        },
                    },
                    ModelJsonContext.Options,
                    statusCode: 404);
            })
            .WithTags("Models")
            .WithSummary("Retrieve model with capabilities")
            .WithDescription("Returns detailed information about a specific model including its capabilities");
        }

        private static ModelsListResponseDto BuildModelsListResponse(SynaxisConfiguration config)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var models = new List<ModelDto>();

            foreach (var cm in config.CanonicalModels)
            {
                models.Add(BuildCanonicalModel(cm, now));
            }

            models.AddRange(config.Aliases.Select(alias => BuildAliasModel(alias.Key, now)));

            return new ModelsListResponseDto
            {
                Object = "list",
                Data = models,
                Providers = config.Providers.Select(BuildProviderSummary).ToList(),
            };
        }

        private static ModelDto? TryResolveModel(string id, SynaxisConfiguration config)
        {
            var cm = config.CanonicalModels.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.Ordinal));
            if (cm != null)
            {
                return BuildCanonicalModel(cm, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            }

            if (config.Aliases.ContainsKey(id))
            {
                return BuildAliasModel(id, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            }

            return null;
        }

        private static ModelDto BuildCanonicalModel(CanonicalModelConfig config, long created)
        {
            return new ModelDto
            {
                Id = config.Id,
                Object = "model",
                Created = created,
                OwnedBy = config.Provider,
                Provider = config.Provider,
                ModelPath = config.ModelPath,
                Capabilities = new ModelCapabilitiesDto
                {
                    Streaming = config.Streaming,
                    Tools = config.Tools,
                    Vision = config.Vision,
                    StructuredOutput = config.StructuredOutput,
                    LogProbs = config.LogProbs,
                },
            };
        }

        private static ModelDto BuildAliasModel(string aliasId, long created)
        {
            return new ModelDto
            {
                Id = aliasId,
                Object = "model",
                Created = created,
                OwnedBy = SynaxisProvider,
                Provider = SynaxisProvider,
                ModelPath = aliasId,
                Capabilities = new ModelCapabilitiesDto(),
            };
        }

        private static ProviderSummaryDto BuildProviderSummary(KeyValuePair<string, ProviderConfig> provider)
        {
            return new ProviderSummaryDto
            {
                Id = provider.Key,
                Type = provider.Value.Type,
                Enabled = provider.Value.Enabled,
                Tier = provider.Value.Tier,
            };
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
        public IList<ModelDto> Data { get; set; } = new List<ModelDto>();

        /// <summary>
        /// Gets or sets the list of provider summaries.
        /// </summary>
        public IList<ProviderSummaryDto> Providers { get; set; } = new List<ProviderSummaryDto>();
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
        public static readonly System.Text.Json.JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true,
        };
    }
}
