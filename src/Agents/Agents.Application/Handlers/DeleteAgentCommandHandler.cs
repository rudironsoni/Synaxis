// <copyright file="DeleteAgentCommandHandler.cs" company="Synaxis">
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

/// <summary>
/// Handles the <see cref="DeleteAgentCommand"/> to delete an agent configuration.
/// </summary>
public sealed class DeleteAgentCommandHandler : IRequestHandler<DeleteAgentCommand, Unit>
{
    private readonly IAgentConfigurationRepository _repository;
    private readonly ILogger<DeleteAgentCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteAgentCommandHandler"/> class.
    /// </summary>
    /// <param name="repository">The agent configuration repository.</param>
    /// <param name="logger">The logger instance.</param>
    public DeleteAgentCommandHandler(
        IAgentConfigurationRepository repository,
        ILogger<DeleteAgentCommandHandler> logger)
    {
        this._repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async ValueTask<Unit> Handle(DeleteAgentCommand request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        this._logger.LogInformation(
            "Deleting agent with ID {AgentId}. Force: {Force}, Cascade: {Cascade}",
            request.TargetAgentId,
            request.Force,
            request.Cascade);

        var agent = await this._repository.GetByIdAsync(request.TargetAgentId, cancellationToken).ConfigureAwait(false);
        if (agent is null)
        {
            this._logger.LogWarning(
                "Agent with ID {AgentId} not found",
                request.TargetAgentId);
            throw new InvalidOperationException($"Agent with ID '{request.TargetAgentId}' not found.");
        }

        await this._repository.DeleteAsync(request.TargetAgentId, cancellationToken).ConfigureAwait(false);

        this._logger.LogInformation(
            "Agent with ID {AgentId} deleted successfully",
            request.TargetAgentId);

        return Unit.Value;
    }
}
