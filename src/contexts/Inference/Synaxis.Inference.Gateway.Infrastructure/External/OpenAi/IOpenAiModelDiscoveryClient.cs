// <copyright file="IOpenAiModelDiscoveryClient.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.External.OpenAi
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for discovering models from OpenAI-compatible API endpoints.
    /// </summary>
    public interface IOpenAiModelDiscoveryClient
    {
        /// <summary>
        /// Retrieves the list of available models from an OpenAI-compatible endpoint.
        /// </summary>
        /// <param name="baseUrl">The base URL of the OpenAI-compatible API.</param>
        /// <param name="apiKey">The API key for authentication.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A list of model identifiers.</returns>
        Task<IList<string>> GetModelsAsync(string baseUrl, string apiKey, CancellationToken ct);
    }
}
