// <copyright file="IWebhookNotificationService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.BatchProcessing.Services
{
    using System.Threading;
    using System.Threading.Tasks;
    using Synaxis.BatchProcessing.Models;

    /// <summary>
    /// Interface for webhook notification services.
    /// </summary>
    public interface IWebhookNotificationService
    {
        /// <summary>
        /// Sends a completion notification for a batch.
        /// </summary>
        /// <param name="batch">The completed batch.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SendCompletionNotificationAsync(BatchRequest batch, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a failure notification for a batch.
        /// </summary>
        /// <param name="batch">The failed batch.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SendFailureNotificationAsync(BatchRequest batch, string errorMessage, CancellationToken cancellationToken = default);
    }
}
