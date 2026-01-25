using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.OpenApi;
using Synaxis.InferenceGateway.Application.Routing;
using Synaxis.InferenceGateway.WebApi.DTOs;
using Synaxis.InferenceGateway.WebApi.Middleware;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Synaxis.InferenceGateway.WebApi.Endpoints.OpenAI;

public static class LegacyCompletionsEndpoint
{
    public static void MapLegacyCompletions(this IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/completions", async (HttpContext ctx, CompletionRequest request, IChatClient chatClient, IModelResolver resolver) =>
        {
            if (request.Stream && (request.BestOf ?? 1) > 1)
            {
                return Results.BadRequest(new { error = new { message = "Cannot stream with best_of > 1", type = "invalid_request_error", param = "best_of", code = "invalid_value" } });
            }

            var caps = new RequiredCapabilities { Streaming = request.Stream };
            var resolution = await resolver.ResolveAsync(request.Model, EndpointKind.LegacyCompletions, caps);

            ctx.Items["RoutingContext"] = new RoutingContext(request.Model, resolution.CanonicalId.ToString(), resolution.CanonicalId.Provider);

            var promptText = request.Prompt?.ToString() ?? "";
            if (request.Prompt is JsonElement je && je.ValueKind == JsonValueKind.Array)
            {
                promptText = string.Join("\n", je.EnumerateArray().Select(x => x.ToString()));
            }

            var messages = new List<ChatMessage> { new ChatMessage(ChatRole.User, promptText) };
            var options = new ChatOptions
            {
                ModelId = resolution.CanonicalId.ToString(),
                MaxOutputTokens = request.MaxTokens,
                Temperature = (float?)request.Temperature
            };

            if (request.Stream)
            {
                ctx.Response.Headers.ContentType = "text/event-stream";
                await foreach (var update in chatClient.GetStreamingResponseAsync(messages, options))
                {
                    var chunk = new
                    {
                        id = "cmpl-" + Guid.NewGuid(),
                        @object = "text_completion",
                        created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        model = resolution.CanonicalId.ToString(),
                        choices = new[]
                        {
                            new { text = update.Text, index = 0, finish_reason = update.FinishReason?.ToString().ToLowerInvariant() }
                        }
                    };
                    await ctx.Response.WriteAsync($"data: {JsonSerializer.Serialize(chunk)}\n\n");
                    await ctx.Response.Body.FlushAsync();
                }
                await ctx.Response.WriteAsync("data: [DONE]\n\n");
                return Results.Empty;
            }
            else
            {
                var response = await chatClient.GetResponseAsync(messages, options);
                return Results.Ok(new
                {
                    id = "cmpl-" + Guid.NewGuid(),
                    @object = "text_completion",
                    created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    model = resolution.CanonicalId.ToString(),
                    choices = new[]
                    {
                        new { text = response.Text, index = 0, finish_reason = response.FinishReason?.ToString().ToLowerInvariant() ?? "stop" }
                    },
                    usage = new { prompt_tokens = response.Usage?.InputTokenCount ?? 0, completion_tokens = response.Usage?.OutputTokenCount ?? 0, total_tokens = (response.Usage?.InputTokenCount ?? 0) + (response.Usage?.OutputTokenCount ?? 0) }
                });
            }
        })
        .WithTags("Completions")
        .WithSummary("Legacy text completion")
        .WithName("CreateCompletion")
        .AddOpenApiOperationTransformer((operation, context, ct) =>
        {
            operation.Deprecated = true;
            return Task.CompletedTask;
        });
    }
}
