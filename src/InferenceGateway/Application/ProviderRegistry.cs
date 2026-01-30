using Microsoft.Extensions.Options;
using Synaxis.InferenceGateway.Application.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Synaxis.InferenceGateway.Application;

public interface IProviderRegistry
{
    IEnumerable<(string ServiceKey, int Tier)> GetCandidates(string modelId);
    ProviderConfig? GetProvider(string serviceKey);
}

public class ProviderRegistry : IProviderRegistry
{
    private readonly SynaxisConfiguration _config;

    public ProviderRegistry(IOptions<SynaxisConfiguration> config)
    {
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
    }

    public IEnumerable<(string ServiceKey, int Tier)> GetCandidates(string modelId)
    {
        if (modelId == null)
            throw new ArgumentNullException(nameof(modelId));

        var providers = _config.Providers?
            .Where(p => p.Value?.Enabled == true)
            .ToList() ?? new List<KeyValuePair<string, ProviderConfig>>();

        if (!providers.Any())
            return Enumerable.Empty<(string, int)>();

        // First, try exact case-insensitive matches
        var exactMatches = providers
            .Where(p => p.Value.Models?.Any(m => string.Equals(m, modelId, StringComparison.OrdinalIgnoreCase)) == true)
            .Select(p => (p.Key, p.Value.Tier));

        if (exactMatches.Any())
            return exactMatches;

        // If no exact matches, try wildcard providers
        var wildcardMatches = providers
            .Where(p => p.Value.Models?.Contains("*") == true)
            .Select(p => (p.Key, p.Value.Tier));

        return wildcardMatches;
    }

    public ProviderConfig? GetProvider(string serviceKey)
    {
        if (serviceKey == null)
            throw new ArgumentNullException(nameof(serviceKey));

        if (string.IsNullOrEmpty(serviceKey))
            return null;

        if (_config.Providers?.TryGetValue(serviceKey, out var provider) == true && provider?.Enabled == true)
        {
            provider.Key = serviceKey;
            return provider;
        }
        return null;
    }
}
