// <copyright file="MigrationExecutionService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Kernel.Infrastructure.Data.Migrations.Execution;

using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

/// <summary>
/// Service for executing and tracking production database migrations.
/// </summary>
public interface IMigrationExecutionService
{
    /// <summary>
    /// Creates a new migration execution context.
    /// </summary>
    /// <param name="environment">The target environment.</param>
    /// <param name="isDryRun">Whether this is a dry run.</param>
    /// <returns>The created execution log.</returns>
    MigrationExecutionLog CreateExecutionContext(string environment, bool isDryRun = false);

    /// <summary>
    /// Executes a phase of the migration.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="log">The execution log.</param>
    /// <param name="phaseName">The phase name.</param>
    /// <param name="action">The phase action.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The phase result.</returns>
    Task<TResult> ExecutePhaseAsync<TResult>(
        MigrationExecutionLog log,
        string phaseName,
        Func<CancellationToken, Task<TResult>> action,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records an issue in the execution log.
    /// </summary>
    /// <param name="log">The execution log.</param>
    /// <param name="severity">The issue severity.</param>
    /// <param name="message">The issue message.</param>
    /// <param name="component">The affected component.</param>
    void RecordIssue(MigrationExecutionLog log, IssueSeverity severity, string message, string component = "unknown");

    /// <summary>
    /// Records a decision in the execution log.
    /// </summary>
    /// <param name="log">The execution log.</param>
    /// <param name="decision">The decision.</param>
    /// <param name="reason">The decision reason.</param>
    /// <param name="approver">The decision approver.</param>
    void RecordDecision(MigrationExecutionLog log, string decision, string reason, string approver);

    /// <summary>
    /// Finalizes the migration execution.
    /// </summary>
    /// <param name="log">The execution log.</param>
    /// <param name="success">Whether the migration succeeded.</param>
    void FinalizeExecution(MigrationExecutionLog log, bool success);

    /// <summary>
    /// Saves the execution log to disk.
    /// </summary>
    /// <param name="log">The execution log.</param>
    /// <param name="outputPath">The output file path.</param>
    Task SaveExecutionLogAsync(MigrationExecutionLog log, string outputPath);
}

/// <summary>
/// Implementation of the migration execution service.
/// </summary>
public sealed class MigrationExecutionService : IMigrationExecutionService
{
    private readonly ILogger<MigrationExecutionService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationExecutionService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public MigrationExecutionService(ILogger<MigrationExecutionService> logger)
    {
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this._jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true
        };
    }

    /// <inheritdoc/>
    public MigrationExecutionLog CreateExecutionContext(string environment, bool isDryRun = false)
    {
        if (string.IsNullOrWhiteSpace(environment))
        {
            throw new ArgumentException("Environment cannot be null or empty", nameof(environment));
        }

        var migrationId = $"{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}_{environment}";

        this._logger.LogInformation(
            "Creating migration execution context: {MigrationId} for environment: {Environment}",
            migrationId,
            environment);

        return new MigrationExecutionLog
        {
            MigrationId = migrationId,
            Environment = environment,
            IsDryRun = isDryRun,
            Status = MigrationStatus.Initializing,
            StartedAt = DateTimeOffset.UtcNow
        };
    }

    /// <inheritdoc/>
    public async Task<TResult> ExecutePhaseAsync<TResult>(
        MigrationExecutionLog log,
        string phaseName,
        Func<CancellationToken, Task<TResult>> action,
        CancellationToken cancellationToken = default)
    {
        if (log is null)
        {
            throw new ArgumentNullException(nameof(log));
        }

        if (string.IsNullOrWhiteSpace(phaseName))
        {
            throw new ArgumentException("Phase name cannot be null or empty", nameof(phaseName));
        }

        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        this._logger.LogInformation("Starting migration phase: {PhaseName}", phaseName);

        var stopwatch = Stopwatch.StartNew();
        TResult result;

        try
        {
            result = await action(cancellationToken);
            stopwatch.Stop();

            log.AddPhase(phaseName, MigrationPhaseStatus.Completed, (int)stopwatch.Elapsed.TotalSeconds);

            this._logger.LogInformation(
                "Completed migration phase: {PhaseName} in {ElapsedMs}ms",
                phaseName,
                stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            log.AddPhase(phaseName, MigrationPhaseStatus.Failed, (int)stopwatch.Elapsed.TotalSeconds);
            this._logger.LogWarning("Migration phase cancelled: {PhaseName}", phaseName);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            log.AddPhase(phaseName, MigrationPhaseStatus.Failed, (int)stopwatch.Elapsed.TotalSeconds);
            this._logger.LogError(
                ex,
                "Migration phase failed: {PhaseName} after {ElapsedMs}ms",
                phaseName,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    /// <inheritdoc/>
    public void RecordIssue(MigrationExecutionLog log, IssueSeverity severity, string message, string component = "unknown")
    {
        if (log is null)
        {
            throw new ArgumentNullException(nameof(log));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Message cannot be null or empty", nameof(message));
        }

        log.RecordIssue(severity, message, component);

        switch (severity)
        {
            case IssueSeverity.Critical:
                this._logger.LogCritical("[{Component}] {Message}", component, message);
                break;
            case IssueSeverity.Error:
                this._logger.LogError("[{Component}] {Message}", component, message);
                break;
            case IssueSeverity.Warning:
                this._logger.LogWarning("[{Component}] {Message}", component, message);
                break;
            default:
                this._logger.LogInformation("[{Component}] {Message}", component, message);
                break;
        }
    }

    /// <inheritdoc/>
    public void RecordDecision(MigrationExecutionLog log, string decision, string reason, string approver)
    {
        if (log is null)
        {
            throw new ArgumentNullException(nameof(log));
        }

        if (string.IsNullOrWhiteSpace(decision))
        {
            throw new ArgumentException("Decision cannot be null or empty", nameof(decision));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Reason cannot be null or empty", nameof(reason));
        }

        if (string.IsNullOrWhiteSpace(approver))
        {
            throw new ArgumentException("Approver cannot be null or empty", nameof(approver));
        }

        log.RecordDecision(decision, reason, approver);

        this._logger.LogInformation(
            "Migration decision recorded: {Decision} by {Approver}. Reason: {Reason}",
            decision,
            approver,
            reason);
    }

    /// <inheritdoc/>
    public void FinalizeExecution(MigrationExecutionLog log, bool success)
    {
        if (log is null)
        {
            throw new ArgumentNullException(nameof(log));
        }

        if (success)
        {
            log.MarkCompleted();
            this._logger.LogInformation(
                "Migration {MigrationId} completed successfully in {Duration}s",
                log.MigrationId,
                log.DurationSeconds);
        }
        else
        {
            log.MarkFailed();
            this._logger.LogError(
                "Migration {MigrationId} failed after {Duration}s",
                log.MigrationId,
                log.DurationSeconds);
        }
    }

    /// <inheritdoc/>
    public async Task SaveExecutionLogAsync(MigrationExecutionLog log, string outputPath)
    {
        if (log is null)
        {
            throw new ArgumentNullException(nameof(log));
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Output path cannot be null or empty", nameof(outputPath));
        }

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(log, this._jsonOptions);
        await File.WriteAllTextAsync(outputPath, json);

        this._logger.LogInformation("Migration execution log saved to: {OutputPath}", outputPath);
    }
}
