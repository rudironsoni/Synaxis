using Synaplexer.Contracts.IntegrationEvents;

namespace Synaplexer.Contracts.IntegrationEvents;

public class IntelSignal
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public double Score { get; set; }
}

public record TrendIndexedIntegrationEvent(
    Guid Id,
    DateTime CreatedAt,
    string Source,
    IntelSignal[] Signals) : IntegrationEvent(Id, CreatedAt);
