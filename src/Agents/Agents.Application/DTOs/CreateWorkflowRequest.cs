// <copyright file="CreateWorkflowRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.DTOs;

/// <summary>
/// Request to create a new agent workflow.
/// </summary>
public record CreateWorkflowRequest
{
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
}
