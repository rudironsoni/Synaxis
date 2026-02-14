// <copyright file="SummarizationResponseMessage.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using Synaxis.Contracts.V1.Messages;

namespace Synaxis.Samples.Microservices;

public record SummarizationResponseMessage
{
    public string RequestId { get; init; } = string.Empty;
    public ChatResponse Response { get; init; } = null!;
}
