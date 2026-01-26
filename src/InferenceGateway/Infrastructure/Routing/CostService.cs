using Microsoft.EntityFrameworkCore;
using Synaxis.InferenceGateway.Application.ControlPlane.Entities;
using Synaxis.InferenceGateway.Application.Routing;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane;

namespace Synaxis.InferenceGateway.Infrastructure.Routing;

public sealed class CostService : ICostService
{
    private readonly ControlPlaneDbContext _dbContext;

    public CostService(ControlPlaneDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ModelCost?> GetCostAsync(string provider, string model, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ModelCosts
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Provider == provider && c.Model == model, cancellationToken);
    }
}