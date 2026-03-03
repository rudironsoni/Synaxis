// <copyright file="WorkflowRetried.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Domain.Events;

using Mediator;
using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when a workflow step is retried.
/// </summary>
public record WorkflowRetried : IDomainEvent, INotification
{
    /// <inheritdoc/>
    public string EventId { get; init; } = Guid.NewGuid().ToString();

    /// <inheritdoc/>
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

    /// <inheritdoc/>
    public string EventType { get; init; } = nameof(WorkflowRetried);

    /// <summary>
    /// Gets the unique identifier of the workflow.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the step number being retried.
    /// </summary>
    public required int StepNumber { get; init; }

    /// <summary>
    /// Gets the retry attempt number.
    /// </summary>
    public required int RetryAttempt { get; init; }

    /// <summary>
    /// Gets the timestamp when the retry was initiated.
    /// </summary>
    public required DateTime RetriedAt { get; init; }
}
