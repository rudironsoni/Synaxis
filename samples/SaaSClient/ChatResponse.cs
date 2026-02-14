// <copyright file="ChatResponse.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Samples.SaaSClient;

public record ChatResponse
{
    public string Id { get; init; } = string.Empty;
    public string Object { get; init; } = string.Empty;
    public long Created { get; init; }
    public string Model { get; init; } = string.Empty;
    public ChatChoice[] Choices { get; init; } = System.Array.Empty<ChatChoice>();
    public ChatUsage? Usage { get; init; }
}
