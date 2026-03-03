// <copyright file="GetAgentExecutionByIdQueryHandler.cs" company="Synaxis">
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
/// Handler for <see cref="GetAgentExecutionByIdQuery"/>.
/// </summary>
public class GetAgentExecutionByIdQueryHandler : IRequestHandler<GetAgentExecutionByIdQuery, ExecutionDto?>
{
    private readonly IAgentExecutionRepository _executionRepository;
    private readonly IAgentConfigurationRepository _agentRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetAgentExecutionByIdQueryHandler"/> class.
    /// </summary>
    /// <param name="executionRepository">The agent execution repository.</param>
    /// <param name="agentRepository">The agent configuration repository.</param>
    public GetAgentExecutionByIdQueryHandler(
        IAgentExecutionRepository executionRepository,
        IAgentConfigurationRepository agentRepository)
    {
        this._executionRepository = executionRepository;
        this._agentRepository = agentRepository;
    }

    /// <summary>
    /// Handles the query to get an execution by its identifier.
    /// </summary>
    /// <param name="request">The query request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The execution DTO if found; otherwise, <see langword="null"/>.</returns>
    public async ValueTask<ExecutionDto?> Handle(GetAgentExecutionByIdQuery request, CancellationToken cancellationToken)
    {
        var execution = await this._executionRepository.GetByIdAsync(request.ExecutionId, cancellationToken).ConfigureAwait(false);

        if (execution == null)
        {
            return null;
        }

        var agent = await this._agentRepository.GetByIdAsync(execution.AgentId, cancellationToken).ConfigureAwait(false);
        var agentName = agent?.Name ?? "Unknown";

        return MapToDto(execution, agentName);
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
