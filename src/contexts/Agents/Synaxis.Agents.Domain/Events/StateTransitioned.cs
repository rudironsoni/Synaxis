// <copyright file="StateTransitioned.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Domain.Events;

using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when an agent execution state transitions.
/// </summary>
public record StateTransitioned : IDomainEvent
{
    /// <summary>
    /// Gets the unique identifier for the event.
    /// </summary>
    public string EventId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the type name of the event.
    /// </summary>
    public string EventType { get; init; } = nameof(StateTransitioned);

    /// <summary>
    /// Gets the unique identifier of the agent execution.
    /// </summary>
    public required Guid ExecutionId { get; init; }

    /// <summary>
    /// Gets the state before the transition.
    /// </summary>
    public required AgentExecutionState FromState { get; init; }

    /// <summary>
    /// Gets the state after the transition.
    /// </summary>
    public required AgentExecutionState ToState { get; init; }

    /// <summary>
    /// Gets the timestamp when the transition occurred.
    /// </summary>
    public required DateTime Timestamp { get; init; }
}
