// <copyright file="IAuditQueryService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Kernel.Infrastructure.Services.Audit;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Synaxis.Shared.Kernel.Domain.Models;

/// <summary>
/// Service for querying audit logs with pagination and filtering.
/// </summary>
public interface IAuditQueryService
{
    /// <summary>
    /// Queries audit logs with pagination and filtering.
    /// </summary>
    /// <param name="request">The query request parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paged result of audit log DTOs.</returns>
    Task<PagedResult<AuditLogDto>> QueryLogsAsync(AuditQueryRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific audit log by its identifier.
    /// </summary>
    /// <param name="id">The audit log identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The audit log DTO if found; otherwise, null.</returns>
    Task<AuditLogDto?> GetLogByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets statistics for audit logs within an organization.
    /// </summary>
    /// <param name="organizationId">The organization identifier.</param>
    /// <param name="from">Optional start date filter.</param>
    /// <param name="to">Optional end date filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Aggregated statistics for the audit logs.</returns>
    Task<AuditStatisticsDto> GetStatisticsAsync(Guid organizationId, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
}
