// <copyright file="DeviationRegistry.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.ControlPlane
{
    using Microsoft.EntityFrameworkCore;
    using Synaxis.InferenceGateway.Application.ControlPlane;
    using Synaxis.InferenceGateway.Application.ControlPlane.Entities;

    public sealed class DeviationRegistry : IDeviationRegistry
    {
        private readonly ControlPlaneDbContext _dbContext;

        public DeviationRegistry(ControlPlaneDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task RegisterAsync(DeviationEntry entry, CancellationToken cancellationToken = default)
        {
            if (entry.Id == Guid.Empty)
            {
                entry.Id = Guid.NewGuid();
            }

            _dbContext.Deviations.Add(entry);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<DeviationEntry>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Deviations
                .AsNoTracking()
                .Where(d => d.TenantId == tenantId)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task UpdateStatusAsync(Guid deviationId, DeviationStatus status, CancellationToken cancellationToken = default)
        {
            var deviation = await _dbContext.Deviations
                .FirstOrDefaultAsync(d => d.Id == deviationId, cancellationToken);

            if (deviation == null)
            {
                return;
            }

            deviation.Status = status;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}