// <copyright file="AuditQueryService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Application.Services.Audit;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Synaxis.Core.Contracts;
using Synaxis.Core.Models;

/// <summary>
/// Implementation of audit query service.
/// </summary>
public class AuditQueryService : IAuditQueryService
{
    private readonly IAuditLogRepository _auditLogRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditQueryService"/> class.
    /// </summary>
    /// <param name="auditLogRepository">The audit log repository.</param>
    public AuditQueryService(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
    }

    /// <inheritdoc/>
    public Task<PagedResult<AuditLog>> QueryLogsAsync(AuditQueryRequest request, CancellationToken cancellationToken = default)
    {
        // Guard clauses for input validation
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.OrganizationId == Guid.Empty)
        {
            throw new ArgumentException("OrganizationId must be provided.", nameof(request));
        }

        if (request.Page < 1)
        {
            throw new ArgumentException("Page must be greater than 0.", nameof(request));
        }

        if (request.PageSize < 1 || request.PageSize > 1000)
        {
            throw new ArgumentException("PageSize must be between 1 and 1000.", nameof(request));
        }

        if (request.FromDate.HasValue && request.ToDate.HasValue && request.FromDate.Value > request.ToDate.Value)
        {
            throw new ArgumentException("FromDate must be less than or equal to ToDate.", nameof(request));
        }

        // Create search criteria from the request
        var criteria = new AuditSearchCriteria(
            OrganizationId: request.OrganizationId,
            UserId: request.UserId,
            SearchTerm: request.SearchTerm,
            EventType: request.EventType,
            EventCategory: request.EventCategory,
            FromDate: request.FromDate,
            ToDate: request.ToDate,
            Page: request.Page,
            PageSize: request.PageSize,
        );

        // Perform the search using the repository
        return this._auditLogRepository.SearchAsync(criteria, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<AuditLog?> GetLogByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Guard clause for input validation
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Id must be provided.", nameof(id));
        }

        return this._auditLogRepository.GetByIdAsync(id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<AuditStatisticsDto> GetStatisticsAsync(Guid organizationId, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        // Guard clause for input validation
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("OrganizationId must be provided.", nameof(organizationId));
        }

        if (from.HasValue && to.HasValue && from.Value > to.Value)
        {
            throw new ArgumentException("FromDate must be less than or equal to ToDate.", nameof(from));
        }

        // Create search criteria for getting all logs in the date range
        var criteria = new AuditSearchCriteria(
            OrganizationId: organizationId,
            FromDate: from,
            ToDate: to,
            Page: 1,
            PageSize: int.MaxValue,
        ); // Get all logs for statistics

        var result = await this._auditLogRepository.SearchAsync(criteria, cancellationToken).ConfigureAwait(false);

        // Calculate statistics
        var totalEvents = result.TotalCount;
        var eventsByType = new Dictionary<string, int>(StringComparer.Ordinal);
        var eventsByCategory = new Dictionary<string, int>(StringComparer.Ordinal);
        var eventsOverTime = new Dictionary<DateTime, int>();

        foreach (var log in result.Items)
        {
            // Count events by type
            if (!string.IsNullOrEmpty(log.EventType))
            {
                eventsByType[log.EventType] = eventsByType.GetValueOrDefault(log.EventType, 0) + 1;
            }

            // Count events by category
            if (!string.IsNullOrEmpty(log.EventCategory))
            {
                eventsByCategory[log.EventCategory] = eventsByCategory.GetValueOrDefault(log.EventCategory, 0) + 1;
            }

            // Count events by date (group by day)
            var date = log.Timestamp.Date;
            eventsOverTime[date] = eventsOverTime.GetValueOrDefault(date, 0) + 1;
        }

        return new AuditStatisticsDto(totalEvents, eventsByType, eventsByCategory, eventsOverTime);
    }
}
