// <copyright file="ExecutionResumed.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Domain.Events;

using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when a paused agent execution is resumed.
/// </summary>
public record ExecutionResumed : IDomainEvent
{
    /// <inheritdoc/>
    public string EventId { get; init; } = Guid.NewGuid().ToString();

    /// <inheritdoc/>
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

    /// <inheritdoc/>
    public string EventType { get; init; } = nameof(ExecutionResumed);

    /// <summary>
    /// Gets the unique identifier of the execution.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the current step number when resumed.
    /// </summary>
    public required int CurrentStep { get; init; }
}
