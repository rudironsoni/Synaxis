// <copyright file="NoOpServiceDiscoveryHealthCheck.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Health.Checks
{
    using Microsoft.Extensions.Diagnostics.HealthChecks;

    /// <summary>
    /// No-op service discovery health check used when no client is registered.
    /// </summary>
    public class NoOpServiceDiscoveryHealthCheck : IHealthCheck
    {
        /// <summary>
        /// Runs the health check.
        /// </summary>
        /// <param name="context">The health check context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The health check result.</returns>
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                "Service discovery client not registered",
                data: new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["reason"] = "No IServiceDiscoveryClient implementation registered in DI",
                }));
        }
    }
}
