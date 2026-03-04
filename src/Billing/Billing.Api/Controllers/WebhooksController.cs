// <copyright file="WebhooksController.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Billing.Api.Controllers;

using Billing.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Controller for handling webhooks from payment providers.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public sealed class WebhooksController : ControllerBase
{
    private readonly IPaymentGateway _paymentGateway;
    private readonly ILogger<WebhooksController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhooksController"/> class.
    /// </summary>
    /// <param name="paymentGateway">The payment gateway.</param>
    /// <param name="logger">The logger.</param>
    public WebhooksController(IPaymentGateway paymentGateway, ILogger<WebhooksController> logger)
    {
        _paymentGateway = paymentGateway ?? throw new ArgumentNullException(nameof(paymentGateway));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles Stripe webhook events.
    /// </summary>
    /// <param name="payload">The webhook payload.</param>
    /// <param name="signature">The webhook signature for validation.</param>
    /// <returns>No content on success, bad request on validation failure.</returns>
    [HttpPost("stripe")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> HandleStripeWebhook(
        [FromBody] object payload,
        [FromQuery] string? signature = null)
    {
        if (string.IsNullOrWhiteSpace(signature))
        {
            _logger.LogWarning("Stripe webhook received without signature");
            return BadRequest(new { error = "Missing webhook signature" });
        }

        var payloadJson = payload?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            _logger.LogWarning("Stripe webhook received with empty payload");
            return BadRequest(new { error = "Empty webhook payload" });
        }

        var isValid = await _paymentGateway.ValidateWebhookAsync(payloadJson, signature);
        if (!isValid)
        {
            _logger.LogWarning("Stripe webhook validation failed");
            return BadRequest(new { error = "Invalid webhook signature" });
        }

        _logger.LogInformation("Stripe webhook validated successfully");

        // Process the webhook event
        // In a real implementation, this would deserialize and handle various event types
        return Ok();
    }
}
