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
    /// Configuration options for webhook delivery.
    /// </summary>
    public class WebhookDeliveryOptions
    {
        /// <summary>
        /// Gets or sets the maximum number of retry attempts.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Gets or sets the initial delay between retries in seconds.
        /// </summary>
        public int InitialRetryDelaySeconds { get; set; } = 1;

        /// <summary>
        /// Gets or sets the maximum delay between retries in seconds.
        /// </summary>
        public int MaxRetryDelaySeconds { get; set; } = 60;

        /// <summary>
        /// Gets or sets the timeout for webhook delivery in seconds.
        /// </summary>
        public int DeliveryTimeoutSeconds { get; set; } = 30;
    }

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
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options.Value ?? new WebhookDeliveryOptions();

            // Configure exponential backoff retry policy
            _retryPolicy = Policy
                .HandleResult<HttpResponseMessage>(r =>
                    !r.IsSuccessStatusCode &&
                    r.StatusCode != HttpStatusCode.BadRequest &&
                    r.StatusCode != HttpStatusCode.Unauthorized &&
                    r.StatusCode != HttpStatusCode.NotFound &&
                    r.StatusCode != HttpStatusCode.MethodNotAllowed)
                .Or<HttpRequestException>()
                .Or<TimeoutException>()
                .WaitAndRetryAsync(
                    _options.MaxRetryAttempts,
                    retryAttempt => TimeSpan.FromSeconds(Math.Min(
                        _options.InitialRetryDelaySeconds * Math.Pow(2, retryAttempt - 1),
                        _options.MaxRetryDelaySeconds)),
                    onRetry: (outcome, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning(
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
            var webhooks = await GetActiveWebhooksForEventAsync(eventType, cancellationToken);

            _logger.LogInformation("Delivering event {EventType} to {Count} webhooks", eventType, webhooks.Count);

            var deliveryTasks = webhooks.Select(webhook =>
                DeliverToWebhookAsync(webhook, eventType, payloadJson, cancellationToken));

            await Task.WhenAll(deliveryTasks);
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

                var request = new HttpRequestMessage(HttpMethod.Post, webhook.Url)
                {
                    Content = new StringContent(payload, Encoding.UTF8, "application/json")
                };

                request.Headers.Add(WebhookSignature.GetSignatureHeader(), signature);
                request.Headers.Add("X-Webhook-Event-Type", eventType);
                request.Headers.Add("X-Webhook-Delivery-Id", deliveryLog.Id.ToString());

                _logger.LogInformation(
                    "Delivering webhook {WebhookId} to {Url} for event {EventType}",
                    webhook.Id,
                    webhook.Url,
                    eventType);

                var response = await _retryPolicy.ExecuteAsync(async () =>
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(TimeSpan.FromSeconds(_options.DeliveryTimeoutSeconds));

                    return await _httpClient.SendAsync(request, cts.Token);
                });

                var duration = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                deliveryLog.DurationMs = duration;
                deliveryLog.StatusCode = (int)response.StatusCode;
                deliveryLog.IsSuccess = response.IsSuccessStatusCode;

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    deliveryLog.ResponseBody = responseBody;

                    webhook.LastSuccessfulDeliveryAt = DateTime.UtcNow;
                    webhook.FailedDeliveryAttempts = 0;
                    webhook.UpdatedAt = DateTime.UtcNow;

                    _logger.LogInformation(
                        "Webhook {WebhookId} delivered successfully in {Duration}ms",
                        webhook.Id,
                        duration);
                }
                else
                {
                    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    deliveryLog.ResponseBody = responseBody;
                    deliveryLog.ErrorMessage = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}";

                    webhook.FailedDeliveryAttempts++;
                    webhook.UpdatedAt = DateTime.UtcNow;

                    _logger.LogWarning(
                        "Webhook {WebhookId} delivery failed with status {StatusCode}: {Reason}",
                        webhook.Id,
                        (int)response.StatusCode,
                        response.ReasonPhrase);

                    // Move to dead letter queue after max retries
                    if (webhook.FailedDeliveryAttempts >= _options.MaxRetryAttempts)
                    {
                        await MoveToDeadLetterQueueAsync(webhook, deliveryLog);
                        return;
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

                _logger.LogError(ex, "Webhook {WebhookId} delivery timed out", webhook.Id);

                if (webhook.FailedDeliveryAttempts >= _options.MaxRetryAttempts)
                {
                    await MoveToDeadLetterQueueAsync(webhook, deliveryLog);
                    return;
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

                _logger.LogError(ex, "Webhook {WebhookId} delivery failed", webhook.Id);

                if (webhook.FailedDeliveryAttempts >= _options.MaxRetryAttempts)
                {
                    await MoveToDeadLetterQueueAsync(webhook, deliveryLog);
                    return;
                }
            }
            finally
            {
                _dbContext.WebhookDeliveryLogs.Add(deliveryLog);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Gets all active webhooks that subscribe to a specific event type.
        /// </summary>
        /// <param name="eventType">The event type.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of active webhooks.</returns>
        private async Task<List<Webhook>> GetActiveWebhooksForEventAsync(string eventType, CancellationToken cancellationToken)
        {
            return await _dbContext.Webhooks
                .Where(w => w.IsActive && w.Events.Contains(eventType))
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Moves a failed webhook delivery to the dead letter queue.
        /// </summary>
        /// <param name="webhook">The webhook that failed.</param>
        /// <param name="deliveryLog">The delivery log.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task MoveToDeadLetterQueueAsync(Webhook webhook, WebhookDeliveryLog deliveryLog)
        {
            webhook.IsActive = false;
            webhook.UpdatedAt = DateTime.UtcNow;

            _logger.LogWarning(
                "Webhook {WebhookId} moved to dead letter queue after {Attempts} failed attempts",
                webhook.Id,
                webhook.FailedDeliveryAttempts);

            // In a production system, you might want to:
            // 1. Send a notification to the webhook owner
            // 2. Store the failed delivery in a separate dead letter table
            // 3. Provide a mechanism to retry failed deliveries manually
            await _dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Reactivates a webhook that was moved to the dead letter queue.
        /// </summary>
        /// <param name="webhookId">The webhook ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ReactivateWebhookAsync(Guid webhookId, CancellationToken cancellationToken = default)
        {
            var webhook = await _dbContext.Webhooks.FindAsync(new object[] { webhookId }, cancellationToken);

            if (webhook == null)
            {
                throw new ArgumentException($"Webhook with ID {webhookId} not found.", nameof(webhookId));
            }

            webhook.IsActive = true;
            webhook.FailedDeliveryAttempts = 0;
            webhook.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Webhook {WebhookId} reactivated", webhookId);
        }
    }
}
