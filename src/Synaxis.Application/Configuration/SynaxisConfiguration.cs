using System.Collections.Generic;

namespace Synaxis.Application.Configuration;

public class SynaxisConfiguration
{
    public Dictionary<string, ProviderConfig> Providers { get; set; } = new();
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
