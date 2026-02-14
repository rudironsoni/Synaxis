// <copyright file="ChatResponseMessage.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using Synaxis.Contracts.V1.Messages;

namespace Synaxis.Samples.Microservices;

public record ChatResponseMessage
{
    public string RequestId { get; init; } = string.Empty;
    public ChatResponse Response { get; init; } = null!;
}
