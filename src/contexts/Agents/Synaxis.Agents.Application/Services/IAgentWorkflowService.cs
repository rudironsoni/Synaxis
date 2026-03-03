// <copyright file="IAgentWorkflowService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.Services;

using Synaxis.Agents.Application.DTOs;

/// <summary>
/// Service for managing agent workflow lifecycle.
/// </summary>
public interface IAgentWorkflowService
{
    /// <summary>
    /// Creates a new agent workflow.
    /// </summary>
    /// <param name="request">The create workflow request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created workflow DTO.</returns>
    Task<AgentWorkflowDto> CreateWorkflowAsync(
        CreateWorkflowRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a workflow.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated workflow DTO.</returns>
    Task<AgentWorkflowDto> ExecuteWorkflowAsync(
        Guid workflowId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a workflow by ID.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The workflow DTO if found; otherwise, null.</returns>
    Task<AgentWorkflowDto?> GetWorkflowAsync(
        Guid workflowId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets workflows with pagination and filtering.
    /// </summary>
    /// <param name="request">The get workflows request with pagination.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paginated result of workflow DTOs.</returns>
    Task<PaginatedResult<AgentWorkflowDto>> GetWorkflowsAsync(
        GetWorkflowsRequest request,
        CancellationToken cancellationToken = default);
}
