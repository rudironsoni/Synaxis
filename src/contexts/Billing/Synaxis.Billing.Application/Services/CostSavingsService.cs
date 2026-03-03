// <copyright file="CostSavingsService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Billing.Application.Services;

using Billing.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

/// <summary>
/// Service for managing cost savings operations.
/// </summary>
public class CostSavingsService : ICostSavingsService
{
    private readonly ICostSavingsRepository _costSavingsRepository;
    private readonly ILogger<CostSavingsService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CostSavingsService"/> class.
    /// </summary>
    /// <param name="costSavingsRepository">The cost savings repository.</param>
    /// <param name="logger">The logger.</param>
    public CostSavingsService(
        ICostSavingsRepository costSavingsRepository,
        ILogger<CostSavingsService> logger)
    {
        _costSavingsRepository = costSavingsRepository ?? throw new ArgumentNullException(nameof(costSavingsRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<CostSavingsSummaryDto> GetSavingsSummaryAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var totalSavings = await _costSavingsRepository.GetTotalSavingsAsync(organizationId, cancellationToken);

        var thisMonthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var thisMonthRecords = await _costSavingsRepository.GetByOrganizationAndDateRangeAsync(
            organizationId, thisMonthStart, DateTime.UtcNow, cancellationToken);

        var allRecords = await _costSavingsRepository.GetByOrganizationAndDateRangeAsync(
            organizationId, DateTime.MinValue, DateTime.MaxValue, cancellationToken);

        var savingsByResourceType = allRecords
            .GroupBy(r => r.ResourceType)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.SavingsAmount));

        return new CostSavingsSummaryDto(
            TotalSavings: totalSavings,
            ThisMonthSavings: thisMonthRecords.Sum(r => r.SavingsAmount),
            AverageSavingsPerOptimization: allRecords.Any() ? allRecords.Average(r => r.SavingsAmount) : 0,
            TotalOptimizationsApplied: allRecords.Count,
            SavingsByResourceType: savingsByResourceType);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CostSavingsDetailDto>> GetSavingsDetailsAsync(
        Guid organizationId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        var records = await _costSavingsRepository.GetByOrganizationAndDateRangeAsync(
            organizationId, from, to, cancellationToken);

        return records.Select(r => new CostSavingsDetailDto(
            r.Id,
            r.OptimizationType,
            r.ResourceType,
            r.OriginalCost,
            r.OptimizedCost,
            r.SavingsAmount,
            r.SavingsPercentage,
            r.AppliedAt,
            r.IsAppliedToInvoice)).ToList();
    }

    /// <inheritdoc />
    public async Task<decimal> ApplySavingsToInvoiceAsync(Guid organizationId, Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var unappliedSavings = await _costSavingsRepository.GetUnappliedByOrganizationAsync(
            organizationId, cancellationToken);

        var totalSavings = unappliedSavings.Sum(s => s.SavingsAmount);

        foreach (var savings in unappliedSavings)
        {
            await _costSavingsRepository.MarkAsAppliedAsync(savings.Id, invoiceId, cancellationToken);
        }

        _logger.LogInformation(
            "Applied {Count} cost savings totaling {TotalSavings:C} to invoice {InvoiceId}",
            unappliedSavings.Count,
            totalSavings,
            invoiceId);

        return totalSavings;
    }
}
