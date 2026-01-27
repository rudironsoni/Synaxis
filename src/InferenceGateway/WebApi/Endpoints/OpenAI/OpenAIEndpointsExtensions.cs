using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Agents.AI.Hosting.OpenAI;
using Synaxis.InferenceGateway.WebApi.Agents;
using Synaxis.InferenceGateway.WebApi.Helpers;
using System;
using System.Threading.Tasks;
using System.Text.Json;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Mediator;
using Synaxis.InferenceGateway.WebApi.Features.Chat.Commands;
using Synaxis.InferenceGateway.WebApi.DTOs.OpenAi;

namespace Synaxis.InferenceGateway.WebApi.Endpoints.OpenAI;

public static class OpenAIEndpointsExtensions
{
    public static IEndpointRouteBuilder MapOpenAIEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/openai");
        var apiPrefix = typeof(OpenAIEndpointsExtensions).Assembly.GetName().Name!.Split('.')[0];
        MapOpenAIRoutes(group, apiPrefix);
        return group;
    }

    private static void MapOpenAIRoutes(IEndpointRouteBuilder group, string apiPrefix)
    {
        // Chat Completions
        group.MapPost("/v1/chat/completions", async (HttpContext context, IMediator mediator, CancellationToken ct) =>
        {
            // 1. Parse Request
            var request = await OpenAIRequestParser.ParseAsync(context, ct);
            if (request == null) return Results.BadRequest("Invalid request body");

            // 2. Map Messages
            var messages = OpenAIRequestMapper.ToChatMessages(request);

            // 3. Handle Streaming
            if (request.Stream)
            {
                context.Response.Headers.ContentType = "text/event-stream";
                context.Response.Headers.CacheControl = "no-cache";
                context.Response.Headers.Connection = "keep-alive";

                var id = "chatcmpl-" + Guid.NewGuid().ToString("N");
                var created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                var stream = mediator.CreateStream(new ChatStreamCommand(request, messages), ct);

                await foreach (var update in stream)
                {
                    var content = update.Text; 
                    
                    if (string.IsNullOrEmpty(content)) continue;

                    var chunk = new ChatCompletionChunk
                    {
                        Id = id,
                        Object = "chat.completion.chunk",
                        Created = created,
                        Model = request.Model ?? "default",
                        Choices = new List<ChatCompletionChunkChoice>
                        {
                            new ChatCompletionChunkChoice
                            {
                                Index = 0,
                                Delta = new ChatCompletionChunkDelta { Content = content },
                                FinishReason = null
                            }
                        }
                    };
                    await context.Response.WriteAsync($"data: {JsonSerializer.Serialize(chunk)}\n\n", ct);
                    await context.Response.Body.FlushAsync(ct);
                }
                
                var finalChunk = new ChatCompletionChunk
                {
                    Id = id,
                    Object = "chat.completion.chunk",
                    Created = created,
                    Model = request.Model ?? "default",
                    Choices = new List<ChatCompletionChunkChoice>
                    {
                        new ChatCompletionChunkChoice
                        {
                            Index = 0,
                            Delta = new ChatCompletionChunkDelta(),
                            FinishReason = "stop"
                        }
                    }
                };
                await context.Response.WriteAsync($"data: {JsonSerializer.Serialize(finalChunk)}\n\n", ct);
                await context.Response.WriteAsync("data: [DONE]\n\n", ct);
                
                return Results.Empty;
            }
            else
            {
                // 4. Handle Non-Streaming
                var response = await mediator.Send(new ChatCommand(request, messages), ct);
                
                var message = response.Messages.FirstOrDefault();
                var content = message?.Text ?? "";
                var role = message?.Role.Value ?? "assistant";

                var openAIResponse = new ChatCompletionResponse
                {
                    Id = "chatcmpl-" + Guid.NewGuid().ToString("N"),
                    Object = "chat.completion",
                    Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Model = request.Model ?? "default",
                    Choices = new List<ChatCompletionChoice>
                    {
                        new ChatCompletionChoice
                        {
                            Index = 0,
                            Message = new ChatCompletionMessageDto
                            {
                                Role = role,
                                Content = content,
                            },
                            FinishReason = "stop"
                        }
                    },
                    Usage = new ChatCompletionUsage
                    {
                        PromptTokens = 0,
                        CompletionTokens = 0,
                        TotalTokens = 0
                    }
                };

                return Results.Json(openAIResponse);
            }
        })
        .WithTags("Chat")
        .AddOpenApiOperationTransformer((operation, context, ct) =>
        {
            operation.Summary = "Chat Completions";
            operation.Description = "OpenAI-compatible chat completions endpoint.";
            operation.OperationId = $"{apiPrefix}/CreateChatCompletion";
            return Task.CompletedTask;
        })
        .WithName("ChatCompletions");

        // Responses
        group.MapPost("/v1/responses", async (HttpContext context, RoutingService routingService, CancellationToken ct) =>
        {
             return Results.StatusCode(501); 
        })
        .WithTags("Responses");

        // Legacy & Models
        group.MapLegacyCompletions();
        group.MapModels(); 
    }
}
