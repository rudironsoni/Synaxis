using System.Collections.Generic;

namespace Synaxis.InferenceGateway.Infrastructure.External.ModelsDev.Dto;

// Root object is a dictionary keyed by provider id/name
public sealed class ModelsDevResponse : Dictionary<string, ProviderDto>
{
}
