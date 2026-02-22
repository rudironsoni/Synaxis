// <copyright file="CreateAgentCommandHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.Handlers;

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using Synaxis.Agents.Application.Commands;
using Synaxis.Agents.Application.DTOs;
using Synaxis.Agents.Application.Interfaces;
using Synaxis.Agents.Domain.Aggregates;

/// <summary>
/// Handles the <see cref="CreateAgentCommand"/> to create a new agent configuration.
/// </summary>
public sealed class CreateAgentCommandHandler : IRequestHandler<CreateAgentCommand, AgentDto>
{
    private readonly IAgentConfigurationRepository _repository;
    private readonly ILogger<CreateAgentCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateAgentCommandHandler"/> class.
    /// </summary>
    /// <param name="repository">The agent configuration repository.</param>
    /// <param name="logger">The logger instance.</param>
    public CreateAgentCommandHandler(
        IAgentConfigurationRepository repository,
        ILogger<CreateAgentCommandHandler> logger)
    {
        this._repository = repository!;
        this._logger = logger!;
    }

    /// <inheritdoc/>
    public async ValueTask<AgentDto> Handle(CreateAgentCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        this._logger.LogInformation(
            "Creating agent '{AgentName}' of type '{AgentType}'",
            request.Name,
            request.AgentType);

        var agentId = Guid.NewGuid();
        var tenantId = request.TenantId is not null
            ? Guid.Parse(request.TenantId)
            : Guid.Empty;

        var configurationYaml = request.Configuration is not null
            ? JsonSerializer.Serialize(request.Configuration)
            : string.Empty;

        var agent = AgentConfiguration.Create(
            id: agentId,
            name: request.Name,
            description: request.Description,
            agentType: request.AgentType,
            configurationYaml: configurationYaml,
            tenantId: tenantId,
            teamId: null,
            userId: Guid.TryParse(request.UserId, out var userId) ? userId : null);

        await this._repository.SaveAsync(agent, cancellationToken).ConfigureAwait(false);

        this._logger.LogInformation(
            "Agent '{AgentName}' created successfully with ID {AgentId}",
            request.Name,
            agentId);

        return MapToDto(agent);
    }

    private static AgentDto MapToDto(AgentConfiguration agent)
    {
        IDictionary<string, object>? configuration = null;
        if (!string.IsNullOrEmpty(agent.ConfigurationYaml))
        {
            try
            {
                configuration = JsonSerializer.Deserialize<Dictionary<string, object>>(agent.ConfigurationYaml);
            }
            catch (JsonException)
            {
                configuration = null;
            }
        }

        return new AgentDto
        {
            Id = agent.Id,
            Name = agent.Name,
            Description = agent.Description,
            AgentType = agent.AgentType,
            Status = agent.Status,
            Configuration = configuration,
            CreatedByUserId = agent.UserId?.ToString() ?? string.Empty,
            CreatedAt = new DateTimeOffset(agent.CreatedAt.Ticks, TimeSpan.Zero),
            UpdatedAt = agent.UpdatedAt == default ? null : new DateTimeOffset(agent.UpdatedAt.Ticks, TimeSpan.Zero),
        };
    }
}
