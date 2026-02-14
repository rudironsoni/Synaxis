// <copyright file="EmbeddingResponseMessage.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using Synaxis.Contracts.V1.Messages;

namespace Synaxis.Samples.Microservices;

public record EmbeddingResponseMessage
{
    public string RequestId { get; init; } = string.Empty;
    public EmbeddingResponse Response { get; init; } = null!;
}
