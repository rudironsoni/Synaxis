// <copyright file="ExecuteAgentRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.DTOs;

/// <summary>
/// Request to execute an agent.
/// </summary>
public record ExecuteAgentRequest
{
    /// <summary>
    /// Gets the agent identifier.
    /// </summary>
    public required Guid AgentId { get; init; }

    /// <summary>
    /// Gets the input parameters for the execution.
    /// </summary>
    public IDictionary<string, object>? InputParameters { get; init; }
}
