// <copyright file="IBatchQueueService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.BatchProcessing.Services
{
    using System.Threading;
    using System.Threading.Tasks;
    using Synaxis.BatchProcessing.Models;

    /// <summary>
    /// Interface for batch queue services.
    /// </summary>
    public interface IBatchQueueService
    {
        /// <summary>
        /// Starts the queue processor.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task StartProcessingAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops the queue processor.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task StopProcessingAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Enqueues a batch for processing.
        /// </summary>
        /// <param name="batch">The batch to enqueue.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task EnqueueBatchAsync(BatchRequest batch, CancellationToken cancellationToken = default);
    }
}
