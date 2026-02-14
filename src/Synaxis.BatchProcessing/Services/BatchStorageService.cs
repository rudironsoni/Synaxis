// <copyright file="BatchStorageService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.BatchProcessing.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure;
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Models;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Synaxis.BatchProcessing.Models;

    /// <summary>
    /// Service for storing batch requests and results using Azure Blob Storage.
    /// </summary>
    public class BatchStorageService : IBatchStorageService
    {
        private readonly BlobContainerClient _containerClient;
        private readonly ILogger<BatchStorageService> _logger;
        private readonly BatchStorageOptions _options;
        private readonly Dictionary<Guid, BatchRequest> _inMemoryBatches;

        /// <summary>
        /// Initializes a new instance of the <see cref="BatchStorageService"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="options">The batch storage options.</param>
        public BatchStorageService(
            ILogger<BatchStorageService> logger,
            IOptions<BatchStorageOptions> options)
        {
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this._options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            this._inMemoryBatches = new Dictionary<Guid, BatchRequest>();

            if (string.IsNullOrEmpty(this._options.ConnectionString))
            {
                throw new ArgumentException("Blob Storage connection string is required", nameof(options));
            }

            if (string.IsNullOrEmpty(this._options.ContainerName))
            {
                throw new ArgumentException("Blob Storage container name is required", nameof(options));
            }

            var blobServiceClient = new BlobServiceClient(this._options.ConnectionString);
            this._containerClient = blobServiceClient.GetBlobContainerClient(this._options.ContainerName);
        }

        /// <summary>
        /// Initializes the storage service.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            this._logger.LogInformation("Initializing batch storage service for container {ContainerName}", this._options.ContainerName);
            await this._containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Creates a new batch.
        /// </summary>
        /// <param name="batch">The batch to create.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task CreateBatchAsync(BatchRequest batch, CancellationToken cancellationToken = default)
        {
            if (batch == null)
            {
                throw new ArgumentNullException(nameof(batch));
            }

            this._logger.LogInformation("Creating batch {BatchId}", batch.Id);

            // Store in memory (in production, this would be a database)
            this._inMemoryBatches[batch.Id] = batch;

            // Store batch metadata in blob storage
            var blobName = $"batches/{batch.Id}/metadata.json";
            var blobClient = this._containerClient.GetBlobClient(blobName);
            var batchJson = JsonSerializer.Serialize(batch);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(batchJson));
            await blobClient.UploadAsync(stream, cancellationToken: cancellationToken);

            this._logger.LogInformation("Batch {BatchId} created successfully", batch.Id);
        }

        /// <summary>
        /// Gets a batch by identifier.
        /// </summary>
        /// <param name="batchId">The batch identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The batch, or null if not found.</returns>
        public async Task<BatchRequest> GetBatchAsync(Guid batchId, CancellationToken cancellationToken = default)
        {
            this._logger.LogDebug("Retrieving batch {BatchId}", batchId);

            // Try to get from memory first
            if (this._inMemoryBatches.TryGetValue(batchId, out var batch))
            {
                return batch;
            }

            // Try to get from blob storage
            var blobName = $"batches/{batchId}/metadata.json";
            var blobClient = this._containerClient.GetBlobClient(blobName);

            try
            {
                var response = await blobClient.DownloadContentAsync(cancellationToken);
                var batchJson = response.Value.Content.ToString();
                batch = JsonSerializer.Deserialize<BatchRequest>(batchJson);

                if (batch != null)
                {
                    this._inMemoryBatches[batchId] = batch;
                }

                return batch;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                this._logger.LogWarning("Batch {BatchId} not found", batchId);
                return null;
            }
        }

        /// <summary>
        /// Updates a batch.
        /// </summary>
        /// <param name="batch">The batch to update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task UpdateBatchAsync(BatchRequest batch, CancellationToken cancellationToken = default)
        {
            if (batch == null)
            {
                throw new ArgumentNullException(nameof(batch));
            }

            this._logger.LogDebug("Updating batch {BatchId}", batch.Id);

            // Update in memory
            this._inMemoryBatches[batch.Id] = batch;

            // Update in blob storage
            var blobName = $"batches/{batch.Id}/metadata.json";
            var blobClient = this._containerClient.GetBlobClient(blobName);
            var batchJson = JsonSerializer.Serialize(batch);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(batchJson));
            await blobClient.UploadAsync(stream, overwrite: true, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Stores batch results in blob storage.
        /// </summary>
        /// <param name="batchId">The batch identifier.</param>
        /// <param name="results">The results to store.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The blob storage path.</returns>
        public async Task<string> StoreResultsAsync(Guid batchId, object results, CancellationToken cancellationToken = default)
        {
            this._logger.LogInformation("Storing results for batch {BatchId}", batchId);

            var blobName = $"batches/{batchId}/results.json";
            var blobClient = this._containerClient.GetBlobClient(blobName);
            var resultsJson = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(resultsJson));
            await blobClient.UploadAsync(stream, cancellationToken: cancellationToken);

            this._logger.LogInformation("Results stored for batch {BatchId} at {BlobName}", batchId, blobName);
            return blobName;
        }

        /// <summary>
        /// Retrieves batch results from blob storage.
        /// </summary>
        /// <param name="batchId">The batch identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The results.</returns>
        public async Task<object> GetResultsAsync(Guid batchId, CancellationToken cancellationToken = default)
        {
            this._logger.LogDebug("Retrieving results for batch {BatchId}", batchId);

            var blobName = $"batches/{batchId}/results.json";
            var blobClient = this._containerClient.GetBlobClient(blobName);

            try
            {
                var response = await blobClient.DownloadContentAsync(cancellationToken);
                var resultsJson = response.Value.Content.ToString();
                return JsonSerializer.Deserialize<object>(resultsJson);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                this._logger.LogWarning("Results not found for batch {BatchId}", batchId);
                return null;
            }
        }
    }

    /// <summary>
    /// Configuration options for batch storage.
    /// </summary>
    public class BatchStorageOptions
    {
        /// <summary>
        /// Gets or sets the Azure Blob Storage connection string.
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the container name.
        /// </summary>
        public string ContainerName { get; set; } = "batch-processing";
    }
}
