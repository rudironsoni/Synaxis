// <copyright file="INotificationService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ControlPlane
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Service for sending notifications to clients.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Sends a quota warning notification to clients.
        /// </summary>
        /// <param name="tenantId">The tenant ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <param name="providerKey">The provider key.</param>
        /// <param name="remainingQuota">The remaining quota.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SendQuotaWarningAsync(
            string tenantId,
            string userId,
            string providerKey,
            int remainingQuota,
            CancellationToken cancellationToken = default);
    }
}
