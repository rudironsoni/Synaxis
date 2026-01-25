using Synaxis.InferenceGateway.Application.Configuration;
using System.Collections.Generic;

namespace Synaxis.InferenceGateway.Application.Routing;

public record ResolutionResult(string OriginalModelId, CanonicalModelId CanonicalId, List<ProviderConfig> Candidates);
