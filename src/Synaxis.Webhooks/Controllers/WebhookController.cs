// <copyright file="WebhookController.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Webhooks.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Synaxis.Webhooks.Data;
    using Synaxis.Webhooks.Models;

    /// <summary>
    /// Controller for webhook management endpoints.
    /// </summary>
    [ApiController]
    [Route("api/v1/webhooks")]
    [Authorize]
    public class WebhookController : ControllerBase
    {
        private readonly WebhooksDbContext _dbContext;
        private readonly ILogger<WebhookController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebhookController"/> class.
        /// </summary>
        /// <param name="dbContext">The database context.</param>
        /// <param name="logger">The logger.</param>
        public WebhookController(WebhooksDbContext dbContext, ILogger<WebhookController> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Registers a new webhook.
        /// </summary>
        /// <param name="request">The webhook registration request.</param>
        /// <returns>The created webhook.</returns>
        [HttpPost]
        public async Task<ActionResult<WebhookDto>> CreateWebhook([FromBody] CreateWebhookRequest request)
        {
            try
            {
                var organizationId = GetCurrentOrganizationId();

                _logger.LogInformation(
                    "Creating webhook for organization {OrganizationId} with URL {Url}",
                    organizationId,
                    request.Url);

                var webhook = new Webhook
                {
                    Id = Guid.NewGuid(),
                    Url = request.Url,
                    Secret = GenerateSecret(),
                    Events = request.Events,
                    IsActive = true,
                    OrganizationId = organizationId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };

                _dbContext.Webhooks.Add(webhook);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Webhook {WebhookId} created successfully", webhook.Id);

                return CreatedAtAction(
                    nameof(GetWebhook),
                    new { id = webhook.Id },
                    MapToDto(webhook, includeSecret: true));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating webhook");
                return StatusCode(500, new { message = "An error occurred while creating the webhook" });
            }
        }

        /// <summary>
        /// Lists all webhooks for the current organization.
        /// </summary>
        /// <returns>A list of webhooks.</returns>
        [HttpGet]
        public async Task<ActionResult<List<WebhookDto>>> ListWebhooks()
        {
            try
            {
                var organizationId = GetCurrentOrganizationId();

                var webhooks = await _dbContext.Webhooks
                    .Where(w => w.OrganizationId == organizationId)
                    .OrderByDescending(w => w.CreatedAt)
                    .ToListAsync();

                return Ok(webhooks.Select(w => MapToDto(w, includeSecret: false)).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing webhooks");
                return StatusCode(500, new { message = "An error occurred while listing webhooks" });
            }
        }

        /// <summary>
        /// Gets a specific webhook by ID.
        /// </summary>
        /// <param name="id">The webhook ID.</param>
        /// <returns>The webhook.</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<WebhookDto>> GetWebhook(Guid id)
        {
            try
            {
                var organizationId = GetCurrentOrganizationId();

                var webhook = await _dbContext.Webhooks
                    .FirstOrDefaultAsync(w => w.Id == id && w.OrganizationId == organizationId);

                if (webhook == null)
                {
                    return NotFound(new { message = "Webhook not found" });
                }

                return Ok(MapToDto(webhook, includeSecret: false));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting webhook {WebhookId}", id);
                return StatusCode(500, new { message = "An error occurred while getting the webhook" });
            }
        }

        /// <summary>
        /// Updates an existing webhook.
        /// </summary>
        /// <param name="id">The webhook ID.</param>
        /// <param name="request">The webhook update request.</param>
        /// <returns>The updated webhook.</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult<WebhookDto>> UpdateWebhook(Guid id, [FromBody] UpdateWebhookRequest request)
        {
            try
            {
                var organizationId = GetCurrentOrganizationId();

                var webhook = await _dbContext.Webhooks
                    .FirstOrDefaultAsync(w => w.Id == id && w.OrganizationId == organizationId);

                if (webhook == null)
                {
                    return NotFound(new { message = "Webhook not found" });
                }

                if (!string.IsNullOrEmpty(request.Url))
                {
                    webhook.Url = request.Url;
                }

                if (request.Events.Count > 0)
                {
                    webhook.Events = request.Events;
                }

                webhook.IsActive = request.IsActive;

                webhook.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Webhook {WebhookId} updated successfully", id);

                return Ok(MapToDto(webhook, includeSecret: false));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating webhook {WebhookId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the webhook" });
            }
        }

        /// <summary>
        /// Deletes a webhook.
        /// </summary>
        /// <param name="id">The webhook ID.</param>
        /// <returns>204 No Content on success.</returns>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteWebhook(Guid id)
        {
            try
            {
                var organizationId = GetCurrentOrganizationId();

                var webhook = await _dbContext.Webhooks
                    .FirstOrDefaultAsync(w => w.Id == id && w.OrganizationId == organizationId);

                if (webhook == null)
                {
                    return NotFound(new { message = "Webhook not found" });
                }

                _dbContext.Webhooks.Remove(webhook);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Webhook {WebhookId} deleted successfully", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting webhook {WebhookId}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the webhook" });
            }
        }

        /// <summary>
        /// Regenerates the secret for a webhook.
        /// </summary>
        /// <param name="id">The webhook ID.</param>
        /// <returns>The webhook with the new secret.</returns>
        [HttpPost("{id}/regenerate-secret")]
        public async Task<ActionResult<WebhookDto>> RegenerateSecret(Guid id)
        {
            try
            {
                var organizationId = GetCurrentOrganizationId();

                var webhook = await _dbContext.Webhooks
                    .FirstOrDefaultAsync(w => w.Id == id && w.OrganizationId == organizationId);

                if (webhook == null)
                {
                    return NotFound(new { message = "Webhook not found" });
                }

                webhook.Secret = GenerateSecret();
                webhook.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Secret regenerated for webhook {WebhookId}", id);

                return Ok(MapToDto(webhook, includeSecret: true));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error regenerating secret for webhook {WebhookId}", id);
                return StatusCode(500, new { message = "An error occurred while regenerating the secret" });
            }
        }

        /// <summary>
        /// Gets the delivery logs for a webhook.
        /// </summary>
        /// <param name="id">The webhook ID.</param>
        /// <param name="limit">The maximum number of logs to return.</param>
        /// <returns>A list of delivery logs.</returns>
        [HttpGet("{id}/logs")]
        public async Task<ActionResult<List<WebhookDeliveryLogDto>>> GetDeliveryLogs(Guid id, [FromQuery] int limit = 50)
        {
            try
            {
                var organizationId = GetCurrentOrganizationId();

                var webhook = await _dbContext.Webhooks
                    .FirstOrDefaultAsync(w => w.Id == id && w.OrganizationId == organizationId);

                if (webhook == null)
                {
                    return NotFound(new { message = "Webhook not found" });
                }

                var logs = await _dbContext.WebhookDeliveryLogs
                    .Where(l => l.WebhookId == id)
                    .OrderByDescending(l => l.DeliveredAt)
                    .Take(limit)
                    .ToListAsync();

                return Ok(logs.Select(MapToLogDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting delivery logs for webhook {WebhookId}", id);
                return StatusCode(500, new { message = "An error occurred while getting delivery logs" });
            }
        }

        /// <summary>
        /// Reactivates a webhook that was moved to the dead letter queue.
        /// </summary>
        /// <param name="id">The webhook ID.</param>
        /// <returns>204 No Content on success.</returns>
        [HttpPost("{id}/reactivate")]
        public async Task<ActionResult> ReactivateWebhook(Guid id)
        {
            try
            {
                var organizationId = GetCurrentOrganizationId();

                var webhook = await _dbContext.Webhooks
                    .FirstOrDefaultAsync(w => w.Id == id && w.OrganizationId == organizationId);

                if (webhook == null)
                {
                    return NotFound(new { message = "Webhook not found" });
                }

                webhook.IsActive = true;
                webhook.FailedDeliveryAttempts = 0;
                webhook.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Webhook {WebhookId} reactivated", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reactivating webhook {WebhookId}", id);
                return StatusCode(500, new { message = "An error occurred while reactivating the webhook" });
            }
        }

        private Guid GetCurrentOrganizationId()
        {
            var organizationIdClaim = User.FindFirst("organization_id");
            if (organizationIdClaim == null || !Guid.TryParse(organizationIdClaim.Value, out var organizationId))
            {
                throw new UnauthorizedAccessException("Invalid organization");
            }

            return organizationId;
        }

        private static string GenerateSecret()
        {
            var bytes = new byte[32];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        private static WebhookDto MapToDto(Webhook webhook, bool includeSecret)
        {
            return new WebhookDto
            {
                Id = webhook.Id,
                Url = webhook.Url,
                Secret = includeSecret ? webhook.Secret : null,
                Events = webhook.Events,
                IsActive = webhook.IsActive,
                OrganizationId = webhook.OrganizationId,
                CreatedAt = webhook.CreatedAt,
                UpdatedAt = webhook.UpdatedAt,
                LastSuccessfulDeliveryAt = webhook.LastSuccessfulDeliveryAt,
                FailedDeliveryAttempts = webhook.FailedDeliveryAttempts,
            };
        }

        private static WebhookDeliveryLogDto MapToLogDto(WebhookDeliveryLog log)
        {
            return new WebhookDeliveryLogDto
            {
                Id = log.Id,
                WebhookId = log.WebhookId,
                EventType = log.EventType,
                Payload = log.Payload,
                StatusCode = log.StatusCode,
                ResponseBody = log.ResponseBody,
                ErrorMessage = log.ErrorMessage,
                RetryAttempt = log.RetryAttempt,
                IsSuccess = log.IsSuccess,
                DeliveredAt = log.DeliveredAt,
                DurationMs = log.DurationMs,
            };
        }
    }

    /// <summary>
    /// DTO for creating a webhook.
    /// </summary>
    public class CreateWebhookRequest
    {
        /// <summary>
        /// Gets or sets the URL where webhook events will be sent.
        /// </summary>
        [Required]
        [Url]
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of events this webhook subscribes to.
        /// </summary>
        [Required]
        public List<string> Events { get; set; } = new List<string>();
    }

    /// <summary>
    /// DTO for updating a webhook.
    /// </summary>
    public class UpdateWebhookRequest
    {
        /// <summary>
        /// Gets or sets the URL where webhook events will be sent.
        /// </summary>
        [Url]
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of events this webhook subscribes to.
        /// </summary>
        public List<string> Events { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets a value indicating whether the webhook is active.
        /// </summary>
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// DTO for a webhook.
    /// </summary>
    public class WebhookDto
    {
        /// <summary>
        /// Gets or sets the unique identifier for the webhook.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the URL where webhook events will be sent.
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the secret key used for HMAC-SHA256 signature verification.
        /// </summary>
        public string Secret { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of events this webhook subscribes to.
        /// </summary>
        public List<string> Events { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets a value indicating whether the webhook is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the organization ID that owns this webhook.
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the webhook was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the webhook was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the webhook was last successfully delivered.
        /// </summary>
        public DateTime LastSuccessfulDeliveryAt { get; set; }

        /// <summary>
        /// Gets or sets the number of consecutive failed delivery attempts.
        /// </summary>
        public int FailedDeliveryAttempts { get; set; }
    }

    /// <summary>
    /// DTO for a webhook delivery log.
    /// </summary>
    public class WebhookDeliveryLogDto
    {
        /// <summary>
        /// Gets or sets the unique identifier for the delivery log.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the webhook ID this delivery log belongs to.
        /// </summary>
        public Guid WebhookId { get; set; }

        /// <summary>
        /// Gets or sets the event type that was delivered.
        /// </summary>
        public string EventType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the payload that was sent.
        /// </summary>
        public string Payload { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the HTTP status code of the delivery response.
        /// </summary>
        public int? StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the response body from the webhook endpoint.
        /// </summary>
        public string ResponseBody { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the error message if the delivery failed.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the retry attempt number (0 for first attempt).
        /// </summary>
        public int RetryAttempt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the delivery was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the delivery was attempted.
        /// </summary>
        public DateTime DeliveredAt { get; set; }

        /// <summary>
        /// Gets or sets the duration of the delivery attempt in milliseconds.
        /// </summary>
        public long DurationMs { get; set; }
    }
}
