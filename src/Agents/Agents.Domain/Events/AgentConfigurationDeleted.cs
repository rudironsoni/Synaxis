// <copyright file="AgentConfigurationDeleted.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Domain.Events;

using Mediator;
using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when an agent configuration is deleted.
/// </summary>
public record AgentConfigurationDeleted : IDomainEvent, INotification
{
    /// <inheritdoc/>
    public string EventId { get; init; } = Guid.NewGuid().ToString();

    /// <inheritdoc/>
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

    /// <inheritdoc/>
    public string EventType { get; init; } = nameof(AgentConfigurationDeleted);

    /// <summary>
    /// Gets the unique identifier of the agent configuration.
    /// </summary>
    public required Guid AgentId { get; init; }

    /// <summary>
    /// Gets the version of the configuration at deletion.
    /// </summary>
    public required int Version { get; init; }
}
