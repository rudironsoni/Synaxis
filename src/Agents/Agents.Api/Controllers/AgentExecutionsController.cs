// <copyright file="AgentExecutionsController.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Api.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Synaxis.Agents.Application.DTOs;
using IAgentExecutionService = Synaxis.Agents.Application.Services.IAgentExecutionService;

/// <summary>
/// Controller for managing agent executions.
/// </summary>
[ApiController]
[Authorize]
[Route("api/agents/executions")]
public class AgentExecutionsController : ControllerBase
{
    private readonly IAgentExecutionService _executionService;
    private readonly ILogger<AgentExecutionsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentExecutionsController"/> class.
    /// </summary>
    /// <param name="executionService">The agent execution service.</param>
    /// <param name="logger">The logger.</param>
    public AgentExecutionsController(
        IAgentExecutionService executionService,
        ILogger<AgentExecutionsController> logger)
    {
        ArgumentNullException.ThrowIfNull(executionService);
        ArgumentNullException.ThrowIfNull(logger);
        this._executionService = executionService;
        this._logger = logger;
    }

    /// <summary>
    /// Starts a new agent execution.
    /// </summary>
    /// <param name="request">The execute agent request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The execution details.</returns>
    [HttpPost]
    public async Task<ActionResult<AgentExecutionDto>> Execute(
        [FromBody] ExecuteAgentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            this._logger.LogInformation("Starting execution for agent: {AgentId}", request.AgentId);

            var result = await this._executionService.StartExecutionAsync(
                request.AgentId,
                request.InputParameters ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase),
                cancellationToken);

            return this.CreatedAtAction(
                nameof(this.GetById),
                new { id = result.Id },
                result);
        }
        catch (InvalidOperationException ex)
        {
            this._logger.LogWarning(ex, "Invalid operation while starting agent execution: {AgentId}", request.AgentId);
            return this.BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error starting agent execution: {AgentId}", request.AgentId);
            return this.StatusCode(500, new { message = "An error occurred while starting the execution" });
        }
    }

    /// <summary>
    /// Gets a specific agent execution by ID.
    /// </summary>
    /// <param name="id">The execution identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The execution details.</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AgentExecutionDto>> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            this._logger.LogInformation("Retrieving agent execution with ID: {Id}", id);

            var result = await this._executionService.GetExecutionAsync(id, cancellationToken);

            if (result == null)
            {
                return this.NotFound(new { message = $"Execution with ID {id} not found" });
            }

            return this.Ok(result);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error retrieving agent execution with ID: {Id}", id);
            return this.StatusCode(500, new { message = "An error occurred while retrieving the execution" });
        }
    }

    /// <summary>
    /// Cancels a running agent execution.
    /// </summary>
    /// <param name="id">The execution identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            this._logger.LogInformation("Cancelling agent execution with ID: {Id}", id);

            await this._executionService.CancelExecutionAsync(id, cancellationToken);

            return this.NoContent();
        }
        catch (InvalidOperationException ex)
        {
            this._logger.LogWarning(ex, "Invalid operation while cancelling agent execution: {Id}", id);
            return this.BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error cancelling agent execution: {Id}", id);
            return this.StatusCode(500, new { message = "An error occurred while cancelling the execution" });
        }
    }
}
