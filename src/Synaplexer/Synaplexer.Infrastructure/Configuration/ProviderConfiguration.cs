using System.Collections.Generic;

namespace Synaplexer.Infrastructure.Configuration;

public class ProviderConfiguration
{
    public int Priority { get; set; } = 100;
    public List<string> ApiKeys { get; set; } = new();
    public string? AccountId { get; set; }
}
