// <copyright file="BackgroundJobStarted.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Domain.Events;

using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when a background job starts execution.
/// </summary>
public class BackgroundJobStarted : DomainEvent
{
    /// <summary>
    /// Gets or sets the job identifier.
    /// </summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc/>
    public override string AggregateId => this.JobId.ToString();

    /// <inheritdoc/>
    public override string EventType => nameof(BackgroundJobStarted);
}
