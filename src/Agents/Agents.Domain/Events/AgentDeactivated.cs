// <copyright file="AgentDeactivated.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Domain.Events;

using Mediator;
using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when an agent is deactivated.
/// </summary>
public record AgentDeactivated : IDomainEvent, INotification
{
    /// <inheritdoc/>
    public string EventId { get; init; } = Guid.NewGuid().ToString();

    /// <inheritdoc/>
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

    /// <inheritdoc/>
    public string EventType { get; init; } = nameof(AgentDeactivated);

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
