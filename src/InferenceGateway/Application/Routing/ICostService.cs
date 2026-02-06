// <copyright file="ICostService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Routing
{
    using System.Threading;
    using System.Threading.Tasks;
    using Synaxis.InferenceGateway.Application.ControlPlane.Entities;

    /// <summary>
    /// Provides cost information for models.
    /// </summary>
    public interface ICostService
    {
        /// <summary>
        /// Gets the cost information for a specific provider and model.
        /// </summary>
        /// <param name="provider">The provider name.</param>
        /// <param name="model">The model name.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The model cost information, or null if not found.</returns>
        Task<ModelCost?> GetCostAsync(string provider, string model, CancellationToken cancellationToken = default);
    }
}