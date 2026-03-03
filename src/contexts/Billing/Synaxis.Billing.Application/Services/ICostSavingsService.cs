// <copyright file="ICostSavingsService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Billing.Application.Services;

/// <summary>
/// Service for managing cost savings operations.
/// </summary>
public interface ICostSavingsService
{
    /// <summary>
    /// Gets a summary of cost savings for an organization.
    /// </summary>
    /// <param name="organizationId">The organization ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The cost savings summary.</returns>
    Task<CostSavingsSummaryDto> GetSavingsSummaryAsync(Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed cost savings records for an organization within a date range.
    /// </summary>
    /// <param name="organizationId">The organization ID.</param>
    /// <param name="from">The start date.</param>
    /// <param name="to">The end date.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of cost savings details.</returns>
    Task<IReadOnlyList<CostSavingsDetailDto>> GetSavingsDetailsAsync(
        Guid organizationId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies unapplied cost savings to an invoice.
    /// </summary>
    /// <param name="organizationId">The organization ID.</param>
    /// <param name="invoiceId">The invoice ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The total savings amount applied.</returns>
    Task<decimal> ApplySavingsToInvoiceAsync(Guid organizationId, Guid invoiceId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Data transfer object for cost savings summary.
/// </summary>
public record CostSavingsSummaryDto(
    decimal TotalSavings,
    decimal ThisMonthSavings,
    decimal AverageSavingsPerOptimization,
    int TotalOptimizationsApplied,
    Dictionary<string, decimal> SavingsByResourceType);

/// <summary>
/// Data transfer object for cost savings details.
/// </summary>
public record CostSavingsDetailDto(
    Guid Id,
    string OptimizationType,
    string ResourceType,
    decimal OriginalCost,
    decimal OptimizedCost,
    decimal SavingsAmount,
    decimal SavingsPercentage,
    DateTime AppliedAt,
    bool IsAppliedToInvoice);
