// <copyright file="ConfigHealthCheck.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Health
{
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using Microsoft.Extensions.Options;
    using Synaxis.InferenceGateway.Application.Configuration;

    /// <summary>
    /// Health check for configuration validation.
    /// </summary>
    public class ConfigHealthCheck : IHealthCheck
    {
        private readonly SynaxisConfiguration _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigHealthCheck"/> class.
        /// </summary>
        /// <param name="config">The configuration options.</param>
        public ConfigHealthCheck(IOptions<SynaxisConfiguration> config)
        {
            _config = config.Value;
        }

        /// <summary>
        /// Checks the health of the configuration.
        /// </summary>
        /// <param name="context">The health check context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The health check result.</returns>
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (_config.Providers == null || _config.Providers.Count == 0)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("No providers configured."));
        }

        foreach (var model in _config.CanonicalModels)
        {
            if (!_config.Providers.ContainsKey(model.Provider))
            {
                return Task.FromResult(HealthCheckResult.Unhealthy($"Canonical model '{model.Id}' references unknown provider '{model.Provider}'."));
            }
        }

        foreach (var alias in _config.Aliases)
        {
            foreach (var candidate in alias.Value.Candidates)
            {
                if (!_config.CanonicalModels.Any(m => m.Id == candidate))
                {
                    return Task.FromResult(HealthCheckResult.Unhealthy($"Alias '{alias.Key}' references unknown canonical model '{candidate}'."));
                }
            }
        }

        return Task.FromResult(HealthCheckResult.Healthy("Configuration is consistent."));
    }
    }

}