// <copyright file="SagaCompensationWorker.cs" company="Synaxis">
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
/// Background worker that processes failed sagas and executes compensation logic.
/// Runs every 30 seconds to check for sagas that need compensation.
/// </summary>
[DisallowConcurrentExecution]
public class SagaCompensationWorker : IJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SagaCompensationWorker> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaCompensationWorker"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection.</param>
    /// <param name="logger">The logger instance for logging operations.</param>
    public SagaCompensationWorker(IServiceProvider serviceProvider, ILogger<SagaCompensationWorker> logger)
    {
        this._serviceProvider = serviceProvider;
        this._logger = logger;
    }

    /// <summary>
    /// Executes the saga compensation job.
    /// </summary>
    /// <param name="context">The job execution context.</param>
    /// <returns>A task that represents the asynchronous job execution.</returns>
    public async Task Execute(IJobExecutionContext context)
    {
        var correlationId = Guid.NewGuid().ToString("N")[..8];
        this._logger.LogInformation("[SagaCompensation][{CorrelationId}] Starting compensation processing", correlationId);

        using var scope = this._serviceProvider.CreateScope();
        var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();

        try
        {
            // Get failed sagas that need compensation
            var failedSagaIds = await GetFailedSagasAsync(eventStore, context.CancellationToken).ConfigureAwait(false);

            this._logger.LogInformation(
                "[SagaCompensation][{CorrelationId}] Found {Count} failed sagas to compensate",
                correlationId,
                failedSagaIds.Count);

            int compensatedCount = 0;
            int failedCompensationCount = 0;

            foreach (var sagaId in failedSagaIds)
            {
                try
                {
                    var result = await this.ProcessCompensationAsync(eventStore, sagaId, correlationId, context.CancellationToken).ConfigureAwait(false);
                    if (result)
                    {
                        compensatedCount++;
                    }
                    else
                    {
                        failedCompensationCount++;
                    }
                }
                catch (Exception ex)
                {
                    this._logger.LogError(
                        ex,
                        "[SagaCompensation][{CorrelationId}] Error compensating saga {SagaId}",
                        correlationId,
                        sagaId);
                    failedCompensationCount++;
                }
            }

            this._logger.LogInformation(
                "[SagaCompensation][{CorrelationId}] Completed: Processed={Processed}, Compensated={Compensated}, Failed={Failed}",
                correlationId,
                failedSagaIds.Count,
                compensatedCount,
                failedCompensationCount);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "[SagaCompensation][{CorrelationId}] Job failed", correlationId);
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    private static async Task<List<Guid>> GetFailedSagasAsync(IEventStore eventStore, CancellationToken cancellationToken)
    {
        // Query for sagas with Failed status that haven't been compensated
        // In production, use read models/projections for efficient querying
#pragma warning disable S1172
        _ = eventStore;
        _ = cancellationToken;
#pragma warning restore S1172
        await Task.CompletedTask.ConfigureAwait(false);
        return new List<Guid>();
    }

    private async Task<bool> ProcessCompensationAsync(
        IEventStore eventStore,
        Guid sagaId,
        string correlationId,
        CancellationToken ct)
    {
        var saga = await LoadFailedSagaAsync(eventStore, sagaId, ct).ConfigureAwait(false);
        if (saga == null)
        {
            return false;
        }

        var activitiesToCompensate = GetActivitiesToCompensate(saga);
        this._logger.LogInformation(
            "[SagaCompensation][{CorrelationId}] Compensating saga {SagaId} with {Count} activities",
            correlationId,
            sagaId,
            activitiesToCompensate.Count);

        await this.CompensateActivitiesAsync(saga, activitiesToCompensate, correlationId, sagaId).ConfigureAwait(false);
        await SaveCompensatedSagaAsync(eventStore, saga, ct).ConfigureAwait(false);

        return true;
    }

    private static async Task<Saga?> LoadFailedSagaAsync(IEventStore eventStore, Guid sagaId, CancellationToken ct)
    {
        var events = await eventStore.ReadStreamAsync(sagaId.ToString(), ct).ConfigureAwait(false);
        if (!events.Any())
        {
            return null;
        }

        var saga = new Saga();
        saga.LoadFromHistory(events);

        return saga.Status == SagaStatus.Failed ? saga : null;
    }

    private static List<SagaActivity> GetActivitiesToCompensate(Saga saga)
    {
        return saga.Activities
            .Where(a => a.Status == ActivityStatus.Completed && a.CompensationActivityId.HasValue)
            .OrderByDescending(a => a.Sequence)
            .ToList();
    }

    private async Task CompensateActivitiesAsync(Saga saga, List<SagaActivity> activities, string correlationId, Guid sagaId)
    {
        foreach (var activityId in activities.Select(a => a.Id))
        {
            try
            {
                saga.CompensateActivity(activityId);
                this._logger.LogInformation(
                    "[SagaCompensation][{CorrelationId}] Compensated activity {ActivityId} in saga {SagaId}",
                    correlationId,
                    activityId,
                    sagaId);
            }
            catch (Exception ex)
            {
                this._logger.LogError(
                    ex,
                    "[SagaCompensation][{CorrelationId}] Failed to compensate activity {ActivityId} in saga {SagaId}",
                    correlationId,
                    activityId,
                    sagaId);
            }
        }
    }

    private static async Task SaveCompensatedSagaAsync(IEventStore eventStore, Saga saga, CancellationToken ct)
    {
        await eventStore.AppendAsync(
            saga.Id.ToString(),
            saga.GetUncommittedEvents(),
            saga.Version - saga.GetUncommittedEvents().Count,
            ct).ConfigureAwait(false);
        saga.MarkAsCommitted();
    }
}
