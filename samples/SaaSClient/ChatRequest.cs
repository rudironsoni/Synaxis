// <copyright file="ChatRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Samples.SaaSClient;

public record ChatRequest
{
    public ChatMessage[] Messages { get; init; } = System.Array.Empty<ChatMessage>();
    public string Model { get; init; } = "gpt-3.5-turbo";
    public double? Temperature { get; init; }
    public int? MaxTokens { get; init; }
    public bool Stream { get; init; }
}
