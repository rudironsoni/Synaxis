using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Synaxis.InferenceGateway.Infrastructure.External.OpenAi
{
    public interface IOpenAiModelDiscoveryClient
    {
        Task<List<string>> GetModelsAsync(string baseUrl, string apiKey, CancellationToken ct);
    }
}
