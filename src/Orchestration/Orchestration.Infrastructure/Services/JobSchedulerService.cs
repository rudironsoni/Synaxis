// <copyright file="JobSchedulerService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Infrastructure.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Synaxis.Abstractions.Cloud;
using Synaxis.Orchestration.Domain.Aggregates;

/// <summary>
/// Background service that schedules and executes jobs from the event store.
/// </summary>
public class JobSchedulerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<JobSchedulerService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobSchedulerService"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="logger">The logger.</param>
    public JobSchedulerService(IServiceProvider serviceProvider, ILogger<JobSchedulerService> logger)
    {
        this._serviceProvider = serviceProvider;
        this._logger = logger;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this._logger.LogInformation("Job Scheduler Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = this._serviceProvider.CreateScope();
                var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();

                await this.ProcessScheduledJobsAsync(eventStore, stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error in Job Scheduler Service");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken).ConfigureAwait(false);
        }

        this._logger.LogInformation("Job Scheduler Service stopped");
    }

    private Task ProcessScheduledJobsAsync(IEventStore eventStore, CancellationToken stoppingToken)
    {
        // In production, this would query a read model or projection
        // for jobs that are scheduled and ready to execute
        this._logger.LogDebug("Checking for scheduled jobs...");
#pragma warning disable S1172
        _ = eventStore;
        _ = stoppingToken;
#pragma warning restore S1172
        return Task.CompletedTask;
    }

    /// <summary>
    /// Schedules a new job for execution.
    /// </summary>
    /// <param name="name">The name of the job.</param>
    /// <param name="jobType">The type of the job.</param>
    /// <param name="payload">The payload data for the job.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="scheduledAt">The scheduled execution time. If null, the job executes immediately.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The scheduled background job.</returns>
    public async Task<BackgroundJob> ScheduleJobAsync(
        string name,
        string jobType,
        string payload,
        Guid tenantId,
        DateTime? scheduledAt = null,
        CancellationToken ct = default)
    {
        using var scope = this._serviceProvider.CreateScope();
        var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();

        var jobId = Guid.NewGuid();
        var job = BackgroundJob.Create(jobId, name, jobType, payload, tenantId, scheduledAt);

        await eventStore.AppendAsync(
            jobId.ToString(),
            job.GetUncommittedEvents(),
            0,
            ct).ConfigureAwait(false);

        job.MarkAsCommitted();

        this._logger.LogInformation(
            "Scheduled job {JobId} of type {JobType} for tenant {TenantId}",
            jobId,
            jobType,
            tenantId);

        return job;
    }

    /// <summary>
    /// Executes a pending job.
    /// </summary>
    /// <param name="jobId">The unique identifier of the job to execute.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ExecuteJobAsync(Guid jobId, CancellationToken ct)
    {
        using var scope = this._serviceProvider.CreateScope();
        var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();

        var job = await this.LoadExecutableJobAsync(eventStore, jobId, ct).ConfigureAwait(false);
        if (job == null)
        {
            return;
        }

        try
        {
            await this.ExecuteAndCompleteJobAsync(eventStore, job, jobId, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await this.HandleJobFailureAsync(eventStore, job, jobId, ex, ct).ConfigureAwait(false);
        }
    }

    private async Task<BackgroundJob?> LoadExecutableJobAsync(IEventStore eventStore, Guid jobId, CancellationToken ct)
    {
        var events = await eventStore.ReadStreamAsync(jobId.ToString(), ct).ConfigureAwait(false);
        if (!events.Any())
        {
            this._logger.LogWarning("Job {JobId} not found", jobId);
            return null;
        }

        var job = new BackgroundJob();
        job.LoadFromHistory(events);

        if (job.Status != BackgroundJobStatus.Pending && job.Status != BackgroundJobStatus.Retrying)
        {
            this._logger.LogWarning("Job {JobId} cannot be executed in status {Status}", jobId, job.Status);
            return null;
        }

        return job;
    }

    private async Task ExecuteAndCompleteJobAsync(IEventStore eventStore, BackgroundJob job, Guid jobId, CancellationToken ct)
    {
        await StartJobAsync(eventStore, job, jobId, ct).ConfigureAwait(false);

        var result = await ExecuteJobLogicAsync(job, ct).ConfigureAwait(false);

        await CompleteJobAsync(eventStore, job, jobId, result, ct).ConfigureAwait(false);
        this._logger.LogInformation("Job {JobId} completed successfully", jobId);
    }

    private static Task StartJobAsync(IEventStore eventStore, BackgroundJob job, Guid jobId, CancellationToken ct)
    {
        job.Start();
        return PersistJobAsync(eventStore, job, jobId, ct);
    }

    private static Task CompleteJobAsync(IEventStore eventStore, BackgroundJob job, Guid jobId, string result, CancellationToken ct)
    {
        job.Complete(result);
        return PersistJobAsync(eventStore, job, jobId, ct);
    }

    private async Task HandleJobFailureAsync(IEventStore eventStore, BackgroundJob job, Guid jobId, Exception ex, CancellationToken ct)
    {
        this._logger.LogError(ex, "Job {JobId} failed", jobId);

        job.Fail(ex.Message);
        await PersistJobAsync(eventStore, job, jobId, ct).ConfigureAwait(false);

        await this.TryScheduleRetryAsync(eventStore, job, jobId, ct).ConfigureAwait(false);
    }

    private async Task TryScheduleRetryAsync(IEventStore eventStore, BackgroundJob job, Guid jobId, CancellationToken ct)
    {
        if (job.RetryCount >= job.MaxRetries)
        {
            return;
        }

        var retryDelay = TimeSpan.FromMinutes(Math.Pow(2, job.RetryCount));
        job.ScheduleRetry(DateTime.UtcNow.Add(retryDelay));
        await PersistJobAsync(eventStore, job, jobId, ct).ConfigureAwait(false);

        this._logger.LogInformation(
            "Job {JobId} scheduled for retry at {RetryAt}",
            jobId,
            DateTime.UtcNow.Add(retryDelay));
    }

    private static async Task PersistJobAsync(IEventStore eventStore, BackgroundJob job, Guid jobId, CancellationToken ct)
    {
        await eventStore.AppendAsync(
            jobId.ToString(),
            job.GetUncommittedEvents(),
            job.Version - job.GetUncommittedEvents().Count,
            ct).ConfigureAwait(false);
        job.MarkAsCommitted();
    }

    private static async Task<string> ExecuteJobLogicAsync(BackgroundJob job, CancellationToken ct)
    {
        // Job execution logic based on job type
        // This is a placeholder - in production, use a job handler registry
        await Task.Delay(100, ct).ConfigureAwait(false);

        return $"Executed {job.JobType} job successfully";
    }
}
