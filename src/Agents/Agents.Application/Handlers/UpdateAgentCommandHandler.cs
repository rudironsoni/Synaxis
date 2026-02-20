// <copyright file="UpdateAgentCommandHandler.cs" company="Synaxis">
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
/// Handles the <see cref="UpdateAgentCommand"/> to update an existing agent configuration.
/// </summary>
public sealed class UpdateAgentCommandHandler : IRequestHandler<UpdateAgentCommand, AgentDto>
{
    private readonly IAgentConfigurationRepository _repository;
    private readonly ILogger<UpdateAgentCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateAgentCommandHandler"/> class.
    /// </summary>
    /// <param name="repository">The agent configuration repository.</param>
    /// <param name="logger">The logger instance.</param>
    public UpdateAgentCommandHandler(
        IAgentConfigurationRepository repository,
        ILogger<UpdateAgentCommandHandler> logger)
    {
        this._repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async ValueTask<AgentDto> Handle(UpdateAgentCommand request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        this._logger.LogInformation(
            "Updating agent with ID {AgentId}",
            request.TargetAgentId);

        var agent = await this._repository.GetByIdAsync(request.TargetAgentId, cancellationToken).ConfigureAwait(false);
        if (agent is null)
        {
            this._logger.LogWarning(
                "Agent with ID {AgentId} not found",
                request.TargetAgentId);
            throw new InvalidOperationException($"Agent with ID '{request.TargetAgentId}' not found.");
        }

        var name = request.Name ?? agent.Name;
        var description = request.Description ?? agent.Description;

        var configurationYaml = agent.ConfigurationYaml;
        if (request.Configuration is not null)
        {
            configurationYaml = JsonSerializer.Serialize(request.Configuration);
        }

        agent.Update(name, description, configurationYaml);

        if (!string.IsNullOrEmpty(request.Status))
        {
            _ = Enum.TryParse<Domain.ValueObjects.AgentStatus>(
                request.Status,
                ignoreCase: true,
                out var newStatus);

            if (newStatus == Domain.ValueObjects.AgentStatus.Active && agent.Status != Domain.ValueObjects.AgentStatus.Active)
            {
                agent.Activate();
            }
            else if (newStatus == Domain.ValueObjects.AgentStatus.Inactive && agent.Status != Domain.ValueObjects.AgentStatus.Inactive)
            {
                agent.Deactivate();
            }
        }

        await this._repository.SaveAsync(agent, cancellationToken).ConfigureAwait(false);

        this._logger.LogInformation(
            "Agent with ID {AgentId} updated successfully",
            request.TargetAgentId);

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
