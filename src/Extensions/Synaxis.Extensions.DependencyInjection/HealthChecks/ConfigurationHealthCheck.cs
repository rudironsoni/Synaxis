// <copyright file="ConfigurationHealthCheck.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Extensions.DependencyInjection.HealthChecks;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Synaxis.Configuration.Options;

/// <summary>
/// Health check for validating configuration settings.
/// </summary>
public class ConfigurationHealthCheck : IHealthCheck
{
    private readonly IOptionsMonitor<CloudProviderOptions> _cloudOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationHealthCheck"/> class.
    /// </summary>
    /// <param name="cloudOptions">The cloud provider options monitor.</param>
    public ConfigurationHealthCheck(IOptionsMonitor<CloudProviderOptions> cloudOptions)
    {
        this._cloudOptions = cloudOptions ?? throw new ArgumentNullException(nameof(cloudOptions));
    }

    /// <summary>
    /// Runs the health check, returning the status of the configuration.
    /// </summary>
    /// <param name="context">A context object associated with the current execution.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the health check.</param>
    /// <returns>A <see cref="Task"/> that completes when the health check has finished, yielding the health check result.</returns>
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var config = this._cloudOptions.CurrentValue;

        if (string.IsNullOrWhiteSpace(config.DefaultProvider))
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                "Default cloud provider is not set."));
        }

        var provider = config.DefaultProvider.ToLowerInvariant();
        var isValidProvider = provider switch
        {
            "azure" => config.Azure != null,
            "aws" => config.Aws != null,
            "gcp" => config.Gcp != null,
            "onpremise" => config.OnPremise != null,
            _ => false,
        };

        if (!isValidProvider)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                $"Cloud provider '{config.DefaultProvider}' configuration is not available."));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            "Configuration is valid."));
    }
}
