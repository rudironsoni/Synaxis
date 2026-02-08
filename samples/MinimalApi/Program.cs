// <copyright file="Program.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using System;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Synaxis.Commands.Chat;
using Synaxis.Contracts.V1.Messages;
using Synaxis.DependencyInjection;
using Synaxis.Providers.OpenAI.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add Synaxis services
builder.Services.AddSynaxis();

// Add OpenAI provider
var openAiApiKey = builder.Configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key not configured");
builder.Services.AddOpenAIProvider(openAiApiKey);

// Add required services for Mediator execution
builder.Services.AddLogging();

var app = builder.Build();

// Map chat endpoint
app.MapPost("/chat", async (ChatRequest request, IMediator mediator) =>
{
    // Create chat command
    var command = new ChatCommand(
        Messages: request.Messages,
        Model: request.Model ?? "gpt-3.5-turbo",
        Temperature: request.Temperature,
        MaxTokens: request.MaxTokens,
        Provider: request.Provider);

    // Execute command through mediator
    var response = await mediator.Send(command);

    return Results.Ok(response);
})
.WithName("Chat");

app.MapGet("/", () => Results.Ok(new
{
    Name = "Synaxis Minimal API Sample",
    Version = "1.0.0",
    Endpoints = new[]
    {
        new { Method = "POST", Path = "/chat", Description = "Send chat messages and receive AI responses" },
    },
}))
.WithName("Root");

await app.RunAsync();

/// <summary>
/// Request model for chat endpoint.
/// </summary>
public record ChatRequest
{
    /// <summary>
    /// Gets or initializes the conversation messages.
    /// </summary>
    public ChatMessage[] Messages { get; init; } = System.Array.Empty<ChatMessage>();

    /// <summary>
    /// Gets or initializes the model to use (e.g., "gpt-3.5-turbo", "gpt-4").
    /// </summary>
    public string? Model { get; init; }

    /// <summary>
    /// Gets or initializes the sampling temperature (0.0-2.0).
    /// </summary>
    public double? Temperature { get; init; }

    /// <summary>
    /// Gets or initializes the maximum number of tokens to generate.
    /// </summary>
    public int? MaxTokens { get; init; }

    /// <summary>
    /// Gets or initializes the optional provider name override.
    /// </summary>
    public string? Provider { get; init; }
}
