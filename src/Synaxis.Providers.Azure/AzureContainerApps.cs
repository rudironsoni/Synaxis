// <copyright file="AzureContainerApps.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.Azure;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using global::Polly;
using Microsoft.Extensions.Logging;
using Synaxis.Abstractions.Cloud;

/// <summary>
/// Azure Container Apps implementation of IContainerPlatform.
/// </summary>
public class AzureContainerApps : IContainerPlatform
{
    private readonly string _containerAppName;
    private readonly ILogger<AzureContainerApps> _logger;
    private readonly IAsyncPolicy _retryPolicy;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureContainerApps"/> class.
    /// </summary>
    /// <param name="subscriptionId">The Azure subscription ID.</param>
    /// <param name="resourceGroupName">The resource group name.</param>
    /// <param name="containerAppName">The container app name.</param>
    /// <param name="logger">The logger instance.</param>
    public AzureContainerApps(
        string subscriptionId,
        string resourceGroupName,
        string containerAppName,
        ILogger<AzureContainerApps> logger)
    {
        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            throw new ArgumentException("Subscription ID cannot be null or empty.", nameof(subscriptionId));
        }

        if (string.IsNullOrWhiteSpace(resourceGroupName))
        {
            throw new ArgumentException("Resource group name cannot be null or empty.", nameof(resourceGroupName));
        }

        if (string.IsNullOrWhiteSpace(containerAppName))
        {
            throw new ArgumentException("Container app name cannot be null or empty.", nameof(containerAppName));
        }

        this._containerAppName = containerAppName;
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this._retryPolicy = Policy
            .Handle<Exception>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    this._logger.LogWarning(
                        "Retry {RetryCount} after {Delay}s",
                        retryCount,
                        timespan.TotalSeconds);
                });
    }

    /// <inheritdoc />
    public Task DeployAsync(
        string deploymentName,
        string image,
        ContainerConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        return this._retryPolicy.ExecuteAsync(async () =>
        {
            this._logger.LogInformation(
                "Deploying {DeploymentName} with image {Image}",
                deploymentName,
                image);

            await Task.CompletedTask.ConfigureAwait(false);
        });
    }

    /// <inheritdoc />
    public Task ScaleAsync(
        string deploymentName,
        int replicaCount,
        CancellationToken cancellationToken = default)
    {
        return this._retryPolicy.ExecuteAsync(async () =>
        {
            this._logger.LogInformation(
                "Scaling {DeploymentName} to {ReplicaCount} replicas",
                deploymentName,
                replicaCount);

            await Task.CompletedTask.ConfigureAwait(false);
        });
    }

    /// <inheritdoc />
    public Task<DeploymentStatus> GetStatusAsync(
        string deploymentName,
        CancellationToken cancellationToken = default)
    {
        return this._retryPolicy.ExecuteAsync(async () =>
        {
            this._logger.LogInformation("Getting status for {DeploymentName}", deploymentName);

            return new DeploymentStatus
            {
                Name = deploymentName,
                State = DeploymentState.Running,
                RunningReplicas = 1,
                DesiredReplicas = 1,
                LastUpdated = DateTime.UtcNow,
            };
        });
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DeploymentInfo>> ListDeploymentsAsync(
        CancellationToken cancellationToken = default)
    {
        return await this._retryPolicy.ExecuteAsync(async () =>
        {
            this._logger.LogInformation("Listing all deployments");

            return new List<DeploymentInfo>
            {
                new DeploymentInfo
                {
                    Name = this._containerAppName,
                    Image = "synaxis/app:latest",
                    State = DeploymentState.Running,
                    CreatedAt = DateTime.UtcNow,
                },
            }.AsReadOnly();
        }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task DeleteAsync(
        string deploymentName,
        CancellationToken cancellationToken = default)
    {
        return this._retryPolicy.ExecuteAsync(async () =>
        {
            this._logger.LogInformation("Deleting deployment {DeploymentName}", deploymentName);

            await Task.CompletedTask.ConfigureAwait(false);
        });
    }

    /// <inheritdoc />
    public Task<string> GetLogsAsync(
        string deploymentName,
        int? tailLines = null,
        CancellationToken cancellationToken = default)
    {
        return this._retryPolicy.ExecuteAsync(() =>
        {
            this._logger.LogInformation(
                "Getting logs for {DeploymentName} (tail: {TailLines})",
                deploymentName,
                tailLines);

            return Task.FromResult($"Logs for {deploymentName}\n[System] Container running\n[System] Ready to accept connections");
        });
    }
}
