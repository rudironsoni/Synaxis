// <copyright file="PreferredModelUpdated.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Domain.Events;

using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when preferred model is updated.
/// </summary>
public class PreferredModelUpdated : DomainEvent
{
    /// <summary>
    /// Gets or sets the preferences identifier.
    /// </summary>
    public Guid PreferencesId { get; set; }

    /// <summary>
    /// Gets or sets the model identifier.
    /// </summary>
    public string? ModelId { get; set; }

    /// <summary>
    /// Gets or sets the provider identifier.
    /// </summary>
    public string? ProviderId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc/>
    public override string AggregateId => this.PreferencesId.ToString();

    /// <inheritdoc/>
    public override string EventType => nameof(PreferredModelUpdated);
}
