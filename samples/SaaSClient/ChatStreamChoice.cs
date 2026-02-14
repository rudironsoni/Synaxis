// <copyright file="ChatStreamChoice.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Samples.SaaSClient;

public record ChatStreamChoice
{
    public int Index { get; init; }
    public ChatDelta? Delta { get; init; }
    public string? FinishReason { get; init; }
}
