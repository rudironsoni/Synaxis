using System.Threading;
using System.Threading.Tasks;

namespace Synaxis.InferenceGateway.Application.Routing;

public interface IHealthStore
{
    Task<bool> IsHealthyAsync(string providerKey, CancellationToken cancellationToken = default);
    Task MarkFailureAsync(string providerKey, TimeSpan cooldown, CancellationToken cancellationToken = default);
    Task MarkSuccessAsync(string providerKey, CancellationToken cancellationToken = default);
}
