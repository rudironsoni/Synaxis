// <copyright file="PreferencesReset.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Domain.Events;

using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when preferences are reset to defaults.
/// </summary>
public class PreferencesReset : DomainEvent
{
    /// <summary>
    /// Gets or sets the preferences identifier.
    /// </summary>
    public Guid PreferencesId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc/>
    public override string AggregateId => this.PreferencesId.ToString();

    /// <inheritdoc/>
    public override string EventType => nameof(PreferencesReset);
}
