// <copyright file="AuditLogPartitionJob.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Jobs
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Quartz;
    using Synaxis.InferenceGateway.Infrastructure.ControlPlane;

    /// <summary>
    /// Quartz job for managing AuditLog table partitions.
    /// Creates new partitions for upcoming months and cleans up old partitions beyond retention period.
    /// </summary>
    [DisallowConcurrentExecution]
    public class AuditLogPartitionJob : IJob
    {
        private readonly SynaxisDbContext _dbContext;
        private readonly ILogger<AuditLogPartitionJob> _logger;
        private readonly IConfiguration _configuration;

        public AuditLogPartitionJob(
            SynaxisDbContext dbContext,
            ILogger<AuditLogPartitionJob> logger,
            IConfiguration configuration)
        {
            this._dbContext = dbContext;
            this._logger = logger;
            this._configuration = configuration;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            this._logger.LogInformation("Starting AuditLog partition maintenance job");

            try
            {
                // Step 1: Ensure partitions for current and next month exist
                await this.EnsurePartitionsAsync(context.CancellationToken).ConfigureAwait(false);

                // Step 2: Cleanup old partitions beyond retention period
                await this.CleanupOldPartitionsAsync(context.CancellationToken).ConfigureAwait(false);

                this._logger.LogInformation("AuditLog partition maintenance completed successfully");
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "AuditLog partition maintenance failed");
                throw new JobExecutionException("Partition maintenance failed", ex, true);
            }
        }

        private async Task EnsurePartitionsAsync(CancellationToken cancellationToken)
        {
            this._logger.LogDebug("Ensuring partitions exist for current and next month");

            // Create partition for current month
            var currentMonthResult = await this._dbContext.Database
                .ExecuteSqlRawAsync("SELECT audit.ensure_auditlog_partition(CURRENT_DATE)", cancellationToken).ConfigureAwait(false);
            this._logger.LogInformation("Ensured partition for current month: {Result}", currentMonthResult);

            // Create partition for next month (proactive creation)
            var nextMonthResult = await this._dbContext.Database
                .ExecuteSqlRawAsync("SELECT audit.ensure_auditlog_partition(CURRENT_DATE + INTERVAL '1 month')", cancellationToken).ConfigureAwait(false);
            this._logger.LogInformation("Ensured partition for next month: {Result}", nextMonthResult);
        }

        private async Task CleanupOldPartitionsAsync(CancellationToken cancellationToken)
        {
            var retentionDays = this._configuration.GetValue<int?>("Synaxis:AuditLogRetentionDays") ?? 90;

            this._logger.LogInformation("Cleaning up partitions older than {RetentionDays} days", retentionDays);

            // Query for partitions that would be dropped (dry run first for logging)
            var dryRunSql = @"
                SELECT child.relname AS partition_name,
                       pg_get_expr(child.relminexpr, child.oid) AS partition_bound
                FROM pg_inherits
                JOIN pg_class parent ON pg_inherits.inhparent = parent.oid
                JOIN pg_class child ON pg_inherits.inhrelid = child.oid
                JOIN pg_namespace ns ON parent.relnamespace = ns.oid
                WHERE parent.relname = 'AuditLogs'
                AND ns.nspname = 'audit';
            ";

            var partitions = await this._dbContext.Database
                .SqlQueryRaw<string>(dryRunSql)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            this._logger.LogDebug("Found {Count} existing partitions", partitions.Count);

            // Execute cleanup function
            var cleanupSql = $"SELECT * FROM audit.cleanup_auditlog_partitions({retentionDays})";

            var droppedPartitions = await this._dbContext.Database
                .SqlQueryRaw<string>(cleanupSql)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            if (droppedPartitions.Count > 0)
            {
                this._logger.LogInformation("Dropped {Count} old partitions: {Partitions}",
                    droppedPartitions.Count,
                    string.Join(", ", droppedPartitions));
            }
            else
            {
                this._logger.LogDebug("No partitions needed cleanup");
            }
        }
    }
}
