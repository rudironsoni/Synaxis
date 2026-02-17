// <copyright file="ChatHistorySettingsUpdated.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Domain.Events;

using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when chat history settings are updated.
/// </summary>
public class ChatHistorySettingsUpdated : DomainEvent
{
    /// <summary>
    /// Gets or sets the preferences identifier.
    /// </summary>
    public Guid PreferencesId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to save history.
    /// </summary>
    public bool SaveHistory { get; set; }

    /// <summary>
    /// Gets or sets the retention days.
    /// </summary>
    public int RetentionDays { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc/>
    public override string AggregateId => this.PreferencesId.ToString();

    /// <inheritdoc/>
    public override string EventType => nameof(ChatHistorySettingsUpdated);
}
