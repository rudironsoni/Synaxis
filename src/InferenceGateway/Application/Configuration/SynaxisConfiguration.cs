using System.Collections.Generic;

namespace Synaxis.InferenceGateway.Application.Configuration;

public class SynaxisConfiguration
{
    public Dictionary<string, ProviderConfig> Providers { get; set; } = new();
    public List<CanonicalModelConfig> CanonicalModels { get; set; } = new();
    public Dictionary<string, AliasConfig> Aliases { get; set; } = new();
}

public class ProviderConfig
{
    public string? Key { get; set; }
    public string? AccountId { get; set; } // For Cloudflare
    public int Tier { get; set; }
    public List<string> Models { get; set; } = new();
    public string Type { get; set; } = string.Empty; // "OpenAI", "Groq", "Cohere", "Cloudflare", etc.
    public string? Endpoint { get; set; } // Optional override
}

public class CanonicalModelConfig
{
    public string Id { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string ModelPath { get; set; } = string.Empty;
    public bool Streaming { get; set; }
    public bool Tools { get; set; }
    public bool Vision { get; set; }
    public bool StructuredOutput { get; set; }
    public bool LogProbs { get; set; }
}

public class AliasConfig
{
    public List<string> Candidates { get; set; } = new();
}
