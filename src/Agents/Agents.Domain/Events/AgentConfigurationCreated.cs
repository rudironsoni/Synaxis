// <copyright file="AgentConfigurationCreated.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Domain.Events;

using System;
using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when a new agent configuration is created.
/// </summary>
public record AgentConfigurationCreated : IDomainEvent
{
    /// <inheritdoc/>
    public string EventId { get; init; } = Guid.NewGuid().ToString();

    /// <inheritdoc/>
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

    /// <inheritdoc/>
    public string EventType => nameof(AgentConfigurationCreated);

    /// <summary>
    /// Gets the agent identifier.
    /// </summary>
    public Guid AgentId { get; init; }

    /// <summary>
    /// Gets the name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the agent type.
    /// </summary>
    public required string AgentType { get; init; }

    /// <summary>
    /// Gets the configuration YAML.
    /// </summary>
    public required string ConfigurationYaml { get; init; }

    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    public Guid TenantId { get; init; }

    /// <summary>
    /// Gets the team identifier.
    /// </summary>
    public Guid? TeamId { get; init; }

    /// <summary>
    /// Gets the user identifier.
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    /// Gets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Gets the version.
    /// </summary>
    public int Version { get; init; }
}
