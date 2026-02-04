using Microsoft.Extensions.Logging;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Audit;

namespace Synaxis.InferenceGateway.Infrastructure.Agents.Tools;

public class AuditTool : IAuditTool
{
    private readonly ControlPlaneDbContext _db;
    private readonly ILogger<AuditTool> _logger;

    public AuditTool(ControlPlaneDbContext db, ILogger<AuditTool> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task LogActionAsync(string agentName, string action, Guid? organizationId, Guid? userId, string details, string correlationId, CancellationToken ct = default)
    {
        try
        {
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                Action = $"{agentName}:{action}",
                UserId = userId,
                OrganizationId = organizationId,
                NewValues = System.Text.Json.JsonSerializer.Serialize(new
                {
                    agent = agentName,
                    action,
                    details,
                    correlationId,
                    timestamp = DateTime.UtcNow
                }),
                CreatedAt = DateTime.UtcNow
            };

            _db.AuditLogs.Add(auditLog);
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log action to audit");
        }
    }

    public async Task LogOptimizationAsync(Guid organizationId, string modelId, string oldProvider, string newProvider, decimal savingsPercent, string reason, CancellationToken ct = default)
    {
        try
        {
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                Action = "CostOptimization:ProviderSwitch",
                OrganizationId = organizationId,
                NewValues = System.Text.Json.JsonSerializer.Serialize(new
                {
                    modelId,
                    oldProvider,
                    newProvider,
                    savingsPercent,
                    reason,
                    timestamp = DateTime.UtcNow
                }),
                CreatedAt = DateTime.UtcNow
            };

            _db.AuditLogs.Add(auditLog);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Optimization logged: OrgId={OrgId}, Model={Model}, {Old}->{New}, Savings={Savings}%",
                organizationId, modelId, oldProvider, newProvider, savingsPercent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log optimization");
        }
    }
}
