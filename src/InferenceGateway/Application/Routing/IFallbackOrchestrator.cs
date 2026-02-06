// <copyright file="IFallbackOrchestrator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Routing
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Orchestrates multi-tier fallback for provider selection.
    /// Ensures high availability while respecting user preferences and cost constraints.
    /// </summary>
    public interface IFallbackOrchestrator
    {
        /// <summary>
        /// Executes a request with intelligent multi-tier fallback.
        /// </summary>
        /// <typeparam name="T">The return type of the operation.</typeparam>
        /// <param name="modelId">The model identifier.</param>
        /// <param name="streaming">Whether streaming is required.</param>
        /// <param name="preferredProviderKey">Optional user-preferred provider key.</param>
        /// <param name="operation">The operation to execute with the selected provider.</param>
        /// <param name="tenantId">Optional tenant ID for routing configuration.</param>
        /// <param name="userId">Optional user ID for routing configuration.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result of the operation.</returns>
        Task<T> ExecuteWithFallbackAsync<T>(
            string modelId,
            bool streaming,
            string? preferredProviderKey,
            Func<EnrichedCandidate, Task<T>> operation,
            string? tenantId = null,
            string? userId = null,
            CancellationToken cancellationToken = default);
    }
}
