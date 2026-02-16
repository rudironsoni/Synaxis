// <copyright file="AgentActivated.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Domain.Events;

using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when an agent is activated.
/// </summary>
public record AgentActivated : IDomainEvent
{
    /// <inheritdoc/>
    public string EventId { get; init; } = Guid.NewGuid().ToString();

    /// <inheritdoc/>
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

    /// <inheritdoc/>
    public string EventType { get; init; } = nameof(AgentActivated);

    /// <summary>
    /// Gets the unique identifier of the agent configuration.
    /// </summary>
    public required Guid AgentId { get; init; }

    /// <summary>
    /// Gets the version of the configuration.
    /// </summary>
    public required int Version { get; init; }

    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }
}
