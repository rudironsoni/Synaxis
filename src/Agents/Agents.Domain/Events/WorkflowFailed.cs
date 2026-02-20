// <copyright file="WorkflowFailed.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Domain.Events;

using Mediator;
using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when a workflow fails.
/// </summary>
public record WorkflowFailed : IDomainEvent, INotification
{
    /// <inheritdoc/>
    public string EventId { get; init; } = Guid.NewGuid().ToString();

    /// <inheritdoc/>
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

    /// <inheritdoc/>
    public string EventType { get; init; } = nameof(WorkflowFailed);

    /// <summary>
    /// Gets the unique identifier of the workflow.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the step number where the failure occurred.
    /// </summary>
    public required int StepNumber { get; init; }

    /// <summary>
    /// Gets the error message.
    /// </summary>
    public required string Error { get; init; }

    /// <summary>
    /// Gets the timestamp when the workflow failed.
    /// </summary>
    public required DateTime FailedAt { get; init; }

    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }
}
