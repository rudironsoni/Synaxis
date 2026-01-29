using System.Collections.Generic;

namespace Synaxis.InferenceGateway.Infrastructure.External.ModelsDev.Dto;

public sealed class ProviderDto
{
    public Dictionary<string, ModelDto>? models { get; set; }
}
