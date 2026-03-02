// <copyright file="InferenceRepository.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Synaxis.Inference.Domain.Aggregates;

/// <summary>
/// Repository implementation for inference request operations.
/// </summary>
public class InferenceRepository : IInferenceRepository
{
    private readonly InferenceDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="InferenceRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public InferenceRepository(InferenceDbContext context)
    {
        this._context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public Task<InferenceRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return this._context.InferenceRequests
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => e.ToDomain())
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<InferenceRequest>> GetByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await this._context.InferenceRequests
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => e.ToDomain())
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<InferenceRequest>> GetByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await this._context.InferenceRequests
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => e.ToDomain())
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<InferenceRequest>> GetPendingAsync(
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        return await this._context.InferenceRequests
            .AsNoTracking()
            .Where(e => e.Status == InferenceStatus.Pending)
            .OrderBy(e => e.CreatedAt)
            .Take(limit)
            .Select(e => e.ToDomain())
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<InferenceRequest>> GetByStatusAsync(
        InferenceStatus status,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var query = this._context.InferenceRequests
            .AsNoTracking()
            .Where(e => e.Status == status);

        if (tenantId.HasValue)
        {
            query = query.Where(e => e.TenantId == tenantId.Value);
        }

        return await query
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => e.ToDomain())
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task AddAsync(InferenceRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entity = InferenceRequestEntity.FromDomain(request);
        _ = await this._context.InferenceRequests.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        _ = await this._context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(InferenceRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entity = InferenceRequestEntity.FromDomain(request);
        this._context.InferenceRequests.Update(entity);
        _ = await this._context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<InferenceRequest>> GetByDateRangeAsync(
        Guid tenantId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await this._context.InferenceRequests
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId && e.CreatedAt >= startDate && e.CreatedAt <= endDate)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => e.ToDomain())
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
