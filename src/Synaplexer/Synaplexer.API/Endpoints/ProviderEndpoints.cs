using Synaplexer.Application.Commands;
using Synaplexer.Application.Queries;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Synaplexer.API.Endpoints;

public static class ProviderEndpoints
{
    public static void MapProviderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/providers");

        group.MapGet("/status/{name}", async (string name, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetProviderStatusQuery(name), ct);
            return Results.Ok(result);
        })
        .WithName("GetProviderStatus")
        .WithOpenApi();

        group.MapPost("/{type}/initialize", async (string type, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new InitializeProviderCommand(type), ct);
            return result ? Results.Ok() : Results.BadRequest();
        })
        .WithName("InitializeProvider")
        .WithOpenApi();
    }
}
