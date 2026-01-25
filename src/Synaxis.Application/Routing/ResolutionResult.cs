using Synaxis.Application.Configuration;
using System.Collections.Generic;

namespace Synaxis.Application.Routing;

public record ResolutionResult(string OriginalModelId, CanonicalModelId CanonicalId, List<ProviderConfig> Candidates);
