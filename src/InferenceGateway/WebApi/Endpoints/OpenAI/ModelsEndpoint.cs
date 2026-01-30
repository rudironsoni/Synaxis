using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Synaxis.InferenceGateway.Application.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Synaxis.InferenceGateway.WebApi.Endpoints.OpenAI;

public static class ModelsEndpoint
{
    public static void MapModels(this IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/models", (IOptions<SynaxisConfiguration> config) =>
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var models = new List<ModelDto>();

            foreach (var cm in config.Value.CanonicalModels)
            {
                var providerConfig = config.Value.Providers.GetValueOrDefault(cm.Provider);
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

public class ModelDto
{
    public string Id { get; set; } = "";
    public string Object { get; set; } = "";
    public long Created { get; set; }
    public string OwnedBy { get; set; } = "";
    public string Provider { get; set; } = "";
    public string ModelPath { get; set; } = "";
    public ModelCapabilitiesDto Capabilities { get; set; } = new();
}

public class ModelCapabilitiesDto
{
    public bool Streaming { get; set; }
    public bool Tools { get; set; }
    public bool Vision { get; set; }
    public bool StructuredOutput { get; set; }
    public bool LogProbs { get; set; }
}

public class ModelsListResponseDto
{
    public string Object { get; set; } = "";
    public List<ModelDto> Data { get; set; } = new();
    public List<ProviderSummaryDto> Providers { get; set; } = new();
}

public class ProviderSummaryDto
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public bool Enabled { get; set; }
    public int Tier { get; set; }
}

public static class ModelJsonContext
{
    public static readonly System.Text.Json.JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };
}
