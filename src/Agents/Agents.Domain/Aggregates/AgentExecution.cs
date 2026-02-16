// <copyright file="AgentExecution.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Domain.Aggregates;

using Synaxis.Abstractions.Cloud;
using Synaxis.Agents.Domain.Events;
using Synaxis.Agents.Domain.ValueObjects;
using Synaxis.Infrastructure.EventSourcing;

/// <summary>
/// Aggregate root representing an agent execution.
/// </summary>
public class AgentExecution : AggregateRoot
{
    private readonly List<ExecutionStep> _steps = new();
    private Guid _agentId;
    private string _executionId = string.Empty;
    private AgentStatus _status;
    private Dictionary<string, object> _inputParameters = new(StringComparer.Ordinal);
    private int _currentStep;
    private DateTime? _startedAt;
    private DateTime? _completedAt;
    private string? _error;
    private long? _durationMs;

    /// <summary>
    /// Gets the unique identifier of the agent.
    /// </summary>
    public Guid AgentId => this._agentId;

    /// <summary>
    /// Gets the execution identifier.
    /// </summary>
    public string ExecutionId => this._executionId;

    /// <summary>
    /// Gets the current status of the execution.
    /// </summary>
    public AgentStatus Status => this._status;

    /// <summary>
    /// Gets the input parameters for the execution.
    /// </summary>
    public IReadOnlyDictionary<string, object> InputParameters => this._inputParameters;

    /// <summary>
    /// Gets the current step number.
    /// </summary>
    public int CurrentStep => this._currentStep;

    /// <summary>
    /// Gets the timestamp when the execution started.
    /// </summary>
    public DateTime? StartedAt => this._startedAt;

    /// <summary>
    /// Gets the timestamp when the execution completed.
    /// </summary>
    public DateTime? CompletedAt => this._completedAt;

    /// <summary>
    /// Gets the error message if the execution failed.
    /// </summary>
    public string? Error => this._error;

    /// <summary>
    /// Gets the duration of the execution in milliseconds.
    /// </summary>
    public long? DurationMs => this._durationMs;

    /// <summary>
    /// Gets the execution steps.
    /// </summary>
    public IReadOnlyList<ExecutionStep> Steps => this._steps.AsReadOnly();

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentExecution"/> class.
    /// Required for deserialization.
    /// </summary>
    private AgentExecution()
    {
    }

    /// <summary>
    /// Creates a new agent execution.
    /// </summary>
    /// <param name="id">The unique identifier of the execution.</param>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <param name="executionId">The execution identifier.</param>
    /// <param name="inputParameters">The input parameters for the execution.</param>
    /// <returns>A new instance of <see cref="AgentExecution"/>.</returns>
    public static AgentExecution Create(
        Guid id,
        Guid agentId,
        string executionId,
        IReadOnlyDictionary<string, object> inputParameters)
    {
        var execution = new AgentExecution();
        var @event = new ExecutionStarted
        {
            Id = id,
            AgentId = agentId,
            ExecutionId = executionId,
            InputParameters = inputParameters,
            StartedAt = DateTime.UtcNow,
        };
        execution.ApplyEvent(@event);
        return execution;
    }

    /// <summary>
    /// Progresses the execution to a new step.
    /// </summary>
    /// <param name="step">The execution step details.</param>
    public void Progress(ExecutionStep step)
    {
        if (this._status != AgentStatus.Running)
        {
            throw new InvalidOperationException($"Cannot progress execution from status {this._status}.");
        }

        var @event = new ExecutionProgressed
        {
            Id = Guid.Parse(this.Id),
            CurrentStep = step.StepNumber,
            Step = step,
        };
        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Completes the execution successfully.
    /// </summary>
    public void Complete()
    {
        if (this._status != AgentStatus.Running)
        {
            throw new InvalidOperationException($"Cannot complete execution from status {this._status}.");
        }

        if (!this._startedAt.HasValue)
        {
            throw new InvalidOperationException("Execution has not started.");
        }

        var completedAt = DateTime.UtcNow;
        var durationMs = (long)(completedAt - this._startedAt.Value).TotalMilliseconds;

        var @event = new ExecutionCompleted
        {
            Id = Guid.Parse(this.Id),
            CompletedAt = completedAt,
            DurationMs = durationMs,
        };
        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Fails the execution with an error.
    /// </summary>
    /// <param name="error">The error message.</param>
    public void Fail(string error)
    {
        if (this._status != AgentStatus.Running)
        {
            throw new InvalidOperationException($"Cannot fail execution from status {this._status}.");
        }

        if (!this._startedAt.HasValue)
        {
            throw new InvalidOperationException("Execution has not started.");
        }

        var failedAt = DateTime.UtcNow;
        var durationMs = (long)(failedAt - this._startedAt.Value).TotalMilliseconds;

        var @event = new ExecutionFailed
        {
            Id = Guid.Parse(this.Id),
            Error = error,
            FailedAt = failedAt,
            DurationMs = durationMs,
        };
        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Pauses the execution.
    /// </summary>
    public void Pause()
    {
        if (this._status != AgentStatus.Running)
        {
            throw new InvalidOperationException($"Cannot pause execution from status {this._status}.");
        }

        var @event = new ExecutionPaused
        {
            Id = Guid.Parse(this.Id),
            CurrentStep = this._currentStep,
        };
        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Resumes a paused execution.
    /// </summary>
    public void Resume()
    {
        if (this._status != AgentStatus.Paused)
        {
            throw new InvalidOperationException($"Cannot resume execution from status {this._status}.");
        }

        var @event = new ExecutionResumed
        {
            Id = Guid.Parse(this.Id),
            CurrentStep = this._currentStep,
        };
        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Cancels the execution.
    /// </summary>
    public void Cancel()
    {
        if (this._status != AgentStatus.Running && this._status != AgentStatus.Paused)
        {
            throw new InvalidOperationException($"Cannot cancel execution from status {this._status}.");
        }

        if (!this._startedAt.HasValue)
        {
            throw new InvalidOperationException("Execution has not started.");
        }

        var cancelledAt = DateTime.UtcNow;
        var durationMs = (long)(cancelledAt - this._startedAt.Value).TotalMilliseconds;

        var @event = new ExecutionCancelled
        {
            Id = Guid.Parse(this.Id),
            CurrentStep = this._currentStep,
            CancelledAt = cancelledAt,
            DurationMs = durationMs,
        };
        this.ApplyEvent(@event);
    }

    /// <inheritdoc/>
    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case ExecutionStarted started:
                this.Apply(started);
                break;
            case ExecutionProgressed progressed:
                this.Apply(progressed);
                break;
            case ExecutionCompleted completed:
                this.Apply(completed);
                break;
            case ExecutionFailed failed:
                this.Apply(failed);
                break;
            case ExecutionPaused paused:
                this.Apply(paused);
                break;
            case ExecutionResumed resumed:
                this.Apply(resumed);
                break;
            case ExecutionCancelled cancelled:
                this.Apply(cancelled);
                break;
        }
    }

    private void Apply(ExecutionStarted @event)
    {
        this.Id = @event.Id.ToString();
        this._agentId = @event.AgentId;
        this._executionId = @event.ExecutionId;
        this._inputParameters = new Dictionary<string, object>(@event.InputParameters, StringComparer.Ordinal);
        this._status = AgentStatus.Running;
        this._currentStep = 0;
        this._startedAt = @event.StartedAt;
        this._completedAt = null;
        this._error = null;
        this._durationMs = null;
    }

    private void Apply(ExecutionProgressed @event)
    {
        this._currentStep = @event.CurrentStep;
        this._steps.Add(@event.Step);
    }

    private void Apply(ExecutionCompleted @event)
    {
        this._status = AgentStatus.Completed;
        this._completedAt = @event.CompletedAt;
        this._durationMs = @event.DurationMs;
    }

    private void Apply(ExecutionFailed @event)
    {
        this._status = AgentStatus.Failed;
        this._error = @event.Error;
        this._completedAt = @event.FailedAt;
        this._durationMs = @event.DurationMs;
    }

    private void Apply(ExecutionPaused @event)
    {
        this._status = AgentStatus.Paused;
        this._currentStep = @event.CurrentStep;
    }

    private void Apply(ExecutionResumed @event)
    {
        this._status = AgentStatus.Running;
        this._currentStep = @event.CurrentStep;
    }

    private void Apply(ExecutionCancelled @event)
    {
        this._status = AgentStatus.Failed;
        this._completedAt = @event.CancelledAt;
        this._durationMs = @event.DurationMs;
    }
}
