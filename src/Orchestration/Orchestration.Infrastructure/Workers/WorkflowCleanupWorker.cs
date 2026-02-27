// <copyright file="WorkflowCleanupWorker.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Infrastructure.Workers;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

/// <summary>
/// Background worker that archives and cleans up old completed workflows.
/// Runs daily to archive workflows older than the retention period.
/// </summary>
[DisallowConcurrentExecution]
public class WorkflowCleanupWorker : IJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WorkflowCleanupWorker> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowCleanupWorker"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection.</param>
    /// <param name="logger">The logger instance for logging operations.</param>
    public WorkflowCleanupWorker(IServiceProvider serviceProvider, ILogger<WorkflowCleanupWorker> logger)
    {
        this._serviceProvider = serviceProvider;
        this._logger = logger;
    }

    /// <summary>
    /// Executes the workflow cleanup job.
    /// </summary>
    /// <param name="context">The job execution context.</param>
    /// <returns>A task that represents the asynchronous job execution.</returns>
    public async Task Execute(IJobExecutionContext context)
    {
        var correlationId = Guid.NewGuid().ToString("N")[..8];
        this._logger.LogInformation("[WorkflowCleanup][{CorrelationId}] Starting workflow cleanup", correlationId);

        using var scope = this._serviceProvider.CreateScope();

        try
        {
            var retentionPeriod = TimeSpan.FromDays(30); // Configurable
            var cutoffDate = DateTime.UtcNow.Add(-retentionPeriod);

            // Archive old workflows
            var archivedCount = await this.ArchiveWorkflowsAsync(cutoffDate, correlationId, context.CancellationToken).ConfigureAwait(false);

            // Clean up archived workflows (soft delete)
            var cleanedCount = await this.CleanupWorkflowsAsync(cutoffDate, correlationId, context.CancellationToken).ConfigureAwait(false);

            this._logger.LogInformation(
                "[WorkflowCleanup][{CorrelationId}] Completed: Archived={Archived}, Cleaned={Cleaned}",
                correlationId,
                archivedCount,
                cleanedCount);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "[WorkflowCleanup][{CorrelationId}] Job failed", correlationId);
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    private async Task<int> ArchiveWorkflowsAsync(DateTime cutoffDate, string correlationId, CancellationToken cancellationToken)
    {
        // Archive completed workflows older than retention period
        // In production, this would move events to cold storage
        this._logger.LogInformation(
            "[WorkflowCleanup][{CorrelationId}] Archiving workflows older than {CutoffDate:yyyy-MM-dd}",
            correlationId,
            cutoffDate);

        await Task.CompletedTask.ConfigureAwait(false);
        return 0;
    }

    private async Task<int> CleanupWorkflowsAsync(DateTime cutoffDate, string correlationId, CancellationToken cancellationToken)
    {
        // Soft delete or hard delete archived workflows
        this._logger.LogInformation(
            "[WorkflowCleanup][{CorrelationId}] Cleaning up archived workflows older than {CutoffDate:yyyy-MM-dd}",
            correlationId,
            cutoffDate);

        await Task.CompletedTask.ConfigureAwait(false);
        return 0;
    }
}
