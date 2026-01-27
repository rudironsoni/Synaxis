using Synaxis.InferenceGateway.Application.Configuration;
using Synaxis.InferenceGateway.Application.ControlPlane.Entities;

namespace Synaxis.InferenceGateway.Application.Routing;

public record EnrichedCandidate(ProviderConfig Config, ModelCost? Cost, string CanonicalModelPath)
{
    public string Key => Config.Key!;
    public bool IsFree => Cost?.FreeTier ?? false;
    public decimal CostPerToken => Cost?.CostPerToken ?? decimal.MaxValue;
}
