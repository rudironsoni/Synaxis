// <copyright file="ProviderRegistry.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;
using Synaxis.InferenceGateway.Application.Configuration;

/// <summary>
/// Provider registry implementation.
/// </summary>
public class ProviderRegistry : IProviderRegistry
{
    private readonly SynaxisConfiguration config;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderRegistry"/> class.
    /// </summary>
    /// <param name="config">Synaxis configuration options.</param>
    public ProviderRegistry(IOptions<SynaxisConfiguration> config)
    {
        this.config = config?.Value ?? throw new ArgumentNullException(nameof(config));
    }

    /// <inheritdoc/>
    public IEnumerable<(string ServiceKey, int Tier)> GetCandidates(string modelId)
    {
        if (modelId == null)
        {
            throw new ArgumentNullException(nameof(modelId));
        }

        var providers = this.config.Providers?
            .Where(p => p.Value?.Enabled == true)
            .ToList() ?? new List<KeyValuePair<string, ProviderConfig>>();

        if (!providers.Any())
        {
            return Enumerable.Empty<(string, int)>();
        }

        // First, try exact case-insensitive matches
        var exactMatches = providers
            .Where(p => p.Value.Models?.Any(m => string.Equals(m, modelId, StringComparison.OrdinalIgnoreCase)) == true)
            .Select(p => (p.Key, p.Value.Tier));

        if (exactMatches.Any())
        {
            return exactMatches;
        }

        // If no exact matches, try wildcard providers
        var wildcardMatches = providers
            .Where(p => p.Value.Models?.Contains("*", StringComparer.Ordinal) == true)
            .Select(p => (p.Key, p.Value.Tier));

        return wildcardMatches;
    }

    /// <inheritdoc/>
    public ProviderConfig? GetProvider(string serviceKey)
    {
        if (serviceKey == null)
        {
            throw new ArgumentNullException(nameof(serviceKey));
        }

        if (string.IsNullOrEmpty(serviceKey))
        {
            return null;
        }

        if (this.config.Providers?.TryGetValue(serviceKey, out var provider) == true && provider?.Enabled == true)
        {
            provider.Key = serviceKey;
            return provider;
        }

        return null;
    }
}
