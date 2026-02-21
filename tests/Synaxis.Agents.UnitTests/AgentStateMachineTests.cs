// <copyright file="AgentStateMachineTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.UnitTests;

using FluentAssertions;
using Synaxis.Agents.Domain;
using Xunit;

[Trait("Category", "Unit")]
public class AgentStateMachineTests
{
    [Fact]
    public void Constructor_Default_InitializesToIdle()
    {
        // Arrange & Act
        var stateMachine = new AgentStateMachine();

        // Assert
        stateMachine.CurrentState.Should().Be(AgentExecutionState.Idle);
    }

    [Fact]
    public void Constructor_WithInitialState_InitializesToGivenState()
    {
        // Arrange & Act
        var stateMachine = new AgentStateMachine(AgentExecutionState.Running);

        // Assert
        stateMachine.CurrentState.Should().Be(AgentExecutionState.Running);
    }

    [Fact]
    public void CanTransition_IdleToRunning_ReturnsTrue()
    {
        // Arrange
        var stateMachine = new AgentStateMachine(AgentExecutionState.Idle);

        // Act
        var canTransition = stateMachine.CanTransitionTo(AgentExecutionState.Running);

        // Assert
        canTransition.Should().BeTrue();
    }

    [Fact]
    public void CanTransition_IdleToPaused_ReturnsFalse()
    {
        // Arrange
        var stateMachine = new AgentStateMachine(AgentExecutionState.Idle);

        // Act
        var canTransition = stateMachine.CanTransitionTo(AgentExecutionState.Paused);

        // Assert
        canTransition.Should().BeFalse();
    }

    [Fact]
    public void CanTransition_RunningToCompleted_ReturnsTrue()
    {
        // Arrange
        var stateMachine = new AgentStateMachine(AgentExecutionState.Running);

        // Act
        var canTransition = stateMachine.CanTransitionTo(AgentExecutionState.Completed);

        // Assert
        canTransition.Should().BeTrue();
    }

    [Fact]
    public void CanTransition_RunningToFailed_ReturnsTrue()
    {
        // Arrange
        var stateMachine = new AgentStateMachine(AgentExecutionState.Running);

        // Act
        var canTransition = stateMachine.CanTransitionTo(AgentExecutionState.Failed);

        // Assert
        canTransition.Should().BeTrue();
    }

    [Fact]
    public void CanTransition_RunningToPaused_ReturnsTrue()
    {
        // Arrange
        var stateMachine = new AgentStateMachine(AgentExecutionState.Running);

        // Act
        var canTransition = stateMachine.CanTransitionTo(AgentExecutionState.Paused);

        // Assert
        canTransition.Should().BeTrue();
    }

    [Fact]
    public void CanTransition_RunningToCancelled_ReturnsTrue()
    {
        // Arrange
        var stateMachine = new AgentStateMachine(AgentExecutionState.Running);

        // Act
        var canTransition = stateMachine.CanTransitionTo(AgentExecutionState.Cancelled);

        // Assert
        canTransition.Should().BeTrue();
    }

    [Fact]
    public void CanTransition_RunningToIdle_ReturnsFalse()
    {
        // Arrange
        var stateMachine = new AgentStateMachine(AgentExecutionState.Running);

        // Act
        var canTransition = stateMachine.CanTransitionTo(AgentExecutionState.Idle);

        // Assert
        canTransition.Should().BeFalse();
    }

    [Fact]
    public void CanTransition_PausedToRunning_ReturnsTrue()
    {
        // Arrange
        var stateMachine = new AgentStateMachine(AgentExecutionState.Paused);

        // Act
        var canTransition = stateMachine.CanTransitionTo(AgentExecutionState.Running);

        // Assert
        canTransition.Should().BeTrue();
    }

    [Fact]
    public void CanTransition_PausedToCancelled_ReturnsTrue()
    {
        // Arrange
        var stateMachine = new AgentStateMachine(AgentExecutionState.Paused);

        // Act
        var canTransition = stateMachine.CanTransitionTo(AgentExecutionState.Cancelled);

        // Assert
        canTransition.Should().BeTrue();
    }

    [Fact]
    public void CanTransition_PausedToCompleted_ReturnsFalse()
    {
        // Arrange
        var stateMachine = new AgentStateMachine(AgentExecutionState.Paused);

        // Act
        var canTransition = stateMachine.CanTransitionTo(AgentExecutionState.Completed);

        // Assert
        canTransition.Should().BeFalse();
    }

    [Fact]
    public void CanTransition_CompletedToRunning_ReturnsFalse()
    {
        // Arrange
        var stateMachine = new AgentStateMachine(AgentExecutionState.Completed);

        // Act
        var canTransition = stateMachine.CanTransitionTo(AgentExecutionState.Running);

        // Assert
        canTransition.Should().BeFalse();
    }

    [Fact]
    public void CanTransition_FailedToRunning_ReturnsFalse()
    {
        // Arrange
        var stateMachine = new AgentStateMachine(AgentExecutionState.Failed);

        // Act
        var canTransition = stateMachine.CanTransitionTo(AgentExecutionState.Running);

        // Assert
        canTransition.Should().BeFalse();
    }

    [Fact]
    public void CanTransition_CancelledToRunning_ReturnsFalse()
    {
        // Arrange
        var stateMachine = new AgentStateMachine(AgentExecutionState.Cancelled);

        // Act
        var canTransition = stateMachine.CanTransitionTo(AgentExecutionState.Running);

        // Assert
        canTransition.Should().BeFalse();
    }

    [Fact]
    public void TransitionTo_ValidTransition_ChangesState()
    {
        // Arrange
        var stateMachine = new AgentStateMachine(AgentExecutionState.Idle);

        // Act
        stateMachine.TransitionTo(AgentExecutionState.Running);

        // Assert
        stateMachine.CurrentState.Should().Be(AgentExecutionState.Running);
    }

    [Fact]
    public void TransitionTo_InvalidTransition_ThrowsInvalidOperationException()
    {
        // Arrange
        var stateMachine = new AgentStateMachine(AgentExecutionState.Completed);

        // Act
        Action act = () => stateMachine.TransitionTo(AgentExecutionState.Running);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"Cannot transition from {AgentExecutionState.Completed} to {AgentExecutionState.Running}");
    }

    [Fact]
    public void TryTransitionTo_ValidTransition_ReturnsTrueAndChangesState()
    {
        // Arrange
        var stateMachine = new AgentStateMachine(AgentExecutionState.Idle);

        // Act
        var result = stateMachine.TryTransitionTo(AgentExecutionState.Running);

        // Assert
        result.Should().BeTrue();
        stateMachine.CurrentState.Should().Be(AgentExecutionState.Running);
    }

    [Fact]
    public void TryTransitionTo_InvalidTransition_ReturnsFalseAndDoesNotChangeState()
    {
        // Arrange
        var stateMachine = new AgentStateMachine(AgentExecutionState.Completed);
        var originalState = stateMachine.CurrentState;

        // Act
        var result = stateMachine.TryTransitionTo(AgentExecutionState.Running);

        // Assert
        result.Should().BeFalse();
        stateMachine.CurrentState.Should().Be(originalState);
    }
}
