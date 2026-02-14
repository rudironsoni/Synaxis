// <copyright file="EmbeddingRequestMessage.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using System;

namespace Synaxis.Samples.Microservices;

public record EmbeddingRequestMessage
{
    public string RequestId { get; init; } = Guid.NewGuid().ToString();
    public string Input { get; init; } = string.Empty;
    public string? Model { get; init; }
}
