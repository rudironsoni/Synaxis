// <copyright file="GetAgentWorkflowsQueryHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.Handlers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Synaxis.Agents.Application.Interfaces;
using Synaxis.Agents.Application.Queries;
using Synaxis.Contracts.V2.Common;
using Synaxis.Contracts.V2.DTOs;
using DomainAgentStatus = Synaxis.Agents.Domain.ValueObjects.AgentStatus;
using WorkflowStatus = Synaxis.Contracts.V2.Common.WorkflowStatus;

/// <summary>
/// Handler for <see cref="GetAgentWorkflowsQuery"/>.
/// </summary>
public class GetAgentWorkflowsQueryHandler : IRequestHandler<GetAgentWorkflowsQuery, PaginatedResult<WorkflowDto>>
{
    private readonly IAgentWorkflowRepository _workflowRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetAgentWorkflowsQueryHandler"/> class.
    /// </summary>
    /// <param name="workflowRepository">The agent workflow repository.</param>
    public GetAgentWorkflowsQueryHandler(IAgentWorkflowRepository workflowRepository)
    {
        this._workflowRepository = workflowRepository;
    }

    /// <summary>
    /// Handles the query to get workflows.
    /// </summary>
    /// <param name="request">The query request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paginated result of workflow DTOs.</returns>
    public async ValueTask<PaginatedResult<WorkflowDto>> Handle(GetAgentWorkflowsQuery request, CancellationToken cancellationToken)
    {
        IEnumerable<Domain.Aggregates.AgentWorkflow> workflows;

        if (!string.IsNullOrWhiteSpace(request.TenantId) && Guid.TryParse(request.TenantId, out var tenantId))
        {
            workflows = await this._workflowRepository.GetByTenantAsync(tenantId, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            workflows = Array.Empty<Domain.Aggregates.AgentWorkflow>();
        }

        var filteredWorkflows = ApplyFilters(workflows, request);
        var pagedWorkflows = ApplyPagination(filteredWorkflows, request);

        return new PaginatedResult<WorkflowDto>
        {
            Items = pagedWorkflows.Items.Select(MapToDto).ToList(),
            Page = pagedWorkflows.Page,
            PageSize = pagedWorkflows.PageSize,
            TotalCount = pagedWorkflows.TotalCount,
        };
    }

    private static IEnumerable<Domain.Aggregates.AgentWorkflow> ApplyFilters(
        IEnumerable<Domain.Aggregates.AgentWorkflow> workflows,
        GetAgentWorkflowsQuery request)
    {
        var filteredWorkflows = workflows.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<DomainAgentStatus>(request.Status, true, out var status))
        {
            filteredWorkflows = filteredWorkflows.Where(w => w.Status == status);
        }

        return ApplySorting(filteredWorkflows, request);
    }

    private static IEnumerable<Domain.Aggregates.AgentWorkflow> ApplySorting(
        IEnumerable<Domain.Aggregates.AgentWorkflow> workflows,
        GetAgentWorkflowsQuery request)
    {
        return request.SortBy?.ToLowerInvariant() switch
        {
            "name" => request.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase)
                ? workflows.OrderByDescending(w => w.Name, StringComparer.Ordinal)
                : workflows.OrderBy(w => w.Name, StringComparer.Ordinal),
            "status" => request.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase)
                ? workflows.OrderByDescending(w => w.Status)
                : workflows.OrderBy(w => w.Status),
            _ => workflows,
        };
    }

    private static (IReadOnlyList<Domain.Aggregates.AgentWorkflow> Items, int Page, int PageSize, int TotalCount) ApplyPagination(
        IEnumerable<Domain.Aggregates.AgentWorkflow> workflows,
        GetAgentWorkflowsQuery request)
    {
        var totalCount = workflows.Count();
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var pagedWorkflows = workflows
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return (pagedWorkflows, page, pageSize, totalCount);
    }

    private static WorkflowDto MapToDto(Domain.Aggregates.AgentWorkflow workflow)
    {
        return new WorkflowDto
        {
            Id = Guid.Parse(workflow.Id),
            Name = workflow.Name,
            Description = workflow.Description,
            Status = MapStatus(workflow.Status),
            Steps = Array.Empty<WorkflowStepDto>(),
            Variables = null,
            Schedule = null,
            CreatedByUserId = "system",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = null,
            StartedAt = null,
            CompletedAt = null,
            ExecutionCount = 0,
        };
    }

    private static WorkflowStatus MapStatus(DomainAgentStatus status)
    {
        return status switch
        {
            DomainAgentStatus.Idle => WorkflowStatus.Pending,
            DomainAgentStatus.Running => WorkflowStatus.InProgress,
            DomainAgentStatus.Paused => WorkflowStatus.Paused,
            DomainAgentStatus.Completed => WorkflowStatus.Completed,
            DomainAgentStatus.Failed => WorkflowStatus.Failed,
            DomainAgentStatus.Active => WorkflowStatus.InProgress,
            DomainAgentStatus.Inactive => WorkflowStatus.Cancelled,
            _ => WorkflowStatus.Pending,
        };
    }
}
