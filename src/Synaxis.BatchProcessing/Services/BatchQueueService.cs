// <copyright file="BatchQueueService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.BatchProcessing.Services
{
    using System;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Messaging.ServiceBus;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Synaxis.BatchProcessing.Models;

    /// <summary>
    /// Service for managing batch processing queues using Azure Service Bus.
    /// </summary>
    public class BatchQueueService : IBatchQueueService, IAsyncDisposable
    {
        private readonly ServiceBusClient _client;
        private readonly ServiceBusSender _sender;
        private readonly ServiceBusProcessor _processor;
        private readonly IBatchStorageService _storageService;
        private readonly BatchProcessor _batchProcessor;
        private readonly ILogger<BatchQueueService> _logger;
        private readonly BatchQueueOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="BatchQueueService"/> class.
        /// </summary>
        /// <param name="storageService">The batch storage service.</param>
        /// <param name="batchProcessor">The batch processor.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="options">The batch queue options.</param>
        public BatchQueueService(
            IBatchStorageService storageService,
            BatchProcessor batchProcessor,
            ILogger<BatchQueueService> logger,
            IOptions<BatchQueueOptions> options)
        {
            this._storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            this._batchProcessor = batchProcessor ?? throw new ArgumentNullException(nameof(batchProcessor));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this._options = options?.Value ?? throw new ArgumentNullException(nameof(options));

            if (string.IsNullOrEmpty(this._options.ConnectionString))
            {
                throw new ArgumentException("Service Bus connection string is required", nameof(options));
            }

            if (string.IsNullOrEmpty(this._options.QueueName))
            {
                throw new ArgumentException("Service Bus queue name is required", nameof(options));
            }

            this._client = new ServiceBusClient(this._options.ConnectionString);
            this._sender = this._client.CreateSender(this._options.QueueName);
            this._processor = this._client.CreateProcessor(this._options.QueueName, new ServiceBusProcessorOptions
            {
                MaxConcurrentCalls = this._options.MaxConcurrentCalls,
                AutoCompleteMessages = false
            });

            this._processor.ProcessMessageAsync += this.ProcessMessageAsync;
            this._processor.ProcessErrorAsync += this.ProcessErrorAsync;
        }

        /// <summary>
        /// Starts the queue processor.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task StartProcessingAsync(CancellationToken cancellationToken = default)
        {
            this._logger.LogInformation("Starting batch queue processor for queue {QueueName}", this._options.QueueName);
            await this._processor.StartProcessingAsync(cancellationToken);
        }

        /// <summary>
        /// Stops the queue processor.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task StopProcessingAsync(CancellationToken cancellationToken = default)
        {
            this._logger.LogInformation("Stopping batch queue processor for queue {QueueName}", this._options.QueueName);
            await this._processor.StopProcessingAsync(cancellationToken);
        }

        /// <summary>
        /// Enqueues a batch for processing.
        /// </summary>
        /// <param name="batch">The batch to enqueue.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task EnqueueBatchAsync(BatchRequest batch, CancellationToken cancellationToken = default)
        {
            if (batch == null)
            {
                throw new ArgumentNullException(nameof(batch));
            }

            this._logger.LogInformation("Enqueuing batch {BatchId} to queue {QueueName}", batch.Id, this._options.QueueName);

            // Update batch status to Queued
            batch.Status = BatchStatus.Queued;
            await this._storageService.UpdateBatchAsync(batch, cancellationToken);

            // Create message with priority
            var messageBody = JsonSerializer.Serialize(batch);
            var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(messageBody))
            {
                MessageId = batch.Id.ToString(),
                Subject = batch.OperationType,
                CorrelationId = batch.OrganizationId.ToString()
            };

            // Add priority as application property (Service Bus doesn't have built-in priority)
            message.ApplicationProperties.Add("Priority", batch.Priority.ToString());

            // Add retry count as application property
            message.ApplicationProperties.Add("RetryCount", batch.RetryCount);

            await this._sender.SendMessageAsync(message, cancellationToken);

            this._logger.LogInformation("Batch {BatchId} enqueued successfully", batch.Id);
        }

        /// <summary>
        /// Processes a message from the queue.
        /// </summary>
        /// <param name="args">The message arguments.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
        {
            var message = args.Message;
            var batchId = message.MessageId;

            this._logger.LogInformation("Processing message for batch {BatchId}", batchId);

            try
            {
                // Deserialize batch from message
                var batch = JsonSerializer.Deserialize<BatchRequest>(message.Body.ToString());
                if (batch == null)
                {
                    this._logger.LogError("Failed to deserialize batch {BatchId}", batchId);
                    await args.DeadLetterMessageAsync(message, "Deserialization failed");
                    return;
                }

                // Process the batch
                await this._batchProcessor.ProcessBatchAsync(batch, args.CancellationToken);

                // Complete the message
                await args.CompleteMessageAsync(message);

                this._logger.LogInformation("Successfully processed batch {BatchId}", batchId);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error processing batch {BatchId}", batchId);

                // Check if we should retry or dead-letter
                var retryCount = message.ApplicationProperties.ContainsKey("RetryCount")
                    ? (int)message.ApplicationProperties["RetryCount"]
                    : 0;

                if (retryCount < 3)
                {
                    // Increment retry count and requeue
                    var clonedMessage = new ServiceBusMessage(message)
                    {
                        ApplicationProperties =
                        {
                            ["RetryCount"] = retryCount + 1
                        }
                    };

                    await this._sender.SendMessageAsync(clonedMessage);
                    await args.CompleteMessageAsync(message);

                    this._logger.LogWarning("Retrying batch {BatchId} (attempt {RetryCount})", batchId, retryCount + 1);
                }
                else
                {
                    // Dead-letter the message
                    await args.DeadLetterMessageAsync(message, $"Max retries exceeded: {ex.Message}");

                    this._logger.LogError("Batch {BatchId} exceeded max retries and was dead-lettered", batchId);
                }
            }
        }

        /// <summary>
        /// Handles errors from the queue processor.
        /// </summary>
        /// <param name="args">The error arguments.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private Task ProcessErrorAsync(ProcessErrorEventArgs args)
        {
            this._logger.LogError(
                args.Exception,
                "Error processing message: {ErrorSource}, {EntityPath}, {Error}",
                args.ErrorSource,
                args.EntityPath,
                args.Exception.Message);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Disposes the resources used by the service.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await this._processor.DisposeAsync();
            await this._sender.DisposeAsync();
            await this._client.DisposeAsync();
        }
    }

    /// <summary>
    /// Configuration options for batch queue.
    /// </summary>
    public class BatchQueueOptions
    {
        /// <summary>
        /// Gets or sets the Azure Service Bus connection string.
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the queue name.
        /// </summary>
        public string QueueName { get; set; } = "batch-processing";

        /// <summary>
        /// Gets or sets the maximum concurrent calls.
        /// </summary>
        public int MaxConcurrentCalls { get; set; } = 5;
    }
}
