namespace Orchestration.Application.Interfaces;

using Orchestration.Application.DTOs;

/// <summary>
/// Service interface for managing webhooks.
/// </summary>
public interface IWebhookService
{
    /// <summary>
    /// Gets a paginated list of webhooks based on query criteria.
    /// </summary>
    /// <param name="request">The query request containing filter criteria.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated result of webhook DTOs.</returns>
    Task<PagedResult<WebhookDto>> GetWebhooksAsync(WebhookQueryRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a specific webhook by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the webhook.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The webhook DTO, or null if not found.</returns>
    Task<WebhookDto?> GetWebhookByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new webhook.
    /// </summary>
    /// <param name="request">The creation request containing webhook details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created webhook DTO.</returns>
    Task<WebhookDto> CreateWebhookAsync(CreateWebhookRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing webhook.
    /// </summary>
    /// <param name="id">The unique identifier of the webhook to update.</param>
    /// <param name="request">The update request containing new webhook details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the webhook was successfully updated; otherwise, false.</returns>
    Task<bool> UpdateWebhookAsync(Guid id, UpdateWebhookRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a webhook.
    /// </summary>
    /// <param name="id">The unique identifier of the webhook to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the webhook was successfully deleted; otherwise, false.</returns>
    Task<bool> DeleteWebhookAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Tests a webhook by sending a test payload.
    /// </summary>
    /// <param name="id">The unique identifier of the webhook to test.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The test result DTO, or null if the webhook was not found.</returns>
    Task<WebhookTestResultDto?> TestWebhookAsync(Guid id, CancellationToken cancellationToken);
}
