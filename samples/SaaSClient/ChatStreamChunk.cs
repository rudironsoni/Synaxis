// <copyright file="ChatStreamChunk.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Samples.SaaSClient;

public record ChatStreamChunk
{
    public string Id { get; init; } = string.Empty;
    public string Object { get; init; } = string.Empty;
    public long Created { get; init; }
    public string Model { get; init; } = string.Empty;
    public ChatStreamChoice[] Choices { get; init; } = System.Array.Empty<ChatStreamChoice>();
}
