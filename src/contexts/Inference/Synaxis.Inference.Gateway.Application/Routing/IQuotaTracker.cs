// <copyright file="IQuotaTracker.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Routing
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Tracks quota and health for providers.
    /// </summary>
    public interface IQuotaTracker
    {
        /// <summary>
        /// Checks if a provider has available quota.
        /// </summary>
        /// <param name="providerKey">The provider key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if quota is available, otherwise false.</returns>
        Task<bool> CheckQuotaAsync(string providerKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a provider is healthy.
        /// </summary>
        /// <param name="providerKey">The provider key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if healthy, otherwise false.</returns>
        Task<bool> IsHealthyAsync(string providerKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Records token usage for a provider.
        /// </summary>
        /// <param name="providerKey">The provider key.</param>
        /// <param name="inputTokens">Number of input tokens.</param>
        /// <param name="outputTokens">Number of output tokens.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RecordUsageAsync(string providerKey, long inputTokens, long outputTokens, CancellationToken cancellationToken = default);
    }
}
