using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Synaxis.InferenceGateway.Application.Routing;

public interface ISmartRouter
{
    Task<List<EnrichedCandidate>> GetCandidatesAsync(string modelId, bool streaming, CancellationToken cancellationToken = default);
}
