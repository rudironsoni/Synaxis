// <copyright file="NotificationService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.ControlPlane
{
    using Microsoft.Extensions.Logging;
    using Synaxis.InferenceGateway.Application.ControlPlane;

    /// <summary>
    /// Stub implementation of notification service.
    /// In production, this should integrate with email/SMS/push notification providers.
    /// </summary>
    /// <summary>
    /// NotificationService class.
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationService"/> class.
        /// </summary>
        /// <param name="logger">Logger instance for notification service.</param>
        public NotificationService(ILogger<NotificationService> logger)
        {
            this._logger = logger;
        }

        /// <inheritdoc/>
        public Task SendQuotaWarningAsync(
            string tenantId,
            string userId,
            string providerKey,
            int remainingQuota,
            CancellationToken cancellationToken = default)
        {
            this._logger.LogWarning(
                "Quota warning for tenant {TenantId}, user {UserId}, provider {ProviderKey}: {RemainingQuota} remaining",
                tenantId,
                userId,
                providerKey,
                remainingQuota);
            return Task.CompletedTask;
        }
    }
}
