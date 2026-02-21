// <copyright file="AgentConfigurationsController.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Synaxis.Agents.Application.DTOs;
using Synaxis.Agents.Application.Services;

/// <summary>
/// Controller for managing agent configurations.
/// </summary>
[ApiController]
[Route("api/agents/configurations")]
public class AgentConfigurationsController : ControllerBase
{
    private readonly IAgentConfigurationService _configurationService;
    private readonly ILogger<AgentConfigurationsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentConfigurationsController"/> class.
    /// </summary>
    /// <param name="configurationService">The agent configuration service.</param>
    /// <param name="logger">The logger.</param>
    public AgentConfigurationsController(
        IAgentConfigurationService configurationService,
        ILogger<AgentConfigurationsController> logger)
    {
        this._configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all agent configurations with pagination.
    /// </summary>
    /// <param name="request">The pagination and filter request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paginated list of agent configurations.</returns>
    [HttpGet]
    public async Task<ActionResult<PaginatedResult<AgentDto>>> GetAll(
        [FromQuery] GetAgentsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            this._logger.LogInformation("Retrieving agent configurations with pagination");

            var result = await this._configurationService.GetAgentsAsync(request, cancellationToken);

            return this.Ok(result);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error retrieving agent configurations");
            return this.StatusCode(500, new { message = "An error occurred while retrieving agents" });
        }
    }

    /// <summary>
    /// Gets a specific agent configuration by ID.
    /// </summary>
    /// <param name="id">The agent configuration identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The agent configuration.</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AgentDto>> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            this._logger.LogInformation("Retrieving agent configuration with ID: {Id}", id);

            var result = await this._configurationService.GetAgentAsync(id, cancellationToken);

            if (result == null)
            {
                return this.NotFound(new { message = $"Agent configuration with ID {id} not found" });
            }

            return this.Ok(result);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error retrieving agent configuration with ID: {Id}", id);
            return this.StatusCode(500, new { message = "An error occurred while retrieving the agent" });
        }
    }

    /// <summary>
    /// Creates a new agent configuration.
    /// </summary>
    /// <param name="request">The create agent configuration request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created agent configuration.</returns>
    [HttpPost]
    public async Task<ActionResult<AgentDto>> Create(
        [FromBody] CreateAgentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            this._logger.LogInformation("Creating new agent configuration: {Name}", request.Name);

            var result = await this._configurationService.CreateAgentAsync(request, cancellationToken);

            return this.CreatedAtAction(
                nameof(this.GetById),
                new { id = result.Id },
                result);
        }
        catch (InvalidOperationException ex)
        {
            this._logger.LogWarning(ex, "Invalid operation while creating agent configuration: {Name}", request.Name);
            return this.BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error creating agent configuration: {Name}", request.Name);
            return this.StatusCode(500, new { message = "An error occurred while creating the agent" });
        }
    }

    /// <summary>
    /// Updates an existing agent configuration.
    /// </summary>
    /// <param name="id">The agent configuration identifier.</param>
    /// <param name="request">The update agent configuration request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated agent configuration.</returns>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AgentDto>> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateAgentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            this._logger.LogInformation("Updating agent configuration with ID: {Id}", id);

            var result = await this._configurationService.UpdateAgentAsync(id, request, cancellationToken);

            return this.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            this._logger.LogWarning(ex, "Invalid operation while updating agent configuration: {Id}", id);
            return this.BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error updating agent configuration: {Id}", id);
            return this.StatusCode(500, new { message = "An error occurred while updating the agent" });
        }
    }

    /// <summary>
    /// Deletes an agent configuration.
    /// </summary>
    /// <param name="id">The agent configuration identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            this._logger.LogInformation("Deleting agent configuration with ID: {Id}", id);

            await this._configurationService.DeleteAgentAsync(id, cancellationToken);

            return this.NoContent();
        }
        catch (InvalidOperationException ex)
        {
            this._logger.LogWarning(ex, "Invalid operation while deleting agent configuration: {Id}", id);
            return this.BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error deleting agent configuration: {Id}", id);
            return this.StatusCode(500, new { message = "An error occurred while deleting the agent" });
        }
    }
}
