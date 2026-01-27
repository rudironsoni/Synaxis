using Synaxis.InferenceGateway.Application.ControlPlane.Entities;

namespace Synaxis.InferenceGateway.Application.Routing;

public interface ICostService
{
    Task<ModelCost?> GetCostAsync(string provider, string model, CancellationToken cancellationToken = default);
}