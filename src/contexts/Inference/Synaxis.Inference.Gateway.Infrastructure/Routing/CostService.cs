// <copyright file="CostService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Routing
{
    using Microsoft.EntityFrameworkCore;
    using Synaxis.InferenceGateway.Application.ControlPlane.Entities;
    using Synaxis.InferenceGateway.Application.Routing;
    using Synaxis.InferenceGateway.Infrastructure.ControlPlane;

    /// <summary>
    /// CostService class.
    /// </summary>
    public sealed class CostService : ICostService
    {
        private readonly ControlPlaneDbContext _dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="CostService"/> class.
        /// </summary>
        /// <param name="dbContext">The dbContext.</param>
        public CostService(ControlPlaneDbContext dbContext)
        {
            ArgumentNullException.ThrowIfNull(dbContext);
            this._dbContext = dbContext;
        }

        /// <inheritdoc/>
        public Task<ModelCost?> GetCostAsync(string provider, string model, CancellationToken cancellationToken = default)
        {
            return this._dbContext.ModelCosts
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Provider == provider && c.Model == model, cancellationToken);
        }
    }
}
