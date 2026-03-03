// <copyright file="ActivityFailed.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Domain.Events;

using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when an activity fails.
/// </summary>
public class ActivityFailed : DomainEvent
{
    /// <summary>
    /// Gets or sets the activity identifier.
    /// </summary>
    public Guid ActivityId { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc/>
    public override string AggregateId => this.ActivityId.ToString();

    /// <inheritdoc/>
    public override string EventType => nameof(ActivityFailed);
}
