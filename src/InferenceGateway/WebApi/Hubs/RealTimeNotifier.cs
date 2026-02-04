namespace Synaxis.InferenceGateway.WebApi.Hubs;

using Microsoft.AspNetCore.SignalR;
using Synaxis.InferenceGateway.Application.RealTime;

/// <summary>
/// Implementation of real-time notifier using SignalR.
/// Broadcasts updates to connected clients in organization groups.
/// </summary>
public class RealTimeNotifier : IRealTimeNotifier
{
    private readonly IHubContext<SynaxisHub> _hubContext;
    private readonly ILogger<RealTimeNotifier> _logger;

    public RealTimeNotifier(
        IHubContext<SynaxisHub> hubContext,
        ILogger<RealTimeNotifier> logger)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task NotifyProviderHealthChanged(Guid organizationId, ProviderHealthUpdate update)
    {
        try
        {
            await _hubContext.Clients.Group(organizationId.ToString())
                .SendAsync("ProviderHealthChanged", update);
            
            _logger.LogDebug("Sent ProviderHealthChanged notification to organization {OrganizationId}: {ProviderName} is {Status}",
                organizationId, update.ProviderName, update.IsHealthy ? "healthy" : "unhealthy");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send ProviderHealthChanged notification to organization {OrganizationId}",
                organizationId);
        }
    }

    /// <inheritdoc/>
    public async Task NotifyCostOptimizationApplied(Guid organizationId, CostOptimizationResult result)
    {
        try
        {
            await _hubContext.Clients.Group(organizationId.ToString())
                .SendAsync("CostOptimizationApplied", result);
            
            _logger.LogDebug("Sent CostOptimizationApplied notification to organization {OrganizationId}: {FromProvider} -> {ToProvider}",
                organizationId, result.FromProvider, result.ToProvider);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send CostOptimizationApplied notification to organization {OrganizationId}",
                organizationId);
        }
    }

    /// <inheritdoc/>
    public async Task NotifyModelDiscovered(Guid organizationId, ModelDiscoveryResult result)
    {
        try
        {
            await _hubContext.Clients.Group(organizationId.ToString())
                .SendAsync("ModelDiscovered", result);
            
            _logger.LogDebug("Sent ModelDiscovered notification to organization {OrganizationId}: {ModelName}",
                organizationId, result.DisplayName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send ModelDiscovered notification to organization {OrganizationId}",
                organizationId);
        }
    }

    /// <inheritdoc/>
    public async Task NotifySecurityAlert(Guid organizationId, SecurityAlert alert)
    {
        try
        {
            await _hubContext.Clients.Group(organizationId.ToString())
                .SendAsync("SecurityAlert", alert);
            
            _logger.LogInformation("Sent SecurityAlert notification to organization {OrganizationId}: {AlertType} - {Severity}",
                organizationId, alert.AlertType, alert.Severity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SecurityAlert notification to organization {OrganizationId}",
                organizationId);
        }
    }

    /// <inheritdoc/>
    public async Task NotifyAuditEvent(Guid organizationId, AuditEvent @event)
    {
        try
        {
            await _hubContext.Clients.Group(organizationId.ToString())
                .SendAsync("AuditEvent", @event);
            
            _logger.LogDebug("Sent AuditEvent notification to organization {OrganizationId}: {Action} on {EntityType}",
                organizationId, @event.Action, @event.EntityType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send AuditEvent notification to organization {OrganizationId}",
                organizationId);
        }
    }
}
