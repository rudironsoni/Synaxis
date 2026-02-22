// <copyright file="DatabaseHealthCheck.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Health.Checks
{
    using System.Diagnostics;
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using Microsoft.Extensions.Logging;
    using Npgsql;

    /// <summary>
    /// Health check for PostgreSQL database connectivity.
    /// </summary>
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;
        private readonly ILogger<DatabaseHealthCheck> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseHealthCheck"/> class.
        /// </summary>
        /// <param name="connectionString">The PostgreSQL connection string.</param>
        /// <param name="logger">The logger.</param>
        public DatabaseHealthCheck(string connectionString, ILogger<DatabaseHealthCheck> logger)
        {
            ArgumentNullException.ThrowIfNull(connectionString);
            this._connectionString = connectionString;
            ArgumentNullException.ThrowIfNull(logger);
            this._logger = logger;
        }

        /// <summary>
        /// Runs the health check, returning the status of the PostgreSQL database.
        /// </summary>
        /// <param name="context">The health check context.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The health check result.</returns>
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var data = new Dictionary<string, object>(StringComparer.Ordinal);

            try
            {
                this._logger.LogDebug("Checking PostgreSQL health...");

                var result = await this.ExecuteHealthCheckAsync(stopwatch, data, cancellationToken)
                    .ConfigureAwait(false);
                return result;
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                data["latency_ms"] = stopwatch.ElapsedMilliseconds;
                data["error"] = "Health check was cancelled";
                throw;
            }
            catch (NpgsqlException ex)
            {
                stopwatch.Stop();
                data["latency_ms"] = stopwatch.ElapsedMilliseconds;
                data["error"] = ex.Message;
                data["error_code"] = ex.ErrorCode;

                this._logger.LogError(
                    ex,
                    "PostgreSQL health check failed after {LatencyMs}ms",
                    stopwatch.ElapsedMilliseconds);

                return HealthCheckResult.Unhealthy(
                    $"PostgreSQL connection failed: {ex.Message}",
                    ex,
                    data);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                data["latency_ms"] = stopwatch.ElapsedMilliseconds;
                data["error"] = ex.Message;

                this._logger.LogError(
                    ex,
                    "PostgreSQL health check failed with unexpected error after {LatencyMs}ms",
                    stopwatch.ElapsedMilliseconds);

                return HealthCheckResult.Unhealthy(
                    $"PostgreSQL health check failed: {ex.Message}",
                    ex,
                    data);
            }
        }

        private async Task<HealthCheckResult> ExecuteHealthCheckAsync(
            Stopwatch stopwatch,
            Dictionary<string, object> data,
            CancellationToken cancellationToken)
        {
#pragma warning disable MA0004 // await using doesn't need ConfigureAwait
            await using var connection = new NpgsqlConnection(this._connectionString);
#pragma warning restore MA0004
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            // Execute simple query to verify connectivity
#pragma warning disable MA0004 // await using doesn't need ConfigureAwait
            await using var command = new NpgsqlCommand("SELECT 1", connection);
#pragma warning restore MA0004
            _ = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();
            var latencyMs = stopwatch.ElapsedMilliseconds;

            data["latency_ms"] = latencyMs;
            data["connection_state"] = connection.State.ToString();
            data["server_version"] = connection.ServerVersion;

            this._logger.LogInformation(
                "PostgreSQL health check passed in {LatencyMs}ms",
                latencyMs);

            return HealthCheckResult.Healthy(
                "PostgreSQL is healthy",
                data);
        }
    }
}
