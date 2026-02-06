// <copyright file="OpenAIEndpointsExtensions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Endpoints.OpenAI
{
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
    using Microsoft.Extensions.AI;
    using Synaxis.InferenceGateway.WebApi.Features.Chat.Commands;
    using Synaxis.InferenceGateway.WebApi.DTOs.OpenAi;

    /// <summary>
    /// Extensions for mapping OpenAI-compatible endpoints.
    /// </summary>
    public static class OpenAIEndpointsExtensions
    {
        /// <summary>
        /// Maps OpenAI-compatible endpoints to the application.
        /// </summary>
        /// <param name="endpoints">The endpoint route builder.</param>
        /// <returns>The endpoint route builder for chaining.</returns>
        public static IEndpointRouteBuilder MapOpenAIEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/openai")
            .RequireCors("PublicAPI");
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
            var request = await OpenAIRequestParser.ParseAsync(context, ct, allowEmptyModel: false, allowEmptyMessages: false);
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
        group.MapPost("/v1/responses", async (HttpContext context, IMediator mediator, CancellationToken ct) =>
        {
            var request = await OpenAIRequestParser.ParseAsync(context, ct, allowEmptyModel: true, allowEmptyMessages: true);

            if (request == null) return Results.BadRequest("Invalid request body");

            // Store the parsed request in HTTP context so RoutingAgent doesn't re-parse
            context.Items["ParsedOpenAIRequest"] = request;

            var messages = OpenAIRequestMapper.ToChatMessages(request);

            if (request.Stream)
            {
                context.Response.Headers.ContentType = "text/event-stream";
                context.Response.Headers.CacheControl = "no-cache";
                context.Response.Headers.Connection = "keep-alive";

                var id = "resp_" + Guid.NewGuid().ToString("N");
                var created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                var stream = mediator.CreateStream(new ChatStreamCommand(request, messages ?? Enumerable.Empty<ChatMessage>()), ct);

                await foreach (var update in stream)
                {
                    var content = update.Text;

                    if (string.IsNullOrEmpty(content)) continue;

                    var chunk = new ResponseStreamChunk
                    {
                        Id = id,
                        Object = "response.output_item.delta",
                        Created = created,
                        Model = request.Model ?? "default",
                        Delta = new ResponseDelta { Content = content }
                    };
                    await context.Response.WriteAsync($"data: {JsonSerializer.Serialize(chunk, ModelJsonContext.Options)}\n\n", ct);
                    await context.Response.Body.FlushAsync(ct);
                }

                var finalChunk = new ResponseStreamChunk
                {
                    Id = id,
                    Object = "response.completed",
                    Created = created,
                    Model = request.Model ?? "default",
                    Delta = new ResponseDelta()
                };
                await context.Response.WriteAsync($"data: {JsonSerializer.Serialize(finalChunk, ModelJsonContext.Options)}\n\n", ct);
                await context.Response.WriteAsync("data: [DONE]\n\n", ct);

                return Results.Empty;
            }
            else
            {
                var response = await mediator.Send(new ChatCommand(request, messages ?? Enumerable.Empty<ChatMessage>()), ct);

                var message = response.Messages.FirstOrDefault();
                var content = message?.Text ?? "";

                var openAIResponse = new ResponseCompletion
                {
                    Id = "resp_" + Guid.NewGuid().ToString("N"),
                    Object = "response",
                    Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Model = request.Model ?? "default",
                    Output = new List<ResponseOutput>
                    {
                        new ResponseOutput
                        {
                            Type = "message",
                            Role = "assistant",
                            Content = new List<ResponseContent>
                            {
                                new ResponseContent
                                {
                                    Type = "output_text",
                                    Text = content
                                }
                            }
                        }
                    }
                };

                return Results.Json(openAIResponse, ModelJsonContext.Options);
            }
        })
        .WithTags("Responses")
        .WithSummary("Create response")
        .WithDescription("OpenAI-compatible responses endpoint supporting both streaming and non-streaming modes");

    // Legacy & Models
        group.MapLegacyCompletions();
        group.MapModels();
    }
    }

    /// <summary>
    /// Streaming response chunk for chat completions.
    /// </summary>
    public class ResponseStreamChunk
    {
        /// <summary>
        /// Gets or sets the unique identifier for this completion.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the object type.
        /// </summary>
        public string Object { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Unix timestamp when the completion was created.
        /// </summary>
        public long Created { get; set; }

        /// <summary>
        /// Gets or sets the model used for the completion.
        /// </summary>
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the delta content for this chunk.
        /// </summary>
        public ResponseDelta Delta { get; set; } = new ResponseDelta();
    }

    /// <summary>
    /// Delta content in a streaming response.
    /// </summary>
    public class ResponseDelta
    {
        /// <summary>
        /// Gets or sets the content text.
        /// </summary>
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// Completion response for non-streaming requests.
    /// </summary>
    public class ResponseCompletion
    {
        /// <summary>
        /// Gets or sets the unique identifier for this completion.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the object type.
        /// </summary>
        public string Object { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Unix timestamp when the completion was created.
        /// </summary>
        public long Created { get; set; }

        /// <summary>
        /// Gets or sets the model used for the completion.
        /// </summary>
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of output choices.
        /// </summary>
        public List<ResponseOutput> Output { get; set; } = new List<ResponseOutput>();
    }

    /// <summary>
    /// Output choice in a completion response.
    /// </summary>
    public class ResponseOutput
    {
        /// <summary>
        /// Gets or sets the output type.
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the role of the message author.
        /// </summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of content parts.
        /// </summary>
        public List<ResponseContent> Content { get; set; } = new List<ResponseContent>();
    }

    /// <summary>
    /// Content part in an output choice.
    /// </summary>
    public class ResponseContent
    {
        /// <summary>
        /// Gets or sets the content type.
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the text content.
        /// </summary>
        public string Text { get; set; } = string.Empty;
    }
}