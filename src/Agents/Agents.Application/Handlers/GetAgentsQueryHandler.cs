// <copyright file="GetAgentsQueryHandler.cs" company="Synaxis">
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
using Synaxis.Contracts.V2.DTOs;
using DomainAgentStatus = Synaxis.Agents.Domain.ValueObjects.AgentStatus;

/// <summary>
/// Handler for <see cref="GetAgentsQuery"/>.
/// </summary>
public class GetAgentsQueryHandler : IRequestHandler<GetAgentsQuery, PaginatedResult<AgentDto>>
{
    private readonly IAgentConfigurationRepository _agentRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetAgentsQueryHandler"/> class.
    /// </summary>
    /// <param name="agentRepository">The agent configuration repository.</param>
    public GetAgentsQueryHandler(IAgentConfigurationRepository agentRepository)
    {
        this._agentRepository = agentRepository;
    }

    /// <summary>
    /// Handles the query to get a paginated list of agents.
    /// </summary>
    /// <param name="request">The query request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paginated result of agent DTOs.</returns>
    public async ValueTask<PaginatedResult<AgentDto>> Handle(GetAgentsQuery request, CancellationToken cancellationToken)
    {
        var agents = await this._agentRepository.GetByStatusAsync(DomainAgentStatus.Active, cancellationToken).ConfigureAwait(false);
        var filteredAgents = ApplyFilters(agents, request);
        var pagedAgents = ApplyPagination(filteredAgents, request);

        return new PaginatedResult<AgentDto>
        {
            Items = pagedAgents.Items.Select(MapToDto).ToList(),
            Page = pagedAgents.Page,
            PageSize = pagedAgents.PageSize,
            TotalCount = pagedAgents.TotalCount,
            Cursor = request.Cursor,
        };
    }

    private static IEnumerable<Domain.Aggregates.AgentConfiguration> ApplyFilters(
        IReadOnlyList<Domain.Aggregates.AgentConfiguration> agents,
        GetAgentsQuery request)
    {
        var filteredAgents = agents.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(request.AgentType))
        {
            filteredAgents = filteredAgents.Where(a =>
                a.AgentType.Equals(request.AgentType, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<DomainAgentStatus>(request.Status, true, out var status))
        {
            filteredAgents = filteredAgents.Where(a => a.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLowerInvariant();
            filteredAgents = filteredAgents.Where(a =>
                a.Name.ToLowerInvariant().Contains(search) ||
                (a.Description != null && a.Description.ToLowerInvariant().Contains(search)));
        }

        return ApplySorting(filteredAgents, request);
    }

    private static IEnumerable<Domain.Aggregates.AgentConfiguration> ApplySorting(
        IEnumerable<Domain.Aggregates.AgentConfiguration> agents,
        GetAgentsQuery request)
    {
        return request.SortBy?.ToLowerInvariant() switch
        {
            "name" => request.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase)
                ? agents.OrderByDescending(a => a.Name, StringComparer.Ordinal)
                : agents.OrderBy(a => a.Name, StringComparer.Ordinal),
            "updatedat" => request.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase)
                ? agents.OrderByDescending(a => a.UpdatedAt)
                : agents.OrderBy(a => a.UpdatedAt),
            _ => request.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase)
                ? agents.OrderByDescending(a => a.CreatedAt)
                : agents.OrderBy(a => a.CreatedAt),
        };
    }

    private static (IReadOnlyList<Domain.Aggregates.AgentConfiguration> Items, int Page, int PageSize, int TotalCount) ApplyPagination(
        IEnumerable<Domain.Aggregates.AgentConfiguration> agents,
        GetAgentsQuery request)
    {
        var totalCount = agents.Count();
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var pagedAgents = agents
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return (pagedAgents, page, pageSize, totalCount);
    }

    private static AgentDto MapToDto(Domain.Aggregates.AgentConfiguration agent)
    {
        return new AgentDto
        {
            Id = agent.Id,
            Name = agent.Name,
            Description = agent.Description,
            AgentType = agent.AgentType,
            Status = MapStatus(agent.Status),
            Configuration = null,
            Labels = null,
            CreatedByUserId = agent.UserId?.ToString() ?? string.Empty,
            CreatedAt = new DateTimeOffset(agent.CreatedAt.Ticks, TimeSpan.Zero),
            UpdatedAt = agent.UpdatedAt == default ? null : new DateTimeOffset(agent.UpdatedAt.Ticks, TimeSpan.Zero),
        };
    }

    private static Contracts.V2.Common.AgentStatus MapStatus(DomainAgentStatus status)
    {
        return status switch
        {
            DomainAgentStatus.Idle => Contracts.V2.Common.AgentStatus.Idle,
            DomainAgentStatus.Running => Contracts.V2.Common.AgentStatus.Running,
            DomainAgentStatus.Paused => Contracts.V2.Common.AgentStatus.Processing,
            DomainAgentStatus.Completed => Contracts.V2.Common.AgentStatus.Idle,
            DomainAgentStatus.Failed => Contracts.V2.Common.AgentStatus.Error,
            DomainAgentStatus.Active => Contracts.V2.Common.AgentStatus.Idle,
            DomainAgentStatus.Inactive => Contracts.V2.Common.AgentStatus.Disabled,
            _ => Contracts.V2.Common.AgentStatus.Provisioning,
        };
    }
}
