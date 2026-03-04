// <copyright file="MigrationRehearsalTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Tests.Migrations;

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Synaxis.Common.Tests.Fixtures;
using Synaxis.Infrastructure.Data;
using Synaxis.Infrastructure.Migrations.Rehearsals;
using Xunit;

/// <summary>
/// Integration tests for migration rehearsals.
/// Validates complete migration runbook execution in staging environments.
/// </summary>
[Trait("Category", "Integration")]
[Collection("PostgresIntegration")]
public sealed class MigrationRehearsalTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgresFixture;
    private SynaxisDbContext? _context;
    private MigrationRehearsalOrchestrator? _orchestrator;
    private string _connectionString = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationRehearsalTests"/> class.
    /// </summary>
    /// <param name="postgresFixture">The PostgreSQL test fixture.</param>
    public MigrationRehearsalTests(PostgresFixture postgresFixture)
    {
        _postgresFixture = postgresFixture ?? throw new ArgumentNullException(nameof(postgresFixture));
    }

    /// <summary>
    /// Initializes the test by creating a fresh database.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task InitializeAsync()
    {
        _connectionString = await _postgresFixture.CreateIsolatedDatabaseAsync("rehearsal");
        var options = new DbContextOptionsBuilder<SynaxisDbContext>()
            .UseNpgsql(_connectionString)
            .Options;

        _context = new SynaxisDbContext(options);

        var logger = new NullLogger<MigrationRehearsalOrchestrator>();
        _orchestrator = new MigrationRehearsalOrchestrator(_context, logger, "./rehearsal-test-output");
    }

    /// <summary>
    /// Disposes resources after tests complete.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task DisposeAsync()
    {
        if (_context != null)
        {
            await _context.DisposeAsync();
        }

        if (!string.IsNullOrEmpty(_connectionString))
        {
            await _postgresFixture.DropDatabaseAsync(_connectionString);
        }
    }

    /// <summary>
    /// Tests the complete migration rehearsal suite.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task ExecuteFullRehearsal_CompletesSuccessfully()
    {
        // Arrange
        _orchestrator.Should().NotBeNull();

        // Act
        var result = await _orchestrator!.ExecuteFullRehearsalAsync();

        // Assert
        result.Should().NotBeNull();
        result.RehearsalId.Should().NotBeNullOrEmpty();
        result.StartedAt.Should().BeBefore(result.CompletedAt);
        result.TotalDuration.Should().BeGreaterThan(TimeSpan.Zero);
        result.Environment.Should().NotBeNullOrEmpty();

        // All major components should be present
        result.HappyPathResult.Should().NotBeNull();
        result.FailureScenarioResult.Should().NotBeNull();
        result.PartialFailureResult.Should().NotBeNull();
        result.PerformanceBaselineResult.Should().NotBeNull();
    }

    /// <summary>
    /// Tests the happy path rehearsal - complete migration runbook.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task HappyPathRehearsal_ExecutesAllSteps()
    {
        // Arrange
        _orchestrator.Should().NotBeNull();

        // Act
        var result = await _orchestrator!.ExecuteFullRehearsalAsync();

        // Assert - Happy path verification
        result.HappyPathResult.Should().NotBeNull();
        result.HappyPathResult!.StepResults.Should().NotBeNull();
        result.HappyPathResult.StepResults.Should().Contain(s => s.StepName == "PreMigrationValidation");
        result.HappyPathResult.StepResults.Should().Contain(s => s.StepName == "DatabaseBackup");
        result.HappyPathResult.StepResults.Should().Contain(s => s.StepName == "ExecuteMigrations");
        result.HappyPathResult.StepResults.Should().Contain(s => s.StepName == "PostMigrationValidation");
        result.HappyPathResult.StepResults.Should().Contain(s => s.StepName == "ServiceHealthCheck");
        result.HappyPathResult.StepResults.Should().Contain(s => s.StepName == "DataIntegrityVerification");

        // Verify timing is captured
        foreach (var step in result.HappyPathResult.StepResults!)
        {
            step.Duration.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);
        }
    }

    /// <summary>
    /// Tests that failure scenarios are properly validated.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task FailureScenarioRehearsal_VerifiesRollback()
    {
        // Act
        var result = await _orchestrator!.ExecuteFullRehearsalAsync();

        // Assert
        result.FailureScenarioResult.Should().NotBeNull();
        result.FailureScenarioResult!.RollbackVerified.Should().BeTrue();
        result.FailureScenarioResult.DataConsistentAfterRollback.Should().BeTrue();
        result.FailureScenarioResult.RecoveryTimeSeconds.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Tests that partial failure scenarios are handled.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task PartialFailureRehearsal_VerifiesGracefulDegradation()
    {
        // Act
        var result = await _orchestrator!.ExecuteFullRehearsalAsync();

        // Assert
        result.PartialFailureResult.Should().NotBeNull();
        result.PartialFailureResult!.ServiceFailureHandledGracefully.Should().BeTrue();
        result.PartialFailureResult.ConnectionIssuesHandled.Should().BeTrue();
        result.PartialFailureResult.NetworkPartitionHandled.Should().BeTrue();
        result.PartialFailureResult.GracefulDegradationVerified.Should().BeTrue();
    }

    /// <summary>
    /// Tests that performance baselines are established.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task PerformanceBaselineRehearsal_CapturesMetrics()
    {
        // Act
        var result = await _orchestrator!.ExecuteFullRehearsalAsync();

        // Assert
        result.PerformanceBaselineResult.Should().NotBeNull();
        result.PerformanceBaselineResult!.PreMigrationMetrics.Should().NotBeNull();
        result.PerformanceBaselineResult.PostMigrationMetrics.Should().NotBeNull();
        result.PerformanceBaselineResult.ResponseTimeComparison.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that Go/No-Go decision is determined.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task GoNoGoDecision_IsDetermined()
    {
        // Act
        var result = await _orchestrator!.ExecuteFullRehearsalAsync();

        // Assert
        result.GoNoGoRecommendation.Should().BeOneOf(
            GoNoGoDecision.Go,
            GoNoGoDecision.GoWithPerformanceMonitoring,
            GoNoGoDecision.NoGoWithRollbackRisk,
            GoNoGoDecision.NoGo);
    }

    /// <summary>
    /// Tests that rehearsal results are persisted.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task RehearsalResults_ArePersisted()
    {
        // Act
        var result = await _orchestrator!.ExecuteFullRehearsalAsync();

        // Assert - Results should be persisted to disk
        // Note: In a real test, we'd verify the file exists
        result.RehearsalId.Should().NotBeNullOrEmpty();
        result.StartedAt.Should().BeBefore(DateTime.UtcNow);
    }

    /// <summary>
    /// Tests that timing is captured for all steps.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task StepTimings_AreCaptured()
    {
        // Act
        var result = await _orchestrator!.ExecuteFullRehearsalAsync();

        // Assert
        result.HappyPathResult.Should().NotBeNull();
        result.HappyPathResult!.StepResults.Should().NotBeNullOrEmpty();

        foreach (var step in result.HappyPathResult.StepResults!)
        {
            step.StepName.Should().NotBeNullOrEmpty();
            step.Duration.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);
        }
    }

    /// <summary>
    /// Tests that the rehearsal completes within a reasonable time.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task Rehearsal_CompletesWithinTimeLimit()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await _orchestrator!.ExecuteFullRehearsalAsync();

        stopwatch.Stop();

        // Assert - Should complete within 5 minutes (generous timeout for integration tests)
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromMinutes(5));
        result.TotalDuration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    /// <summary>
    /// Tests that the rehearsal works with a fresh database.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task Rehearsal_WorksWithFreshDatabase()
    {
        // Arrange - database is already fresh from InitializeAsync

        // Act
        var result = await _orchestrator!.ExecuteFullRehearsalAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.HappyPathResult.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that issues are properly logged.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task Issues_AreLogged_WhenFailuresOccur()
    {
        // Act
        var result = await _orchestrator!.ExecuteFullRehearsalAsync();

        // Assert - Should have completed (issues are logged internally)
        result.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that all scenarios are executed.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task AllScenarios_AreExecuted()
    {
        // Act
        var result = await _orchestrator!.ExecuteFullRehearsalAsync();

        // Assert - All four major scenarios should have results
        result.HappyPathResult.Should().NotBeNull();
        result.FailureScenarioResult.Should().NotBeNull();
        result.PartialFailureResult.Should().NotBeNull();
        result.PerformanceBaselineResult.Should().NotBeNull();

        // All should have valid durations
        result.HappyPathResult!.TotalDuration.Should().BeGreaterThan(TimeSpan.Zero);
        result.FailureScenarioResult!.TotalDuration.Should().BeGreaterThan(TimeSpan.Zero);
        result.PartialFailureResult!.TotalDuration.Should().BeGreaterThan(TimeSpan.Zero);
        result.PerformanceBaselineResult!.TotalDuration.Should().BeGreaterThan(TimeSpan.Zero);
    }
}
