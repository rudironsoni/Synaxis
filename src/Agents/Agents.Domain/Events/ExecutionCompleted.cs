// <copyright file="ExecutionCompleted.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Domain.Events;

using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when an agent execution completes successfully.
/// </summary>
public record ExecutionCompleted : IDomainEvent
{
    /// <inheritdoc/>
    public string EventId { get; init; } = Guid.NewGuid().ToString();

    /// <inheritdoc/>
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

    /// <inheritdoc/>
    public string EventType { get; init; } = nameof(ExecutionCompleted);

    /// <summary>
    /// Gets the unique identifier of the execution.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the timestamp when the execution completed.
    /// </summary>
    public required DateTime CompletedAt { get; init; }

    /// <summary>
    /// Gets the duration of the execution in milliseconds.
    /// </summary>
    public required long DurationMs { get; init; }
}
