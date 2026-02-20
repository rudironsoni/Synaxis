// <copyright file="UpdateAgentRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.DTOs;

/// <summary>
/// Request to update an existing agent configuration.
/// </summary>
public record UpdateAgentRequest
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
    /// Gets configuration YAML for the agent.
    /// </summary>
    public required string ConfigurationYaml { get; init; }
}
