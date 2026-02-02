using Synaxis.InferenceGateway.Application.Configuration;
using Synaxis.InferenceGateway.Application.ControlPlane.Entities;

namespace Synaxis.InferenceGateway.Application.Routing;

public record EnrichedCandidate(ProviderConfig Config, ModelCost? Cost, string CanonicalModelPath)
{
    public string Key => Config.Key!;
    /// <summary>
    /// Determines if this candidate is a free tier provider.
    /// Checks both config flag (static free providers) and cost database (dynamic free tier).
    /// </summary>
    public bool IsFree => Config.IsFree || (Cost?.FreeTier ?? false);
    public decimal CostPerToken => Cost?.CostPerToken ?? decimal.MaxValue;
}
