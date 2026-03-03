// <copyright file="CreateAgentRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.DTOs;

/// <summary>
/// Request to create a new agent configuration.
/// </summary>
public record CreateAgentRequest
{
    /// <summary>
    /// Gets name of the agent.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets description of the agent.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets type of the agent.
    /// </summary>
    public required string AgentType { get; init; }

    /// <summary>
    /// Gets configuration YAML for the agent.
    /// </summary>
    public required string ConfigurationYaml { get; init; }

    /// <summary>
    /// Gets tenant identifier.
    /// </summary>
    public required Guid TenantId { get; init; }

    /// <summary>
    /// Gets team identifier.
    /// </summary>
    public Guid? TeamId { get; init; }

    /// <summary>
    /// Gets user identifier who created the agent.
    /// </summary>
    public Guid? UserId { get; init; }
}
