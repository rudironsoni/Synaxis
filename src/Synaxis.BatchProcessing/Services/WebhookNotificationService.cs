// <copyright file="WebhookNotificationService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.BatchProcessing.Services
{
    using System;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Synaxis.BatchProcessing.Models;

    /// <summary>
    /// Service for sending webhook notifications.
    /// </summary>
    public class WebhookNotificationService : IWebhookNotificationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WebhookNotificationService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebhookNotificationService"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="logger">The logger.</param>
        public WebhookNotificationService(
            HttpClient httpClient,
            ILogger<WebhookNotificationService> logger)
        {
            this._httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Sends a completion notification for a batch.
        /// </summary>
        /// <param name="batch">The completed batch.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SendCompletionNotificationAsync(BatchRequest batch, CancellationToken cancellationToken = default)
        {
            if (batch == null)
            {
                throw new ArgumentNullException(nameof(batch));
            }

            if (string.IsNullOrEmpty(batch.WebhookUrl))
            {
                this._logger.LogWarning("No webhook URL configured for batch {BatchId}", batch.Id);
                return;
            }

            this._logger.LogInformation("Sending completion notification for batch {BatchId} to {WebhookUrl}", batch.Id, batch.WebhookUrl);

            var payload = new
            {
                eventType = "batch.completed",
                batchId = batch.Id,
                organizationId = batch.OrganizationId,
                userId = batch.UserId,
                status = batch.Status.ToString(),
                totalItems = batch.TotalItems,
                processedItems = batch.ProcessedItems,
                failedItems = batch.FailedItems,
                progressPercentage = batch.ProgressPercentage,
                completedAt = batch.CompletedAt,
                resultBlobPath = batch.ResultBlobPath
            };

            await this.SendWebhookAsync(batch.WebhookUrl, payload, cancellationToken);
        }

        /// <summary>
        /// Sends a failure notification for a batch.
        /// </summary>
        /// <param name="batch">The failed batch.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SendFailureNotificationAsync(BatchRequest batch, string errorMessage, CancellationToken cancellationToken = default)
        {
            if (batch == null)
            {
                throw new ArgumentNullException(nameof(batch));
            }

            if (string.IsNullOrEmpty(batch.WebhookUrl))
            {
                this._logger.LogWarning("No webhook URL configured for batch {BatchId}", batch.Id);
                return;
            }

            this._logger.LogInformation("Sending failure notification for batch {BatchId} to {WebhookUrl}", batch.Id, batch.WebhookUrl);

            var payload = new
            {
                eventType = "batch.failed",
                batchId = batch.Id,
                organizationId = batch.OrganizationId,
                userId = batch.UserId,
                status = batch.Status.ToString(),
                totalItems = batch.TotalItems,
                processedItems = batch.ProcessedItems,
                failedItems = batch.FailedItems,
                errorMessage = errorMessage,
                completedAt = batch.CompletedAt
            };

            await this.SendWebhookAsync(batch.WebhookUrl, payload, cancellationToken);
        }

        /// <summary>
        /// Sends a webhook notification.
        /// </summary>
        /// <param name="url">The webhook URL.</param>
        /// <param name="payload">The payload to send.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task SendWebhookAsync(string url, object payload, CancellationToken cancellationToken = default)
        {
            try
            {
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await this._httpClient.PostAsync(url, content, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    this._logger.LogInformation("Webhook notification sent successfully to {Url}", url);
                }
                else
                {
                    this._logger.LogWarning(
                        "Webhook notification failed with status {StatusCode}: {Reason}",
                        response.StatusCode,
                        response.ReasonPhrase);
                }
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error sending webhook notification to {Url}", url);
                // Don't throw - webhook failures should not break the batch processing
            }
        }
    }
}
