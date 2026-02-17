// <copyright file="WorkflowTimeoutMonitor.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Infrastructure.Workers;

using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Synaxis.Abstractions.Cloud;
using Synaxis.Orchestration.Domain;
using Synaxis.Orchestration.Domain.Aggregates;

/// <summary>
/// Background worker that monitors running workflows and times out stale executions.
/// Runs every 5 minutes to check for workflows that have exceeded their timeout threshold.
/// </summary>
[DisallowConcurrentExecution]
public class WorkflowTimeoutMonitor : IJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WorkflowTimeoutMonitor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowTimeoutMonitor"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection.</param>
    /// <param name="logger">The logger instance for logging operations.</param>
    public WorkflowTimeoutMonitor(IServiceProvider serviceProvider, ILogger<WorkflowTimeoutMonitor> logger)
    {
        this._serviceProvider = serviceProvider;
        this._logger = logger;
    }

    /// <summary>
    /// Executes the workflow timeout monitoring job.
    /// </summary>
    /// <param name="context">The job execution context.</param>
    /// <returns>A task that represents the asynchronous job execution.</returns>
    public async Task Execute(IJobExecutionContext context)
    {
        var correlationId = Guid.NewGuid().ToString("N")[..8];
        this._logger.LogInformation("[WorkflowTimeout][{CorrelationId}] Starting workflow timeout check", correlationId);

        using var scope = this._serviceProvider.CreateScope();
        var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();

        try
        {
            var timedOutCount = await this.ProcessWorkflowTimeoutsAsync(eventStore, correlationId, context.CancellationToken).ConfigureAwait(false);
            this._logger.LogInformation(
                "[WorkflowTimeout][{CorrelationId}] Completed. TimedOut={TimedOut}",
                correlationId,
                timedOutCount);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "[WorkflowTimeout][{CorrelationId}] Job failed", correlationId);
        }
    }

    private async Task<int> ProcessWorkflowTimeoutsAsync(IEventStore eventStore, string correlationId, CancellationToken ct)
    {
        var runningWorkflowIds = await GetRunningWorkflowIdsAsync(eventStore, ct).ConfigureAwait(false);
        this._logger.LogInformation(
            "[WorkflowTimeout][{CorrelationId}] Found {Count} running workflows to check",
            correlationId,
            runningWorkflowIds.Count);

        int timedOutCount = 0;
        var timeoutThreshold = TimeSpan.FromHours(1);

        foreach (var workflowId in runningWorkflowIds)
        {
            var wasTimedOut = await this.TryTimeoutWorkflowAsync(eventStore, workflowId, timeoutThreshold, correlationId, ct).ConfigureAwait(false);
            if (wasTimedOut)
            {
                timedOutCount++;
            }
        }

        return timedOutCount;
    }

    private async Task<bool> TryTimeoutWorkflowAsync(
        IEventStore eventStore,
        Guid workflowId,
        TimeSpan timeoutThreshold,
        string correlationId,
        CancellationToken ct)
    {
        try
        {
            var workflow = await LoadWorkflowAsync(eventStore, workflowId, ct).ConfigureAwait(false);
            if (workflow == null || !ShouldTimeout(workflow, timeoutThreshold))
            {
                return false;
            }

            await this.TimeoutWorkflowAsync(eventStore, workflow, timeoutThreshold, correlationId, ct).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            this._logger.LogError(
                ex,
                "[WorkflowTimeout][{CorrelationId}] Error processing workflow {WorkflowId}",
                correlationId,
                workflowId);
            return false;
        }
    }

    private async Task TimeoutWorkflowAsync(
        IEventStore eventStore,
        OrchestrationWorkflow workflow,
        TimeSpan timeoutThreshold,
        string correlationId,
        CancellationToken ct)
    {
        workflow.Fail($"Workflow timed out after {timeoutThreshold.TotalMinutes} minutes");
        await eventStore.AppendAsync(
            workflow.Id.ToString(),
            workflow.GetUncommittedEvents(),
            workflow.Version - workflow.GetUncommittedEvents().Count,
            ct).ConfigureAwait(false);
        workflow.MarkAsCommitted();

        this._logger.LogWarning(
            "[WorkflowTimeout][{CorrelationId}] Workflow {WorkflowId} timed out",
            correlationId,
            workflow.Id);
    }

    private static async Task<List<Guid>> GetRunningWorkflowIdsAsync(IEventStore eventStore, CancellationToken cancellationToken)
    {
        // Query event store for workflows with Running status
        // This is a simplified implementation - in production, use projections/read models
#pragma warning disable S1172
        _ = eventStore;
        _ = cancellationToken;
#pragma warning restore S1172

        // Return empty for now - implement based on actual event store query capabilities
        await Task.CompletedTask.ConfigureAwait(false);
        return new List<Guid>();
    }

    private static async Task<OrchestrationWorkflow?> LoadWorkflowAsync(IEventStore eventStore, Guid workflowId, CancellationToken ct)
    {
        var events = await eventStore.ReadStreamAsync(workflowId.ToString(), ct).ConfigureAwait(false);
        if (!events.Any())
        {
            return null;
        }

        var workflow = new OrchestrationWorkflow();
        workflow.LoadFromHistory(events);
        return workflow;
    }

    private static bool ShouldTimeout(OrchestrationWorkflow workflow, TimeSpan threshold)
    {
        if (workflow.StartedAt == null)
        {
            return false;
        }

        return DateTime.UtcNow - workflow.StartedAt > threshold;
    }
}
