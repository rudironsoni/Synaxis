// <copyright file="AgentWorkflowService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.Services;

using Microsoft.Extensions.Logging;
using Synaxis.Agents.Application.DTOs;
using Synaxis.Agents.Application.Interfaces;
using Synaxis.Agents.Domain.Aggregates;
using Synaxis.Agents.Domain.ValueObjects;

/// <summary>
/// Implementation of agent workflow service.
/// </summary>
public class AgentWorkflowService : IAgentWorkflowService
{
    private readonly IAgentWorkflowRepository _workflowRepository;
    private readonly ILogger<AgentWorkflowService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentWorkflowService"/> class.
    /// </summary>
    /// <param name="workflowRepository">The agent workflow repository.</param>
    /// <param name="logger">The logger.</param>
    public AgentWorkflowService(
        IAgentWorkflowRepository workflowRepository,
        ILogger<AgentWorkflowService> logger)
    {
        this._workflowRepository = workflowRepository ?? throw new ArgumentNullException(nameof(workflowRepository));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<AgentWorkflowDto> CreateWorkflowAsync(
        CreateWorkflowRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var id = Guid.NewGuid();
        var workflow = AgentWorkflow.Create(
            id,
            request.Name,
            request.Description,
            request.WorkflowYaml,
            request.TenantId,
            request.TeamId);

        await this._workflowRepository.SaveAsync(workflow, cancellationToken).ConfigureAwait(false);

        this._logger.LogInformation(
            "Created workflow {WorkflowId} with name {WorkflowName} for tenant {TenantId}",
            id,
            request.Name,
            request.TenantId);

        return this.MapToDto(workflow);
    }

    /// <inheritdoc/>
    public async Task<AgentWorkflowDto> ExecuteWorkflowAsync(
        Guid workflowId,
        CancellationToken cancellationToken = default)
    {
        var workflow = await this._workflowRepository.GetByIdAsync(workflowId, cancellationToken).ConfigureAwait(false);
        if (workflow == null)
        {
            throw new InvalidOperationException($"Workflow with ID {workflowId} not found.");
        }

        // Note: Actual workflow execution would be handled by the infrastructure layer
        // This service method provides the application layer entry point
        this._logger.LogInformation("Initiating execution of workflow {WorkflowId}", workflowId);

        return this.MapToDto(workflow);
    }

    /// <inheritdoc/>
    public async Task<AgentWorkflowDto?> GetWorkflowAsync(
        Guid workflowId,
        CancellationToken cancellationToken = default)
    {
        var workflow = await this._workflowRepository.GetByIdAsync(workflowId, cancellationToken).ConfigureAwait(false);
        return workflow == null ? null : this.MapToDto(workflow);
    }

    /// <inheritdoc/>
    public async Task<PaginatedResult<AgentWorkflowDto>> GetWorkflowsAsync(
        GetWorkflowsRequest request,
        CancellationToken cancellationToken = default)
    {
        // Get all workflows by tenant (or empty list if tenant not specified)
        var tenantId = request.TenantId ?? string.Empty;
        var workflows = await this._workflowRepository.GetByTenantAsync(
            Guid.Parse(tenantId),
            cancellationToken).ConfigureAwait(false);

        // Apply status filter if specified
        if (request.Status.HasValue)
        {
            workflows = workflows.Where(w => w.Status == request.Status.Value).ToList();
        }

        // Apply search filter if specified
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLowerInvariant();
            workflows = workflows.Where(w =>
                w.Name.ToLowerInvariant().Contains(searchTerm) ||
                (w.Description?.ToLowerInvariant().Contains(searchTerm) ?? false)).ToList();
        }

        var totalCount = workflows.Count;
        var pageSize = Math.Max(1, request.PageSize);
        var page = Math.Max(1, request.Page);

        var pagedWorkflows = workflows
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(this.MapToDto)
            .ToList();

        return new PaginatedResult<AgentWorkflowDto>
        {
            Items = pagedWorkflows,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    private AgentWorkflowDto MapToDto(AgentWorkflow workflow)
    {
        return new AgentWorkflowDto
        {
            Id = Guid.Parse(workflow.Id),
            Name = workflow.Name,
            Description = workflow.Description,
            WorkflowYaml = workflow.WorkflowYaml,
            TenantId = workflow.TenantId,
            TeamId = workflow.TeamId,
            Status = workflow.Status,
            CurrentStep = workflow.CurrentStep,
            RetryCount = workflow.RetryCount,
            CompletedSteps = workflow.CompletedSteps,
            Version = workflow.Version,
        };
    }
}
