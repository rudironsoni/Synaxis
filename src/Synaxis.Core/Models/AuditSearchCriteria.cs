// <copyright file="AuditSearchCriteria.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Models
{
    /// <summary>
    /// Criteria for searching audit logs with full-text search support.
    /// </summary>
    /// <param name="OrganizationId">Filter by organization ID.</param>
    /// <param name="UserId">Filter by user ID.</param>
    /// <param name="SearchTerm">Full-text search term for EventType, Action, ResourceType, and ResourceId.</param>
    /// <param name="EventType">Filter by exact event type.</param>
    /// <param name="EventCategory">Filter by exact event category.</param>
    /// <param name="FromDate">Filter logs from this date (inclusive).</param>
    /// <param name="ToDate">Filter logs to this date (inclusive).</param>
    /// <param name="Page">The page number (1-based).</param>
    /// <param name="PageSize">The number of items per page.</param>
    public record AuditSearchCriteria(
        Guid? OrganizationId = null,
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
        /// Validates the search criteria.
        /// </summary>
        /// <returns>True if the criteria are valid; otherwise, false.</returns>
        public bool IsValid()
        {
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
}
