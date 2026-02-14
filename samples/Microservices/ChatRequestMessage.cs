// <copyright file="ChatRequestMessage.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using System;
using Synaxis.Contracts.V1.Messages;

namespace Synaxis.Samples.Microservices;

public record ChatRequestMessage
{
    public string RequestId { get; init; } = Guid.NewGuid().ToString();
    public ChatMessage[] Messages { get; init; } = Array.Empty<ChatMessage>();
    public string? Model { get; init; }
    public double? Temperature { get; init; }
    public int? MaxTokens { get; init; }
}
