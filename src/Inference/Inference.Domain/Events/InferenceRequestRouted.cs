// <copyright file="InferenceRequestRouted.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Domain.Events;

using Synaxis.Abstractions.Cloud;
using Synaxis.Inference.Domain.Aggregates;

/// <summary>
/// Event raised when an inference request is routed.
/// </summary>
public class InferenceRequestRouted : DomainEvent
{
    /// <summary>
    /// Gets or sets the request identifier.
    /// </summary>
    public Guid RequestId { get; set; }

    /// <summary>
    /// Gets or sets the provider identifier.
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model identifier.
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the routing decision.
    /// </summary>
    public RoutingDecision? RoutingDecision { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc/>
    public override string AggregateId => this.RequestId.ToString();

    /// <inheritdoc/>
    public override string EventType => nameof(InferenceRequestRouted);
}
