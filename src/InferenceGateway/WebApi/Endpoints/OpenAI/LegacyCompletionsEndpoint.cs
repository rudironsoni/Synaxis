// <copyright file="LegacyCompletionsEndpoint.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Endpoints.OpenAI
{
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

                var caps = new RequiredCapabilities { Streaming = request.Stream };
                var resolution = await resolver.ResolveAsync(request.Model, EndpointKind.LegacyCompletions, caps);

                ctx.Items["RoutingContext"] = new RoutingContext(request.Model, resolution.canonicalId.ToString(), resolution.canonicalId.provider);

                if (!TryParsePrompt(request.Prompt, out var promptText, out var parseError))
                {
                    return Results.BadRequest(new { error = new { message = parseError, type = "invalid_request_error", param = "prompt", code = "invalid_value" } });
                }

                var messages = new List<ChatMessage> { new ChatMessage(ChatRole.User, promptText) };
                var options = new ChatOptions
                {
                    ModelId = resolution.canonicalId.ToString(),
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
                            model = resolution.canonicalId.ToString(),
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
                        model = resolution.canonicalId.ToString(),
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

        private static bool TryParsePrompt(object? promptObj, out string promptText, out string? errorMessage)
        {
            promptText = string.Empty;
            errorMessage = null;

            if (promptObj == null)
            {
                // treat null as empty prompt
                return true;
            }

            // Straight string
            if (promptObj is string s)
            {
                promptText = s;
                return true;
            }

            // JsonElement from System.Text.Json (common when model binding raw JSON)
            if (promptObj is JsonElement je)
            {
                try
                {
                    switch (je.ValueKind)
                    {
                        case JsonValueKind.String:
                            promptText = je.GetString() ?? string.Empty;
                            return true;
                        case JsonValueKind.Array:
                            var parts = new List<string>();
                            foreach (var el in je.EnumerateArray())
                            {
                                if (el.ValueKind == JsonValueKind.String)
                                {
                                    parts.Add(el.GetString() ?? string.Empty);
                                }
                                else
                                {
                                    errorMessage = "Prompt array must contain only strings.";
                                    return false;
                                }
                            }

                            promptText = string.Join("\n", parts);
                            return true;
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

            // Enumerable types (e.g., string[] bound by some serializers)
            if (promptObj is System.Collections.IEnumerable ie)
            {
                var parts = new List<string>();
                foreach (var item in ie)
                {
                    if (item == null)
                    {
                        parts.Add(string.Empty);
                        continue;
                    }

                    if (item is string si)
                    {
                        parts.Add(si);
                        continue;
                    }

                    // If items are JsonElement strings
                    if (item is JsonElement jel && jel.ValueKind == JsonValueKind.String)
                    {
                        parts.Add(jel.GetString() ?? string.Empty);
                        continue;
                    }

                    errorMessage = "Prompt array must contain only strings.";
                    return false;
                }

                promptText = string.Join("\n", parts);
                return true;
            }

            // Fallback: unsupported type
            errorMessage = "Prompt must be a string or an array of strings.";
            return false;
        }
    }
}
