// <copyright file="IAuditArchivalService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Services
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Service for archiving old audit logs to JSON files with compression.
    /// </summary>
    public interface IAuditArchivalService
    {
        /// <summary>
        /// Archives audit logs older than the specified retention period.
        /// </summary>
        /// <param name="retentionDays">The number of days to retain logs. Logs older than this will be archived.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The result of the archival operation.</returns>
        Task<ArchivalResult> ArchiveOldLogsAsync(int retentionDays, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the count of audit logs that would be archived based on retention period.
        /// </summary>
        /// <param name="retentionDays">The number of days to retain logs.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The count of archivable logs.</returns>
        Task<int> GetArchivableCountAsync(int retentionDays, CancellationToken cancellationToken = default);
    }
}
