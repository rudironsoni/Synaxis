// <copyright file="InferenceRequestCompleted.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Domain.Events;

using Synaxis.Abstractions.Cloud;
using Synaxis.Inference.Domain.Aggregates;

/// <summary>
/// Event raised when an inference request completes.
/// </summary>
public class InferenceRequestCompleted : DomainEvent
{
    /// <summary>
    /// Gets or sets the request identifier.
    /// </summary>
    public Guid RequestId { get; set; }

    /// <summary>
    /// Gets or sets the response content.
    /// </summary>
    public string ResponseContent { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the token usage.
    /// </summary>
    public TokenUsage TokenUsage { get; set; } = new();

    /// <summary>
    /// Gets or sets the cost.
    /// </summary>
    public decimal Cost { get; set; }

    /// <summary>
    /// Gets or sets the latency in milliseconds.
    /// </summary>
    public long LatencyMs { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc/>
    public override string AggregateId => this.RequestId.ToString();

    /// <inheritdoc/>
    public override string EventType => nameof(InferenceRequestCompleted);
}
