// <copyright file="AgentStateMachine.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Domain;

/// <summary>
/// Manages state transitions for agent execution lifecycle.
/// </summary>
public class AgentStateMachine
{
    private readonly Lock _lock = new();
    private AgentExecutionState _currentState;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentStateMachine"/> class.
    /// </summary>
    public AgentStateMachine()
    {
        this._currentState = AgentExecutionState.Idle;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentStateMachine"/> class with a specific initial state.
    /// </summary>
    /// <param name="initialState">The initial state of the machine.</param>
    public AgentStateMachine(AgentExecutionState initialState)
    {
        this._currentState = initialState;
    }

    /// <summary>
    /// Gets the current state of the agent execution.
    /// </summary>
    public AgentExecutionState CurrentState
    {
        get
        {
            lock (this._lock)
            {
                return this._currentState;
            }
        }
    }

    /// <summary>
    /// Determines whether a transition to the specified state is valid.
    /// </summary>
    /// <param name="newState">The target state.</param>
    /// <returns><c>true</c> if the transition is valid; otherwise, <c>false</c>.</returns>
    public bool CanTransitionTo(AgentExecutionState newState)
    {
        lock (this._lock)
        {
            return this._currentState switch
            {
                AgentExecutionState.Idle => newState == AgentExecutionState.Running,
                AgentExecutionState.Running => newState is AgentExecutionState.Paused or AgentExecutionState.Completed or AgentExecutionState.Failed or AgentExecutionState.Cancelled,
                AgentExecutionState.Paused => newState is AgentExecutionState.Running or AgentExecutionState.Cancelled,
                AgentExecutionState.Completed or AgentExecutionState.Failed or AgentExecutionState.Cancelled => false,
                _ => false,
            };
        }
    }

    /// <summary>
    /// Transitions the state machine to the specified state.
    /// </summary>
    /// <param name="newState">The target state.</param>
    /// <exception cref="InvalidOperationException">Thrown when the transition is not valid.</exception>
    public void TransitionTo(AgentExecutionState newState)
    {
        lock (this._lock)
        {
            if (!this.CanTransitionTo(newState))
            {
                throw new InvalidOperationException($"Cannot transition from {this._currentState} to {newState}");
            }

            this._currentState = newState;
        }
    }

    /// <summary>
    /// Attempts to transition to the specified state without throwing an exception.
    /// </summary>
    /// <param name="newState">The target state.</param>
    /// <returns><c>true</c> if the transition was successful; otherwise, <c>false</c>.</returns>
    public bool TryTransitionTo(AgentExecutionState newState)
    {
        lock (this._lock)
        {
            if (!this.CanTransitionTo(newState))
            {
                return false;
            }

            this._currentState = newState;
            return true;
        }
    }
}
