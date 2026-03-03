// <copyright file="BackgroundJobCreated.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Domain.Events;

using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when a new background job is created.
/// </summary>
public class BackgroundJobCreated : DomainEvent
{
    /// <summary>
    /// Gets or sets the job identifier.
    /// </summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// Gets or sets the job name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the job type.
    /// </summary>
    public string JobType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the job payload.
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the scheduled execution time.
    /// </summary>
    public DateTime? ScheduledAt { get; set; }

    /// <summary>
    /// Gets or sets the maximum retry count.
    /// </summary>
    public int MaxRetries { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc/>
    public override string AggregateId => this.JobId.ToString();

    /// <inheritdoc/>
    public override string EventType => nameof(BackgroundJobCreated);
}
