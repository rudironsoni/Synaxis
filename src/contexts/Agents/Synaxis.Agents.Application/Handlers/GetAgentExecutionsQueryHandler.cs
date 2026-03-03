// <copyright file="GetAgentExecutionsQueryHandler.cs" company="Synaxis">
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
using ExecutionStatus = Synaxis.Contracts.V2.Common.ExecutionStatus;

/// <summary>
/// Handler for <see cref="GetAgentExecutionsQuery"/>.
/// </summary>
public class GetAgentExecutionsQueryHandler : IRequestHandler<GetAgentExecutionsQuery, PaginatedResult<ExecutionDto>>
{
    private readonly IAgentExecutionRepository _executionRepository;
    private readonly IAgentConfigurationRepository _agentRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetAgentExecutionsQueryHandler"/> class.
    /// </summary>
    /// <param name="executionRepository">The agent execution repository.</param>
    /// <param name="agentRepository">The agent configuration repository.</param>
    public GetAgentExecutionsQueryHandler(
        IAgentExecutionRepository executionRepository,
        IAgentConfigurationRepository agentRepository)
    {
        this._executionRepository = executionRepository;
        this._agentRepository = agentRepository;
    }

    /// <summary>
    /// Handles the query to get executions for an agent.
    /// </summary>
    /// <param name="request">The query request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paginated result of execution DTOs.</returns>
    public async ValueTask<PaginatedResult<ExecutionDto>> Handle(GetAgentExecutionsQuery request, CancellationToken cancellationToken)
    {
        var agent = await this._agentRepository.GetByIdAsync(request.AgentId, cancellationToken).ConfigureAwait(false);
        var agentName = agent?.Name ?? "Unknown";

        var executions = await this._executionRepository.GetByAgentIdAsync(request.AgentId, cancellationToken).ConfigureAwait(false);
        var filteredExecutions = ApplyFilters(executions, request);
        var pagedExecutions = ApplyPagination(filteredExecutions, request);

        return new PaginatedResult<ExecutionDto>
        {
            Items = pagedExecutions.Items.Select(e => MapToDto(e, agentName)).ToList(),
            Page = pagedExecutions.Page,
            PageSize = pagedExecutions.PageSize,
            TotalCount = pagedExecutions.TotalCount,
        };
    }

    private static IEnumerable<Domain.Aggregates.AgentExecution> ApplyFilters(
        IReadOnlyList<Domain.Aggregates.AgentExecution> executions,
        GetAgentExecutionsQuery request)
    {
        var filteredExecutions = executions.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<DomainAgentStatus>(request.Status, true, out var status))
        {
            filteredExecutions = filteredExecutions.Where(e => e.Status == status);
        }

        return ApplySorting(filteredExecutions, request);
    }

    private static IEnumerable<Domain.Aggregates.AgentExecution> ApplySorting(
        IEnumerable<Domain.Aggregates.AgentExecution> executions,
        GetAgentExecutionsQuery request)
    {
        return request.SortBy?.ToLowerInvariant() switch
        {
            "status" => request.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase)
                ? executions.OrderByDescending(e => e.Status)
                : executions.OrderBy(e => e.Status),
            "duration" => request.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase)
                ? executions.OrderByDescending(e => e.DurationMs)
                : executions.OrderBy(e => e.DurationMs),
            _ => request.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase)
                ? executions.OrderByDescending(e => e.StartedAt)
                : executions.OrderBy(e => e.StartedAt),
        };
    }

    private static (IReadOnlyList<Domain.Aggregates.AgentExecution> Items, int Page, int PageSize, int TotalCount) ApplyPagination(
        IEnumerable<Domain.Aggregates.AgentExecution> executions,
        GetAgentExecutionsQuery request)
    {
        var totalCount = executions.Count();
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var pagedExecutions = executions
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return (pagedExecutions, page, pageSize, totalCount);
    }

    private static ExecutionDto MapToDto(Domain.Aggregates.AgentExecution execution, string agentName)
    {
        return new ExecutionDto
        {
            Id = Guid.Parse(execution.Id),
            AgentId = execution.AgentId,
            AgentName = agentName,
            ParentExecutionId = null,
            WorkflowId = null,
            InitiatedByUserId = "system",
            Status = MapStatus(execution.Status),
            ExecutionContext = null,
            Input = execution.InputParameters.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value,
                StringComparer.Ordinal),
            Output = null,
            Error = execution.Error != null
                ? new ExecutionError
                {
                    Code = "EXECUTION_FAILED",
                    Message = execution.Error,
                    Retryable = false,
                }
                : null,
            StartedAt = execution.StartedAt.HasValue
                ? new DateTimeOffset(execution.StartedAt.Value.Ticks, TimeSpan.Zero)
                : DateTimeOffset.UtcNow,
            CompletedAt = execution.CompletedAt.HasValue
                ? new DateTimeOffset(execution.CompletedAt.Value.Ticks, TimeSpan.Zero)
                : null,
            Duration = execution.DurationMs.HasValue
                ? TimeSpan.FromMilliseconds(execution.DurationMs.Value)
                : null,
            Priority = 0,
            Metrics = null,
            Stages = MapStages(execution.Steps),
        };
    }

    private static ExecutionStatus MapStatus(DomainAgentStatus status)
    {
        return status switch
        {
            DomainAgentStatus.Idle => ExecutionStatus.Pending,
            DomainAgentStatus.Running => ExecutionStatus.Running,
            DomainAgentStatus.Paused => ExecutionStatus.Paused,
            DomainAgentStatus.Completed => ExecutionStatus.Completed,
            DomainAgentStatus.Failed => ExecutionStatus.Failed,
            DomainAgentStatus.Active => ExecutionStatus.Running,
            DomainAgentStatus.Inactive => ExecutionStatus.Cancelled,
            _ => ExecutionStatus.Pending,
        };
    }

    private static IReadOnlyList<ExecutionStage>? MapStages(IReadOnlyList<Domain.ValueObjects.ExecutionStep> steps)
    {
        if (steps.Count == 0)
        {
            return null;
        }

        return steps.Select(s => new ExecutionStage
        {
            Name = s.Name,
            Status = MapStatus(s.Status),
            Duration = s.CompletedAt.HasValue && s.StartedAt.HasValue
                ? s.CompletedAt.Value - s.StartedAt.Value
                : TimeSpan.Zero,
        }).ToList();
    }
}
