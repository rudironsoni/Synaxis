// <copyright file="AuditArchivalJob.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Jobs
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Quartz;
    using Synaxis.Infrastructure.Services;

    /// <summary>
    /// Quartz job for archiving old audit logs.
    /// Runs daily at 2 AM to archive logs older than the retention period.
    /// </summary>
    [DisallowConcurrentExecution]
    public class AuditArchivalJob : IJob
    {
        private readonly IAuditArchivalService _archivalService;
        private readonly ILogger<AuditArchivalJob> _logger;
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuditArchivalJob"/> class.
        /// </summary>
        /// <param name="archivalService">The audit archival service.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="configuration">The configuration.</param>
        public AuditArchivalJob(
            IAuditArchivalService archivalService,
            ILogger<AuditArchivalJob> logger,
            IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(archivalService);
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentNullException.ThrowIfNull(configuration);

            this._archivalService = archivalService;
            this._logger = logger;
            this._configuration = configuration;
        }

        /// <inheritdoc/>
        public async Task Execute(IJobExecutionContext context)
        {
            this._logger.LogInformation("Starting audit log archival job");

            try
            {
                // Get retention days from configuration (default: 90 days)
                var retentionDays = this._configuration.GetValue<int?>("Synaxis:AuditLogRetentionDays") ?? 90;

                this._logger.LogInformation(
                    "Archiving audit logs older than {RetentionDays} days",
                    retentionDays);

                // Get count of logs to archive (for logging)
                var archivableCount = await this._archivalService
                    .GetArchivableCountAsync(retentionDays, context.CancellationToken)
                    .ConfigureAwait(false);

                this._logger.LogInformation(
                    "Found {Count} audit logs eligible for archival",
                    archivableCount);

                if (archivableCount > 0)
                {
                    // Perform archival
                    var result = await this._archivalService
                        .ArchiveOldLogsAsync(retentionDays, context.CancellationToken)
                        .ConfigureAwait(false);

                    this._logger.LogInformation(
                        "Audit archival completed: archived {Count} logs to {Path}",
                        result.ArchivedCount,
                        result.ArchivePath ?? "N/A");
                }
                else
                {
                    this._logger.LogInformation("No audit logs to archive");
                }
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Audit log archival job failed");
                throw new JobExecutionException("Audit archival failed", ex, true);
            }
        }

        /// <summary>
        /// Creates the job schedule for daily execution at 2 AM.
        /// </summary>
        /// <returns>The job key and trigger for scheduling.</returns>
        public static (JobKey JobKey, ITrigger Trigger) CreateSchedule()
        {
            var jobKey = new JobKey("AuditArchivalJob", "Audit");
            var trigger = TriggerBuilder.Create()
                .WithIdentity("AuditArchivalTrigger", "Audit")
                .WithCronSchedule("0 0 2 * * ?") // Daily at 2 AM
                .WithDescription("Archives audit logs older than retention period daily at 2 AM")
                .Build();

            return (jobKey, trigger);
        }
    }
}
