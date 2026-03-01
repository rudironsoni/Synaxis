// <copyright file="IAuditLogRepository.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Synaxis.Core.Models;

    /// <summary>
    /// Repository interface for audit log persistence and search operations.
    /// </summary>
    public interface IAuditLogRepository
    {
        /// <summary>
        /// Gets an audit log by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the audit log.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The audit log if found; otherwise, null.</returns>
        Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Queries audit logs using the legacy query format.
        /// </summary>
        /// <param name="query">The query parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of matching audit logs.</returns>
        Task<IReadOnlyList<AuditLog>> QueryAsync(AuditQuery query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches audit logs using full-text search with pagination.
        /// </summary>
        /// <param name="criteria">The search criteria including full-text search term.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A paginated result of matching audit logs.</returns>
        Task<PagedResult<AuditLog>> SearchAsync(AuditSearchCriteria criteria, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a new audit log entry.
        /// </summary>
        /// <param name="auditLog">The audit log to add.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds multiple audit log entries in a batch operation.
        /// </summary>
        /// <param name="auditLogs">The audit logs to add.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task AddBatchAsync(IEnumerable<AuditLog> auditLogs, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the timestamp of the most recent audit log for an organization.
        /// </summary>
        /// <param name="organizationId">The organization identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The timestamp of the last log, or null if no logs exist.</returns>
        Task<DateTime?> GetLastLogTimestampAsync(Guid organizationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes all audit logs older than the specified cutoff date.
        /// </summary>
        /// <param name="cutoff">The cutoff date. Logs older than this will be deleted.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The number of records deleted.</returns>
        Task<int> DeleteOlderThanAsync(DateTime cutoff, CancellationToken cancellationToken = default);
    }
}
