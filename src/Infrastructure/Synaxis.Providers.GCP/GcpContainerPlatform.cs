// <copyright file="GcpContainerPlatform.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.GCP;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Synaxis.Abstractions.Cloud;

/// <summary>
/// Stub implementation for future GCP ContainerPlatform integration using Cloud Run or GKE.
/// </summary>
public class GcpContainerPlatform : IContainerPlatform
{
    /// <inheritdoc />
    public Task DeployAsync(
        string deploymentName,
        string image,
        ContainerConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("GCP ContainerPlatform integration is not yet implemented. This stub will use Cloud Run or GKE for container deployment.");
    }

    /// <inheritdoc />
    public Task ScaleAsync(
        string deploymentName,
        int replicaCount,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("GCP ContainerPlatform integration is not yet implemented. This stub will use Cloud Run or GKE for scaling operations.");
    }

    /// <inheritdoc />
    public Task<DeploymentStatus> GetStatusAsync(
        string deploymentName,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("GCP ContainerPlatform integration is not yet implemented. This stub will use Cloud Run or GKE for status queries.");
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<DeploymentInfo>> ListDeploymentsAsync(
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("GCP ContainerPlatform integration is not yet implemented. This stub will use Cloud Run or GKE for listing deployments.");
    }

    /// <inheritdoc />
    public Task DeleteAsync(
        string deploymentName,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("GCP ContainerPlatform integration is not yet implemented. This stub will use Cloud Run or GKE for deletion operations.");
    }

    /// <inheritdoc />
    public Task<string> GetLogsAsync(
        string deploymentName,
        int? tailLines = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("GCP ContainerPlatform integration is not yet implemented. This stub will use Cloud Logging for log retrieval.");
    }
}
