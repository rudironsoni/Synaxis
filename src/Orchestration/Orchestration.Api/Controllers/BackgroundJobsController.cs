using Microsoft.AspNetCore.Mvc;
using Orchestration.Application.DTOs;
using Orchestration.Application.Interfaces;

namespace Orchestration.Api.Controllers;

/// <summary>
/// API controller for managing background jobs.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class BackgroundJobsController : ControllerBase
{
    private readonly IBackgroundJobService _jobService;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundJobsController"/> class.
    /// </summary>
    /// <param name="jobService">The background job service.</param>
    public BackgroundJobsController(IBackgroundJobService jobService)
    {
        _jobService = jobService ?? throw new ArgumentNullException(nameof(jobService));
    }

    /// <summary>
    /// Gets a paginated list of background jobs.
    /// </summary>
    /// <param name="request">The query request containing filter criteria.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of background jobs.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<BackgroundJobDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetJobs(
        [FromQuery] JobQueryRequest request,
        CancellationToken cancellationToken)
    {
        var jobs = await _jobService.GetJobsAsync(request, cancellationToken);
        return Ok(jobs);
    }

    /// <summary>
    /// Gets a specific background job by ID.
    /// </summary>
    /// <param name="id">The unique identifier of the job.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The background job details.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BackgroundJobDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetJob(Guid id, CancellationToken cancellationToken)
    {
        var job = await _jobService.GetJobByIdAsync(id, cancellationToken);
        if (job == null)
            return NotFound();
        return Ok(job);
    }

    /// <summary>
    /// Creates a new background job.
    /// </summary>
    /// <param name="request">The creation request containing job details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created job with its location.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(BackgroundJobDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateJob(
        [FromBody] CreateJobRequest request,
        CancellationToken cancellationToken)
    {
        var job = await _jobService.CreateJobAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetJob), new { id = job.Id }, job);
    }

    /// <summary>
    /// Cancels a background job.
    /// </summary>
    /// <param name="id">The unique identifier of the job to cancel.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful, not found if the job doesn't exist.</returns>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelJob(Guid id, CancellationToken cancellationToken)
    {
        var result = await _jobService.CancelJobAsync(id, cancellationToken);
        if (!result)
            return NotFound();
        return NoContent();
    }

    /// <summary>
    /// Gets the current status of a background job.
    /// </summary>
    /// <param name="id">The unique identifier of the job.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The job status details.</returns>
    [HttpGet("{id:guid}/status")]
    [ProducesResponseType(typeof(JobStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetJobStatus(Guid id, CancellationToken cancellationToken)
    {
        var status = await _jobService.GetJobStatusAsync(id, cancellationToken);
        if (status == null)
            return NotFound();
        return Ok(status);
    }
}
