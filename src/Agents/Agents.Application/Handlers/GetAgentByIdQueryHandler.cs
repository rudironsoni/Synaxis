// <copyright file="GetAgentByIdQueryHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.Handlers;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Synaxis.Agents.Application.Interfaces;
using Synaxis.Agents.Application.Queries;
using Synaxis.Contracts.V2.DTOs;
using DomainAgentStatus = Synaxis.Agents.Domain.ValueObjects.AgentStatus;

/// <summary>
/// Handler for <see cref="GetAgentByIdQuery"/>.
/// </summary>
public class GetAgentByIdQueryHandler : IRequestHandler<GetAgentByIdQuery, AgentDto?>
{
    private readonly IAgentConfigurationRepository _agentRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetAgentByIdQueryHandler"/> class.
    /// </summary>
    /// <param name="agentRepository">The agent configuration repository.</param>
    public GetAgentByIdQueryHandler(IAgentConfigurationRepository agentRepository)
    {
        this._agentRepository = agentRepository;
    }

    /// <summary>
    /// Handles the query to get an agent by its identifier.
    /// </summary>
    /// <param name="request">The query request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The agent DTO if found; otherwise, <see langword="null"/>.</returns>
    public async ValueTask<AgentDto?> Handle(GetAgentByIdQuery request, CancellationToken cancellationToken)
    {
        var agent = await this._agentRepository.GetByIdAsync(request.TargetAgentId, cancellationToken).ConfigureAwait(false);

        if (agent == null)
        {
            return null;
        }

        return MapToDto(agent, request);
    }

    private static AgentDto MapToDto(Domain.Aggregates.AgentConfiguration agent, GetAgentByIdQuery request)
    {
        var dto = new AgentDto
        {
            Id = agent.Id,
            Name = agent.Name,
            Description = agent.Description,
            AgentType = agent.AgentType,
            Status = MapStatus(agent.Status),
            Configuration = request.IncludeConfiguration ? ParseConfiguration() : null,
            Labels = null,
            CreatedByUserId = agent.UserId?.ToString() ?? string.Empty,
            CreatedAt = new DateTimeOffset(agent.CreatedAt.Ticks, TimeSpan.Zero),
            UpdatedAt = agent.UpdatedAt == default ? null : new DateTimeOffset(agent.UpdatedAt.Ticks, TimeSpan.Zero),
        };

        return dto;
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

    private static Dictionary<string, object>? ParseConfiguration()
    {
        return null;
    }
}
