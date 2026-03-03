// <copyright file="ModelPricingUpdated.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Domain.Events;

using Synaxis.Abstractions.Cloud;
using Synaxis.Inference.Domain.Aggregates;

/// <summary>
/// Event raised when model pricing is updated.
/// </summary>
public class ModelPricingUpdated : DomainEvent
{
    /// <summary>
    /// Gets or sets the configuration identifier.
    /// </summary>
    public Guid ConfigId { get; set; }

    /// <summary>
    /// Gets or sets the pricing.
    /// </summary>
    public ModelPricing Pricing { get; set; } = new();

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc/>
    public override string AggregateId => this.ConfigId.ToString();

    /// <inheritdoc/>
    public override string EventType => nameof(ModelPricingUpdated);
}
