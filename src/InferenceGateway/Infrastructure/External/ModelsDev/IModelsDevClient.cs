// <copyright file="IModelsDevClient.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.External.ModelsDev
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Synaxis.InferenceGateway.Infrastructure.External.ModelsDev.Dto;

    /// <summary>
    /// Interface for retrieving model information from models.dev API.
    /// </summary>
    public interface IModelsDevClient
    {
        /// <summary>
        /// Retrieves all available models from models.dev.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A list of model data transfer objects.</returns>
        Task<IList<ModelDto>> GetAllModelsAsync(CancellationToken ct);
    }
}
