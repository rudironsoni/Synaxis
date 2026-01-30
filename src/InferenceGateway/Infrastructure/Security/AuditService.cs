using System.Text.Json;
using Synaxis.InferenceGateway.Application.ControlPlane.Entities;
using Synaxis.InferenceGateway.Application.Security;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane;

namespace Synaxis.InferenceGateway.Infrastructure.Security;

public sealed class AuditService : IAuditService
{
    private readonly ControlPlaneDbContext _dbContext;

    public AuditService(ControlPlaneDbContext dbContext)
    {
        if (dbContext is null)
        {
            throw new ArgumentNullException(nameof(dbContext));
        }
        
        _dbContext = dbContext;
    }

    public async Task LogAsync(Guid tenantId, Guid? userId, string action, object? payload, CancellationToken cancellationToken = default)
    {
        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            Action = action,
            PayloadJson = payload != null ? JsonSerializer.Serialize(payload) : null,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.AuditLogs.Add(log);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}