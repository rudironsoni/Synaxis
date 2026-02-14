// <copyright file="IBatchStorageService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.BatchProcessing.Services
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Synaxis.BatchProcessing.Models;

    /// <summary>
    /// Interface for batch storage services.
    /// </summary>
    public interface IBatchStorageService
    {
        /// <summary>
        /// Creates a new batch.
        /// </summary>
        /// <param name="batch">The batch to create.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task CreateBatchAsync(BatchRequest batch, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a batch by identifier.
        /// </summary>
        /// <param name="batchId">The batch identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The batch, or null if not found.</returns>
        Task<BatchRequest> GetBatchAsync(Guid batchId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates a batch.
        /// </summary>
        /// <param name="batch">The batch to update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateBatchAsync(BatchRequest batch, CancellationToken cancellationToken = default);

        /// <summary>
        /// Stores batch results in blob storage.
        /// </summary>
        /// <param name="batchId">The batch identifier.</param>
        /// <param name="results">The results to store.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The blob storage path.</returns>
        Task<string> StoreResultsAsync(Guid batchId, object results, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves batch results from blob storage.
        /// </summary>
        /// <param name="batchId">The batch identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The results.</returns>
        Task<object> GetResultsAsync(Guid batchId, CancellationToken cancellationToken = default);
    }
}
