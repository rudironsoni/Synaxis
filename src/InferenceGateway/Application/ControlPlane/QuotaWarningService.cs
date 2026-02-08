// <copyright file="QuotaWarningService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ControlPlane
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Synaxis.InferenceGateway.Application.Routing;

    /// <summary>
    /// Implements quota warning monitoring.
    /// </summary>
    public class QuotaWarningService : IQuotaWarningService
    {
        private const double WarningThreshold = 0.2; // 20%
        private readonly IQuotaTracker _quotaTracker;
        private readonly INotificationService notificationService;
        private readonly ILogger<QuotaWarningService> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="QuotaWarningService"/> class.
        /// </summary>
        /// <param name="quotaTracker">The quota tracker service.</param>
        /// <param name="notificationService">The notification service.</param>
        /// <param name="logger">The logger instance.</param>
        public QuotaWarningService(
            IQuotaTracker quotaTracker,
            INotificationService notificationService,
            ILogger<QuotaWarningService> logger)
        {
            this._quotaTracker = quotaTracker ?? throw new ArgumentNullException(nameof(quotaTracker));
            this.notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Checks if quota is below warning threshold and sends warning if needed.
        /// </summary>
        /// <param name="providerKey">The provider key to check.</param>
        /// <param name="tenantId">The optional tenant ID.</param>
        /// <param name="userId">The optional user ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if warning was sent, false otherwise.</returns>
        public async Task<bool> CheckQuotaWarningAsync(
            string providerKey,
            string? tenantId = null,
            string? userId = null,
            CancellationToken cancellationToken = default)
        {
            // Interim implementation: IQuotaTracker interface needs a method to query remaining/total quota
            // When available, replace placeholder values with: _quotaTracker.GetQuotaAsync(providerKey, ...)
            _ = this._quotaTracker; // Field stored for future use
            var remainingQuota = 100; // Placeholder - should come from _quotaTracker
            var totalQuota = 1000; // Placeholder - should come from _quotaTracker
            var usageRatio = (double)remainingQuota / totalQuota;

            if (usageRatio < WarningThreshold)
            {
                this.logger.LogWarning("Quota warning for provider '{ProviderKey}': {RemainingQuota}/{TotalQuota} ({UsageRatio:P0}) remaining", providerKey, remainingQuota, totalQuota, usageRatio);

                await this.notificationService.SendQuotaWarningAsync(
                    tenantId ?? "default",
                    userId ?? "default",
                    providerKey,
                    remainingQuota,
                    cancellationToken).ConfigureAwait(false);

                return true;
            }

            return false;
        }
    }
}
