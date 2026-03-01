namespace Orchestration.Application.Interfaces;

using Orchestration.Application.DTOs;

/// <summary>
/// Service interface for managing outbox messages.
/// </summary>
public interface IOutboxService
{
    /// <summary>
    /// Gets a paginated list of outbox messages based on query criteria.
    /// </summary>
    /// <param name="request">The query request containing filter criteria.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated result of outbox message DTOs.</returns>
    Task<PagedResult<OutboxMessageDto>> GetMessagesAsync(OutboxQueryRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a paginated list of pending outbox messages.
    /// </summary>
    /// <param name="page">The page number to retrieve.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated result of pending outbox message DTOs.</returns>
    Task<PagedResult<OutboxMessageDto>> GetPendingMessagesAsync(int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a paginated list of failed outbox messages.
    /// </summary>
    /// <param name="page">The page number to retrieve.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated result of failed outbox message DTOs.</returns>
    Task<PagedResult<OutboxMessageDto>> GetFailedMessagesAsync(int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>
    /// Retries processing a failed outbox message.
    /// </summary>
    /// <param name="id">The unique identifier of the message to retry.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the message was successfully queued for retry; otherwise, false.</returns>
    Task<bool> RetryMessageAsync(Guid id, CancellationToken cancellationToken);
}
