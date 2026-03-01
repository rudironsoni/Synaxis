// <copyright file="IUsageTrackingRepository.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Billing.Infrastructure.Repositories;

using Billing.Domain.Aggregates.UsageTracking;

/// <summary>
/// Repository interface for usage tracking aggregates.
/// </summary>
public interface IUsageTrackingRepository
{
    /// <summary>
    /// Gets a usage tracking aggregate by its identifier.
    /// </summary>
    /// <param name="id">The usage tracking identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The usage tracking aggregate, or null if not found.</returns>
    Task<UsageTracking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a usage tracking aggregate by organization, resource type, and billing period.
    /// </summary>
    /// <param name="organizationId">The organization identifier.</param>
    /// <param name="resourceType">The resource type.</param>
    /// <param name="billingPeriod">The billing period.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The usage tracking aggregate, or null if not found.</returns>
    Task<UsageTracking?> GetByOrganizationAndPeriodAsync(
        Guid organizationId,
        string resourceType,
        string billingPeriod,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all usage tracking aggregates for an organization.
    /// </summary>
    /// <param name="organizationId">The organization identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of usage tracking aggregates.</returns>
    Task<IReadOnlyList<UsageTracking>> GetByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new usage tracking aggregate.
    /// </summary>
    /// <param name="usageTracking">The usage tracking aggregate to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(UsageTracking usageTracking, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing usage tracking aggregate.
    /// </summary>
    /// <param name="usageTracking">The usage tracking aggregate to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(UsageTracking usageTracking, CancellationToken cancellationToken = default);
}
