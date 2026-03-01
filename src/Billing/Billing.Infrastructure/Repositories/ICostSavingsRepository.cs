// <copyright file="ICostSavingsRepository.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Billing.Infrastructure.Repositories;

using Billing.Domain.Entities;

/// <summary>
/// Repository interface for cost savings operations.
/// </summary>
public interface ICostSavingsRepository
{
    /// <summary>
    /// Gets a cost savings record by its ID.
    /// </summary>
    /// <param name="id">The cost savings record ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The cost savings record or null if not found.</returns>
    Task<CostSavingsRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a cost savings record by optimization details (for idempotency checks).
    /// </summary>
    /// <param name="organizationId">The organization ID.</param>
    /// <param name="resourceType">The resource type.</param>
    /// <param name="resourceId">The resource ID.</param>
    /// <param name="appliedAt">The applied timestamp.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The cost savings record or null if not found.</returns>
    Task<CostSavingsRecord?> GetByOptimizationDetailsAsync(
        Guid organizationId,
        string resourceType,
        string? resourceId,
        DateTime appliedAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all unapplied cost savings for an organization.
    /// </summary>
    /// <param name="organizationId">The organization ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of unapplied cost savings records.</returns>
    Task<IReadOnlyList<CostSavingsRecord>> GetUnappliedByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all cost savings for an organization within a date range.
    /// </summary>
    /// <param name="organizationId">The organization ID.</param>
    /// <param name="from">The start date.</param>
    /// <param name="to">The end date.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of cost savings records.</returns>
    Task<IReadOnlyList<CostSavingsRecord>> GetByOrganizationAndDateRangeAsync(
        Guid organizationId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total savings for an organization.
    /// </summary>
    /// <param name="organizationId">The organization ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The total savings amount.</returns>
    Task<decimal> GetTotalSavingsAsync(Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new cost savings record.
    /// </summary>
    /// <param name="costSavings">The cost savings record to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task AddAsync(CostSavingsRecord costSavings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing cost savings record.
    /// </summary>
    /// <param name="costSavings">The cost savings record to update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task UpdateAsync(CostSavingsRecord costSavings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a cost savings record as applied to an invoice.
    /// </summary>
    /// <param name="costSavingsId">The cost savings record ID.</param>
    /// <param name="invoiceId">The invoice ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task MarkAsAppliedAsync(Guid costSavingsId, Guid invoiceId, CancellationToken cancellationToken = default);
}
