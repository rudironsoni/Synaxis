using Microsoft.AspNetCore.Mvc;
using Orchestration.Application.DTOs;
using Orchestration.Application.Interfaces;

namespace Orchestration.Api.Controllers;

/// <summary>
/// API controller for managing outbox messages.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class OutboxController : ControllerBase
{
    private readonly IOutboxService _outboxService;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxController"/> class.
    /// </summary>
    /// <param name="outboxService">The outbox service.</param>
    public OutboxController(IOutboxService outboxService)
    {
        _outboxService = outboxService ?? throw new ArgumentNullException(nameof(outboxService));
    }

    /// <summary>
    /// Gets a paginated list of outbox messages.
    /// </summary>
    /// <param name="request">The query request containing filter criteria.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of outbox messages.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OutboxMessageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMessages(
        [FromQuery] OutboxQueryRequest request,
        CancellationToken cancellationToken)
    {
        var messages = await _outboxService.GetMessagesAsync(request, cancellationToken);
        return Ok(messages);
    }

    /// <summary>
    /// Gets pending outbox messages.
    /// </summary>
    /// <param name="page">The page number to retrieve.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of pending outbox messages.</returns>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(PagedResult<OutboxMessageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingMessages(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var messages = await _outboxService.GetPendingMessagesAsync(page, pageSize, cancellationToken);
        return Ok(messages);
    }

    /// <summary>
    /// Gets failed outbox messages.
    /// </summary>
    /// <param name="page">The page number to retrieve.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of failed outbox messages.</returns>
    [HttpGet("failed")]
    [ProducesResponseType(typeof(PagedResult<OutboxMessageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFailedMessages(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var messages = await _outboxService.GetFailedMessagesAsync(page, pageSize, cancellationToken);
        return Ok(messages);
    }

    /// <summary>
    /// Retries a failed outbox message.
    /// </summary>
    /// <param name="id">The unique identifier of the message to retry.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful, not found if the message doesn't exist.</returns>
    [HttpPost("{id:guid}/retry")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RetryMessage(Guid id, CancellationToken cancellationToken)
    {
        var result = await _outboxService.RetryMessageAsync(id, cancellationToken);
        if (!result)
            return NotFound();
        return NoContent();
    }
}
