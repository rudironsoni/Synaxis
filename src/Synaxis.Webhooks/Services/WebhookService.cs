// <copyright file="WebhookService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Webhooks.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Polly;
    using Polly.Retry;
    using Synaxis.Webhooks.Data;
    using Synaxis.Webhooks.Models;

    /// <summary>
    /// Service for delivering webhook events with retry logic.
    /// </summary>
    public class WebhookService
    {
        private readonly WebhooksDbContext _dbContext;
        private readonly HttpClient _httpClient;
        private readonly ILogger<WebhookService> _logger;
        private readonly WebhookDeliveryOptions _options;
        private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebhookService"/> class.
        /// </summary>
        /// <param name="dbContext">The database context.</param>
        /// <param name="httpClient">The HTTP client for delivering webhooks.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="options">The webhook delivery options.</param>
        public WebhookService(
            WebhooksDbContext dbContext,
            HttpClient httpClient,
            ILogger<WebhookService> logger,
            IOptions<WebhookDeliveryOptions> options)
        {
            this._dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            this._httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this._options = options.Value ?? new WebhookDeliveryOptions();

            // Configure exponential backoff retry policy
            this._retryPolicy = Policy
                .HandleResult<HttpResponseMessage>(r =>
                    !r.IsSuccessStatusCode &&
                    r.StatusCode != HttpStatusCode.BadRequest &&
                    r.StatusCode != HttpStatusCode.Unauthorized &&
                    r.StatusCode != HttpStatusCode.NotFound &&
                    r.StatusCode != HttpStatusCode.MethodNotAllowed)
                .Or<HttpRequestException>()
                .Or<TimeoutException>()
                .WaitAndRetryAsync(
                    this._options.MaxRetryAttempts,
                    retryAttempt => TimeSpan.FromSeconds(Math.Min(
                        this._options.InitialRetryDelaySeconds * Math.Pow(2, retryAttempt - 1),
                        this._options.MaxRetryDelaySeconds)),
                    onRetry: (outcome, timeSpan, retryCount, context) =>
                    {
                        this._logger.LogWarning(
                            "Webhook delivery attempt {RetryCount} failed after {Delay}s. Retrying...",
                            retryCount,
                            timeSpan.TotalSeconds);
                    });
        }

        /// <summary>
        /// Delivers a webhook event to all active webhooks that subscribe to the event type.
        /// </summary>
        /// <param name="eventType">The event type.</param>
        /// <param name="payload">The event payload.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task DeliverEventAsync(string eventType, object payload, CancellationToken cancellationToken = default)
        {
            var payloadJson = JsonSerializer.Serialize(payload);
            var webhooks = await this.GetActiveWebhooksForEventAsync(eventType, cancellationToken).ConfigureAwait(false);

            this._logger.LogInformation("Delivering event {EventType} to {Count} webhooks", eventType, webhooks.Count);

            var deliveryTasks = webhooks.Select(webhook =>
                this.DeliverToWebhookAsync(webhook, eventType, payloadJson, cancellationToken));

            await Task.WhenAll(deliveryTasks).ConfigureAwait(false);
        }

        /// <summary>
        /// Delivers a webhook event to a specific webhook.
        /// </summary>
        /// <param name="webhook">The webhook to deliver to.</param>
        /// <param name="eventType">The event type.</param>
        /// <param name="payload">The event payload.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task DeliverToWebhookAsync(Webhook webhook, string eventType, string payload, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            var deliveryLog = new WebhookDeliveryLog
            {
                Id = Guid.NewGuid(),
                WebhookId = webhook.Id,
                EventType = eventType,
                Payload = payload,
                DeliveredAt = startTime,
                RetryAttempt = 0,
            };

            try
            {
                var signature = WebhookSignature.GenerateSignature(payload, webhook.Secret);

                using var request = new HttpRequestMessage(HttpMethod.Post, webhook.Url)
                {
                    Content = new StringContent(payload, Encoding.UTF8, "application/json"),
                };

                request.Headers.Add(WebhookSignature.GetSignatureHeader(), signature);
                request.Headers.Add("X-Webhook-Event-Type", eventType);
                request.Headers.Add("X-Webhook-Delivery-Id", deliveryLog.Id.ToString());

                this._logger.LogInformation(
                    "Delivering webhook {WebhookId} to {Url} for event {EventType}",
                    webhook.Id,
                    webhook.Url,
                    eventType);

                var response = await this._retryPolicy.ExecuteAsync(async () =>
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(TimeSpan.FromSeconds(this._options.DeliveryTimeoutSeconds));

                    return await this._httpClient.SendAsync(request, cts.Token).ConfigureAwait(false);
                }).ConfigureAwait(false);

                var duration = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                deliveryLog.DurationMs = duration;
                deliveryLog.StatusCode = (int)response.StatusCode;
                deliveryLog.IsSuccess = response.IsSuccessStatusCode;

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    deliveryLog.ResponseBody = responseBody;

                    webhook.LastSuccessfulDeliveryAt = DateTime.UtcNow;
                    webhook.FailedDeliveryAttempts = 0;
                    webhook.UpdatedAt = DateTime.UtcNow;

                    this._logger.LogInformation(
                        "Webhook {WebhookId} delivered successfully in {Duration}ms",
                        webhook.Id,
                        duration);
                }
                else
                {
                    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    deliveryLog.ResponseBody = responseBody;
                    deliveryLog.ErrorMessage = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}";

                    webhook.FailedDeliveryAttempts++;
                    webhook.UpdatedAt = DateTime.UtcNow;

                    this._logger.LogWarning(
                        "Webhook {WebhookId} delivery failed with status {StatusCode}: {Reason}",
                        webhook.Id,
                        (int)response.StatusCode,
                        response.ReasonPhrase);

                    // Move to dead letter queue after max retries
                    if (webhook.FailedDeliveryAttempts >= this._options.MaxRetryAttempts)
                    {
                        await this.MoveToDeadLetterQueueAsync(webhook).ConfigureAwait(false);
                    }
                }
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                var duration = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                deliveryLog.DurationMs = duration;
                deliveryLog.IsSuccess = false;
                deliveryLog.ErrorMessage = $"Timeout after {duration}ms";

                webhook.FailedDeliveryAttempts++;
                webhook.UpdatedAt = DateTime.UtcNow;

                this._logger.LogError(ex, "Webhook {WebhookId} delivery timed out", webhook.Id);

                if (webhook.FailedDeliveryAttempts >= this._options.MaxRetryAttempts)
                {
                    await this.MoveToDeadLetterQueueAsync(webhook).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                var duration = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                deliveryLog.DurationMs = duration;
                deliveryLog.IsSuccess = false;
                deliveryLog.ErrorMessage = ex.Message;

                webhook.FailedDeliveryAttempts++;
                webhook.UpdatedAt = DateTime.UtcNow;

                this._logger.LogError(ex, "Webhook {WebhookId} delivery failed", webhook.Id);

                if (webhook.FailedDeliveryAttempts >= this._options.MaxRetryAttempts)
                {
                    await this.MoveToDeadLetterQueueAsync(webhook).ConfigureAwait(false);
                }
            }
            finally
            {
                this._dbContext.WebhookDeliveryLogs.Add(deliveryLog);
                await this._dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets all active webhooks that subscribe to a specific event type.
        /// </summary>
        /// <param name="eventType">The event type.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of active webhooks.</returns>
        private Task<List<Webhook>> GetActiveWebhooksForEventAsync(string eventType, CancellationToken cancellationToken)
        {
            return this._dbContext.Webhooks
                .Where(w => w.IsActive && w.Events.Contains(eventType))
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Moves a failed webhook delivery to the dead letter queue.
        /// </summary>
        /// <param name="webhook">The webhook that failed.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private Task MoveToDeadLetterQueueAsync(Webhook webhook)
        {
            webhook.IsActive = false;
            webhook.UpdatedAt = DateTime.UtcNow;

            this._logger.LogWarning(
                "Webhook {WebhookId} moved to dead letter queue after {Attempts} failed attempts",
                webhook.Id,
                webhook.FailedDeliveryAttempts);

            // In a production system, you might want to:
            // 1. Send a notification to the webhook owner
            // 2. Store the failed delivery in a separate dead letter table
            // 3. Provide a mechanism to retry failed deliveries manually
            return this._dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Reactivates a webhook that was moved to the dead letter queue.
        /// </summary>
        /// <param name="webhookId">The webhook ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ReactivateWebhookAsync(Guid webhookId, CancellationToken cancellationToken = default)
        {
            var webhook = await this._dbContext.Webhooks.FindAsync(new object[] { webhookId }, cancellationToken).ConfigureAwait(false);

            if (webhook == null)
            {
                throw new ArgumentException($"Webhook with ID {webhookId} not found.", nameof(webhookId));
            }

            webhook.IsActive = true;
            webhook.FailedDeliveryAttempts = 0;
            webhook.UpdatedAt = DateTime.UtcNow;

            await this._dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            this._logger.LogInformation("Webhook {WebhookId} reactivated", webhookId);
        }
    }
}
