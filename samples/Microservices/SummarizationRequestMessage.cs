// <copyright file="SummarizationRequestMessage.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using System;

namespace Synaxis.Samples.Microservices;

public record SummarizationRequestMessage
{
    public string RequestId { get; init; } = Guid.NewGuid().ToString();
    public string Text { get; init; } = string.Empty;
    public int? MaxTokens { get; init; }
}
