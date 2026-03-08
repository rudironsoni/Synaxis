// <copyright file="AuditQueryRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Kernel.Infrastructure.Services.Audit;

using System;

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
