// <copyright file="CostSavingsRepository.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Billing.Infrastructure.Repositories;

using Billing.Domain.Entities;
using Billing.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Repository for cost savings record operations.
/// </summary>
public class CostSavingsRepository : ICostSavingsRepository
{
    private readonly BillingDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="CostSavingsRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public CostSavingsRepository(BillingDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task<CostSavingsRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.CostSavingsRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CostSavingsRecord?> GetByOptimizationDetailsAsync(
        Guid organizationId,
        string resourceType,
        string? resourceId,
        DateTime appliedAt,
        CancellationToken cancellationToken = default)
    {
        return await _context.CostSavingsRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.OrganizationId == organizationId &&
                     c.ResourceType == resourceType &&
                     c.ResourceId == resourceId &&
                     c.AppliedAt == appliedAt,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CostSavingsRecord>> GetUnappliedByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return await _context.CostSavingsRecords
            .Where(c => c.OrganizationId == organizationId && !c.IsAppliedToInvoice)
            .OrderBy(c => c.AppliedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CostSavingsRecord>> GetByOrganizationAndDateRangeAsync(
        Guid organizationId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        return await _context.CostSavingsRecords
            .Where(c => c.OrganizationId == organizationId && c.AppliedAt >= from && c.AppliedAt <= to)
            .OrderByDescending(c => c.AppliedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<decimal> GetTotalSavingsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _context.CostSavingsRecords
            .Where(c => c.OrganizationId == organizationId)
            .SumAsync(c => c.SavingsAmount, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(CostSavingsRecord costSavings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(costSavings);
        await _context.CostSavingsRecords.AddAsync(costSavings, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(CostSavingsRecord costSavings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(costSavings);
        _context.CostSavingsRecords.Update(costSavings);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task MarkAsAppliedAsync(Guid costSavingsId, Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var record = await _context.CostSavingsRecords.FindAsync(new object[] { costSavingsId }, cancellationToken);
        if (record != null)
        {
            record.IsAppliedToInvoice = true;
            record.AppliedInvoiceId = invoiceId;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
