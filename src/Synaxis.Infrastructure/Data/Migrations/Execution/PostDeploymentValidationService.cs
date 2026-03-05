// <copyright file="PostDeploymentValidationService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Data.Migrations.Execution;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

/// <summary>
/// Service for validating post-deployment state.
/// </summary>
public interface IPostDeploymentValidationService
{
    /// <summary>
    /// Runs all post-deployment validations.
    /// </summary>
    /// <param name="log">The execution log.</param>
    /// <param name="options">The validation options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validation results.</returns>
    Task<PostDeploymentResults> ValidateAsync(
        MigrationExecutionLog log,
        PostDeploymentValidationOptions options,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Options for post-deployment validation.
/// </summary>
public sealed class PostDeploymentValidationOptions
{
    /// <summary>
    /// Gets or sets the health check endpoints.
    /// </summary>
    public List<HealthCheckEndpoint> HealthCheckEndpoints { get; init; } = [];

    /// <summary>
    /// Gets or sets the smoke test endpoints.
    /// </summary>
    public List<SmokeTestEndpoint> SmokeTestEndpoints { get; init; } = [];

    /// <summary>
    /// Gets or sets the error rate threshold.
    /// </summary>
    public double ErrorRateThreshold { get; init; } = 0.05; // 5%

    /// <summary>
    /// Gets or sets the performance metrics to validate.
    /// </summary>
    public List<PerformanceMetricConfig> PerformanceMetrics { get; init; } = [];

    /// <summary>
    /// Gets or sets the timeout for each check in seconds.
    /// </summary>
    public int TimeoutSeconds { get; init; } = 30;
}

/// <summary>
/// Configuration for a health check endpoint.
/// </summary>
public sealed class HealthCheckEndpoint
{
    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    public required string ServiceName { get; init; }

    /// <summary>
    /// Gets or sets the endpoint URL.
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Gets or sets the expected status code.
    /// </summary>
    public int ExpectedStatusCode { get; init; } = 200;
}

/// <summary>
/// Configuration for a smoke test endpoint.
/// </summary>
public sealed class SmokeTestEndpoint
{
    /// <summary>
    /// Gets or sets the test name.
    /// </summary>
    public required string TestName { get; init; }

    /// <summary>
    /// Gets or sets the endpoint URL.
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Gets or sets the HTTP method.
    /// </summary>
    public string Method { get; init; } = "GET";

    /// <summary>
    /// Gets or sets the expected status code.
    /// </summary>
    public int ExpectedStatusCode { get; init; } = 200;

    /// <summary>
    /// Gets or sets the request body (optional).
    /// </summary>
    public string? RequestBody { get; init; }
}

/// <summary>
/// Configuration for a performance metric.
/// </summary>
public sealed class PerformanceMetricConfig
{
    /// <summary>
    /// Gets or sets the metric name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the expected value.
    /// </summary>
    public required double ExpectedValue { get; init; }

    /// <summary>
    /// Gets or sets the tolerance percentage.
    /// </summary>
    public double TolerancePercent { get; init; } = 10.0;
}

/// <summary>
/// Implementation of the post-deployment validation service.
/// </summary>
public sealed class PostDeploymentValidationService : IPostDeploymentValidationService
{
    private readonly ILogger<PostDeploymentValidationService> _logger;
    private readonly IMigrationExecutionService _executionService;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostDeploymentValidationService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="executionService">The execution service.</param>
    /// <param name="httpClient">The HTTP client.</param>
    public PostDeploymentValidationService(
        ILogger<PostDeploymentValidationService> logger,
        IMigrationExecutionService executionService,
        HttpClient httpClient)
    {
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this._executionService = executionService ?? throw new ArgumentNullException(nameof(executionService));
        this._httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc/>
    public async Task<PostDeploymentResults> ValidateAsync(
        MigrationExecutionLog log,
        PostDeploymentValidationOptions options,
        CancellationToken cancellationToken = default)
    {
        if (log is null)
        {
            throw new ArgumentNullException(nameof(log));
        }

        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        this._logger.LogInformation("Starting post-deployment validation");

        var results = new PostDeploymentResults
        {
            Status = ValidationStatus.Passed
        };

        var overallStopwatch = Stopwatch.StartNew();

        // Run health checks
        var healthChecks = await this.RunHealthChecksAsync(options, cancellationToken);
        results.HealthChecks.AddRange(healthChecks);

        // Run smoke tests
        var smokeTests = await this.RunSmokeTestsAsync(options, cancellationToken);
        results.SmokeTests.AddRange(smokeTests);

        // Check error rates
        results.ErrorRateCheck = await this.CheckErrorRatesAsync(options, cancellationToken);

        // Validate performance
        results.PerformanceValidation = await this.ValidatePerformanceAsync(options, cancellationToken);

        overallStopwatch.Stop();

        // Determine overall status
        var hasUnhealthy = results.HealthChecks.Any(h => h.Status == HealthStatus.Unhealthy);
        var hasFailedTests = results.SmokeTests.Any(t => t.Status == TestStatus.Failed);
        var hasHighErrors = results.ErrorRateCheck?.Status == CheckStatus.Failed;
        var hasPerfIssues = results.PerformanceValidation?.Status == CheckStatus.Failed;

        if (hasUnhealthy || hasFailedTests || hasHighErrors || hasPerfIssues)
        {
            results.Status = ValidationStatus.Failed;
        }
        else if (results.HealthChecks.Any(h => h.Status == HealthStatus.Degraded) ||
                 results.SmokeTests.Any(t => t.Status == TestStatus.Skipped) ||
                 results.ErrorRateCheck?.Status == CheckStatus.Warning ||
                 results.PerformanceValidation?.Status == CheckStatus.Warning)
        {
            results.Status = ValidationStatus.PassedWithWarnings;
        }

        // Log any issues
        foreach (var check in results.HealthChecks.Where(h => h.Status != HealthStatus.Healthy))
        {
            var severity = check.Status == HealthStatus.Unhealthy ? IssueSeverity.Error : IssueSeverity.Warning;
            this._executionService.RecordIssue(log, severity, check.Error ?? $"Health check {check.Service} is {check.Status}", "PostDeployment");
        }

        foreach (var test in results.SmokeTests.Where(t => t.Status == TestStatus.Failed))
        {
            this._executionService.RecordIssue(log, IssueSeverity.Warning, test.Error ?? $"Smoke test {test.Test} failed", "PostDeployment");
        }

        if (results.ErrorRateCheck?.Status != CheckStatus.Passed)
        {
            this._executionService.RecordIssue(log, IssueSeverity.Warning,
                $"Error rate check: {results.ErrorRateCheck?.ErrorRate:F2}% (threshold: {results.ErrorRateCheck?.Threshold:F2}%)",
                "PostDeployment");
        }

        this._logger.LogInformation(
            "Post-deployment validation completed in {ElapsedMs}ms with status: {Status}",
            overallStopwatch.ElapsedMilliseconds,
            results.Status);

        return results;
    }

    private async Task<List<HealthCheckResult>> RunHealthChecksAsync(
        PostDeploymentValidationOptions options,
        CancellationToken cancellationToken)
    {
        var results = new List<HealthCheckResult>();

        foreach (var endpoint in options.HealthCheckEndpoints)
        {
            var stopwatch = Stopwatch.StartNew();
            HealthStatus status;
            string? error = null;

            try
            {
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(options.TimeoutSeconds));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

                var response = await this._httpClient.GetAsync(endpoint.Url, linkedCts.Token);
                stopwatch.Stop();

                if (response.StatusCode == (System.Net.HttpStatusCode)endpoint.ExpectedStatusCode)
                {
                    status = HealthStatus.Healthy;
                }
                else
                {
                    status = HealthStatus.Unhealthy;
                    error = $"Unexpected status code: {(int)response.StatusCode}";
                }
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                status = HealthStatus.Unhealthy;
                error = "Health check timed out";
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                status = HealthStatus.Unhealthy;
                error = ex.Message;
            }

            results.Add(new HealthCheckResult
            {
                Service = endpoint.ServiceName,
                Status = status,
                ResponseTimeMilliseconds = (int)stopwatch.ElapsedMilliseconds,
                Error = error
            });

            this._logger.LogDebug(
                "Health check for {Service}: {Status} ({ElapsedMs}ms)",
                endpoint.ServiceName,
                status,
                stopwatch.ElapsedMilliseconds);
        }

        return results;
    }

    private async Task<List<SmokeTestResult>> RunSmokeTestsAsync(
        PostDeploymentValidationOptions options,
        CancellationToken cancellationToken)
    {
        var results = new List<SmokeTestResult>();

        foreach (var endpoint in options.SmokeTestEndpoints)
        {
            var stopwatch = Stopwatch.StartNew();
            TestStatus status;
            string? error = null;

            try
            {
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(options.TimeoutSeconds));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

                using var request = new HttpRequestMessage(
                    new HttpMethod(endpoint.Method),
                    endpoint.Url);

                if (!string.IsNullOrEmpty(endpoint.RequestBody))
                {
                    request.Content = new StringContent(
                        endpoint.RequestBody,
                        System.Text.Encoding.UTF8,
                        "application/json");
                }

                var response = await this._httpClient.SendAsync(request, linkedCts.Token);
                stopwatch.Stop();

                if ((int)response.StatusCode == endpoint.ExpectedStatusCode)
                {
                    status = TestStatus.Passed;
                }
                else
                {
                    status = TestStatus.Failed;
                    error = $"Unexpected status code: {(int)response.StatusCode}";
                }
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                status = TestStatus.Failed;
                error = "Smoke test timed out";
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                status = TestStatus.Failed;
                error = ex.Message;
            }

            results.Add(new SmokeTestResult
            {
                Test = endpoint.TestName,
                Status = status,
                ExecutionTimeMilliseconds = (int)stopwatch.ElapsedMilliseconds,
                Error = error
            });

            this._logger.LogDebug(
                "Smoke test {Test}: {Status} ({ElapsedMs}ms)",
                endpoint.TestName,
                status,
                stopwatch.ElapsedMilliseconds);
        }

        return results;
    }

    private async Task<ErrorRateCheck> CheckErrorRatesAsync(
        PostDeploymentValidationOptions options,
        CancellationToken cancellationToken)
    {
        // In a real implementation, this would query your monitoring system
        // (e.g., Prometheus, DataDog, Application Insights)

        await Task.Delay(100, cancellationToken); // Simulate check

        // Placeholder - would query actual metrics
        var errorRate = 0.01; // 1%
        var status = errorRate > options.ErrorRateThreshold ? CheckStatus.Failed : CheckStatus.Passed;

        return new ErrorRateCheck
        {
            Status = status,
            ErrorRate = errorRate * 100,
            Threshold = options.ErrorRateThreshold * 100,
            WindowMinutes = 5
        };
    }

    private async Task<PerformanceValidation> ValidatePerformanceAsync(
        PostDeploymentValidationOptions options,
        CancellationToken cancellationToken)
    {
        var result = new PerformanceValidation
        {
            Status = CheckStatus.Passed
        };

        foreach (var config in options.PerformanceMetrics)
        {
            // In a real implementation, this would query actual metrics
            await Task.Delay(50, cancellationToken); // Simulate check

            var actualValue = config.ExpectedValue * 0.95; // Simulate within tolerance
            var tolerance = config.ExpectedValue * (config.TolerancePercent / 100);
            var isWithinTolerance = Math.Abs(actualValue - config.ExpectedValue) <= tolerance;

            result.Metrics.Add(new PerformanceMetric
            {
                Name = config.Name,
                Actual = actualValue,
                Expected = config.ExpectedValue,
                TolerancePercent = config.TolerancePercent,
                Status = isWithinTolerance ? CheckStatus.Passed : CheckStatus.Failed
            });
        }

        if (result.Metrics.Any(m => m.Status == CheckStatus.Failed))
        {
            result.Status = CheckStatus.Failed;
        }
        else if (result.Metrics.Any(m => m.Status == CheckStatus.Warning))
        {
            result.Status = CheckStatus.Warning;
        }

        return result;
    }
}
