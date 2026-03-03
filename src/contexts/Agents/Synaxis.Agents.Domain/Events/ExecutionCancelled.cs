// <copyright file="ExecutionCancelled.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Domain.Events;

using Mediator;
using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when an agent execution is cancelled.
/// </summary>
public record ExecutionCancelled : IDomainEvent, INotification
{
    /// <inheritdoc/>
    public string EventId { get; init; } = Guid.NewGuid().ToString();

    /// <inheritdoc/>
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

    /// <inheritdoc/>
    public string EventType { get; init; } = nameof(ExecutionCancelled);

    /// <summary>
    /// Gets the unique identifier of the execution.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the current step number when cancelled.
    /// </summary>
    public required int CurrentStep { get; init; }

    /// <summary>
    /// Gets the timestamp when the execution was cancelled.
    /// </summary>
    public required DateTime CancelledAt { get; init; }

    /// <summary>
    /// Gets the duration of the execution in milliseconds before cancellation.
    /// </summary>
    public required long DurationMs { get; init; }
}
