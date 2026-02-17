// <copyright file="InferenceRequestCreated.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Domain.Events;

using Synaxis.Abstractions.Cloud;
using Synaxis.Inference.Domain.Aggregates;

/// <summary>
/// Event raised when an inference request is created.
/// </summary>
public class InferenceRequestCreated : DomainEvent
{
    /// <summary>
    /// Gets or sets the request identifier.
    /// </summary>
    public Guid RequestId { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Gets or sets the API key identifier.
    /// </summary>
    public Guid? ApiKeyId { get; set; }

    /// <summary>
    /// Gets or sets the model identifier.
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the request content.
    /// </summary>
    public string RequestContent { get; set; } = string.Empty;

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
    public override string EventType => nameof(InferenceRequestCreated);
}
