using Synaplexer.Application.Commands;
using Synaplexer.Application.Dtos;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Text.Json;

namespace Synaplexer.API.Endpoints;

public static class ChatEndpoints
{
    public static void MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/chat");

        group.MapPost("/completions", async (ChatCompletionRequest request, IMediator mediator, HttpContext context, CancellationToken ct) =>
        {
            var command = new ChatCompletionCommand(
                request.Model,
                request.Messages,
                request.Temperature ?? 0.7f,
                request.MaxTokens ?? 2048,
                request.Stream ?? false
            );

            if (request.Stream == true)
            {
                context.Response.ContentType = "text/event-stream";
                
                // For now, since the handler returns a single result, we wrap it in a stream format
                // In a real implementation, the handler would return IAsyncEnumerable
                var result = await mediator.Send(command, ct);
                
                var response = new
                {
                    id = Guid.NewGuid().ToString(),
                    @object = "chat.completion.chunk",
                    created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    model = result.Model,
                    choices = new[]
                    {
                        new
                        {
                            delta = new { content = result.Content },
                            index = 0,
                            finish_reason = result.FinishReason
                        }
                    }
                };

                await context.Response.WriteAsync($"data: {JsonSerializer.Serialize(response)}\n\n", ct);
                await context.Response.WriteAsync("data: [DONE]\n\n", ct);
                await context.Response.Body.FlushAsync(ct);
                return Results.Empty;
            }
            else
            {
                var result = await mediator.Send(command, ct);
                return Results.Ok(new
                {
                    id = Guid.NewGuid().ToString(),
                    @object = "chat.completion",
                    created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    model = result.Model,
                    choices = new[]
                    {
                        new
                        {
                            message = new { role = "assistant", content = result.Content },
                            index = 0,
                            finish_reason = result.FinishReason
                        }
                    },
                    usage = new
                    {
                        total_tokens = result.UsageTokens
                    }
                });
            }
        })
        .AddEndpointFilter(async (context, next) =>
        {
            var request = context.GetArgument<ChatCompletionRequest>(0);
            if (string.IsNullOrWhiteSpace(request.Model))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["Model"] = ["Model is required."] });
            if (request.Messages == null || request.Messages.Length == 0)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["Messages"] = ["Messages are required."] });
            return await next(context);
        })
        .WithName("ChatCompletion")
        .WithOpenApi();
    }
}

public record ChatCompletionRequest(
    string Model,
    ChatMessage[] Messages,
    float? Temperature = 0.7f,
    int? MaxTokens = 2048,
    bool? Stream = false
);
