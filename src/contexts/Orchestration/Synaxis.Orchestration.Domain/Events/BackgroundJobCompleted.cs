// <copyright file="BackgroundJobCompleted.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Domain.Events;

using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when a background job completes successfully.
/// </summary>
public class BackgroundJobCompleted : DomainEvent
{
    /// <summary>
    /// Gets or sets the job identifier.
    /// </summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// Gets or sets the result.
    /// </summary>
    public string Result { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc/>
    public override string AggregateId => this.JobId.ToString();

    /// <inheritdoc/>
    public override string EventType => nameof(BackgroundJobCompleted);
}
