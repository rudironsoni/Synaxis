namespace Synaxis.InferenceGateway.WebApi.Hubs;

using Microsoft.AspNetCore.SignalR;

/// <summary>
/// SignalR hub for real-time configuration updates and notifications.
/// Enables configuration hot-reload without server restart.
/// </summary>
public class ConfigurationHub : Hub
{
    private readonly ILogger<ConfigurationHub> _logger;

    public ConfigurationHub(ILogger<ConfigurationHub> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Subscribes the client to configuration updates for a specific tenant and user.
    /// </summary>
    /// <param name="tenantId">Optional tenant ID</param>
    /// <param name="userId">Optional user ID</param>
    public async Task SubscribeToConfigurationUpdates(string? tenantId = null, string? userId = null)
    {
        var connectionId = Context.ConnectionId;

        if (!string.IsNullOrEmpty(tenantId))
        {
            var groupName = $"tenant-{tenantId}";
            await Groups.AddToGroupAsync(connectionId, groupName);
            _logger.LogInformation("Connection {ConnectionId} subscribed to tenant {TenantId} configuration updates", connectionId, tenantId);
        }

        if (!string.IsNullOrEmpty(userId))
        {
            var groupName = $"user-{userId}";
            await Groups.AddToGroupAsync(connectionId, groupName);
            _logger.LogInformation("Connection {ConnectionId} subscribed to user {UserId} configuration updates", connectionId, userId);
        }

        await Groups.AddToGroupAsync(connectionId, "global");
        _logger.LogInformation("Connection {ConnectionId} subscribed to global configuration updates", connectionId);
    }

    /// <summary>
    /// Notifies clients that a configuration has changed.
    /// </summary>
    /// <param name="configurationType">Type of configuration that changed</param>
    /// <param name="tenantId">Optional tenant ID</param>
    /// <param name="userId">Optional user ID</param>
    public async Task NotifyConfigurationChanged(string configurationType, string? tenantId = null, string? userId = null)
    {
        _logger.LogInformation("Configuration changed: {ConfigurationType} for tenant {TenantId}, user {UserId}", configurationType, tenantId, userId);

        await Clients.Group("global").SendAsync("ConfigurationChanged", configurationType, tenantId, userId);

        if (!string.IsNullOrEmpty(tenantId))
        {
            await Clients.Group($"tenant-{tenantId}").SendAsync("ConfigurationChanged", configurationType, tenantId, userId);
        }

        if (!string.IsNullOrEmpty(userId))
        {
            await Clients.Group($"user-{userId}").SendAsync("ConfigurationChanged", configurationType, tenantId, userId);
        }
    }

    /// <summary>
    /// Notifies clients that a provider's status has changed.
    /// </summary>
    /// <param name="providerKey">The provider key</param>
    /// <param name="status">The new status (healthy/unhealthy)</param>
    public async Task NotifyProviderStatusChanged(string providerKey, string status)
    {
        _logger.LogInformation("Provider status changed: {ProviderKey} is now {Status}", providerKey, status);

        await Clients.All.SendAsync("ProviderStatusChanged", providerKey, status);
    }

    /// <summary>
    /// Notifies clients about quota warnings.
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="userId">The user ID</param>
    /// <param name="providerKey">The provider key</param>
    /// <param name="remainingQuota">The remaining quota</param>
    public async Task NotifyQuotaWarning(string tenantId, string userId, string providerKey, int remainingQuota)
    {
        _logger.LogWarning("Quota warning for tenant {TenantId}, user {UserId}, provider {ProviderKey}: {RemainingQuota} remaining",
            tenantId, userId, providerKey, remainingQuota);

        await Clients.Group($"tenant-{tenantId}").SendAsync("QuotaWarning", tenantId, userId, providerKey, remainingQuota);

        await Clients.Group($"user-{userId}").SendAsync("QuotaWarning", tenantId, userId, providerKey, remainingQuota);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            _logger.LogError(exception, "Client disconnected: {ConnectionId}", Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        }
        await base.OnDisconnectedAsync(exception);
    }
}
