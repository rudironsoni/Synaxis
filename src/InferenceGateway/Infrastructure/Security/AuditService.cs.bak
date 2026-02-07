// <copyright file="AuditService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Security
{
    using System.Text.Json;
    using Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Audit;
    using Synaxis.InferenceGateway.Application.Security;
    using Synaxis.InferenceGateway.Infrastructure.ControlPlane;

    public sealed class AuditService : IAuditService
    {
        private readonly ControlPlaneDbContext _dbContext;

        public AuditService(ControlPlaneDbContext dbContext)
        {
            if (dbContext is null)
            {
                throw new ArgumentNullException(nameof(dbContext));
            }

            this._dbContext = dbContext;
        }

        public async Task LogAsync(Guid tenantId, Guid? userId, string action, object? payload, CancellationToken cancellationToken = default)
        {
            var log = new AuditLog
            {
                Id = Guid.NewGuid(),
                OrganizationId = tenantId,
                UserId = userId,
                Action = action,
                NewValues = payload != null ? JsonSerializer.Serialize(payload) : null,
                CreatedAt = DateTime.UtcNow,
            };

            this._dbContext.AuditLogs.Add(log);
            await this._dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
