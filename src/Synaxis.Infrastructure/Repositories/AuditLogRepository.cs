// <copyright file="AuditLogRepository.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Synaxis.Core.Contracts;
    using Synaxis.Core.Models;
    using Synaxis.Infrastructure.Data;

    /// <summary>
    /// Repository implementation for audit log persistence and search operations.
    /// Uses PostgreSQL full-text search with tsvector for efficient text queries.
    /// </summary>
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly SynaxisDbContext _context;
        private readonly ILogger<AuditLogRepository> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuditLogRepository"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="logger">The logger.</param>
        public AuditLogRepository(SynaxisDbContext context, ILogger<AuditLogRepository> logger)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(logger);
            this._context = context;
            this._logger = logger;
        }

        /// <inheritdoc/>
        public Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return this._context.AuditLogs
                .AsNoTracking()
                .FirstOrDefaultAsync(al => al.Id == id, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<AuditLog>> QueryAsync(AuditQuery query, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);

            if (query.OrganizationId == Guid.Empty)
            {
                throw new ArgumentException("OrganizationId is required", nameof(query));
            }

            var queryable = this.BuildQueryFilters(query);

            var results = await queryable
                .OrderByDescending(al => al.Timestamp)
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return results;
        }

        /// <inheritdoc/>
        public async Task<PagedResult<AuditLog>> SearchAsync(AuditSearchCriteria criteria, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(criteria);

            if (!criteria.IsValid())
            {
                throw new ArgumentException("Invalid search criteria", nameof(criteria));
            }

            var queryable = this.BuildSearchFilters(criteria);

            // Get total count before pagination
            var totalCount = await queryable.CountAsync(cancellationToken).ConfigureAwait(false);

            // Apply pagination
            var items = await queryable
                .OrderByDescending(al => al.Timestamp)
                .Skip((criteria.Page - 1) * criteria.PageSize)
                .Take(criteria.PageSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            this._logger.LogDebug(
                "Audit log search returned {Count} of {Total} results",
                items.Count,
                totalCount);

            return new PagedResult<AuditLog>(items, totalCount, criteria.Page, criteria.PageSize);
        }

        /// <inheritdoc/>
        public async Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(auditLog);

            await this._context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);
            await this._context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            this._logger.LogDebug("Added audit log {LogId}", auditLog.Id);
        }

        /// <inheritdoc/>
        public async Task AddBatchAsync(IEnumerable<AuditLog> auditLogs, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(auditLogs);

            var logList = auditLogs.ToList();
            if (logList.Count == 0)
            {
                return;
            }

            await this._context.AuditLogs.AddRangeAsync(logList, cancellationToken).ConfigureAwait(false);
            await this._context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            this._logger.LogDebug("Added {Count} audit logs in batch", logList.Count);
        }

        /// <inheritdoc/>
        public async Task<DateTime?> GetLastLogTimestampAsync(Guid organizationId, CancellationToken cancellationToken = default)
        {
            if (organizationId == Guid.Empty)
            {
                throw new ArgumentException("OrganizationId is required", nameof(organizationId));
            }

            var lastLog = await this._context.AuditLogs
                .AsNoTracking()
                .Where(al => al.OrganizationId == organizationId)
                .OrderByDescending(al => al.Timestamp)
                .Select(al => al.Timestamp)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return lastLog == default ? null : lastLog;
        }

        /// <inheritdoc/>
        public async Task<int> DeleteOlderThanAsync(DateTime cutoff, CancellationToken cancellationToken = default)
        {
            var deletedCount = await this._context.AuditLogs
                .Where(al => al.Timestamp < cutoff)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            if (deletedCount > 0)
            {
                this._logger.LogInformation(
                    "Deleted {Count} audit logs older than {Cutoff}",
                    deletedCount,
                    cutoff);
            }

            return deletedCount;
        }

        /// <summary>
        /// Builds query filters for legacy AuditQuery.
        /// </summary>
        /// <param name="query">The query parameters.</param>
        /// <returns>A filtered queryable.</returns>
        private IQueryable<AuditLog> BuildQueryFilters(AuditQuery query)
        {
            var queryable = this._context.AuditLogs
                .AsNoTracking()
                .Where(al => al.OrganizationId == query.OrganizationId);

            if (query.UserId.HasValue)
            {
                queryable = queryable.Where(al => al.UserId == query.UserId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.EventType))
            {
                queryable = queryable.Where(al => al.EventType == query.EventType);
            }

            if (!string.IsNullOrWhiteSpace(query.EventCategory))
            {
                queryable = queryable.Where(al => al.EventCategory == query.EventCategory);
            }

            if (query.StartDate.HasValue)
            {
                queryable = queryable.Where(al => al.Timestamp >= query.StartDate.Value);
            }

            if (query.EndDate.HasValue)
            {
                queryable = queryable.Where(al => al.Timestamp <= query.EndDate.Value);
            }

            return queryable;
        }

        /// <summary>
        /// Builds search filters for AuditSearchCriteria with full-text search support.
        /// </summary>
        /// <param name="criteria">The search criteria.</param>
        /// <returns>A filtered queryable.</returns>
        private IQueryable<AuditLog> BuildSearchFilters(AuditSearchCriteria criteria)
        {
            var queryable = this._context.AuditLogs.AsNoTracking();

            // Apply organization filter
            if (criteria.OrganizationId.HasValue)
            {
                queryable = queryable.Where(al => al.OrganizationId == criteria.OrganizationId.Value);
            }

            // Apply user filter
            if (criteria.UserId.HasValue)
            {
                queryable = queryable.Where(al => al.UserId == criteria.UserId.Value);
            }

            // Apply exact match filters
            if (!string.IsNullOrWhiteSpace(criteria.EventType))
            {
                queryable = queryable.Where(al => al.EventType == criteria.EventType);
            }

            if (!string.IsNullOrWhiteSpace(criteria.EventCategory))
            {
                queryable = queryable.Where(al => al.EventCategory == criteria.EventCategory);
            }

            // Apply date range filters
            if (criteria.FromDate.HasValue)
            {
                queryable = queryable.Where(al => al.Timestamp >= criteria.FromDate.Value);
            }

            if (criteria.ToDate.HasValue)
            {
                queryable = queryable.Where(al => al.Timestamp <= criteria.ToDate.Value);
            }

            // Apply full-text search using PostgreSQL tsvector
            if (!string.IsNullOrWhiteSpace(criteria.SearchTerm))
            {
                var searchQuery = FormatSearchQuery(criteria.SearchTerm);
                queryable = queryable.Where(al =>
                    EF.Functions.ToTsVector("english", al.EventType + " " + al.Action + " " + al.ResourceType + " " + al.ResourceId)
                        .Matches(EF.Functions.ToTsQuery("english", searchQuery)));
            }

            return queryable;
        }

        /// <summary>
        /// Formats a search term for PostgreSQL tsquery.
        /// Converts spaces to AND operators and escapes special characters.
        /// </summary>
        /// <param name="searchTerm">The raw search term.</param>
        /// <returns>A formatted tsquery string.</returns>
        private static string FormatSearchQuery(string searchTerm)
        {
            // Escape special tsquery characters
            var escaped = searchTerm
                .Replace("'", "''", StringComparison.Ordinal)
                .Replace("&", "\\&", StringComparison.Ordinal)
                .Replace("|", "\\|", StringComparison.Ordinal)
                .Replace("!", "\\!", StringComparison.Ordinal)
                .Replace("(", "\\(", StringComparison.Ordinal)
                .Replace(")", "\\)", StringComparison.Ordinal)
                .Replace("*", "\\*", StringComparison.Ordinal);

            // Split by whitespace and join with & (AND operator)
            var terms = escaped.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" & ", terms);
        }
    }
}
