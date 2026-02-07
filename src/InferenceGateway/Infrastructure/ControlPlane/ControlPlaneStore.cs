// <copyright file="ControlPlaneStore.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.ControlPlane
{
    using Microsoft.EntityFrameworkCore;
    using Synaxis.InferenceGateway.Application.ControlPlane;
    using Synaxis.InferenceGateway.Application.ControlPlane.Entities;

    /// <summary>
    /// Control plane data store implementation.
    /// </summary>
    public sealed class ControlPlaneStore : IControlPlaneStore
    {
        private readonly ControlPlaneDbContext _dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlPlaneStore"/> class.
        /// </summary>
        /// <param name="dbContext">The database context.</param>
        public ControlPlaneStore(ControlPlaneDbContext dbContext)
        {
            this._dbContext = dbContext;
        }

        /// <inheritdoc/>
        public Task<ModelAlias?> GetAliasAsync(Guid tenantId, string alias, CancellationToken cancellationToken = default)
        {
            return this._dbContext.ModelAliases
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Alias == alias, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<ModelCombo?> GetComboAsync(Guid tenantId, string name, CancellationToken cancellationToken = default)
        {
            return this._dbContext.ModelCombos
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Name == name, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<GlobalModel?> GetGlobalModelAsync(string id, CancellationToken cancellationToken = default)
        {
            return this._dbContext.GlobalModels
                .AsNoTracking()
                .Include(g => g.ProviderModels)
                .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
        }
    }
}
