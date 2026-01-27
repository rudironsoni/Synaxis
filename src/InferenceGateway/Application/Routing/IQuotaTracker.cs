using System.Threading;
using System.Threading.Tasks;

namespace Synaxis.InferenceGateway.Application.Routing;

public interface IQuotaTracker
{
    Task<bool> CheckQuotaAsync(string providerKey, CancellationToken cancellationToken = default);
    Task<bool> IsHealthyAsync(string providerKey, CancellationToken cancellationToken = default);
    Task RecordUsageAsync(string providerKey, long inputTokens, long outputTokens, CancellationToken cancellationToken = default);
}
