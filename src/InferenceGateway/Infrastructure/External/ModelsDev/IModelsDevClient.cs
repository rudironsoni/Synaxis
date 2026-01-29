using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Synaxis.InferenceGateway.Infrastructure.External.ModelsDev.Dto;

namespace Synaxis.InferenceGateway.Infrastructure.External.ModelsDev;

public interface IModelsDevClient
{
    Task<List<ModelDto>> GetAllModelsAsync(CancellationToken ct);
}
