// <copyright file="ExecutionFailed.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Domain.Events;

using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when an agent execution fails.
/// </summary>
public record ExecutionFailed : IDomainEvent
{
    /// <inheritdoc/>
    public string EventId { get; init; } = Guid.NewGuid().ToString();

    /// <inheritdoc/>
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

    /// <inheritdoc/>
    public string EventType { get; init; } = nameof(ExecutionFailed);

    /// <summary>
    /// Gets the unique identifier of the execution.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the error message.
    /// </summary>
    public required string Error { get; init; }

    /// <summary>
    /// Gets the timestamp when the execution failed.
    /// </summary>
    public required DateTime FailedAt { get; init; }

    /// <summary>
    /// Gets the duration of the execution in milliseconds before failure.
    /// </summary>
    public required long DurationMs { get; init; }
}
