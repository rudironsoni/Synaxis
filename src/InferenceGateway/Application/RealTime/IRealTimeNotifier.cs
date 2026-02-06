// <copyright file="IRealTimeNotifier.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.RealTime
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for broadcasting real-time notifications to connected clients.
    /// </summary>
    public interface IRealTimeNotifier
    {
        /// <summary>
        /// Notify about provider health status changes.
        /// </summary>
        /// <param name="organizationId">The organization identifier.</param>
        /// <param name="update">The provider health update.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task NotifyProviderHealthChanged(Guid organizationId, ProviderHealthUpdate update);

        /// <summary>
        /// Notify when cost optimization is applied.
        /// </summary>
        /// <param name="organizationId">The organization identifier.</param>
        /// <param name="result">The cost optimization result.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task NotifyCostOptimizationApplied(Guid organizationId, CostOptimizationResult result);

        /// <summary>
        /// Notify when a new model is discovered.
        /// </summary>
        /// <param name="organizationId">The organization identifier.</param>
        /// <param name="result">The model discovery result.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task NotifyModelDiscovered(Guid organizationId, ModelDiscoveryResult result);

        /// <summary>
        /// Notify about security alerts.
        /// </summary>
        /// <param name="organizationId">The organization identifier.</param>
        /// <param name="alert">The security alert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task NotifySecurityAlert(Guid organizationId, SecurityAlert alert);

        /// <summary>
        /// Notify about audit events.
        /// </summary>
        /// <param name="organizationId">The organization identifier.</param>
        /// <param name="event">The audit event.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task NotifyAuditEvent(Guid organizationId, AuditEvent @event);
    }
}
