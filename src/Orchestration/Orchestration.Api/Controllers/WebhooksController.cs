using Microsoft.AspNetCore.Mvc;
using Orchestration.Application.DTOs;
using Orchestration.Application.Interfaces;

namespace Orchestration.Api.Controllers;

/// <summary>
/// API controller for managing webhooks.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class WebhooksController : ControllerBase
{
    private readonly IWebhookService _webhookService;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhooksController"/> class.
    /// </summary>
    /// <param name="webhookService">The webhook service.</param>
    public WebhooksController(IWebhookService webhookService)
    {
        _webhookService = webhookService ?? throw new ArgumentNullException(nameof(webhookService));
    }

    /// <summary>
    /// Gets a paginated list of webhooks.
    /// </summary>
    /// <param name="request">The query request containing filter criteria.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of webhooks.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<WebhookDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWebhooks(
        [FromQuery] WebhookQueryRequest request,
        CancellationToken cancellationToken)
    {
        var webhooks = await _webhookService.GetWebhooksAsync(request, cancellationToken);
        return Ok(webhooks);
    }

    /// <summary>
    /// Gets a specific webhook by ID.
    /// </summary>
    /// <param name="id">The unique identifier of the webhook.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The webhook details.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WebhookDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWebhook(Guid id, CancellationToken cancellationToken)
    {
        var webhook = await _webhookService.GetWebhookByIdAsync(id, cancellationToken);
        if (webhook == null)
            return NotFound();
        return Ok(webhook);
    }

    /// <summary>
    /// Creates a new webhook.
    /// </summary>
    /// <param name="request">The creation request containing webhook details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created webhook with its location.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(WebhookDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateWebhook(
        [FromBody] CreateWebhookRequest request,
        CancellationToken cancellationToken)
    {
        var webhook = await _webhookService.CreateWebhookAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetWebhook), new { id = webhook.Id }, webhook);
    }

    /// <summary>
    /// Updates an existing webhook.
    /// </summary>
    /// <param name="id">The unique identifier of the webhook to update.</param>
    /// <param name="request">The update request containing new webhook details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateWebhook(
        Guid id,
        [FromBody] UpdateWebhookRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _webhookService.UpdateWebhookAsync(id, request, cancellationToken);
        if (!result)
            return NotFound();
        return NoContent();
    }

    /// <summary>
    /// Deletes a webhook.
    /// </summary>
    /// <param name="id">The unique identifier of the webhook to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteWebhook(Guid id, CancellationToken cancellationToken)
    {
        var result = await _webhookService.DeleteWebhookAsync(id, cancellationToken);
        if (!result)
            return NotFound();
        return NoContent();
    }

    /// <summary>
    /// Tests a webhook by sending a test payload.
    /// </summary>
    /// <param name="id">The unique identifier of the webhook to test.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The test result.</returns>
    [HttpPost("{id:guid}/test")]
    [ProducesResponseType(typeof(WebhookTestResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TestWebhook(Guid id, CancellationToken cancellationToken)
    {
        var result = await _webhookService.TestWebhookAsync(id, cancellationToken);
        if (result == null)
            return NotFound();
        return Ok(result);
    }
}
