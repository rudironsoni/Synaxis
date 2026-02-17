// <copyright file="NotificationPreferencesUpdated.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Domain.Events;

using Synaxis.Abstractions.Cloud;
using Synaxis.Inference.Domain.Aggregates;

/// <summary>
/// Event raised when notification preferences are updated.
/// </summary>
public class NotificationPreferencesUpdated : DomainEvent
{
    /// <summary>
    /// Gets or sets the preferences identifier.
    /// </summary>
    public Guid PreferencesId { get; set; }

    /// <summary>
    /// Gets or sets the notification preferences.
    /// </summary>
    public NotificationPreferences Notifications { get; set; } = new();

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc/>
    public override string AggregateId => this.PreferencesId.ToString();

    /// <inheritdoc/>
    public override string EventType => nameof(NotificationPreferencesUpdated);
}
