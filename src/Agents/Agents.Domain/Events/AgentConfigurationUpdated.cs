// <copyright file="AgentConfigurationUpdated.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Domain.Events;

using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when an agent configuration is updated.
/// </summary>
public record AgentConfigurationUpdated : IDomainEvent
{
    /// <inheritdoc/>
    public string EventId { get; init; } = Guid.NewGuid().ToString();

    /// <inheritdoc/>
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

    /// <inheritdoc/>
    public string EventType { get; init; } = nameof(AgentConfigurationUpdated);

    /// <summary>
    /// Gets the unique identifier of the agent configuration.
    /// </summary>
    public required Guid AgentId { get; init; }

    /// <summary>
    /// Gets the updated name of the agent configuration.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the updated description of the agent configuration.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the updated YAML configuration for the agent.
    /// </summary>
    public required string ConfigurationYaml { get; init; }

    /// <summary>
    /// Gets the version of the configuration after the update.
    /// </summary>
    public required int Version { get; init; }

    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }
}
