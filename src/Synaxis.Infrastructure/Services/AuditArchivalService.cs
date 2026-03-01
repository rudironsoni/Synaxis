// <copyright file="AuditArchivalService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Synaxis.Core.Contracts;
    using Synaxis.Core.Models;

    /// <summary>
    /// Implementation of audit log archival service.
    /// Moves old logs to compressed JSON archive files and deletes them from the database.
    /// </summary>
    public class AuditArchivalService : IAuditArchivalService
    {
        private readonly IAuditLogRepository _repository;
        private readonly ILogger<AuditArchivalService> _logger;
        private readonly string _archiveDirectory;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuditArchivalService"/> class.
        /// </summary>
        /// <param name="repository">The audit log repository.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="logger">The logger.</param>
        public AuditArchivalService(
            IAuditLogRepository repository,
            IConfiguration configuration,
            ILogger<AuditArchivalService> logger)
        {
            ArgumentNullException.ThrowIfNull(repository);
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(logger);

            this._repository = repository;
            this._logger = logger;

            // Get archive directory from configuration or use default
            this._archiveDirectory = configuration["Synaxis:AuditArchiveDirectory"]
                ?? Path.Combine("data", "archives", "audit");

            // Ensure directory exists
            Directory.CreateDirectory(this._archiveDirectory);
        }

        /// <inheritdoc/>
        public async Task<ArchivalResult> ArchiveOldLogsAsync(int retentionDays, CancellationToken cancellationToken = default)
        {
            if (retentionDays < 1)
            {
                throw new ArgumentException("Retention days must be at least 1", nameof(retentionDays));
            }

            var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
            this._logger.LogInformation("Starting archival of audit logs older than {Cutoff}", cutoff);

            // Get logs to archive (we need to query them before deleting)
            var logsToArchive = await this.GetLogsForArchivalAsync(cutoff, cancellationToken).ConfigureAwait(false);

            if (logsToArchive.Count == 0)
            {
                this._logger.LogInformation("No audit logs to archive");
                return new ArchivalResult(0, null);
            }

            // Create archive file
            var archiveFileName = $"audit_logs_{cutoff:yyyyMMdd}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json.gz";
            var archivePath = Path.Combine(this._archiveDirectory, archiveFileName);

            // Export to compressed JSON
            await this.ExportToArchiveAsync(logsToArchive, archivePath, cancellationToken).ConfigureAwait(false);

            // Delete from database
            var deletedCount = await this._repository.DeleteOlderThanAsync(cutoff, cancellationToken).ConfigureAwait(false);

            this._logger.LogInformation(
                "Archived {Count} audit logs to {Path}, deleted {Deleted} from database",
                logsToArchive.Count,
                archivePath,
                deletedCount);

            return new ArchivalResult(deletedCount, archivePath);
        }

        /// <inheritdoc/>
        public async Task<int> GetArchivableCountAsync(int retentionDays, CancellationToken cancellationToken = default)
        {
            if (retentionDays < 1)
            {
                throw new ArgumentException("Retention days must be at least 1", nameof(retentionDays));
            }

            // This is a simplified count - in a real implementation, you might want to add a dedicated count method
            var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
            var criteria = new AuditSearchCriteria(
                FromDate: DateTime.MinValue,
                ToDate: cutoff,
                PageSize: 1);

            var result = await this._repository.SearchAsync(criteria, cancellationToken).ConfigureAwait(false);
            return result.TotalCount;
        }

        /// <summary>
        /// Gets logs that should be archived (older than cutoff).
        /// </summary>
        /// <param name="cutoff">The cutoff date.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>List of logs to archive.</returns>
        private async Task<List<AuditLog>> GetLogsForArchivalAsync(DateTime cutoff, CancellationToken cancellationToken)
        {
            var allLogs = new List<AuditLog>();
            var page = 1;
            const int batchSize = 1000;

            while (true)
            {
                var criteria = new AuditSearchCriteria(
                    FromDate: DateTime.MinValue,
                    ToDate: cutoff,
                    Page: page,
                    PageSize: batchSize);

                var result = await this._repository.SearchAsync(criteria, cancellationToken).ConfigureAwait(false);

                if (result.Items.Count == 0)
                {
                    break;
                }

                allLogs.AddRange(result.Items);

                if (result.Items.Count < batchSize)
                {
                    break;
                }

                page++;
            }

            return allLogs;
        }

        /// <summary>
        /// Exports logs to a compressed JSON archive file.
        /// </summary>
        /// <param name="logs">The logs to export.</param>
        /// <param name="archivePath">The path to the archive file.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private async Task ExportToArchiveAsync(
            List<AuditLog> logs,
            string archivePath,
            CancellationToken cancellationToken)
        {
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };

            // Create a serializable version without circular references
            var exportData = logs.ConvertAll(log => new
            {
                log.Id,
                log.OrganizationId,
                log.UserId,
                log.EventType,
                log.EventCategory,
                log.Action,
                log.ResourceType,
                log.ResourceId,
                log.Metadata,
                log.IpAddress,
                log.UserAgent,
                log.Region,
                log.IntegrityHash,
                log.PreviousHash,
                log.Timestamp,
            });

#pragma warning disable MA0004 // ConfigureAwait not applicable to IAsyncDisposable
            await using var fileStream = File.Create(archivePath);
            await using var gzipStream = new GZipStream(fileStream, CompressionLevel.Optimal);
            await JsonSerializer.SerializeAsync(gzipStream, exportData, jsonOptions, cancellationToken).ConfigureAwait(false);
#pragma warning restore MA0004
        }
    }
}
