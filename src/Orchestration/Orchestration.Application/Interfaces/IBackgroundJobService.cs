namespace Orchestration.Application.Interfaces;

using Orchestration.Application.DTOs;

/// <summary>
/// Service interface for managing background jobs.
/// </summary>
public interface IBackgroundJobService
{
    /// <summary>
    /// Gets a paginated list of background jobs based on query criteria.
    /// </summary>
    /// <param name="request">The query request containing filter criteria.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated result of background job DTOs.</returns>
    Task<PagedResult<BackgroundJobDto>> GetJobsAsync(JobQueryRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a specific background job by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the job.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The background job DTO, or null if not found.</returns>
    Task<BackgroundJobDto?> GetJobByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new background job.
    /// </summary>
    /// <param name="request">The creation request containing job details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created background job DTO.</returns>
    Task<BackgroundJobDto> CreateJobAsync(CreateJobRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to cancel a background job.
    /// </summary>
    /// <param name="id">The unique identifier of the job to cancel.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the job was successfully cancelled; otherwise, false.</returns>
    Task<bool> CancelJobAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the current status of a background job.
    /// </summary>
    /// <param name="id">The unique identifier of the job.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The job status DTO, or null if not found.</returns>
    Task<JobStatusDto?> GetJobStatusAsync(Guid id, CancellationToken cancellationToken);
}
