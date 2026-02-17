// <copyright file="BackgroundJob.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Domain.Aggregates;

using Synaxis.Infrastructure.EventSourcing;
using Synaxis.Orchestration.Domain.Events;

/// <summary>
/// Aggregate root representing a background job for durable task execution.
/// </summary>
public class BackgroundJob : AggregateRoot
{
    /// <summary>
    /// Gets the job identifier.
    /// </summary>
    public new Guid Id { get; private set; }

    /// <summary>
    /// Gets the job name.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the job type.
    /// </summary>
    public string JobType { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the job payload.
    /// </summary>
    public string Payload { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Gets the current status.
    /// </summary>
    public BackgroundJobStatus Status { get; private set; }

    /// <summary>
    /// Gets the scheduled time.
    /// </summary>
    public DateTime? ScheduledAt { get; private set; }

    /// <summary>
    /// Gets the retry count.
    /// </summary>
    public int RetryCount { get; private set; }

    /// <summary>
    /// Gets the maximum retries.
    /// </summary>
    public int MaxRetries { get; private set; }

    /// <summary>
    /// Gets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the started timestamp.
    /// </summary>
    public DateTime? StartedAt { get; private set; }

    /// <summary>
    /// Gets the completed timestamp.
    /// </summary>
    public DateTime? CompletedAt { get; private set; }

    /// <summary>
    /// Gets the error message if the job failed.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Creates a new background job.
    /// </summary>
    public static BackgroundJob Create(
        Guid id,
        string name,
        string jobType,
        string payload,
        Guid tenantId,
        DateTime? scheduledAt = null,
        int maxRetries = 3)
    {
        var job = new BackgroundJob();
        var @event = new BackgroundJobCreated
        {
            JobId = id,
            Name = name,
            JobType = jobType,
            Payload = payload,
            TenantId = tenantId,
            ScheduledAt = scheduledAt,
            MaxRetries = maxRetries,
            Timestamp = DateTime.UtcNow,
        };

        job.ApplyEvent(@event);
        return job;
    }

    /// <summary>
    /// Starts the job execution.
    /// </summary>
    public void Start()
    {
        if (this.Status != BackgroundJobStatus.Pending && this.Status != BackgroundJobStatus.Retrying)
        {
            throw new InvalidOperationException($"Cannot start job in {this.Status} status.");
        }

        var @event = new BackgroundJobStarted
        {
            JobId = this.Id,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Completes the job successfully.
    /// </summary>
    public void Complete(string result)
    {
        if (this.Status != BackgroundJobStatus.Running)
        {
            throw new InvalidOperationException($"Cannot complete job in {this.Status} status.");
        }

        var @event = new BackgroundJobCompleted
        {
            JobId = this.Id,
            Result = result,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Fails the job.
    /// </summary>
    public void Fail(string errorMessage)
    {
        if (this.Status != BackgroundJobStatus.Running)
        {
            throw new InvalidOperationException($"Cannot fail job in {this.Status} status.");
        }

        var @event = new BackgroundJobFailed
        {
            JobId = this.Id,
            ErrorMessage = errorMessage,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Retries the job after a failure.
    /// </summary>
    public void ScheduleRetry(DateTime retryAt)
    {
        if (this.Status != BackgroundJobStatus.Failed)
        {
            throw new InvalidOperationException($"Cannot retry job in {this.Status} status.");
        }

        if (this.RetryCount >= this.MaxRetries)
        {
            throw new InvalidOperationException("Maximum retry count exceeded.");
        }

        var @event = new BackgroundJobScheduledForRetry
        {
            JobId = this.Id,
            RetryAt = retryAt,
            RetryCount = this.RetryCount + 1,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Cancels the job.
    /// </summary>
    public void Cancel()
    {
        if (this.Status == BackgroundJobStatus.Completed || this.Status == BackgroundJobStatus.Cancelled)
        {
            throw new InvalidOperationException($"Cannot cancel job in {this.Status} status.");
        }

        var @event = new BackgroundJobCancelled
        {
            JobId = this.Id,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <inheritdoc/>
    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case BackgroundJobCreated created:
                this.ApplyCreated(created);
                break;
            case BackgroundJobStarted:
                this.ApplyStarted();
                break;
            case BackgroundJobCompleted completed:
                this.ApplyCompleted(completed);
                break;
            case BackgroundJobFailed failed:
                this.ApplyFailed(failed);
                break;
            case BackgroundJobScheduledForRetry scheduled:
                this.ApplyScheduledForRetry(scheduled);
                break;
            case BackgroundJobCancelled:
                this.ApplyCancelled();
                break;
        }
    }

    private void ApplyCreated(BackgroundJobCreated @event)
    {
        this.Id = @event.JobId;
        this.Name = @event.Name;
        this.JobType = @event.JobType;
        this.Payload = @event.Payload;
        this.TenantId = @event.TenantId;
        this.ScheduledAt = @event.ScheduledAt;
        this.MaxRetries = @event.MaxRetries;
        this.Status = BackgroundJobStatus.Pending;
        this.RetryCount = 0;
        this.CreatedAt = @event.Timestamp;
    }

    private void ApplyStarted()
    {
        this.Status = BackgroundJobStatus.Running;
        this.StartedAt = DateTime.UtcNow;
    }

    private void ApplyCompleted(BackgroundJobCompleted @event)
    {
        this.Status = BackgroundJobStatus.Completed;
        this.CompletedAt = DateTime.UtcNow;
    }

    private void ApplyFailed(BackgroundJobFailed @event)
    {
        this.Status = BackgroundJobStatus.Failed;
        this.ErrorMessage = @event.ErrorMessage;
        this.CompletedAt = DateTime.UtcNow;
    }

    private void ApplyScheduledForRetry(BackgroundJobScheduledForRetry @event)
    {
        this.Status = BackgroundJobStatus.Retrying;
        this.RetryCount = @event.RetryCount;
        this.ScheduledAt = @event.RetryAt;
        this.StartedAt = null;
        this.CompletedAt = null;
        this.ErrorMessage = null;
    }

    private void ApplyCancelled()
    {
        this.Status = BackgroundJobStatus.Cancelled;
        this.CompletedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Represents the status of a background job.
/// </summary>
public enum BackgroundJobStatus
{
    /// <summary>
    /// Job is pending execution.
    /// </summary>
    Pending,

    /// <summary>
    /// Job is currently running.
    /// </summary>
    Running,

    /// <summary>
    /// Job completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Job failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Job is scheduled for retry.
    /// </summary>
    Retrying,

    /// <summary>
    /// Job was cancelled.
    /// </summary>
    Cancelled,
}
