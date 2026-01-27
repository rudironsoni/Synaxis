namespace Synaxis.InferenceGateway.Application.Security;

public interface IAuditService
{
    Task LogAsync(Guid tenantId, Guid? userId, string action, object? payload, CancellationToken cancellationToken = default);
}