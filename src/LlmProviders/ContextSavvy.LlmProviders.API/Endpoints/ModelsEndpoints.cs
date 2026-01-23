using ContextSavvy.LlmProviders.Application.Queries;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ContextSavvy.LlmProviders.API.Endpoints;

public static class ModelsEndpoints
{
    public static void MapModelsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/models");

        group.MapGet("/", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new ListAvailableModelsQuery(), ct);
            return Results.Ok(new { data = result });
        })
        .WithName("ListModels")
        .WithOpenApi();
    }
}
