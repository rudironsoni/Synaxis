// <copyright file="AgentExecutionTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.UnitTests.Aggregates;

using System;
using System.Collections.Generic;
using FluentAssertions;
using Synaxis.Agents.Domain.Aggregates;
using Synaxis.Agents.Domain.ValueObjects;
using Xunit;

[Trait("Category", "Unit")]
public class AgentExecutionTests
{
    [Fact]
    public void Create_ValidData_CreatesExecution()
    {
        // Arrange
        var id = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var executionId = "exec-123";
        var inputParameters = new Dictionary<string, object>
        {
            { "param1", "value1" },
            { "param2", 42 }
        };

        // Act
        var execution = AgentExecution.Create(id, agentId, executionId, inputParameters);

        // Assert
        execution.Should().NotBeNull();
        execution.Id.Should().Be(id.ToString());
        execution.AgentId.Should().Be(agentId);
        execution.ExecutionId.Should().Be(executionId);
        execution.Status.Should().Be(AgentStatus.Running);
        execution.CurrentStep.Should().Be(0);
        execution.StartedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        execution.CompletedAt.Should().BeNull();
        execution.Error.Should().BeNull();
        execution.DurationMs.Should().BeNull();
        execution.InputParameters.Should().BeEquivalentTo(inputParameters);
        execution.Steps.Should().BeEmpty();
    }

    [Fact]
    public void StartExecution_SetsStatusToRunning()
    {
        // Arrange
        var execution = AgentExecution.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "exec-123",
            new Dictionary<string, object>());

        // Act
        var status = execution.Status;

        // Assert
        status.Should().Be(AgentStatus.Running);
    }

    [Fact]
    public void Progress_WhenRunning_AddsStepAndUpdatesCurrentStep()
    {
        // Arrange
        var execution = AgentExecution.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "exec-123",
            new Dictionary<string, object>());

        var step = new ExecutionStep
        {
            StepNumber = 1,
            Name = "Step 1",
            Status = AgentStatus.Running,
            StartedAt = DateTime.UtcNow
        };

        // Act
        execution.Progress(step);

        // Assert
        execution.CurrentStep.Should().Be(1);
        execution.Steps.Should().HaveCount(1);
        execution.Steps[0].Should().Be(step);
    }

    [Fact]
    public void Progress_WhenNotRunning_ThrowsInvalidOperationException()
    {
        // Arrange
        var execution = AgentExecution.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "exec-123",
            new Dictionary<string, object>());

        execution.Complete();
        var step = new ExecutionStep { StepNumber = 2, Name = "Step 2", Status = AgentStatus.Running };

        // Act
        Action act = () => execution.Progress(step);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"Cannot progress execution from status {AgentStatus.Completed}.");
    }

    [Fact]
    public void PauseExecution_SetsStatusToPaused()
    {
        // Arrange
        var execution = AgentExecution.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "exec-123",
            new Dictionary<string, object>());

        // Act
        execution.Pause();

        // Assert
        execution.Status.Should().Be(AgentStatus.Paused);
    }

    [Fact]
    public void PauseExecution_WhenNotRunning_ThrowsInvalidOperationException()
    {
        // Arrange
        var execution = AgentExecution.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "exec-123",
            new Dictionary<string, object>());

        execution.Complete();

        // Act
        Action act = () => execution.Pause();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"Cannot pause execution from status {AgentStatus.Completed}.");
    }

    [Fact]
    public void CompleteExecution_SetsStatusToCompleted()
    {
        // Arrange
        var execution = AgentExecution.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "exec-123",
            new Dictionary<string, object>());

        // Act
        execution.Complete();

        // Assert
        execution.Status.Should().Be(AgentStatus.Completed);
        execution.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        execution.DurationMs.Should().NotBeNull();
        execution.DurationMs!.Value.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void CompleteExecution_WhenNotRunning_ThrowsInvalidOperationException()
    {
        // Arrange
        var execution = AgentExecution.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "exec-123",
            new Dictionary<string, object>());

        execution.Pause();

        // Act
        Action act = () => execution.Complete();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"Cannot complete execution from status {AgentStatus.Paused}.");
    }

    [Fact]
    public void FailExecution_SetsStatusToFailedAndRecordsError()
    {
        // Arrange
        var execution = AgentExecution.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "exec-123",
            new Dictionary<string, object>());

        var errorMessage = "Something went wrong";

        // Act
        execution.Fail(errorMessage);

        // Assert
        execution.Status.Should().Be(AgentStatus.Failed);
        execution.Error.Should().Be(errorMessage);
        execution.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        execution.DurationMs.Should().NotBeNull();
        execution.DurationMs!.Value.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void FailExecution_WhenNotRunning_ThrowsInvalidOperationException()
    {
        // Arrange
        var execution = AgentExecution.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "exec-123",
            new Dictionary<string, object>());

        execution.Complete();

        // Act
        Action act = () => execution.Fail("Error");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"Cannot fail execution from status {AgentStatus.Completed}.");
    }

    [Fact]
    public void ResumeExecution_SetsStatusToRunning()
    {
        // Arrange
        var execution = AgentExecution.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "exec-123",
            new Dictionary<string, object>());

        execution.Pause();

        // Act
        execution.Resume();

        // Assert
        execution.Status.Should().Be(AgentStatus.Running);
    }

    [Fact]
    public void ResumeExecution_WhenNotPaused_ThrowsInvalidOperationException()
    {
        // Arrange
        var execution = AgentExecution.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "exec-123",
            new Dictionary<string, object>());

        // Act
        Action act = () => execution.Resume();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"Cannot resume execution from status {AgentStatus.Running}.");
    }

    [Fact]
    public void CancelExecution_SetsStatusToFailed()
    {
        // Arrange
        var execution = AgentExecution.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "exec-123",
            new Dictionary<string, object>());

        // Act
        execution.Cancel();

        // Assert
        execution.Status.Should().Be(AgentStatus.Failed);
        execution.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        execution.DurationMs.Should().NotBeNull();
        execution.DurationMs!.Value.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void CancelExecution_WhenNotRunningOrPaused_ThrowsInvalidOperationException()
    {
        // Arrange
        var execution = AgentExecution.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "exec-123",
            new Dictionary<string, object>());

        execution.Complete();

        // Act
        Action act = () => execution.Cancel();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"Cannot cancel execution from status {AgentStatus.Completed}.");
    }
}
