// <copyright file="ISmartRouter.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Routing
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Routes requests to optimal providers based on model capabilities.
    /// </summary>
    public interface ISmartRouter
    {
        /// <summary>
        /// Gets a list of provider candidates for a model.
        /// </summary>
        /// <param name="modelId">The model identifier.</param>
        /// <param name="streaming">Whether streaming is required.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of enriched candidates sorted by priority.</returns>
        Task<IList<EnrichedCandidate>> GetCandidatesAsync(string modelId, bool streaming, CancellationToken cancellationToken = default);
    }
}
