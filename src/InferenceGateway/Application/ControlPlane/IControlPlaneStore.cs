using Synaxis.InferenceGateway.Application.ControlPlane.Entities;

namespace Synaxis.InferenceGateway.Application.ControlPlane;

public interface IControlPlaneStore
{
    Task<ModelAlias?> GetAliasAsync(Guid tenantId, string alias, CancellationToken cancellationToken = default);
    Task<ModelCombo?> GetComboAsync(Guid tenantId, string name, CancellationToken cancellationToken = default);
}