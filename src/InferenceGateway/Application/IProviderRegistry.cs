// <copyright file="IProviderRegistry.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application;

using System.Collections.Generic;
using Synaxis.InferenceGateway.Application.Configuration;

/// <summary>
/// Interface for provider registry operations.
/// </summary>
public interface IProviderRegistry
{
    /// <summary>
    /// Gets candidates for the specified model ID.
    /// </summary>
    /// <param name="modelId">The model identifier.</param>
    /// <returns>Enumerable of service key and tier tuples.</returns>
    IEnumerable<(string ServiceKey, int Tier)> GetCandidates(string modelId);

    /// <summary>
    /// Gets the provider configuration for the specified service key.
    /// </summary>
    /// <param name="serviceKey">The service key.</param>
    /// <returns>Provider configuration or null if not found.</returns>
    ProviderConfig? GetProvider(string serviceKey);
}
