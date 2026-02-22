// <copyright file="CancelAgentExecutionCommandHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.Handlers;

using System;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using Synaxis.Agents.Application.Commands;
using Synaxis.Agents.Application.Interfaces;
using Synaxis.Agents.Domain.ValueObjects;

/// <summary>
/// Handles the <see cref="CancelAgentExecutionCommand"/> to cancel an agent execution.
/// </summary>
public sealed class CancelAgentExecutionCommandHandler : IRequestHandler<CancelAgentExecutionCommand, Unit>
{
    private readonly IAgentExecutionRepository _executionRepository;
    private readonly IAgentExecutionService _executionService;
    private readonly ILogger<CancelAgentExecutionCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CancelAgentExecutionCommandHandler"/> class.
    /// </summary>
    /// <param name="executionRepository">The agent execution repository.</param>
    /// <param name="executionService">The agent execution service.</param>
    /// <param name="logger">The logger instance.</param>
    public CancelAgentExecutionCommandHandler(
        IAgentExecutionRepository executionRepository,
        IAgentExecutionService executionService,
        ILogger<CancelAgentExecutionCommandHandler> logger)
    {
        this._executionRepository = executionRepository!;
        this._executionService = executionService!;
        this._logger = logger!;
    }

    /// <inheritdoc/>
    public async ValueTask<Unit> Handle(CancelAgentExecutionCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        this._logger.LogInformation(
            "Cancelling execution {ExecutionId}. Force: {Force}, WaitForGracefulShutdown: {WaitForGracefulShutdown}",
            request.ExecutionId,
            request.Force,
            request.WaitForGracefulShutdown);

        var execution = await this._executionRepository
            .GetByIdAsync(request.ExecutionId, cancellationToken)
            .ConfigureAwait(false);

        if (execution is null)
        {
            this._logger.LogWarning(
                "Execution {ExecutionId} not found",
                request.ExecutionId);
            throw new InvalidOperationException($"Execution with ID '{request.ExecutionId}' not found.");
        }

        if (execution.Status != AgentStatus.Running && execution.Status != AgentStatus.Paused)
        {
            this._logger.LogWarning(
                "Execution {ExecutionId} cannot be cancelled from status {Status}",
                request.ExecutionId,
                execution.Status);
            throw new InvalidOperationException(
                $"Execution with ID '{request.ExecutionId}' cannot be cancelled from status '{execution.Status}'.");
        }

        if (request.Force)
        {
            await this._executionService
                .CancelExecutionAsync(execution.ExecutionId, cancellationToken)
                .ConfigureAwait(false);
        }

        execution.Cancel();

        await this._executionRepository.SaveAsync(execution, cancellationToken).ConfigureAwait(false);

        this._logger.LogInformation(
            "Execution {ExecutionId} cancelled successfully",
            request.ExecutionId);

        return Unit.Value;
    }
}
