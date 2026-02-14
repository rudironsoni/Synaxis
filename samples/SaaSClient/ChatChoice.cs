// <copyright file="ChatChoice.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Samples.SaaSClient;

public record ChatChoice
{
    public int Index { get; init; }
    public ChatMessage? Message { get; init; }
    public string? FinishReason { get; init; }
}
