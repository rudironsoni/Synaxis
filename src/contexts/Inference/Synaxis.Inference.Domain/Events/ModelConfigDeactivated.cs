// <copyright file="ModelConfigDeactivated.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Domain.Events;

using Synaxis.Abstractions.Cloud;

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
