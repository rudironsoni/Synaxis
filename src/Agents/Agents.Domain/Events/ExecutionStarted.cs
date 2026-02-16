// <copyright file="ExecutionStarted.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Domain.Events;

using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when an agent execution is started.
/// </summary>
public record ExecutionStarted : IDomainEvent
{
    /// <inheritdoc/>
    public string EventId { get; init; } = Guid.NewGuid().ToString();

    /// <inheritdoc/>
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

    /// <inheritdoc/>
    public string EventType { get; init; } = nameof(ExecutionStarted);

    /// <summary>
    /// Gets the unique identifier of the execution.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the unique identifier of the agent.
    /// </summary>
    public required Guid AgentId { get; init; }

    /// <summary>
    /// Gets the execution identifier.
    /// </summary>
    public required string ExecutionId { get; init; }

    /// <summary>
    /// Gets the input parameters for the execution.
    /// </summary>
    public required IReadOnlyDictionary<string, object> InputParameters { get; init; }

    /// <summary>
    /// Gets the timestamp when the execution started.
    /// </summary>
    public required DateTime StartedAt { get; init; }
}
