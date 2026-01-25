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
            var models = new List<object>();
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            foreach (var cm in config.Value.CanonicalModels)
            {
                models.Add(new { id = cm.Id, @object = "model", created = now, owned_by = cm.Provider });
            }
            foreach (var alias in config.Value.Aliases)
            {
                models.Add(new { id = alias.Key, @object = "model", created = now, owned_by = "synaxis" });
            }

            return Results.Ok(new { @object = "list", data = models });
        })
        .WithTags("Models")
        .WithSummary("List models")
        .WithName("ListModels");

        app.MapGet("/v1/models/{id}", (string id, IOptions<SynaxisConfiguration> config) =>
        {
            var cm = config.Value.CanonicalModels.FirstOrDefault(x => x.Id == id);
            if (cm != null) return Results.Ok(new { id = cm.Id, @object = "model", created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), owned_by = cm.Provider });

            if (config.Value.Aliases.ContainsKey(id)) return Results.Ok(new { id = id, @object = "model", created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), owned_by = "synaxis" });

            return Results.NotFound(new { error = new { message = $"The model '{id}' does not exist", type = "invalid_request_error", param = "model", code = "model_not_found" } });
        })
        .WithTags("Models")
        .WithSummary("Retrieve model")
        .WithName("RetrieveModel");
    }
}
