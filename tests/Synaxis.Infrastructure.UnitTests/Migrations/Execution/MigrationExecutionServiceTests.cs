// <copyright file="MigrationExecutionServiceTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Tests.Migrations.Execution;

using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Synaxis.Infrastructure.Data.Migrations.Execution;
using Xunit;

/// <summary>
/// Tests for the <see cref="MigrationExecutionService"/> class.
/// </summary>
[Trait("Category", "Unit")]
public class MigrationExecutionServiceTests
{
    private readonly ILogger<MigrationExecutionService> _logger;
    private readonly MigrationExecutionService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationExecutionServiceTests"/> class.
    /// </summary>
    public MigrationExecutionServiceTests()
    {
        _logger = Substitute.For<ILogger<MigrationExecutionService>>();
        _service = new MigrationExecutionService(_logger);
    }

    [Fact]
    public void CreateExecutionContext_WithValidEnvironment_CreatesLogWithCorrectProperties()
    {
        // Arrange
        var environment = "production";

        // Act
        var log = _service.CreateExecutionContext(environment);

        // Assert
        log.Should().NotBeNull();
        log.Environment.Should().Be(environment);
        log.Status.Should().Be(MigrationStatus.Initializing);
        log.IsDryRun.Should().BeFalse();
        log.MigrationId.Should().Contain(environment);
        log.StartedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void CreateExecutionContext_WithDryRun_SetsIsDryRunTrue()
    {
        // Arrange
        var environment = "staging";

        // Act
        var log = _service.CreateExecutionContext(environment, isDryRun: true);

        // Assert
        log.IsDryRun.Should().BeTrue();
    }

    [Fact]
    public void CreateExecutionContext_WithNullEnvironment_ThrowsArgumentException()
    {
        // Arrange
        string? environment = null;

        // Act
        Action act = () => _service.CreateExecutionContext(environment!);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*Environment*");
    }

    [Fact]
    public void CreateExecutionContext_WithEmptyEnvironment_ThrowsArgumentException()
    {
        // Arrange
        var environment = string.Empty;

        // Act
        Action act = () => _service.CreateExecutionContext(environment);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*Environment*");
    }

    [Fact]
    public async Task ExecutePhaseAsync_WithSuccessfulAction_AddsCompletedPhase()
    {
        // Arrange
        var log = _service.CreateExecutionContext("test");
        var phaseName = "test-phase";

        // Act
        var result = await _service.ExecutePhaseAsync(log, phaseName, async ct =>
        {
            await Task.Delay(10, ct);
            return "success";
        });

        // Assert
        result.Should().Be("success");
        log.Phases.Should().ContainSingle();
        log.Phases[0].Name.Should().Be(phaseName);
        log.Phases[0].Status.Should().Be(MigrationPhaseStatus.Completed);
        log.Phases[0].DurationSeconds.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task ExecutePhaseAsync_WithFailingAction_AddsFailedPhaseAndThrows()
    {
        // Arrange
        var log = _service.CreateExecutionContext("test");
        var phaseName = "failing-phase";
        var expectedException = new InvalidOperationException("Test failure");

        // Act
        Func<Task> act = async () => await _service.ExecutePhaseAsync<string>(log, phaseName, async ct =>
        {
            await Task.Delay(10, ct);
            throw expectedException;
        });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Test failure");
        log.Phases.Should().ContainSingle();
        log.Phases[0].Name.Should().Be(phaseName);
        log.Phases[0].Status.Should().Be(MigrationPhaseStatus.Failed);
    }

    [Fact]
    public void RecordIssue_WithCriticalSeverity_LogsCriticalIssue()
    {
        // Arrange
        var log = _service.CreateExecutionContext("test");

        // Act
        _service.RecordIssue(log, IssueSeverity.Critical, "Critical error", "TestComponent");

        // Assert
        log.Issues.Should().ContainSingle();
        log.Issues[0].Severity.Should().Be(IssueSeverity.Critical);
        log.Issues[0].Message.Should().Be("Critical error");
        log.Issues[0].Component.Should().Be("TestComponent");
    }

    [Fact]
    public void RecordIssue_WithNullLog_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => _service.RecordIssue(null!, IssueSeverity.Error, "message", "component");

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("log");
    }

    [Fact]
    public void RecordIssue_WithNullMessage_ThrowsArgumentException()
    {
        // Arrange
        var log = _service.CreateExecutionContext("test");

        // Act
        Action act = () => _service.RecordIssue(log, IssueSeverity.Error, null!, "component");

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*Message*");
    }

    [Fact]
    public void RecordDecision_WithValidInputs_RecordsDecision()
    {
        // Arrange
        var log = _service.CreateExecutionContext("test");

        // Act
        _service.RecordDecision(log, "GO", "All checks passed", "admin");

        // Assert
        log.Decisions.Should().ContainSingle();
        log.Decisions[0].Decision.Should().Be("GO");
        log.Decisions[0].Reason.Should().Be("All checks passed");
        log.Decisions[0].Approver.Should().Be("admin");
    }

    [Fact]
    public void RecordDecision_WithNullApprover_ThrowsArgumentException()
    {
        // Arrange
        var log = _service.CreateExecutionContext("test");

        // Act
        Action act = () => _service.RecordDecision(log, "GO", "reason", null!);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*Approver*");
    }

    [Fact]
    public void FinalizeExecution_WithSuccess_MarksCompleted()
    {
        // Arrange
        var log = _service.CreateExecutionContext("test");

        // Act
        _service.FinalizeExecution(log, success: true);

        // Assert
        log.Status.Should().Be(MigrationStatus.Completed);
        log.EndedAt.Should().NotBeNull();
        log.DurationSeconds.Should().NotBeNull();
    }

    [Fact]
    public void FinalizeExecution_WithFailure_MarksFailed()
    {
        // Arrange
        var log = _service.CreateExecutionContext("test");

        // Act
        _service.FinalizeExecution(log, success: false);

        // Assert
        log.Status.Should().Be(MigrationStatus.Failed);
        log.EndedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveExecutionLogAsync_WithValidLog_WritesJsonFile()
    {
        // Arrange
        var log = _service.CreateExecutionContext("test");
        log.AddPhase("phase1", MigrationPhaseStatus.Completed, 1);
        log.AddPhase("phase2", MigrationPhaseStatus.Completed, 2);
        _service.FinalizeExecution(log, success: true);

        var tempPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.json");

        try
        {
            // Act
            await _service.SaveExecutionLogAsync(log, tempPath);

            // Assert
            File.Exists(tempPath).Should().BeTrue();
            var content = await File.ReadAllTextAsync(tempPath);
            content.Should().Contain(log.MigrationId);
            content.Should().Contain("phase1");
            content.Should().Contain("phase2");
            content.Should().Contain("Completed");
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Fact]
    public void MarkCompleted_SetsStatusAndTimestamps()
    {
        // Arrange
        var log = _service.CreateExecutionContext("test");

        // Act
        log.MarkCompleted();

        // Assert
        log.Status.Should().Be(MigrationStatus.Completed);
        log.EndedAt.Should().NotBeNull();
        log.DurationSeconds.Should().NotBeNull();
    }

    [Fact]
    public void MarkRolledBack_SetsStatusAndTimestamps()
    {
        // Arrange
        var log = _service.CreateExecutionContext("test");

        // Act
        log.MarkRolledBack();

        // Assert
        log.Status.Should().Be(MigrationStatus.RolledBack);
        log.EndedAt.Should().NotBeNull();
        log.DurationSeconds.Should().NotBeNull();
    }
}
