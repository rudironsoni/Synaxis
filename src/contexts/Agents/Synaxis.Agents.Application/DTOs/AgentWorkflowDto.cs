// <copyright file="AgentWorkflowDto.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.DTOs;

using Synaxis.Agents.Domain.ValueObjects;

/// <summary>
/// Data transfer object for an agent workflow.
/// </summary>
public record AgentWorkflowDto
{
    /// <summary>
    /// Gets unique identifier of the workflow.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets name of the workflow.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets description of the workflow.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets yAML configuration for the workflow.
    /// </summary>
    public required string WorkflowYaml { get; init; }

    /// <summary>
    /// Gets tenant identifier.
    /// </summary>
    public required string TenantId { get; init; }

    /// <summary>
    /// Gets team identifier.
    /// </summary>
    public string? TeamId { get; init; }

    /// <summary>
    /// Gets current status of the workflow.
    /// </summary>
    public required AgentStatus Status { get; init; }

    /// <summary>
    /// Gets current step number.
    /// </summary>
    public required int CurrentStep { get; init; }

    /// <summary>
    /// Gets number of retry attempts.
    /// </summary>
    public required int RetryCount { get; init; }

    /// <summary>
    /// Gets list of completed step numbers.
    /// </summary>
    public required IReadOnlyList<int> CompletedSteps { get; init; }

    /// <summary>
    /// Gets version number of the workflow.
    /// </summary>
    public required int Version { get; init; }
}
