using Synaxis.InferenceGateway.Application.ControlPlane.Entities;

namespace Synaxis.InferenceGateway.Application.ControlPlane;

public interface IDeviationRegistry
{
    Task RegisterAsync(DeviationEntry entry, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DeviationEntry>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(Guid deviationId, DeviationStatus status, CancellationToken cancellationToken = default);
}
