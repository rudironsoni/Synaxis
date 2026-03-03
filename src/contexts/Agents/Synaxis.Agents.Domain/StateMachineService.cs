// <copyright file="StateMachineService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Domain;

using Synaxis.Abstractions.Cloud;
using Synaxis.Agents.Domain.Events;

/// <summary>
/// Domain service for managing agent execution state transitions.
/// </summary>
public sealed class StateMachineService : IDisposable
{
    private readonly SemaphoreSlim _semaphore;
    private readonly Dictionary<Guid, AgentStateMachine> _stateMachines;
    private readonly List<IDomainEvent> _domainEvents;

    /// <summary>
    /// Initializes a new instance of the <see cref="StateMachineService"/> class.
    /// </summary>
    /// <param name="maxConcurrentExecutions">The maximum number of concurrent agent executions allowed.</param>
    public StateMachineService(int maxConcurrentExecutions = 10)
    {
        this._semaphore = new SemaphoreSlim(maxConcurrentExecutions, maxConcurrentExecutions);
        this._stateMachines = new Dictionary<Guid, AgentStateMachine>();
        this._domainEvents = new List<IDomainEvent>();
    }

    /// <summary>
    /// Gets the domain events raised during state transitions.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => this._domainEvents.AsReadOnly();

    /// <summary>
    /// Creates a new state machine for the specified execution ID.
    /// </summary>
    /// <param name="executionId">The unique identifier of the agent execution.</param>
    /// <returns>The created state machine.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a state machine for the execution ID already exists.</exception>
    public AgentStateMachine CreateStateMachine(Guid executionId)
    {
        lock (this._stateMachines)
        {
            if (this._stateMachines.ContainsKey(executionId))
            {
                throw new InvalidOperationException($"State machine for execution {executionId} already exists");
            }

            var stateMachine = new AgentStateMachine();
            this._stateMachines[executionId] = stateMachine;
            return stateMachine;
        }
    }

    /// <summary>
    /// Gets the state machine for the specified execution ID.
    /// </summary>
    /// <param name="executionId">The unique identifier of the agent execution.</param>
    /// <returns>The state machine if found; otherwise, <c>null</c>.</returns>
    public AgentStateMachine? GetStateMachine(Guid executionId)
    {
        lock (this._stateMachines)
        {
            return this._stateMachines.TryGetValue(executionId, out var stateMachine) ? stateMachine : null;
        }
    }

    /// <summary>
    /// Removes the state machine for the specified execution ID.
    /// </summary>
    /// <param name="executionId">The unique identifier of the agent execution.</param>
    /// <returns><c>true</c> if the state machine was removed; otherwise, <c>false</c>.</returns>
    public bool RemoveStateMachine(Guid executionId)
    {
        lock (this._stateMachines)
        {
            return this._stateMachines.Remove(executionId);
        }
    }

    /// <summary>
    /// Transitions the state machine for the specified execution ID to the new state.
    /// </summary>
    /// <param name="executionId">The unique identifier of the agent execution.</param>
    /// <param name="newState">The target state.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the state machine is not found or the transition is invalid.</exception>
    public async Task TransitionAsync(Guid executionId, AgentExecutionState newState, CancellationToken cancellationToken = default)
    {
        await this._semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var stateMachine = this.GetStateMachine(executionId);
            if (stateMachine is null)
            {
                throw new InvalidOperationException($"State machine for execution {executionId} not found");
            }

            var fromState = stateMachine.CurrentState;
            stateMachine.TransitionTo(newState);

            var domainEvent = new StateTransitioned
            {
                ExecutionId = executionId,
                FromState = fromState,
                ToState = newState,
                Timestamp = DateTime.UtcNow,
            };

            lock (this._domainEvents)
            {
                this._domainEvents.Add(domainEvent);
            }
        }
        finally
        {
            this._semaphore.Release();
        }
    }

    /// <summary>
    /// Attempts to transition the state machine for the specified execution ID to the new state without throwing an exception.
    /// </summary>
    /// <param name="executionId">The unique identifier of the agent execution.</param>
    /// <param name="newState">The target state.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation with a boolean indicating success.</returns>
    public async Task<bool> TryTransitionAsync(Guid executionId, AgentExecutionState newState, CancellationToken cancellationToken = default)
    {
        await this._semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var stateMachine = this.GetStateMachine(executionId);
            if (stateMachine is null)
            {
                return false;
            }

            var fromState = stateMachine.CurrentState;
            if (!stateMachine.TryTransitionTo(newState))
            {
                return false;
            }

            var domainEvent = new StateTransitioned
            {
                ExecutionId = executionId,
                FromState = fromState,
                ToState = newState,
                Timestamp = DateTime.UtcNow,
            };

            lock (this._domainEvents)
            {
                this._domainEvents.Add(domainEvent);
            }

            return true;
        }
        finally
        {
            this._semaphore.Release();
        }
    }

    /// <summary>
    /// Clears all domain events.
    /// </summary>
    public void ClearDomainEvents()
    {
        lock (this._domainEvents)
        {
            this._domainEvents.Clear();
        }
    }

    /// <summary>
    /// Gets the current state for the specified execution ID.
    /// </summary>
    /// <param name="executionId">The unique identifier of the agent execution.</param>
    /// <returns>The current state if the state machine exists; otherwise, <c>null</c>.</returns>
    public AgentExecutionState? GetCurrentState(Guid executionId)
    {
        var stateMachine = this.GetStateMachine(executionId);
        return stateMachine?.CurrentState;
    }

    /// <summary>
    /// Gets the count of active state machines.
    /// </summary>
    /// <returns>The number of active state machines.</returns>
    public int GetActiveStateMachinesCount()
    {
        lock (this._stateMachines)
        {
            return this._stateMachines.Count;
        }
    }

    /// <summary>
    /// Disposes the resources used by the service.
    /// </summary>
    public void Dispose()
    {
        this._semaphore.Dispose();
    }
}
