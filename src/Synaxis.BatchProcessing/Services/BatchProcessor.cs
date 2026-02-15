// <copyright file="BatchProcessor.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.BatchProcessing.Services;

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
        _ = options?.Value ?? throw new ArgumentNullException(nameof(options));
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
            await this.StartBatchProcessingAsync(batch, cancellationToken).ConfigureAwait(false);
            var results = await this.ProcessBatchItemsAsync(batch, cancellationToken).ConfigureAwait(false);
            await this.CompleteBatchProcessingAsync(batch, results, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await this.HandleBatchProcessingErrorAsync(batch, ex, cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException($"Failed to process batch {batch.Id}", ex);
        }
    }

    /// <summary>
    /// Starts the batch processing by updating the status.
    /// </summary>
    /// <param name="batch">The batch to process.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private Task StartBatchProcessingAsync(BatchRequest batch, CancellationToken cancellationToken)
    {
        batch.Status = BatchStatus.Processing;
        batch.StartedAt = DateTime.UtcNow;
        return this._storageService.UpdateBatchAsync(batch, cancellationToken);
    }

    /// <summary>
    /// Processes all items in the batch.
    /// </summary>
    /// <param name="batch">The batch to process.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of batch item results.</returns>
    private async Task<List<BatchItemResult>> ProcessBatchItemsAsync(BatchRequest batch, CancellationToken cancellationToken)
    {
        var results = new List<BatchItemResult>();
        foreach (var item in batch.Items)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                this._logger.LogWarning("Batch processing cancelled for batch {BatchId}", batch.Id);
                batch.Status = BatchStatus.Cancelled;
                await this._storageService.UpdateBatchAsync(batch, cancellationToken).ConfigureAwait(false);
                return results;
            }

            var result = await this.ProcessBatchItemAsync(batch, item, cancellationToken).ConfigureAwait(false);
            results.Add(result);
            await this._storageService.UpdateBatchAsync(batch, cancellationToken).ConfigureAwait(false);
        }

        return results;
    }

    /// <summary>
    /// Processes a single batch item.
    /// </summary>
    /// <param name="batch">The batch containing the item.</param>
    /// <param name="item">The item to process.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The batch item result.</returns>
    private async Task<BatchItemResult> ProcessBatchItemAsync(BatchRequest batch, BatchItem item, CancellationToken cancellationToken)
    {
        try
        {
            item.Status = BatchItemStatus.Processing;
            await this._storageService.UpdateBatchAsync(batch, cancellationToken).ConfigureAwait(false);

            var result = await this.ProcessItemAsync(item, batch.OperationType, cancellationToken).ConfigureAwait(false);

            item.Status = BatchItemStatus.Completed;
            item.Result = JsonSerializer.Serialize(result);
            item.ProcessedAt = DateTime.UtcNow;
            batch.ProcessedItems++;

            this._logger.LogDebug("Processed item {ItemId} for batch {BatchId}", item.Id, batch.Id);

            return new BatchItemResult
            {
                ItemId = item.Id,
                Status = BatchItemStatus.Completed,
                Result = result,
            };
        }
        catch (Exception ex)
        {
            item.Status = BatchItemStatus.Failed;
            item.ErrorMessage = ex.Message;
            item.ProcessedAt = DateTime.UtcNow;
            batch.FailedItems++;

            this._logger.LogError(ex, "Failed to process item {ItemId} for batch {BatchId}", item.Id, batch.Id);

            return new BatchItemResult
            {
                ItemId = item.Id,
                Status = BatchItemStatus.Failed,
                ErrorMessage = ex.Message,
            };
        }
    }

    /// <summary>
    /// Completes the batch processing by storing results and updating status.
    /// </summary>
    /// <param name="batch">The batch to complete.</param>
    /// <param name="results">The batch item results.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task CompleteBatchProcessingAsync(BatchRequest batch, List<BatchItemResult> results, CancellationToken cancellationToken)
    {
        var resultBlobPath = await this._storageService.StoreResultsAsync(batch.Id, results, cancellationToken).ConfigureAwait(false);
        batch.ResultBlobPath = resultBlobPath;

        batch.Status = batch.FailedItems > 0 ? BatchStatus.Failed : BatchStatus.Completed;
        batch.CompletedAt = DateTime.UtcNow;
        await this._storageService.UpdateBatchAsync(batch, cancellationToken).ConfigureAwait(false);

        this._logger.LogInformation(
            "Completed processing for batch {BatchId}. Processed: {Processed}, Failed: {Failed}",
            batch.Id,
            batch.ProcessedItems,
            batch.FailedItems);

        if (!string.IsNullOrEmpty(batch.WebhookUrl))
        {
            await this._webhookService.SendCompletionNotificationAsync(batch, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Handles batch processing errors.
    /// </summary>
    /// <param name="batch">The batch that failed.</param>
    /// <param name="ex">The exception that occurred.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task HandleBatchProcessingErrorAsync(BatchRequest batch, Exception ex, CancellationToken cancellationToken)
    {
        this._logger.LogError(ex, "Error processing batch {BatchId}", batch.Id);
        batch.Status = BatchStatus.Failed;
        batch.ErrorMessage = ex.Message;
        batch.CompletedAt = DateTime.UtcNow;
        await this._storageService.UpdateBatchAsync(batch, cancellationToken).ConfigureAwait(false);

        if (!string.IsNullOrEmpty(batch.WebhookUrl))
        {
            await this._webhookService.SendFailureNotificationAsync(batch, ex.Message, cancellationToken).ConfigureAwait(false);
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
        await Task.Delay(100, cancellationToken).ConfigureAwait(false); // Simulate processing time

        return new
        {
            ItemId = item.Id,
            OperationType = operationType,
            ProcessedAt = DateTime.UtcNow,
            Success = true,
            Data = $"Processed: {item.Data}",
        };
    }
}
