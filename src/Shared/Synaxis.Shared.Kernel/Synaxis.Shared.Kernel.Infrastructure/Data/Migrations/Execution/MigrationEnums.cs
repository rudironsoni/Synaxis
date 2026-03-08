// <copyright file="MigrationEnums.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Kernel.Infrastructure.Data.Migrations.Execution;

using System.Text.Json.Serialization;

/// <summary>
/// Represents the migration execution status.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MigrationStatus
{
    /// <summary>
    /// Migration is initializing.
    /// </summary>
    Initializing,

    /// <summary>
    /// Migration is in progress.
    /// </summary>
    InProgress,

    /// <summary>
    /// Migration completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Migration failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Migration was rolled back.
    /// </summary>
    RolledBack,

    /// <summary>
    /// Migration was cancelled.
    /// </summary>
    Cancelled
}

/// <summary>
/// Represents the migration phase status.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MigrationPhaseStatus
{
    /// <summary>
    /// Phase is pending.
    /// </summary>
    Pending,

    /// <summary>
    /// Phase is in progress.
    /// </summary>
    InProgress,

    /// <summary>
    /// Phase completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Phase failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Phase was skipped.
    /// </summary>
    Skipped,

    /// <summary>
    /// Phase was a dry run.
    /// </summary>
    DryRun
}

/// <summary>
/// Represents issue severity levels.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum IssueSeverity
{
    /// <summary>
    /// Informational issue.
    /// </summary>
    Info,

    /// <summary>
    /// Warning issue.
    /// </summary>
    Warning,

    /// <summary>
    /// Error issue.
    /// </summary>
    Error,

    /// <summary>
    /// Critical issue.
    /// </summary>
    Critical
}

/// <summary>
/// Represents pre-flight check status.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PreflightStatus
{
    /// <summary>
    /// All checks passed.
    /// </summary>
    Passed,

    /// <summary>
    /// Some checks passed with warnings.
    /// </summary>
    PassedWithWarnings,

    /// <summary>
    /// Checks failed.
    /// </summary>
    Failed
}

/// <summary>
/// Represents pre-flight check result.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PreflightCheckResult
{
    /// <summary>
    /// Check passed.
    /// </summary>
    Passed,

    /// <summary>
    /// Check passed with warning.
    /// </summary>
    Warning,

    /// <summary>
    /// Check failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Check was skipped.
    /// </summary>
    Skipped
}

/// <summary>
/// Represents post-deployment validation status.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ValidationStatus
{
    /// <summary>
    /// All validations passed.
    /// </summary>
    Passed,

    /// <summary>
    /// Validations passed with warnings.
    /// </summary>
    PassedWithWarnings,

    /// <summary>
    /// Validations failed.
    /// </summary>
    Failed
}

/// <summary>
/// Represents health status.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum HealthStatus
{
    /// <summary>
    /// Service is healthy.
    /// </summary>
    Healthy,

    /// <summary>
    /// Service is degraded.
    /// </summary>
    Degraded,

    /// <summary>
    /// Service is unhealthy.
    /// </summary>
    Unhealthy
}

/// <summary>
/// Represents test status.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TestStatus
{
    /// <summary>
    /// Test passed.
    /// </summary>
    Passed,

    /// <summary>
    /// Test failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Test was skipped.
    /// </summary>
    Skipped
}

/// <summary>
/// Represents check status.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CheckStatus
{
    /// <summary>
    /// Check passed.
    /// </summary>
    Passed,

    /// <summary>
    /// Check passed with warning.
    /// </summary>
    Warning,

    /// <summary>
    /// Check failed.
    /// </summary>
    Failed
}

/// <summary>
/// Represents go/no-go decision status.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GoNoGoStatus
{
    /// <summary>
    /// Go - proceed with migration.
    /// </summary>
    Go,

    /// <summary>
    /// No-go - do not proceed.
    /// </summary>
    NoGo,

    /// <summary>
    /// Go with conditions.
    /// </summary>
    GoWithConditions,

    /// <summary>
    /// Decision pending.
    /// </summary>
    Pending
}
