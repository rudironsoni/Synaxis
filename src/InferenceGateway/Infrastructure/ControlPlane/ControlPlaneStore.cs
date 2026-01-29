using Microsoft.EntityFrameworkCore;
using Synaxis.InferenceGateway.Application.ControlPlane;
using Synaxis.InferenceGateway.Application.ControlPlane.Entities;

namespace Synaxis.InferenceGateway.Infrastructure.ControlPlane;

public sealed class ControlPlaneStore : IControlPlaneStore
{
    private readonly ControlPlaneDbContext _dbContext;

    public ControlPlaneStore(ControlPlaneDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ModelAlias?> GetAliasAsync(Guid tenantId, string alias, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ModelAliases
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Alias == alias, cancellationToken);
    }

    public async Task<ModelCombo?> GetComboAsync(Guid tenantId, string name, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ModelCombos
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Name == name, cancellationToken);
    }

    public async Task<GlobalModel?> GetGlobalModelAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GlobalModels
            .AsNoTracking()
            .Include(g => g.ProviderModels)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
    }
}
