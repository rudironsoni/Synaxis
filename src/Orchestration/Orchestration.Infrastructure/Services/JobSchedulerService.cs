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

    private async Task ProcessScheduledJobsAsync(IEventStore eventStore, CancellationToken ct)
    {
        // In production, this would query a read model or projection
        // for jobs that are scheduled and ready to execute
        this._logger.LogDebug("Checking for scheduled jobs...");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Schedules a new job for execution.
    /// </summary>
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
    public async Task ExecuteJobAsync(Guid jobId, CancellationToken ct)
    {
        using var scope = this._serviceProvider.CreateScope();
        var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();

        var events = await eventStore.ReadStreamAsync(jobId.ToString(), ct).ConfigureAwait(false);
        if (!events.Any())
        {
            this._logger.LogWarning("Job {JobId} not found", jobId);
            return;
        }

        var job = new BackgroundJob();
        job.LoadFromHistory(events);

        if (job.Status != BackgroundJobStatus.Pending && job.Status != BackgroundJobStatus.Retrying)
        {
            this._logger.LogWarning(
                "Job {JobId} cannot be executed in status {Status}",
                jobId,
                job.Status);
            return;
        }

        try
        {
            job.Start();
            await eventStore.AppendAsync(
                jobId.ToString(),
                job.GetUncommittedEvents(),
                job.Version - job.GetUncommittedEvents().Count,
                ct).ConfigureAwait(false);
            job.MarkAsCommitted();

            // Execute the actual job logic here
            // In production, this would dispatch to a job handler based on job.JobType
            var result = await this.ExecuteJobLogicAsync(job, ct).ConfigureAwait(false);

            job.Complete(result);
            await eventStore.AppendAsync(
                jobId.ToString(),
                job.GetUncommittedEvents(),
                job.Version - job.GetUncommittedEvents().Count,
                ct).ConfigureAwait(false);
            job.MarkAsCommitted();

            this._logger.LogInformation("Job {JobId} completed successfully", jobId);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Job {JobId} failed", jobId);

            job.Fail(ex.Message);
            await eventStore.AppendAsync(
                jobId.ToString(),
                job.GetUncommittedEvents(),
                job.Version - job.GetUncommittedEvents().Count,
                ct).ConfigureAwait(false);
            job.MarkAsCommitted();

            // Schedule retry if applicable
            if (job.RetryCount < job.MaxRetries)
            {
                var retryDelay = TimeSpan.FromMinutes(Math.Pow(2, job.RetryCount));
                job.ScheduleRetry(DateTime.UtcNow.Add(retryDelay));
                await eventStore.AppendAsync(
                    jobId.ToString(),
                    job.GetUncommittedEvents(),
                    job.Version - job.GetUncommittedEvents().Count,
                    ct).ConfigureAwait(false);
                job.MarkAsCommitted();

                this._logger.LogInformation(
                    "Job {JobId} scheduled for retry at {RetryAt}",
                    jobId,
                    DateTime.UtcNow.Add(retryDelay));
            }
        }
    }

    private async Task<string> ExecuteJobLogicAsync(BackgroundJob job, CancellationToken ct)
    {
        // Job execution logic based on job type
        // This is a placeholder - in production, use a job handler registry
        await Task.Delay(100, ct).ConfigureAwait(false);

        return $"Executed {job.JobType} job successfully";
    }
}
