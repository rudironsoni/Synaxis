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
            this._dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                var organizationId = this.GetCurrentOrganizationId();

                this._logger.LogInformation(
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

                this._dbContext.Webhooks.Add(webhook);
                await this._dbContext.SaveChangesAsync();

                this._logger.LogInformation("Webhook {WebhookId} created successfully", webhook.Id);

                return this.CreatedAtAction(
                    nameof(this.GetWebhook),
                    new { id = webhook.Id },
                    MapToDto(webhook, includeSecret: true));
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error creating webhook");
                return this.StatusCode(500, new { message = "An error occurred while creating the webhook" });
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
                var organizationId = this.GetCurrentOrganizationId();

                var webhooks = await this._dbContext.Webhooks
                    .Where(w => w.OrganizationId == organizationId)
                    .OrderByDescending(w => w.CreatedAt)
                    .ToListAsync();

                return this.Ok(webhooks.Select(w => MapToDto(w, includeSecret: false)).ToList());
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error listing webhooks");
                return this.StatusCode(500, new { message = "An error occurred while listing webhooks" });
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
                var organizationId = this.GetCurrentOrganizationId();

                var webhook = await this._dbContext.Webhooks
                    .FirstOrDefaultAsync(w => w.Id == id && w.OrganizationId == organizationId);

                if (webhook == null)
                {
                    return this.NotFound(new { message = "Webhook not found" });
                }

                return this.Ok(MapToDto(webhook, includeSecret: false));
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error getting webhook {WebhookId}", id);
                return this.StatusCode(500, new { message = "An error occurred while getting the webhook" });
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
                var organizationId = this.GetCurrentOrganizationId();

                var webhook = await this._dbContext.Webhooks
                    .FirstOrDefaultAsync(w => w.Id == id && w.OrganizationId == organizationId);

                if (webhook == null)
                {
                    return this.NotFound(new { message = "Webhook not found" });
                }

                if (!string.IsNullOrEmpty(request.Url))
                {
                    webhook.Url = request.Url;
                }

                if (request.Events.Count > 0)
                {
                    webhook.Events = request.Events;
                }

                webhook.IsActive = request.IsActive ?? false;

                webhook.UpdatedAt = DateTime.UtcNow;

                await this._dbContext.SaveChangesAsync();

                this._logger.LogInformation("Webhook {WebhookId} updated successfully", id);

                return this.Ok(MapToDto(webhook, includeSecret: false));
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error updating webhook {WebhookId}", id);
                return this.StatusCode(500, new { message = "An error occurred while updating the webhook" });
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
                var organizationId = this.GetCurrentOrganizationId();

                var webhook = await this._dbContext.Webhooks
                    .FirstOrDefaultAsync(w => w.Id == id && w.OrganizationId == organizationId);

                if (webhook == null)
                {
                    return this.NotFound(new { message = "Webhook not found" });
                }

                this._dbContext.Webhooks.Remove(webhook);
                await this._dbContext.SaveChangesAsync();

                this._logger.LogInformation("Webhook {WebhookId} deleted successfully", id);

                return this.NoContent();
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error deleting webhook {WebhookId}", id);
                return this.StatusCode(500, new { message = "An error occurred while deleting the webhook" });
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
                var organizationId = this.GetCurrentOrganizationId();

                var webhook = await this._dbContext.Webhooks
                    .FirstOrDefaultAsync(w => w.Id == id && w.OrganizationId == organizationId);

                if (webhook == null)
                {
                    return this.NotFound(new { message = "Webhook not found" });
                }

                webhook.Secret = GenerateSecret();
                webhook.UpdatedAt = DateTime.UtcNow;

                await this._dbContext.SaveChangesAsync();

                this._logger.LogInformation("Secret regenerated for webhook {WebhookId}", id);

                return this.Ok(MapToDto(webhook, includeSecret: true));
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error regenerating secret for webhook {WebhookId}", id);
                return this.StatusCode(500, new { message = "An error occurred while regenerating the secret" });
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
                var organizationId = this.GetCurrentOrganizationId();

                var webhook = await this._dbContext.Webhooks
                    .FirstOrDefaultAsync(w => w.Id == id && w.OrganizationId == organizationId);

                if (webhook == null)
                {
                    return this.NotFound(new { message = "Webhook not found" });
                }

                var logs = await this._dbContext.WebhookDeliveryLogs
                    .Where(l => l.WebhookId == id)
                    .OrderByDescending(l => l.DeliveredAt)
                    .Take(limit)
                    .ToListAsync();

                return this.Ok(logs.Select(MapToLogDto).ToList());
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error getting delivery logs for webhook {WebhookId}", id);
                return this.StatusCode(500, new { message = "An error occurred while getting delivery logs" });
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
                var organizationId = this.GetCurrentOrganizationId();

                var webhook = await this._dbContext.Webhooks
                    .FirstOrDefaultAsync(w => w.Id == id && w.OrganizationId == organizationId);

                if (webhook == null)
                {
                    return this.NotFound(new { message = "Webhook not found" });
                }

                webhook.IsActive = true;
                webhook.FailedDeliveryAttempts = 0;
                webhook.UpdatedAt = DateTime.UtcNow;

                await this._dbContext.SaveChangesAsync();

                this._logger.LogInformation("Webhook {WebhookId} reactivated", id);

                return this.NoContent();
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error reactivating webhook {WebhookId}", id);
                return this.StatusCode(500, new { message = "An error occurred while reactivating the webhook" });
            }
        }

        private Guid GetCurrentOrganizationId()
        {
            var organizationIdClaim = this.User.FindFirst("organization_id");
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
                StatusCode = log.StatusCode ?? 0,
                ResponseBody = log.ResponseBody,
                ErrorMessage = log.ErrorMessage,
                RetryAttempt = log.RetryAttempt,
                IsSuccess = log.IsSuccess,
                DeliveredAt = log.DeliveredAt,
                DurationMs = log.DurationMs,
            };
        }
    }
}
