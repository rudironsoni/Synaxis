// <copyright file="IAuditQueryService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Services.Audit;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Synaxis.Core.Models;

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

/// <summary>
/// Request parameters for querying audit logs.
/// </summary>
/// <param name="OrganizationId">The organization identifier (required).</param>
/// <param name="UserId">Optional user identifier filter.</param>
/// <param name="SearchTerm">Optional full-text search term.</param>
/// <param name="EventType">Optional event type filter.</param>
/// <param name="EventCategory">Optional event category filter.</param>
/// <param name="FromDate">Optional start date filter.</param>
/// <param name="ToDate">Optional end date filter.</param>
/// <param name="Page">The page number (1-based).</param>
/// <param name="PageSize">The number of items per page.</param>
public record AuditQueryRequest(
    Guid OrganizationId,
    Guid? UserId = null,
    string? SearchTerm = null,
    string? EventType = null,
    string? EventCategory = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int Page = 1,
    int PageSize = 50)
{
    /// <summary>
    /// Validates the query request.
    /// </summary>
    /// <returns>True if valid; otherwise, false.</returns>
    public bool IsValid()
    {
        if (this.OrganizationId == Guid.Empty)
        {
            return false;
        }

        if (this.Page < 1)
        {
            return false;
        }

        if (this.PageSize < 1 || this.PageSize > 1000)
        {
            return false;
        }

        if (this.FromDate.HasValue && this.ToDate.HasValue && this.FromDate.Value > this.ToDate.Value)
        {
            return false;
        }

        return true;
    }
}

/// <summary>
/// Data transfer object for an audit log entry.
/// </summary>
/// <param name="Id">The unique identifier.</param>
/// <param name="OrganizationId">The organization identifier.</param>
/// <param name="UserId">The user identifier (if applicable).</param>
/// <param name="EventType">The event type.</param>
/// <param name="EventCategory">The event category.</param>
/// <param name="Action">The action performed.</param>
/// <param name="ResourceType">The resource type.</param>
/// <param name="ResourceId">The resource identifier.</param>
/// <param name="Metadata">Additional event metadata.</param>
/// <param name="IpAddress">The IP address.</param>
/// <param name="UserAgent">The user agent string.</param>
/// <param name="Region">The region where the event occurred.</param>
/// <param name="Timestamp">The timestamp when the event occurred.</param>
public record AuditLogDto(
    Guid Id,
    Guid OrganizationId,
    Guid? UserId,
    string EventType,
    string EventCategory,
    string Action,
    string ResourceType,
    string? ResourceId,
    IDictionary<string, object>? Metadata,
    string? IpAddress,
    string? UserAgent,
    string? Region,
    DateTime Timestamp);

/// <summary>
/// Statistics for audit logs.
/// </summary>
/// <param name="TotalEvents">The total number of events.</param>
/// <param name="EventsByType">Events grouped by type.</param>
/// <param name="EventsByCategory">Events grouped by category.</param>
/// <param name="EventsOverTime">Events grouped by date.</param>
public record AuditStatisticsDto(
    int TotalEvents,
    Dictionary<string, int> EventsByType,
    Dictionary<string, int> EventsByCategory,
    Dictionary<DateTime, int> EventsOverTime);
