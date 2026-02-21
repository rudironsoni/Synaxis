// <copyright file="WorkflowsController.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Synaxis.Agents.Application.DTOs;
using Synaxis.Agents.Application.Services;

/// <summary>
/// Controller for managing agent workflows.
/// </summary>
[ApiController]
[Route("api/workflows")]
public class WorkflowsController : ControllerBase
{
    private readonly IAgentWorkflowService _workflowService;
    private readonly ILogger<WorkflowsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowsController"/> class.
    /// </summary>
    /// <param name="workflowService">The agent workflow service.</param>
    /// <param name="logger">The logger.</param>
    public WorkflowsController(
        IAgentWorkflowService workflowService,
        ILogger<WorkflowsController> logger)
    {
        this._workflowService = workflowService ?? throw new ArgumentNullException(nameof(workflowService));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all workflows with pagination.
    /// </summary>
    /// <param name="request">The pagination request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paginated list of workflows.</returns>
    [HttpGet]
    public async Task<ActionResult<PaginatedResult<AgentWorkflowDto>>> GetAll(
        [FromQuery] GetWorkflowsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            this._logger.LogInformation("Retrieving workflows with pagination");

            var result = await this._workflowService.GetWorkflowsAsync(request, cancellationToken);

            return this.Ok(result);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error retrieving workflows");
            return this.StatusCode(500, new { message = "An error occurred while retrieving workflows" });
        }
    }

    /// <summary>
    /// Creates a new workflow.
    /// </summary>
    /// <param name="request">The create workflow request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created workflow.</returns>
    [HttpPost]
    public async Task<ActionResult<AgentWorkflowDto>> Create(
        [FromBody] CreateWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            this._logger.LogInformation("Creating new workflow: {Name}", request.Name);

            var result = await this._workflowService.CreateWorkflowAsync(request, cancellationToken);

            return this.CreatedAtAction(
                nameof(this.GetById),
                new { id = result.Id },
                result);
        }
        catch (InvalidOperationException ex)
        {
            this._logger.LogWarning(ex, "Invalid operation while creating workflow: {Name}", request.Name);
            return this.BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error creating workflow: {Name}", request.Name);
            return this.StatusCode(500, new { message = "An error occurred while creating the workflow" });
        }
    }

    /// <summary>
    /// Gets a specific workflow by ID.
    /// </summary>
    /// <param name="id">The workflow identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The workflow details.</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AgentWorkflowDto>> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            this._logger.LogInformation("Retrieving workflow with ID: {Id}", id);

            var result = await this._workflowService.GetWorkflowAsync(id, cancellationToken);

            if (result == null)
            {
                return this.NotFound(new { message = $"Workflow with ID {id} not found" });
            }

            return this.Ok(result);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error retrieving workflow with ID: {Id}", id);
            return this.StatusCode(500, new { message = "An error occurred while retrieving the workflow" });
        }
    }

    /// <summary>
    /// Executes a workflow.
    /// </summary>
    /// <param name="id">The workflow identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The workflow details.</returns>
    [HttpPost("{id:guid}/execute")]
    public async Task<ActionResult<AgentWorkflowDto>> Execute(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            this._logger.LogInformation("Executing workflow with ID: {Id}", id);

            var result = await this._workflowService.ExecuteWorkflowAsync(id, cancellationToken);

            return this.AcceptedAtAction(
                nameof(AgentExecutionsController.GetById),
                "AgentExecutions",
                new { id = result.Id },
                result);
        }
        catch (InvalidOperationException ex)
        {
            this._logger.LogWarning(ex, "Invalid operation while executing workflow: {Id}", id);
            return this.BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error executing workflow: {Id}", id);
            return this.StatusCode(500, new { message = "An error occurred while executing the workflow" });
        }
    }
}
