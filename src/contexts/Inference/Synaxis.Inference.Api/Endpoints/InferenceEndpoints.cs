// <copyright file="InferenceEndpoints.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Api.Endpoints;

using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Synaxis.Inference.Api.Models;
using Synaxis.Inference.Application.Commands;

/// <summary>
/// Extension methods for mapping inference API endpoints.
/// </summary>
public static class InferenceEndpoints
{
    /// <summary>
    /// Maps inference API endpoints.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapInferenceEndpoints(this IEndpointRouteBuilder app)
    {
        // Chat completions endpoint (minimal API)
        app.MapPost("/v1/chat/completions", async (
            [FromBody] ChatCompletionRequest request,
            IMediator mediator,
            ILogger<object> logger,
            CancellationToken cancellationToken) =>
        {
            logger.LogInformation("Minimal API: Received chat completion request for model {Model}", request.Model);

            if (request.Stream)
            {
                return Results.BadRequest(new { error = "Streaming not supported in minimal API endpoint" });
            }

            var requestId = Guid.NewGuid();
            var routeCommand = new RouteInferenceRequestCommand(requestId, request.Model, false);
            await mediator.Send(routeCommand, cancellationToken).ConfigureAwait(false);

            var executeCommand = new ExecuteInferenceCommand(requestId);
            var result = await mediator.Send(executeCommand, cancellationToken).ConfigureAwait(false);

            var response = new ChatCompletionResponse
            {
                Id = $"chatcmpl-{requestId:N}",
                Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Model = request.Model,
                Choices = new List<ChatCompletionChoice>
                {
                    new()
                    {
                        Index = 0,
                        Message = new ChatCompletionMessage
                        {
                            Role = "assistant",
                            Content = result.ResponseContent,
                        },
                        FinishReason = "stop",
                    },
                },
                Usage = new ChatCompletionUsage
                {
                    PromptTokens = result.TokenUsage?.InputTokenCount ?? 0,
                    CompletionTokens = result.TokenUsage?.OutputTokenCount ?? 0,
                    TotalTokens = (result.TokenUsage?.InputTokenCount ?? 0) + (result.TokenUsage?.OutputTokenCount ?? 0),
                },
            };

            return Results.Ok(response);
        })
        .WithName("CreateChatCompletion")
        .WithDisplayName("Create Chat Completion")
        .WithSummary("Create a chat completion")
        .WithDescription("Creates a non-streaming chat completion for the given messages.")
        .Produces<ChatCompletionResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError);

        // Health check endpoint
        app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
            .WithName("HealthCheck")
            .WithDisplayName("Health Check")
            .AllowAnonymous();

        // Version endpoint
        app.MapGet("/version", () =>
        {
            var version = typeof(InferenceEndpoints).Assembly.GetName().Version?.ToString() ?? "1.0.0";
            return Results.Ok(new { version, apiVersion = "v1" });
        })
        .WithName("GetVersion")
        .WithDisplayName("Get API Version")
        .AllowAnonymous();

        return app;
    }
}
