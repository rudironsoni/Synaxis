// <copyright file="IContainerPlatform.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Abstractions.Cloud;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Defines a contract for container orchestration and management.
/// </summary>
public interface IContainerPlatform
{
    /// <summary>
    /// Deploys a container or container group to the platform.
    /// </summary>
    /// <param name="deploymentName">The name of the deployment.</param>
    /// <param name="image">The container image to deploy.</param>
    /// <param name="configuration">The deployment configuration.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeployAsync(
        string deploymentName,
        string image,
        ContainerConfiguration configuration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Scales a deployment to the specified number of replicas.
    /// </summary>
    /// <param name="deploymentName">The name of the deployment to scale.</param>
    /// <param name="replicaCount">The desired number of replicas.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ScaleAsync(
        string deploymentName,
        int replicaCount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of a deployment.
    /// </summary>
    /// <param name="deploymentName">The name of the deployment.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The deployment status.</returns>
    Task<DeploymentStatus> GetStatusAsync(
        string deploymentName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all deployments in the platform.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A list of deployment information.</returns>
    Task<IReadOnlyList<DeploymentInfo>> ListDeploymentsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a deployment from the platform.
    /// </summary>
    /// <param name="deploymentName">The name of the deployment to delete.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteAsync(
        string deploymentName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets logs from a deployment.
    /// </summary>
    /// <param name="deploymentName">The name of the deployment.</param>
    /// <param name="tailLines">The number of lines to retrieve from the end of the logs.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The deployment logs.</returns>
    Task<string> GetLogsAsync(
        string deploymentName,
        int? tailLines = null,
        CancellationToken cancellationToken = default);
}
