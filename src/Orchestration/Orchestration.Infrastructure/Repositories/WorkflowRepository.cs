// <copyright file="WorkflowRepository.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Infrastructure.Repositories;

using Microsoft.Extensions.Logging;
using Synaxis.Abstractions.Cloud;
using Synaxis.Orchestration.Domain.Aggregates;

/// <summary>
/// Repository for workflow aggregates.
/// </summary>
public interface IWorkflowRepository
{
    /// <summary>
    /// Gets a workflow by ID.
    /// </summary>
    Task<OrchestrationWorkflow?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a workflow.
    /// </summary>
    Task SaveAsync(OrchestrationWorkflow workflow, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets workflows by tenant.
    /// </summary>
    Task<IReadOnlyList<OrchestrationWorkflow>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets workflows by status.
    /// </summary>
    Task<IReadOnlyList<OrchestrationWorkflow>> GetByStatusAsync(WorkflowStatus status, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of workflow repository using event sourcing.
/// </summary>
public class WorkflowRepository : IWorkflowRepository
{
    private readonly IEventStore _eventStore;
    private readonly ILogger<WorkflowRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowRepository"/> class.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="logger">The logger.</param>
    public WorkflowRepository(IEventStore eventStore, ILogger<WorkflowRepository> logger)
    {
        this._eventStore = eventStore;
        this._logger = logger;
    }

    /// <inheritdoc/>
    public async Task<OrchestrationWorkflow?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var events = await this._eventStore.ReadStreamAsync(id.ToString(), cancellationToken).ConfigureAwait(false);
        if (events.Count == 0)
        {
            return null;
        }

        var workflow = new OrchestrationWorkflow();
        workflow.LoadFromHistory(events);

        return workflow;
    }

    /// <inheritdoc/>
    public async Task SaveAsync(OrchestrationWorkflow workflow, CancellationToken cancellationToken = default)
    {
        var uncommittedEvents = workflow.GetUncommittedEvents();
        if (uncommittedEvents.Count == 0)
        {
            return;
        }

        var expectedVersion = workflow.Version - uncommittedEvents.Count;

        await this._eventStore.AppendAsync(
            workflow.Id.ToString(),
            uncommittedEvents,
            expectedVersion,
            cancellationToken).ConfigureAwait(false);

        workflow.MarkAsCommitted();

        this._logger.LogDebug(
            "Saved workflow {WorkflowId} with {EventCount} events",
            workflow.Id,
            uncommittedEvents.Count);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<OrchestrationWorkflow>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        // In production, this would query a read model or projection
        return Task.FromResult<IReadOnlyList<OrchestrationWorkflow>>(new List<OrchestrationWorkflow>());
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<OrchestrationWorkflow>> GetByStatusAsync(WorkflowStatus status, CancellationToken cancellationToken = default)
    {
        // In production, this would query a read model or projection
        return Task.FromResult<IReadOnlyList<OrchestrationWorkflow>>(new List<OrchestrationWorkflow>());
    }
}

/// <summary>
/// Repository for saga aggregates.
/// </summary>
public interface ISagaRepository
{
    /// <summary>
    /// Gets a saga by ID.
    /// </summary>
    Task<Saga?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a saga.
    /// </summary>
    Task SaveAsync(Saga saga, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets sagas by tenant.
    /// </summary>
    Task<IReadOnlyList<Saga>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets sagas by status.
    /// </summary>
    Task<IReadOnlyList<Saga>> GetByStatusAsync(SagaStatus status, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of saga repository using event sourcing.
/// </summary>
public class SagaRepository : ISagaRepository
{
    private readonly IEventStore _eventStore;
    private readonly ILogger<SagaRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaRepository"/> class.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="logger">The logger.</param>
    public SagaRepository(IEventStore eventStore, ILogger<SagaRepository> logger)
    {
        this._eventStore = eventStore;
        this._logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Saga?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var events = await this._eventStore.ReadStreamAsync(id.ToString(), cancellationToken).ConfigureAwait(false);
        if (events.Count == 0)
        {
            return null;
        }

        var saga = new Saga();
        saga.LoadFromHistory(events);

        return saga;
    }

    /// <inheritdoc/>
    public async Task SaveAsync(Saga saga, CancellationToken cancellationToken = default)
    {
        var uncommittedEvents = saga.GetUncommittedEvents();
        if (uncommittedEvents.Count == 0)
        {
            return;
        }

        var expectedVersion = saga.Version - uncommittedEvents.Count;

        await this._eventStore.AppendAsync(
            saga.Id.ToString(),
            uncommittedEvents,
            expectedVersion,
            cancellationToken).ConfigureAwait(false);

        saga.MarkAsCommitted();

        this._logger.LogDebug(
            "Saved saga {SagaId} with {EventCount} events",
            saga.Id,
            uncommittedEvents.Count);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<Saga>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Saga>>(new List<Saga>());
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<Saga>> GetByStatusAsync(SagaStatus status, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Saga>>(new List<Saga>());
    }
}

/// <summary>
/// Repository for activity aggregates.
/// </summary>
public interface IActivityRepository
{
    /// <summary>
    /// Gets an activity by ID.
    /// </summary>
    Task<Activity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves an activity.
    /// </summary>
    Task SaveAsync(Activity activity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets activities by workflow.
    /// </summary>
    Task<IReadOnlyList<Activity>> GetByWorkflowAsync(Guid workflowId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets activities by saga.
    /// </summary>
    Task<IReadOnlyList<Activity>> GetBySagaAsync(Guid sagaId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of activity repository using event sourcing.
/// </summary>
public class ActivityRepository : IActivityRepository
{
    private readonly IEventStore _eventStore;
    private readonly ILogger<ActivityRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityRepository"/> class.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="logger">The logger.</param>
    public ActivityRepository(IEventStore eventStore, ILogger<ActivityRepository> logger)
    {
        this._eventStore = eventStore;
        this._logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Activity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var events = await this._eventStore.ReadStreamAsync(id.ToString(), cancellationToken).ConfigureAwait(false);
        if (events.Count == 0)
        {
            return null;
        }

        var activity = new Activity();
        activity.LoadFromHistory(events);

        return activity;
    }

    /// <inheritdoc/>
    public async Task SaveAsync(Activity activity, CancellationToken cancellationToken = default)
    {
        var uncommittedEvents = activity.GetUncommittedEvents();
        if (uncommittedEvents.Count == 0)
        {
            return;
        }

        var expectedVersion = activity.Version - uncommittedEvents.Count;

        await this._eventStore.AppendAsync(
            activity.Id.ToString(),
            uncommittedEvents,
            expectedVersion,
            cancellationToken).ConfigureAwait(false);

        activity.MarkAsCommitted();

        this._logger.LogDebug(
            "Saved activity {ActivityId} with {EventCount} events",
            activity.Id,
            uncommittedEvents.Count);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<Activity>> GetByWorkflowAsync(Guid workflowId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Activity>>(new List<Activity>());
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<Activity>> GetBySagaAsync(Guid sagaId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Activity>>(new List<Activity>());
    }
}
