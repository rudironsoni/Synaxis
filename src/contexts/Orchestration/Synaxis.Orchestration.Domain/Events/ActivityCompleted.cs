// <copyright file="ActivityCompleted.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Domain.Events;

using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when an activity completes successfully.
/// </summary>
public class ActivityCompleted : DomainEvent
{
    /// <summary>
    /// Gets or sets the activity identifier.
    /// </summary>
    public Guid ActivityId { get; set; }

    /// <summary>
    /// Gets or sets the output data.
    /// </summary>
    public string? OutputData { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc/>
    public override string AggregateId => this.ActivityId.ToString();

    /// <inheritdoc/>
    public override string EventType => nameof(ActivityCompleted);
}
