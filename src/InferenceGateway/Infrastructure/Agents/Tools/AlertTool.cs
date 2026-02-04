using Microsoft.Extensions.Logging;

namespace Synaxis.InferenceGateway.Infrastructure.Agents.Tools;

public class AlertTool : IAlertTool
{
    private readonly ILogger<AlertTool> _logger;

    public AlertTool(ILogger<AlertTool> logger)
    {
        _logger = logger;
    }

    public async Task SendAdminAlertAsync(string subject, string message, AlertSeverity severity, CancellationToken ct = default)
    {
        // TODO: Implement actual alert mechanism (email, Slack, etc.)
        _logger.LogWarning("[ADMIN ALERT][{Severity}] {Subject}: {Message}", severity, subject, message);
        await Task.CompletedTask;
    }

    public async Task SendNotificationAsync(Guid? userId, Guid? organizationId, string message, CancellationToken ct = default)
    {
        // TODO: Implement actual notification mechanism
        _logger.LogInformation("[NOTIFICATION] UserId={UserId}, OrgId={OrgId}, Message={Message}", 
            userId, organizationId, message);
        await Task.CompletedTask;
    }
}
