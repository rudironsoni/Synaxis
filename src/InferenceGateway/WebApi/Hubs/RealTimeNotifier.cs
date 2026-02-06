// <copyright file="RealTimeNotifier.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Hubs
{
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

    /// <summary>
    /// Initializes a new instance of the <see cref="RealTimeNotifier"/> class.
    /// </summary>
    /// <param name="hubContext">The hub context.</param>
    /// <param name="logger">The logger instance.</param>
    public RealTimeNotifier(
        IHubContext<SynaxisHub> hubContext,
        ILogger<RealTimeNotifier> logger)
        {
            this._hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task NotifyProviderHealthChanged(Guid organizationId, ProviderHealthUpdate update)
        {
            try
            {
                await this._hubContext.Clients.Group(organizationId.ToString())
                    .SendAsync("ProviderHealthChanged", update).ConfigureAwait(false);

                this._logger.LogDebug("Sent ProviderHealthChanged notification to organization {OrganizationId}: {ProviderName} is {Status}", organizationId, update.providerName, update.isHealthy ? "healthy" : "unhealthy");
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Failed to send ProviderHealthChanged notification to organization {OrganizationId}", organizationId);
            }
        }

        /// <inheritdoc/>
        public async Task NotifyCostOptimizationApplied(Guid organizationId, CostOptimizationResult result)
        {
            try
            {
                await this._hubContext.Clients.Group(organizationId.ToString())
                    .SendAsync("CostOptimizationApplied", result).ConfigureAwait(false);

                this._logger.LogDebug("Sent CostOptimizationApplied notification to organization {OrganizationId}: {FromProvider} -> {ToProvider}", organizationId, result.fromProvider, result.toProvider);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Failed to send CostOptimizationApplied notification to organization {OrganizationId}", organizationId);
            }
        }

        /// <inheritdoc/>
        public async Task NotifyModelDiscovered(Guid organizationId, ModelDiscoveryResult result)
        {
            try
            {
                await this._hubContext.Clients.Group(organizationId.ToString())
                    .SendAsync("ModelDiscovered", result).ConfigureAwait(false);

                this._logger.LogDebug("Sent ModelDiscovered notification to organization {OrganizationId}: {ModelName}", organizationId, result.displayName);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Failed to send ModelDiscovered notification to organization {OrganizationId}", organizationId);
            }
        }

        /// <inheritdoc/>
        public async Task NotifySecurityAlert(Guid organizationId, SecurityAlert alert)
        {
            try
            {
                await this._hubContext.Clients.Group(organizationId.ToString())
                    .SendAsync("SecurityAlert", alert).ConfigureAwait(false);

                this._logger.LogInformation("Sent SecurityAlert notification to organization {OrganizationId}: {AlertType} - {Severity}", organizationId, alert.alertType, alert.severity);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Failed to send SecurityAlert notification to organization {OrganizationId}", organizationId);
            }
        }

        /// <inheritdoc/>
        public async Task NotifyAuditEvent(Guid organizationId, AuditEvent @event)
        {
            try
            {
                await this._hubContext.Clients.Group(organizationId.ToString())
                    .SendAsync("AuditEvent", @event).ConfigureAwait(false);

                this._logger.LogDebug("Sent AuditEvent notification to organization {OrganizationId}: {Action} on {EntityType}", organizationId, @event.action, @event.entityType);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Failed to send AuditEvent notification to organization {OrganizationId}", organizationId);
            }
        }
    }
}
