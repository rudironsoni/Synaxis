// <copyright file="ISubscriptionRepository.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Billing.Infrastructure.Repositories
{
    using Billing.Domain.Entities;

    /// <summary>
    /// Repository interface for subscription operations.
    /// </summary>
    public interface ISubscriptionRepository
    {
        /// <summary>
        /// Gets a subscription by its ID.
        /// </summary>
        /// <param name="id">The subscription ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The subscription or null if not found.</returns>
        Task<Subscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the active subscription for an organization.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The active subscription or null if not found.</returns>
        Task<Subscription?> GetActiveByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all subscriptions for an organization.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The list of subscriptions.</returns>
        Task<IReadOnlyList<Subscription>> GetByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all expired subscriptions as of a specific date.
        /// </summary>
        /// <param name="asOfDate">The date to check.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The list of expired subscriptions.</returns>
        Task<IReadOnlyList<Subscription>> GetExpiredAsync(DateTime asOfDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a new subscription.
        /// </summary>
        /// <param name="subscription">The subscription to add.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the operation.</returns>
        Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing subscription.
        /// </summary>
        /// <param name="subscription">The subscription to update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the operation.</returns>
        Task UpdateAsync(Subscription subscription, CancellationToken cancellationToken = default);
    }
}
