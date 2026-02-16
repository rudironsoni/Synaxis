// <copyright file="BatchProcessor.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.BatchProcessing.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Synaxis.BatchProcessing.Models;

    /// <summary>
    /// Service for processing batch requests.
    /// </summary>
    public class BatchProcessor
    {
        private readonly IBatchStorageService _storageService;
        private readonly IWebhookNotificationService _webhookService;
        private readonly ILogger<BatchProcessor> _logger;
        private readonly BatchProcessingOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="BatchProcessor"/> class.
        /// </summary>
        /// <param name="storageService">The batch storage service.</param>
        /// <param name="webhookService">The webhook notification service.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="options">The batch processing options.</param>
        public BatchProcessor(
            IBatchStorageService storageService,
            IWebhookNotificationService webhookService,
            ILogger<BatchProcessor> logger,
            IOptions<BatchProcessingOptions> options)
        {
            this._storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            this._webhookService = webhookService ?? throw new ArgumentNullException(nameof(webhookService));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this._options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Processes a batch request asynchronously.
        /// </summary>
        /// <param name="batch">The batch to process.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ProcessBatchAsync(BatchRequest batch, CancellationToken cancellationToken = default)
        {
            if (batch == null)
            {
                throw new ArgumentNullException(nameof(batch));
            }

            this._logger.LogInformation("Starting processing for batch {BatchId}", batch.Id);

            try
            {
                // Update batch status to Processing
                batch.Status = BatchStatus.Processing;
                batch.StartedAt = DateTime.UtcNow;
                await this._storageService.UpdateBatchAsync(batch, cancellationToken);

                // Process each item in the batch
                var results = new List<BatchItemResult>();
                foreach (var item in batch.Items)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        this._logger.LogWarning("Batch processing cancelled for batch {BatchId}", batch.Id);
                        batch.Status = BatchStatus.Cancelled;
                        await this._storageService.UpdateBatchAsync(batch, cancellationToken);
                        return;
                    }

                    try
                    {
                        item.Status = BatchItemStatus.Processing;
                        await this._storageService.UpdateBatchAsync(batch, cancellationToken);

                        // Process the item (this is a placeholder - actual processing logic would go here)
                        var result = await this.ProcessItemAsync(item, batch.OperationType, cancellationToken);

                        item.Status = BatchItemStatus.Completed;
                        item.Result = JsonSerializer.Serialize(result);
                        item.ProcessedAt = DateTime.UtcNow;
                        batch.ProcessedItems++;
                        results.Add(new BatchItemResult
                        {
                            ItemId = item.Id,
                            Status = BatchItemStatus.Completed,
                            Result = result
                        });

                        this._logger.LogDebug("Processed item {ItemId} for batch {BatchId}", item.Id, batch.Id);
                    }
                    catch (Exception ex)
                    {
                        item.Status = BatchItemStatus.Failed;
                        item.ErrorMessage = ex.Message;
                        item.ProcessedAt = DateTime.UtcNow;
                        batch.FailedItems++;
                        results.Add(new BatchItemResult
                        {
                            ItemId = item.Id,
                            Status = BatchItemStatus.Failed,
                            ErrorMessage = ex.Message
                        });

                        this._logger.LogError(ex, "Failed to process item {ItemId} for batch {BatchId}", item.Id, batch.Id);
                    }

                    // Update batch progress
                    await this._storageService.UpdateBatchAsync(batch, cancellationToken);
                }

                // Store results in blob storage
                var resultBlobPath = await this._storageService.StoreResultsAsync(batch.Id, results, cancellationToken);
                batch.ResultBlobPath = resultBlobPath;

                // Update final batch status
                batch.Status = batch.FailedItems > 0 ? BatchStatus.Failed : BatchStatus.Completed;
                batch.CompletedAt = DateTime.UtcNow;
                await this._storageService.UpdateBatchAsync(batch, cancellationToken);

                this._logger.LogInformation(
                    "Completed processing for batch {BatchId}. Processed: {Processed}, Failed: {Failed}",
                    batch.Id,
                    batch.ProcessedItems,
                    batch.FailedItems);

                // Send webhook notification if configured
                if (!string.IsNullOrEmpty(batch.WebhookUrl))
                {
                    await this._webhookService.SendCompletionNotificationAsync(batch, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error processing batch {BatchId}", batch.Id);
                batch.Status = BatchStatus.Failed;
                batch.ErrorMessage = ex.Message;
                batch.CompletedAt = DateTime.UtcNow;
                await this._storageService.UpdateBatchAsync(batch, cancellationToken);

                // Send webhook notification for failure if configured
                if (!string.IsNullOrEmpty(batch.WebhookUrl))
                {
                    await this._webhookService.SendFailureNotificationAsync(batch, ex.Message, cancellationToken);
                }

                throw;
            }
        }

        /// <summary>
        /// Processes a single batch item.
        /// </summary>
        /// <param name="item">The item to process.</param>
        /// <param name="operationType">The type of operation.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The processing result.</returns>
        private async Task<object> ProcessItemAsync(BatchItem item, string operationType, CancellationToken cancellationToken)
        {
            // This is a placeholder implementation
            // In a real scenario, this would contain the actual business logic for processing items
            await Task.Delay(100, cancellationToken); // Simulate processing time

            return new
            {
                ItemId = item.Id,
                OperationType = operationType,
                ProcessedAt = DateTime.UtcNow,
                Success = true,
                Data = $"Processed: {item.Data}"
            };
        }
    }

    /// <summary>
    /// Configuration options for batch processing.
    /// </summary>
    public class BatchProcessingOptions
    {
        /// <summary>
        /// Gets or sets the maximum number of concurrent batches.
        /// </summary>
        public int MaxConcurrentBatches { get; set; } = 5;

        /// <summary>
        /// Gets or sets the maximum retry count.
        /// </summary>
        public int MaxRetryCount { get; set; } = 3;

        /// <summary>
        /// Gets or sets the retry delay in seconds.
        /// </summary>
        public int RetryDelaySeconds { get; set; } = 60;

        /// <summary>
        /// Gets or sets the timeout for batch processing in minutes.
        /// </summary>
        public int BatchTimeoutMinutes { get; set; } = 30;
    }
}
