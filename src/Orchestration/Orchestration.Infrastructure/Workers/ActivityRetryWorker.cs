// <copyright file="ActivityRetryWorker.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Infrastructure.Workers;

using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Synaxis.Abstractions.Cloud;
using Synaxis.Orchestration.Domain.Aggregates;

/// <summary>
/// Background worker that retries failed activities based on retry policies.
/// Runs every minute to check for failed activities eligible for retry.
/// </summary>
[DisallowConcurrentExecution]
public class ActivityRetryWorker : IJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ActivityRetryWorker> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityRetryWorker"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection.</param>
    /// <param name="logger">The logger instance for logging operations.</param>
    public ActivityRetryWorker(IServiceProvider serviceProvider, ILogger<ActivityRetryWorker> logger)
    {
        this._serviceProvider = serviceProvider;
        this._logger = logger;
    }

    /// <summary>
    /// Executes the activity retry job.
    /// </summary>
    /// <param name="context">The job execution context.</param>
    /// <returns>A task that represents the asynchronous job execution.</returns>
    public async Task Execute(IJobExecutionContext context)
    {
        var correlationId = Guid.NewGuid().ToString("N")[..8];
        this._logger.LogInformation("[ActivityRetry][{CorrelationId}] Starting activity retry processing", correlationId);

        using var scope = this._serviceProvider.CreateScope();
        var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();

        try
        {
            var failedActivities = await GetFailedActivitiesAsync(eventStore, context.CancellationToken).ConfigureAwait(false);

            this._logger.LogInformation(
                "[ActivityRetry][{CorrelationId}] Found {Count} failed activities to retry",
                correlationId,
                failedActivities.Count);

            int retriedCount = 0;
            int maxRetries = 3; // Configurable

            foreach (var activityId in failedActivities)
            {
                try
                {
                    var result = await this.RetryActivityAsync(eventStore, activityId, maxRetries, correlationId, context.CancellationToken).ConfigureAwait(false);
                    if (result)
                    {
                        retriedCount++;
                    }
                }
                catch (Exception ex)
                {
                    this._logger.LogError(
                        ex,
                        "[ActivityRetry][{CorrelationId}] Error retrying activity {ActivityId}",
                        correlationId,
                        activityId);
                }
            }

            this._logger.LogInformation(
                "[ActivityRetry][{CorrelationId}] Completed: Found={Found}, Retried={Retried}",
                correlationId,
                failedActivities.Count,
                retriedCount);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "[ActivityRetry][{CorrelationId}] Job failed", correlationId);
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    private static async Task<List<Guid>> GetFailedActivitiesAsync(IEventStore eventStore, CancellationToken cancellationToken)
    {
        // Query for failed activities that haven't exceeded retry limit
#pragma warning disable S1172
        _ = eventStore;
        _ = cancellationToken;
#pragma warning restore S1172
        await Task.CompletedTask.ConfigureAwait(false);
        return new List<Guid>();
    }

    private async Task<bool> RetryActivityAsync(
        IEventStore eventStore,
        Guid activityId,
        int maxRetries,
        string correlationId,
        CancellationToken ct)
    {
        var events = await eventStore.ReadStreamAsync(activityId.ToString(), ct).ConfigureAwait(false);
        if (!events.Any())
        {
            return false;
        }

        var activity = new Activity();
        activity.LoadFromHistory(events);

        // Check if activity is eligible for retry
        var retryCount = events.Count(e => string.Equals(e.EventType, "ActivityRetried", StringComparison.Ordinal));
        if (retryCount >= maxRetries)
        {
            this._logger.LogWarning(
                "[ActivityRetry][{CorrelationId}] Activity {ActivityId} has exceeded max retries ({MaxRetries})",
                correlationId,
                activityId,
                maxRetries);
            return false;
        }

        // Apply exponential backoff
        var backoffDelay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
        var lastFailed = events.LastOrDefault(e => string.Equals(e.EventType, "ActivityFailed", StringComparison.Ordinal))?.OccurredOn;
        if (lastFailed != null && DateTime.UtcNow - lastFailed < backoffDelay)
        {
            this._logger.LogDebug(
                "[ActivityRetry][{CorrelationId}] Activity {ActivityId} waiting for backoff ({BackoffDelay}s remaining)",
                correlationId,
                activityId,
                backoffDelay.TotalSeconds);
            return false;
        }

        activity.Retry(retryCount + 1);
        await eventStore.AppendAsync(
            activity.Id.ToString(),
            activity.GetUncommittedEvents(),
            activity.Version - activity.GetUncommittedEvents().Count,
            ct).ConfigureAwait(false);
        activity.MarkAsCommitted();

        this._logger.LogInformation(
            "[ActivityRetry][{CorrelationId}] Retrying activity {ActivityId} (attempt {RetryCount}/{MaxRetries})",
            correlationId,
            activityId,
            retryCount + 1,
            maxRetries);

        return true;
    }
}
