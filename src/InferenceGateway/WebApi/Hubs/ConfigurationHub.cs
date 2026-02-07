// <copyright file="ConfigurationHub.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Hubs
{
    using Microsoft.AspNetCore.SignalR;

    /// <summary>
    /// SignalR hub for real-time configuration updates and notifications.
    /// Enables configuration hot-reload without server restart.
    /// </summary>
    public class ConfigurationHub : Hub
    {
        private readonly ILogger<ConfigurationHub> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationHub"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public ConfigurationHub(ILogger<ConfigurationHub> logger)
        {
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Subscribes the client to configuration updates for a specific tenant and user.
        /// </summary>
        /// <param name="tenantId">Optional tenant ID.</param>
        /// <param name="userId">Optional user ID.</param>
        public async Task SubscribeToConfigurationUpdates(string? tenantId = null, string? userId = null)
        {
            var connectionId = this.Context.ConnectionId;

            if (!string.IsNullOrEmpty(tenantId))
            {
                var groupName = $"tenant-{tenantId}";
                await this.Groups.AddToGroupAsync(connectionId, groupName).ConfigureAwait(false);
                this._logger.LogInformation("Connection {ConnectionId} subscribed to tenant {TenantId} configuration updates", connectionId, tenantId);
            }

            if (!string.IsNullOrEmpty(userId))
            {
                var groupName = $"user-{userId}";
                await this.Groups.AddToGroupAsync(connectionId, groupName).ConfigureAwait(false);
                this._logger.LogInformation("Connection {ConnectionId} subscribed to user {UserId} configuration updates", connectionId, userId);
            }

            await this.Groups.AddToGroupAsync(connectionId, "global").ConfigureAwait(false);
            this._logger.LogInformation("Connection {ConnectionId} subscribed to global configuration updates", connectionId);
        }

        /// <summary>
        /// Notifies clients that a configuration has changed.
        /// </summary>
        /// <param name="configurationType">Type of configuration that changed.</param>
        /// <param name="tenantId">Optional tenant ID.</param>
        /// <param name="userId">Optional user ID.</param>
        public async Task NotifyConfigurationChanged(string configurationType, string? tenantId = null, string? userId = null)
        {
            this._logger.LogInformation("Configuration changed: {ConfigurationType} for tenant {TenantId}, user {UserId}", configurationType, tenantId, userId);

            await this.Clients.Group("global").SendAsync("ConfigurationChanged", configurationType, tenantId, userId).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(tenantId))
            {
                await this.Clients.Group($"tenant-{tenantId}").SendAsync("ConfigurationChanged", configurationType, tenantId, userId).ConfigureAwait(false);
            }

            if (!string.IsNullOrEmpty(userId))
            {
                await this.Clients.Group($"user-{userId}").SendAsync("ConfigurationChanged", configurationType, tenantId, userId).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Notifies clients that a provider's status has changed.
        /// </summary>
        /// <param name="providerKey">The provider key.</param>
        /// <param name="status">The new status (healthy/unhealthy).</param>
        public async Task NotifyProviderStatusChanged(string providerKey, string status)
        {
            this._logger.LogInformation("Provider status changed: {ProviderKey} is now {Status}", providerKey, status);

            await this.Clients.All.SendAsync("ProviderStatusChanged", providerKey, status).ConfigureAwait(false);
        }

        /// <summary>
        /// Notifies clients about quota warnings.
        /// </summary>
        /// <param name="tenantId">The tenant ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <param name="providerKey">The provider key.</param>
        /// <param name="remainingQuota">The remaining quota.</param>
        public async Task NotifyQuotaWarning(string tenantId, string userId, string providerKey, int remainingQuota)
        {
            this._logger.LogWarning("Quota warning for tenant {TenantId}, user {UserId}, provider {ProviderKey}: {RemainingQuota} remaining", tenantId, userId, providerKey, remainingQuota);

            await this.Clients.Group($"tenant-{tenantId}").SendAsync("QuotaWarning", tenantId, userId, providerKey, remainingQuota).ConfigureAwait(false);

            await this.Clients.Group($"user-{userId}").SendAsync("QuotaWarning", tenantId, userId, providerKey, remainingQuota).ConfigureAwait(false);
        }

        /// <summary>
        /// Called when a new client connects to the hub.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public override async Task OnConnectedAsync()
        {
            this._logger.LogInformation("Client connected: {ConnectionId}", this.Context.ConnectionId);
            await base.OnConnectedAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Called when a client disconnects from the hub.
        /// </summary>
        /// <param name="exception">The exception that caused the disconnect, if any.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (exception != null)
            {
                this._logger.LogError(exception, "Client disconnected: {ConnectionId}", this.Context.ConnectionId);
            }
            else
            {
                this._logger.LogInformation("Client disconnected: {ConnectionId}", this.Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
        }
    }
}
