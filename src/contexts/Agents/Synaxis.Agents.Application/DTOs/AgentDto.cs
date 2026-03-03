// <copyright file="AgentDto.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.DTOs;

using Synaxis.Agents.Domain.ValueObjects;

/// <summary>
/// Data transfer object for an agent in the Application layer.
/// </summary>
public record AgentDto
{
    /// <summary>
    /// Gets unique identifier of the agent.
    /// </summary>
    public required Guid Id { get; init; }

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
    /// Gets current status of the agent.
    /// </summary>
    public required AgentStatus Status { get; init; }

    /// <summary>
    /// Gets configuration settings for the agent.
    /// </summary>
    public IDictionary<string, object>? Configuration { get; init; }

    /// <summary>
    /// Gets resource requirements for the agent.
    /// </summary>
    public ResourceRequirements? Resources { get; init; }

    /// <summary>
    /// Gets labels for the agent.
    /// </summary>
    public IDictionary<string, string>? Labels { get; init; }

    /// <summary>
    /// Gets identifier of the user who created the agent.
    /// </summary>
    public required string CreatedByUserId { get; init; }

    /// <summary>
    /// Gets timestamp when the agent was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets timestamp when the agent was last updated.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; init; }

    /// <summary>
    /// Gets execution statistics.
    /// </summary>
    public ExecutionStats? Stats { get; init; }
}
