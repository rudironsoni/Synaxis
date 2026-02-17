// <copyright file="ModelConfigEvents.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Domain.Events;

using Synaxis.Abstractions.Cloud;
using Synaxis.Inference.Domain.Aggregates;

/// <summary>
/// Event raised when a model configuration is created.
/// </summary>
public class ModelConfigCreated : DomainEvent
{
    /// <summary>
    /// Gets or sets the configuration identifier.
    /// </summary>
    public Guid ConfigId { get; set; }

    /// <summary>
    /// Gets or sets the model identifier.
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the provider identifier.
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the settings.
    /// </summary>
    public ModelSettings Settings { get; set; } = new();

    /// <summary>
    /// Gets or sets the pricing.
    /// </summary>
    public ModelPricing Pricing { get; set; } = new();

    /// <summary>
    /// Gets or sets the capabilities.
    /// </summary>
    public ModelCapabilities Capabilities { get; set; } = new();

    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc/>
    public override string AggregateId => this.ConfigId.ToString();

    /// <inheritdoc/>
    public override string EventType => nameof(ModelConfigCreated);
}

/// <summary>
/// Event raised when a model configuration is updated.
/// </summary>
public class ModelConfigUpdated : DomainEvent
{
    /// <summary>
    /// Gets or sets the configuration identifier.
    /// </summary>
    public Guid ConfigId { get; set; }

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the settings.
    /// </summary>
    public ModelSettings Settings { get; set; } = new();

    /// <summary>
    /// Gets or sets the pricing.
    /// </summary>
    public ModelPricing Pricing { get; set; } = new();

    /// <summary>
    /// Gets or sets the capabilities.
    /// </summary>
    public ModelCapabilities Capabilities { get; set; } = new();

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc/>
    public override string AggregateId => this.ConfigId.ToString();

    /// <inheritdoc/>
    public override string EventType => nameof(ModelConfigUpdated);
}

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

/// <summary>
/// Event raised when a model configuration is activated.
/// </summary>
public class ModelConfigActivated : DomainEvent
{
    /// <summary>
    /// Gets or sets the configuration identifier.
    /// </summary>
    public Guid ConfigId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc/>
    public override string AggregateId => this.ConfigId.ToString();

    /// <inheritdoc/>
    public override string EventType => nameof(ModelConfigActivated);
}

/// <summary>
/// Event raised when a model configuration is deactivated.
/// </summary>
public class ModelConfigDeactivated : DomainEvent
{
    /// <summary>
    /// Gets or sets the configuration identifier.
    /// </summary>
    public Guid ConfigId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc/>
    public override string AggregateId => this.ConfigId.ToString();

    /// <inheritdoc/>
    public override string EventType => nameof(ModelConfigDeactivated);
}
