// <copyright file="ControlPlaneStore.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.ControlPlane
{
    using Microsoft.EntityFrameworkCore;
    using Synaxis.InferenceGateway.Application.ControlPlane;
    using Synaxis.InferenceGateway.Application.ControlPlane.Entities;

    public sealed class ControlPlaneStore : IControlPlaneStore
    {
        private readonly ControlPlaneDbContext _dbContext;

        public ControlPlaneStore(ControlPlaneDbContext dbContext)
        {
            this._dbContext = dbContext;
        }

        public async Task<ModelAlias?> GetAliasAsync(Guid tenantId, string alias, CancellationToken cancellationToken = default)
        {
            return await this._dbContext.ModelAliases
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Alias == alias, cancellationToken).ConfigureAwait(false);
        }

        public async Task<ModelCombo?> GetComboAsync(Guid tenantId, string name, CancellationToken cancellationToken = default)
        {
            return await this._dbContext.ModelCombos
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Name == name, cancellationToken).ConfigureAwait(false);
        }

        public async Task<GlobalModel?> GetGlobalModelAsync(string id, CancellationToken cancellationToken = default)
        {
            return await this._dbContext.GlobalModels
                .AsNoTracking()
                .Include(g => g.ProviderModels)
                .FirstOrDefaultAsync(g => g.Id == id, cancellationToken).ConfigureAwait(false);
        }
    }
}
