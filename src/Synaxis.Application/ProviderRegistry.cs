using Microsoft.Extensions.Options;
using Synaxis.Application.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace Synaxis.Application;

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
        _config = config.Value;
    }

    public IEnumerable<(string ServiceKey, int Tier)> GetCandidates(string modelId)
    {
        return _config.Providers
            .Where(p => p.Value.Models.Contains(modelId, StringComparer.OrdinalIgnoreCase) || p.Value.Models.Contains("*"))
            .Select(p => (p.Key, p.Value.Tier));
    }

    public ProviderConfig? GetProvider(string serviceKey)
    {
        if (_config.Providers.TryGetValue(serviceKey, out var provider))
        {
            provider.Key = serviceKey;
            return provider;
        }
        return null;
    }
}
