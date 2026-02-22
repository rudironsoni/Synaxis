// <copyright file="ServiceDiscoveryHealthCheck.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Health.Checks
{
    using System.Diagnostics;
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Health check for service discovery/registry accessibility.
    /// </summary>
    public class ServiceDiscoveryHealthCheck : IHealthCheck
    {
        private readonly IServiceDiscoveryClient _discoveryClient;
        private readonly ILogger<ServiceDiscoveryHealthCheck> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceDiscoveryHealthCheck"/> class.
        /// </summary>
        /// <param name="discoveryClient">The service discovery client.</param>
        /// <param name="logger">The logger.</param>
        public ServiceDiscoveryHealthCheck(
            IServiceDiscoveryClient discoveryClient,
            ILogger<ServiceDiscoveryHealthCheck> logger)
        {
            ArgumentNullException.ThrowIfNull(discoveryClient);
            this._discoveryClient = discoveryClient;
            ArgumentNullException.ThrowIfNull(logger);
            this._logger = logger;
        }

        /// <summary>
        /// Runs the health check, returning the status of the service discovery registry.
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
                this._logger.LogDebug("Checking service discovery health...");

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
            catch (Exception ex)
            {
                stopwatch.Stop();
                data["latency_ms"] = stopwatch.ElapsedMilliseconds;
                data["error"] = ex.Message;

                this._logger.LogError(
                    ex,
                    "Service discovery health check failed after {LatencyMs}ms",
                    stopwatch.ElapsedMilliseconds);

                return HealthCheckResult.Unhealthy(
                    $"Service discovery health check failed: {ex.Message}",
                    ex,
                    data);
            }
        }

        private async Task<HealthCheckResult> ExecuteHealthCheckAsync(
            Stopwatch stopwatch,
            Dictionary<string, object> data,
            CancellationToken cancellationToken)
        {
            // Check if registry is accessible
            var isAccessible = await this._discoveryClient.IsRegistryAccessibleAsync(cancellationToken)
                .ConfigureAwait(false);

            if (!isAccessible)
            {
                stopwatch.Stop();
                data["latency_ms"] = stopwatch.ElapsedMilliseconds;
                data["registry_accessible"] = false;

                this._logger.LogError(
                    "Service discovery registry is not accessible after {LatencyMs}ms",
                    stopwatch.ElapsedMilliseconds);

                return HealthCheckResult.Unhealthy(
                    "Service discovery registry is not accessible",
                    data: data);
            }

            // Get registered services count
            var servicesCount = await this._discoveryClient.GetRegisteredServicesCountAsync(cancellationToken)
                .ConfigureAwait(false);

            stopwatch.Stop();
            var latencyMs = stopwatch.ElapsedMilliseconds;

            data["latency_ms"] = latencyMs;
            data["registry_accessible"] = true;
            data["registered_services_count"] = servicesCount;

            this._logger.LogInformation(
                "Service discovery health check passed in {LatencyMs}ms with {Count} registered services",
                latencyMs,
                servicesCount);

            return HealthCheckResult.Healthy(
                $"Service discovery is healthy ({servicesCount} services registered)",
                data);
        }
    }
}
