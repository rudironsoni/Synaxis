// <copyright file="ExecuteAgentCommandHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.Handlers;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using Synaxis.Agents.Application.Commands;
using Synaxis.Agents.Application.Interfaces;
using Synaxis.Agents.Domain.Aggregates;

/// <summary>
/// Handles the <see cref="ExecuteAgentCommand"/> to execute an agent.
/// </summary>
public sealed class ExecuteAgentCommandHandler : IRequestHandler<ExecuteAgentCommand, Guid>
{
    private readonly IAgentConfigurationRepository _configurationRepository;
    private readonly IAgentExecutionRepository _executionRepository;
    private readonly IAgentExecutionService _executionService;
    private readonly ILogger<ExecuteAgentCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecuteAgentCommandHandler"/> class.
    /// </summary>
    /// <param name="configurationRepository">The agent configuration repository.</param>
    /// <param name="executionRepository">The agent execution repository.</param>
    /// <param name="executionService">The agent execution service.</param>
    /// <param name="logger">The logger instance.</param>
    public ExecuteAgentCommandHandler(
        IAgentConfigurationRepository configurationRepository,
        IAgentExecutionRepository executionRepository,
        IAgentExecutionService executionService,
        ILogger<ExecuteAgentCommandHandler> logger)
    {
        this._configurationRepository = configurationRepository ?? throw new ArgumentNullException(nameof(configurationRepository));
        this._executionRepository = executionRepository ?? throw new ArgumentNullException(nameof(executionRepository));
        this._executionService = executionService ?? throw new ArgumentNullException(nameof(executionService));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async ValueTask<Guid> Handle(ExecuteAgentCommand request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        this._logger.LogInformation(
            "Starting execution for agent {AgentId}",
            request.TargetAgentId);

        var configuration = await this._configurationRepository
            .GetByIdAsync(request.TargetAgentId, cancellationToken)
            .ConfigureAwait(false);

        if (configuration is null)
        {
            this._logger.LogWarning(
                "Agent with ID {AgentId} not found",
                request.TargetAgentId);
            throw new InvalidOperationException($"Agent with ID '{request.TargetAgentId}' not found.");
        }

        if (configuration.Status != Domain.ValueObjects.AgentStatus.Active)
        {
            this._logger.LogWarning(
                "Agent {AgentId} is not active. Current status: {Status}",
                request.TargetAgentId,
                configuration.Status);
            throw new InvalidOperationException($"Agent with ID '{request.TargetAgentId}' is not active.");
        }

        var executionId = Guid.NewGuid();
        var executionIdString = executionId.ToString("N");

        var inputParameters = request.Input is not null
            ? new Dictionary<string, object>(request.Input, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        var execution = AgentExecution.Create(
            id: executionId,
            agentId: request.TargetAgentId,
            executionId: executionIdString,
            inputParameters: inputParameters);

        await this._executionRepository.SaveAsync(execution, cancellationToken).ConfigureAwait(false);

        this._logger.LogInformation(
            "Execution {ExecutionId} created for agent {AgentId}",
            executionId,
            request.TargetAgentId);

        _ = Task.Run(async () => await this.RunExecutionAsync(executionId, execution, configuration, cancellationToken).ConfigureAwait(false), cancellationToken);

        return executionId;
    }

    private async Task RunExecutionAsync(Guid executionId, AgentExecution execution, AgentConfiguration configuration, CancellationToken cancellationToken)
    {
        try
        {
            await this._executionService.StartExecutionAsync(
                execution,
                configuration,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this._logger.LogError(
                ex,
                "Execution {ExecutionId} failed",
                executionId);
        }
    }
}
