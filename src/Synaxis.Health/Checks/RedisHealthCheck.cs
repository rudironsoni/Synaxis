// <copyright file="RedisHealthCheck.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Health.Checks
{
    using System.Diagnostics;
    using System.Net;
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using Microsoft.Extensions.Logging;
    using StackExchange.Redis;

    /// <summary>
    /// Health check for Redis connectivity.
    /// </summary>
    public class RedisHealthCheck : IHealthCheck
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly ILogger<RedisHealthCheck> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisHealthCheck"/> class.
        /// </summary>
        /// <param name="connectionMultiplexer">The Redis connection multiplexer.</param>
        /// <param name="logger">The logger.</param>
        public RedisHealthCheck(
            IConnectionMultiplexer connectionMultiplexer,
            ILogger<RedisHealthCheck> logger)
        {
            ArgumentNullException.ThrowIfNull(connectionMultiplexer);
            this._connectionMultiplexer = connectionMultiplexer;
            ArgumentNullException.ThrowIfNull(logger);
            this._logger = logger;
        }

        /// <summary>
        /// Runs the health check, returning the status of the Redis server.
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
                this._logger.LogDebug("Checking Redis health...");

                var result = await this.ExecuteHealthCheckAsync(stopwatch, data).ConfigureAwait(false);
                return result;
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                data["latency_ms"] = stopwatch.ElapsedMilliseconds;
                data["error"] = "Health check was cancelled";
                throw;
            }
            catch (RedisConnectionException ex)
            {
                stopwatch.Stop();
                data["latency_ms"] = stopwatch.ElapsedMilliseconds;
                data["error"] = ex.Message;

                this._logger.LogError(
                    ex,
                    "Redis connection failed after {LatencyMs}ms",
                    stopwatch.ElapsedMilliseconds);

                return HealthCheckResult.Unhealthy(
                    $"Redis connection failed: {ex.Message}",
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
                    "Redis health check failed with unexpected error after {LatencyMs}ms",
                    stopwatch.ElapsedMilliseconds);

                return HealthCheckResult.Unhealthy(
                    $"Redis health check failed: {ex.Message}",
                    ex,
                    data);
            }
        }

        private async Task<HealthCheckResult> ExecuteHealthCheckAsync(
            Stopwatch stopwatch,
            Dictionary<string, object> data)
        {
            var endpoints = this._connectionMultiplexer.GetEndPoints();
            data["endpoints_count"] = endpoints.Length;

            // Wait for connection with timeout and retry
            var maxRetries = 30;
            var retryDelay = TimeSpan.FromMilliseconds(200);
            for (var attempt = 0; attempt < maxRetries && !this._connectionMultiplexer.IsConnected; attempt++)
            {
                await Task.Delay(retryDelay).ConfigureAwait(false);
            }

            // Check connection state
            if (!this._connectionMultiplexer.IsConnected)
            {
                stopwatch.Stop();
                data["latency_ms"] = stopwatch.ElapsedMilliseconds;
                data["connection_state"] = "Disconnected";

                this._logger.LogError("Redis is not connected after {MaxRetries} retries", maxRetries);
                return HealthCheckResult.Unhealthy(
                    "Redis is not connected",
                    data: data);
            }

            // Ping Redis
            var db = this._connectionMultiplexer.GetDatabase();
            var pingResult = await db.PingAsync().ConfigureAwait(false);
            stopwatch.Stop();
            var latencyMs = stopwatch.ElapsedMilliseconds;

            data["latency_ms"] = latencyMs;
            data["ping_response"] = pingResult.TotalMilliseconds;
            data["connection_state"] = "Connected";

            // Get server info if connected (best effort - may fail without admin permissions)
            try
            {
                await this.PopulateServerInfoAsync(data, endpoints).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this._logger.LogWarning(
                    ex,
                    "Failed to populate Redis server info (this is normal without admin permissions)");
                data["server_info_error"] = "Unable to retrieve server info (admin permissions required)";
            }

            this._logger.LogInformation(
                "Redis health check passed in {LatencyMs}ms",
                latencyMs);

            return HealthCheckResult.Healthy(
                "Redis is healthy",
                data);
        }

        private async Task PopulateServerInfoAsync(
            Dictionary<string, object> data,
            EndPoint[] endpoints)
        {
            if (endpoints.Length == 0)
            {
                return;
            }

            var server = this._connectionMultiplexer.GetServer(endpoints[0]);
            var info = await server.InfoAsync().ConfigureAwait(false);
            var serverSection = info.FirstOrDefault(s => string.Equals(s.Key, "server", StringComparison.Ordinal));
            if (serverSection != null)
            {
                var version = serverSection.FirstOrDefault(i => string.Equals(i.Key, "redis_version", StringComparison.Ordinal));
                if (!string.IsNullOrEmpty(version.Value))
                {
                    data["redis_version"] = version.Value;
                }
            }
        }
    }
}
