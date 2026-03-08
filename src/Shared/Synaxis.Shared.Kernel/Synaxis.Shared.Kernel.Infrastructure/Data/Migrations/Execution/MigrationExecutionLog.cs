// <copyright file="MigrationExecutionLog.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Kernel.Infrastructure.Data.Migrations.Execution;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

/// <summary>
/// Represents a complete migration execution log.
/// </summary>
public sealed class MigrationExecutionLog
{
    /// <summary>
    /// Gets or sets the unique migration execution identifier.
    /// </summary>
    [JsonPropertyName("migrationId")]
    public required string MigrationId { get; init; }

    /// <summary>
    /// Gets or sets the target environment.
    /// </summary>
    [JsonPropertyName("environment")]
    public required string Environment { get; init; }

    /// <summary>
    /// Gets or sets the execution status.
    /// </summary>
    [JsonPropertyName("status")]
    public MigrationStatus Status { get; set; } = MigrationStatus.Initializing;

    /// <summary>
    /// Gets or sets a value indicating whether this was a dry run.
    /// </summary>
    [JsonPropertyName("dryRun")]
    public bool IsDryRun { get; init; }

    /// <summary>
    /// Gets or sets the start timestamp.
    /// </summary>
    [JsonPropertyName("startedAt")]
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the completion timestamp.
    /// </summary>
    [JsonPropertyName("endedAt")]
    public DateTimeOffset? EndedAt { get; set; }

    /// <summary>
    /// Gets or sets the total duration in seconds.
    /// </summary>
    [JsonPropertyName("duration")]
    public int? DurationSeconds { get; set; }

    /// <summary>
    /// Gets the execution phases.
    /// </summary>
    [JsonPropertyName("phases")]
    public List<MigrationPhase> Phases { get; } = [];

    /// <summary>
    /// Gets the logged issues.
    /// </summary>
    [JsonPropertyName("issues")]
    public List<MigrationIssue> Issues { get; } = [];

    /// <summary>
    /// Gets the recorded decisions.
    /// </summary>
    [JsonPropertyName("decisions")]
    public List<MigrationDecision> Decisions { get; } = [];

    /// <summary>
    /// Gets or sets the pre-flight check results.
    /// </summary>
    [JsonPropertyName("preflight")]
    public PreflightCheckResults? Preflight { get; set; }

    /// <summary>
    /// Gets or sets the post-deployment validation results.
    /// </summary>
    [JsonPropertyName("postDeployment")]
    public PostDeploymentResults? PostDeployment { get; set; }

    /// <summary>
    /// Gets or sets the go/no-go decision.
    /// </summary>
    [JsonPropertyName("goNoGoDecision")]
    public GoNoGoDecision? GoNoGoDecision { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// Adds a phase to the execution log.
    /// </summary>
    /// <param name="name">The phase name.</param>
    /// <param name="status">The phase status.</param>
    /// <param name="duration">The phase duration in seconds.</param>
    public void AddPhase(string name, MigrationPhaseStatus status, int duration)
    {
        this.Phases.Add(new MigrationPhase
        {
                   Name = name,
            Status = status,
            DurationSeconds = duration,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Records an issue.
    /// </summary>
    /// <param name="severity">The issue severity.</param>
    /// <param name="message">The issue message.</param>
    /// <param name="component">The affected component.</param>
    public void RecordIssue(IssueSeverity severity, string message, string component = "unknown")
    {
        this.Issues.Add(new MigrationIssue
        {
            Severity = severity,
            Message = message,
            Component = component,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Records a decision.
    /// </summary>
    /// <param name="decision">The decision.</param>
    /// <param name="reason">The decision reason.</param>
    /// <param name="approver">The decision approver.</param>
    public void RecordDecision(string decision, string reason, string approver)
    {
        this.Decisions.Add(new MigrationDecision
        {
            Decision = decision,
            Reason = reason,
            Approver = approver,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Marks the execution as completed.
    /// </summary>
    public void MarkCompleted()
    {
        this.Status = MigrationStatus.Completed;
        this.EndedAt = DateTimeOffset.UtcNow;
        this.DurationSeconds = (int)(this.EndedAt.Value - this.StartedAt).TotalSeconds;
    }

    /// <summary>
    /// Marks the execution as failed.
    /// </summary>
    public void MarkFailed()
    {
        this.Status = MigrationStatus.Failed;
        this.EndedAt = DateTimeOffset.UtcNow;
        this.DurationSeconds = (int)(this.EndedAt.Value - this.StartedAt).TotalSeconds;
    }

    /// <summary>
    /// Marks the execution as rolled back.
    /// </summary>
    public void MarkRolledBack()
    {
        this.Status = MigrationStatus.RolledBack;
        this.EndedAt = DateTimeOffset.UtcNow;
        this.DurationSeconds = (int)(this.EndedAt.Value - this.StartedAt).TotalSeconds;
    }
}

/// <summary>
/// Represents an execution phase.
/// </summary>
public sealed class MigrationPhase
{
    /// <summary>
    /// Gets or sets the phase name.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the phase status.
    /// </summary>
    [JsonPropertyName("status")]
    public required MigrationPhaseStatus Status { get; init; }

    /// <summary>
    /// Gets or sets the phase duration in seconds.
    /// </summary>
    [JsonPropertyName("duration")]
    public required int DurationSeconds { get; init; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public required DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Represents a migration issue.
/// </summary>
public sealed class MigrationIssue
{
    /// <summary>
    /// Gets or sets the issue severity.
    /// </summary>
    [JsonPropertyName("severity")]
    public required IssueSeverity Severity { get; init; }

    /// <summary>
    /// Gets or sets the issue message.
    /// </summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>
    /// Gets or sets the affected component.
    /// </summary>
    [JsonPropertyName("component")]
    public required string Component { get; init; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public required DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Represents a migration decision.
/// </summary>
public sealed class MigrationDecision
{
    /// <summary>
    /// Gets or sets the decision.
    /// </summary>
    [JsonPropertyName("decision")]
    public required string Decision { get; init; }

    /// <summary>
    /// Gets or sets the decision reason.
    /// </summary>
    [JsonPropertyName("reason")]
    public required string Reason { get; init; }

    /// <summary>
    /// Gets or sets the decision approver.
    /// </summary>
    [JsonPropertyName("approver")]
    public required string Approver { get; init; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public required DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Represents pre-flight check results.
/// </summary>
public sealed class PreflightCheckResults
{
    /// <summary>
    /// Gets or sets the check status.
    /// </summary>
    [JsonPropertyName("status")]
    public PreflightStatus Status { get; set; }

    /// <summary>
    /// Gets the individual check results.
    /// </summary>
    [JsonPropertyName("checks")]
    public List<PreflightCheck> Checks { get; } = [];

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Represents an individual pre-flight check.
/// </summary>
public sealed class PreflightCheck
{
    /// <summary>
    /// Gets or sets the check name.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the check result.
    /// </summary>
    [JsonPropertyName("result")]
    public required PreflightCheckResult Result { get; init; }

    /// <summary>
    /// Gets or sets the check message.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; init; }

    /// <summary>
    /// Gets or sets the check duration in milliseconds.
    /// </summary>
    [JsonPropertyName("durationMs")]
    public int? DurationMilliseconds { get; init; }
}

/// <summary>
/// Represents post-deployment validation results.
/// </summary>
public sealed class PostDeploymentResults
{
    /// <summary>
    /// Gets or sets the validation status.
    /// </summary>
    [JsonPropertyName("status")]
    public ValidationStatus Status { get; set; }

    /// <summary>
    /// Gets the health check results.
    /// </summary>
    [JsonPropertyName("healthChecks")]
    public List<HealthCheckResult> HealthChecks { get; } = [];

    /// <summary>
    /// Gets the smoke test results.
    /// </summary>
    [JsonPropertyName("smokeTests")]
    public List<SmokeTestResult> SmokeTests { get; } = [];

    /// <summary>
    /// Gets or sets the error rate check.
    /// </summary>
    [JsonPropertyName("errorRateCheck")]
    public ErrorRateCheck? ErrorRateCheck { get; set; }

    /// <summary>
    /// Gets or sets the performance validation.
    /// </summary>
    [JsonPropertyName("performanceValidation")]
    public PerformanceValidation? PerformanceValidation { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Represents a health check result.
/// </summary>
public sealed class HealthCheckResult
{
    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    [JsonPropertyName("service")]
    public required string Service { get; init; }

    /// <summary>
    /// Gets or sets the health check status.
    /// </summary>
    [JsonPropertyName("status")]
    public required HealthStatus Status { get; init; }

    /// <summary>
    /// Gets or sets the response time in milliseconds.
    /// </summary>
    [JsonPropertyName("responseTimeMs")]
    public int? ResponseTimeMilliseconds { get; init; }

    /// <summary>
    /// Gets or sets the error message if failed.
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; init; }
}

/// <summary>
/// Represents a smoke test result.
/// </summary>
public sealed class SmokeTestResult
{
    /// <summary>
    /// Gets or sets the test name.
    /// </summary>
    [JsonPropertyName("test")]
    public required string Test { get; init; }

    /// <summary>
    /// Gets or sets the test status.
    /// </summary>
    [JsonPropertyName("status")]
    public required TestStatus Status { get; init; }

    /// <summary>
    /// Gets or sets the execution time in milliseconds.
    /// </summary>
    [JsonPropertyName("executionTimeMs")]
    public int? ExecutionTimeMilliseconds { get; init; }

    /// <summary>
    /// Gets or sets the error message if failed.
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; init; }
}

/// <summary>
/// Represents an error rate check.
/// </summary>
public sealed class ErrorRateCheck
{
    /// <summary>
    /// Gets or sets the check status.
    /// </summary>
    [JsonPropertyName("status")]
    public required CheckStatus Status { get; init; }

    /// <summary>
    /// Gets or sets the error rate percentage.
    /// </summary>
    [JsonPropertyName("errorRate")]
    public required double ErrorRate { get; init; }

    /// <summary>
    /// Gets or sets the threshold percentage.
    /// </summary>
    [JsonPropertyName("threshold")]
    public required double Threshold { get; init; }

    /// <summary>
    /// Gets or sets the check window in minutes.
    /// </summary>
    [JsonPropertyName("windowMinutes")]
    public int WindowMinutes { get; init; } = 5;
}

/// <summary>
/// Represents a performance validation.
/// </summary>
public sealed class PerformanceValidation
{
    /// <summary>
    /// Gets or sets the validation status.
    /// </summary>
    [JsonPropertyName("status")]
    public CheckStatus Status { get; set; }

    /// <summary>
    /// Gets the metric results.
    /// </summary>
    [JsonPropertyName("metrics")]
    public List<PerformanceMetric> Metrics { get; } = [];
}

/// <summary>
/// Represents a performance metric.
/// </summary>
public sealed class PerformanceMetric
{
    /// <summary>
    /// Gets or sets the metric name.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the actual value.
    /// </summary>
    [JsonPropertyName("actual")]
    public required double Actual { get; init; }

    /// <summary>
    /// Gets or sets the expected value.
    /// </summary>
    [JsonPropertyName("expected")]
    public required double Expected { get; init; }

    /// <summary>
    /// Gets or sets the tolerance percentage.
    /// </summary>
    [JsonPropertyName("tolerancePercent")]
    public double TolerancePercent { get; init; } = 10.0;

    /// <summary>
    /// Gets or sets the metric status.
    /// </summary>
    [JsonPropertyName("status")]
    public required CheckStatus Status { get; init; }
}

/// <summary>
/// Represents a go/no-go decision.
/// </summary>
public sealed class GoNoGoDecision
{
    /// <summary>
    /// Gets or sets the decision.
    /// </summary>
    [JsonPropertyName("decision")]
    public required GoNoGoStatus Decision { get; init; }

    /// <summary>
    /// Gets or sets the reason.
    /// </summary>
    [JsonPropertyName("reason")]
    public required string Reason { get; init; }

    /// <summary>
    /// Gets or sets the approver.
    /// </summary>
    [JsonPropertyName("approver")]
    public required string Approver { get; init; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
