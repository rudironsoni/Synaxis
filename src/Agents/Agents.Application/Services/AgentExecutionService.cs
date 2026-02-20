// <copyright file="AgentExecutionService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.Services;

using Microsoft.Extensions.Logging;
using Synaxis.Agents.Application.DTOs;
using Synaxis.Agents.Application.Interfaces;
using Synaxis.Agents.Domain.Aggregates;
using Synaxis.Agents.Domain.ValueObjects;

/// <summary>
/// Implementation of agent execution service at the application layer.
/// </summary>
public class AgentExecutionService : IAgentExecutionService
{
    private readonly IAgentExecutionRepository _executionRepository;
    private readonly IAgentConfigurationRepository _configurationRepository;
    private readonly ILogger<AgentExecutionService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentExecutionService"/> class.
    /// </summary>
    /// <param name="executionRepository">The agent execution repository.</param>
    /// <param name="configurationRepository">The agent configuration repository.</param>
    /// <param name="logger">The logger.</param>
    public AgentExecutionService(
        IAgentExecutionRepository executionRepository,
        IAgentConfigurationRepository configurationRepository,
        ILogger<AgentExecutionService> logger)
    {
        this._executionRepository = executionRepository ?? throw new ArgumentNullException(nameof(executionRepository));
        this._configurationRepository = configurationRepository ?? throw new ArgumentNullException(nameof(configurationRepository));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<AgentExecutionDto> StartExecutionAsync(
        Guid agentId,
        IDictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        // Verify the agent exists and is active
        var agent = await this._configurationRepository.GetByIdAsync(agentId, cancellationToken).ConfigureAwait(false);
        if (agent == null)
        {
            throw new InvalidOperationException($"Agent with ID {agentId} not found.");
        }

        if (agent.Status != AgentStatus.Active)
        {
            throw new InvalidOperationException($"Agent {agentId} is not active.");
        }

        var id = Guid.NewGuid();
        var executionId = Guid.NewGuid().ToString("N");

        var execution = AgentExecution.Create(
            id,
            agentId,
            executionId,
            parameters as IReadOnlyDictionary<string, object> ?? new Dictionary<string, object>(parameters, StringComparer.Ordinal));

        await this._executionRepository.SaveAsync(execution, cancellationToken).ConfigureAwait(false);

        this._logger.LogInformation(
            "Started execution {ExecutionId} for agent {AgentId}",
            executionId,
            agentId);

        return this.MapToDto(execution);
    }

    /// <inheritdoc/>
    public async Task<AgentExecutionDto> CancelExecutionAsync(
        Guid executionId,
        CancellationToken cancellationToken = default)
    {
        var execution = await this._executionRepository.GetByIdAsync(executionId, cancellationToken).ConfigureAwait(false);
        if (execution == null)
        {
            throw new InvalidOperationException($"Execution with ID {executionId} not found.");
        }

        execution.Cancel();

        await this._executionRepository.SaveAsync(execution, cancellationToken).ConfigureAwait(false);

        this._logger.LogInformation("Cancelled execution {ExecutionId}", executionId);

        return this.MapToDto(execution);
    }

    /// <inheritdoc/>
    public async Task<AgentExecutionDto?> GetExecutionAsync(
        Guid executionId,
        CancellationToken cancellationToken = default)
    {
        var execution = await this._executionRepository.GetByIdAsync(executionId, cancellationToken).ConfigureAwait(false);
        return execution == null ? null : this.MapToDto(execution);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AgentExecutionDto>> GetExecutionsAsync(
        Guid agentId,
        CancellationToken cancellationToken = default)
    {
        var executions = await this._executionRepository.GetByAgentIdAsync(agentId, cancellationToken).ConfigureAwait(false);
        return executions.Select(this.MapToDto).ToList();
    }

    private AgentExecutionDto MapToDto(AgentExecution execution)
    {
        return new AgentExecutionDto
        {
            Id = Guid.Parse(execution.Id),
            AgentId = execution.AgentId,
            ExecutionId = execution.ExecutionId,
            Status = execution.Status,
            InputParameters = execution.InputParameters,
            CurrentStep = execution.CurrentStep,
            StartedAt = execution.StartedAt,
            CompletedAt = execution.CompletedAt,
            Error = execution.Error,
            DurationMs = execution.DurationMs,
            Steps = execution.Steps.Select(s => new ExecutionStepDto
            {
                StepNumber = s.StepNumber,
                Name = s.Name,
                Status = s.Status,
                StartedAt = s.StartedAt,
                CompletedAt = s.CompletedAt,
            }).ToList(),
        };
    }
}
