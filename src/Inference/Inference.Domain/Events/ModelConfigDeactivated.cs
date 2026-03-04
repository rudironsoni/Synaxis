// <copyright file="ModelConfigDeactivated.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Domain.Events;

using Mediator;
using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when a model configuration is deactivated.
/// </summary>
public class ModelConfigDeactivated : DomainEvent, INotification
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
