// <copyright file="LegacyCompletionsEndpoint.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Endpoints.OpenAI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.OpenApi;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.Extensions.AI;
    using Microsoft.Extensions.DependencyInjection;
    using Synaxis.InferenceGateway.Application.Routing;
    using Synaxis.InferenceGateway.WebApi.DTOs;
    using Synaxis.InferenceGateway.WebApi.Middleware;

    /// <summary>
    /// Legacy completions endpoint for backward compatibility with OpenAI API.
    /// </summary>
    public static class LegacyCompletionsEndpoint
    {
        /// <summary>
        /// Maps the legacy completions endpoint.
        /// </summary>
        /// <param name="app">The endpoint route builder.</param>
        public static void MapLegacyCompletions(this IEndpointRouteBuilder app)
        {
            app.MapPost("/v1/completions", async (HttpContext ctx, CompletionRequest request, IChatClient chatClient, IModelResolver resolver) =>
            {
                if (request.Stream && (request.BestOf ?? 1) > 1)
                {
                    return Results.BadRequest(new { error = new { message = "Cannot stream with best_of > 1", type = "invalid_request_error", param = "best_of", code = "invalid_value" } });
                }

                var caps = BuildCapabilities(request);
                var resolution = await resolver.ResolveAsync(request.Model, EndpointKind.LegacyCompletions, caps).ConfigureAwait(false);
                ctx.Items["RoutingContext"] = new RoutingContext(request.Model, resolution.CanonicalId.ToString(), resolution.CanonicalId.Provider);

                if (!TryParsePrompt(request.Prompt, out var promptText, out var parseError))
                {
                    return Results.BadRequest(new { error = new { message = parseError, type = "invalid_request_error", param = "prompt", code = "invalid_value" } });
                }

                var messages = BuildPromptMessages(promptText);
                var options = BuildChatOptions(request, resolution.CanonicalId.ToString());

                return request.Stream
                    ? await StreamCompletionAsync(ctx, chatClient, resolution.CanonicalId.ToString(), messages, options).ConfigureAwait(false)
                    : await HandleCompletionAsync(chatClient, resolution.CanonicalId.ToString(), messages, options).ConfigureAwait(false);
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

        private static RequiredCapabilities BuildCapabilities(CompletionRequest request)
        {
            return new RequiredCapabilities { Streaming = request.Stream };
        }

        private static List<ChatMessage> BuildPromptMessages(string promptText)
        {
            return new List<ChatMessage> { new ChatMessage(ChatRole.User, promptText) };
        }

        private static ChatOptions BuildChatOptions(CompletionRequest request, string modelId)
        {
            return new ChatOptions
            {
                ModelId = modelId,
                MaxOutputTokens = request.MaxTokens,
                Temperature = (float?)request.Temperature,
            };
        }

        private static Task<IResult> StreamCompletionAsync(
            HttpContext ctx,
            IChatClient chatClient,
            string modelId,
            List<ChatMessage> messages,
            ChatOptions options)
        {
            ctx.Response.Headers.ContentType = "text/event-stream";
            return StreamCompletionInternalAsync(ctx, chatClient, modelId, messages, options);
        }

        private static async Task<IResult> StreamCompletionInternalAsync(
            HttpContext ctx,
            IChatClient chatClient,
            string modelId,
            List<ChatMessage> messages,
            ChatOptions options)
        {
            await foreach (var update in chatClient.GetStreamingResponseAsync(messages, options).ConfigureAwait(false))
            {
                var chunk = new
                {
                    id = "cmpl-" + Guid.NewGuid(),
                    @object = "text_completion",
                    created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    model = modelId,
                    choices = new[]
                    {
                        new { text = update.Text, index = 0, finish_reason = update.FinishReason?.ToString().ToLowerInvariant() },
                    },
                };
                await ctx.Response.WriteAsync($"data: {JsonSerializer.Serialize(chunk)}\n\n").ConfigureAwait(false);
                await ctx.Response.Body.FlushAsync().ConfigureAwait(false);
            }

            await ctx.Response.WriteAsync("data: [DONE]\n\n").ConfigureAwait(false);
            return Results.Empty;
        }

        private static async Task<IResult> HandleCompletionAsync(
            IChatClient chatClient,
            string modelId,
            List<ChatMessage> messages,
            ChatOptions options)
        {
            var response = await chatClient.GetResponseAsync(messages, options).ConfigureAwait(false);
            return Results.Ok(BuildCompletionResponse(response, modelId));
        }

        private static object BuildCompletionResponse(ChatResponse response, string modelId)
        {
            var usage = BuildUsage(response);
            var choices = BuildChoices(response);
            return new
            {
                id = "cmpl-" + Guid.NewGuid(),
                @object = "text_completion",
                created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                model = modelId,
                choices,
                usage,
            };
        }

        private static object BuildUsage(ChatResponse response)
        {
            var promptTokens = response.Usage?.InputTokenCount ?? 0;
            var completionTokens = response.Usage?.OutputTokenCount ?? 0;
            return new
            {
                prompt_tokens = promptTokens,
                completion_tokens = completionTokens,
                total_tokens = promptTokens + completionTokens,
            };
        }

        private static object[] BuildChoices(ChatResponse response)
        {
            var finishReason = response.FinishReason?.ToString().ToLowerInvariant() ?? "stop";
            return new[]
            {
                new { text = response.Text, index = 0, finish_reason = finishReason },
            };
        }

        private static bool TryParsePrompt(object? promptObj, out string promptText, out string? errorMessage)
        {
            promptText = string.Empty;
            errorMessage = null;

            if (promptObj == null)
            {
                return true;
            }

            if (promptObj is string s)
            {
                promptText = s;
                return true;
            }

            if (promptObj is JsonElement je)
            {
                return TryParseJsonElement(je, out promptText, out errorMessage);
            }

            if (promptObj is System.Collections.IEnumerable ie)
            {
                return TryParseEnumerablePrompt(ie, out promptText, out errorMessage);
            }

            errorMessage = "Prompt must be a string or an array of strings.";
            return false;
        }

        private static bool TryParseJsonElement(JsonElement element, out string promptText, out string? errorMessage)
        {
            promptText = string.Empty;
            errorMessage = null;

            try
            {
                switch (element.ValueKind)
                {
                    case JsonValueKind.String:
                        promptText = element.GetString() ?? string.Empty;
                        return true;
                    case JsonValueKind.Array:
                        return TryParseJsonArray(element, out promptText, out errorMessage);
                    case JsonValueKind.Null:
                        promptText = string.Empty;
                        return true;
                    default:
                        errorMessage = "Prompt must be a string or an array of strings.";
                        return false;
                }
            }
            catch (Exception ex)
            {
                errorMessage = "Malformed prompt." + (ex.Message.Length > 0 ? " " + ex.Message : string.Empty);
                return false;
            }
        }

        private static bool TryParseJsonArray(JsonElement element, out string promptText, out string? errorMessage)
        {
            var parts = new List<string>();
            using var enumerator = element.EnumerateArray();
            while (enumerator.MoveNext())
            {
                var item = enumerator.Current;
                if (item.ValueKind == JsonValueKind.String)
                {
                    parts.Add(item.GetString() ?? string.Empty);
                    continue;
                }

                promptText = string.Empty;
                errorMessage = "Prompt array must contain only strings.";
                return false;
            }

            promptText = string.Join("\n", parts);
            errorMessage = null;
            return true;
        }

        private static bool TryParseEnumerablePrompt(System.Collections.IEnumerable items, out string promptText, out string? errorMessage)
        {
            var parts = new List<string>();
            foreach (var item in items)
            {
                if (item == null)
                {
                    parts.Add(string.Empty);
                    continue;
                }

                if (item is string stringItem)
                {
                    parts.Add(stringItem);
                    continue;
                }

                if (item is JsonElement element && element.ValueKind == JsonValueKind.String)
                {
                    parts.Add(element.GetString() ?? string.Empty);
                    continue;
                }

                promptText = string.Empty;
                errorMessage = "Prompt array must contain only strings.";
                return false;
            }

            promptText = string.Join("\n", parts);
            errorMessage = null;
            return true;
        }
    }
}
