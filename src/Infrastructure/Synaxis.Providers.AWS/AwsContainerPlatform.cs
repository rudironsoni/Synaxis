// <copyright file="AwsContainerPlatform.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.AWS;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Synaxis.Abstractions.Cloud;

/// <summary>
/// Stub implementation for future AWS ContainerPlatform integration using ECS or EKS.
/// </summary>
public class AwsContainerPlatform : IContainerPlatform
{
    /// <inheritdoc />
    public Task DeployAsync(
        string deploymentName,
        string image,
        ContainerConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("AWS ContainerPlatform integration is not yet implemented. This stub will use ECS or EKS for container deployment.");
    }

    /// <inheritdoc />
    public Task ScaleAsync(
        string deploymentName,
        int replicaCount,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("AWS ContainerPlatform integration is not yet implemented. This stub will use ECS or EKS for scaling operations.");
    }

    /// <inheritdoc />
    public Task<DeploymentStatus> GetStatusAsync(
        string deploymentName,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("AWS ContainerPlatform integration is not yet implemented. This stub will use ECS or EKS for status queries.");
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<DeploymentInfo>> ListDeploymentsAsync(
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("AWS ContainerPlatform integration is not yet implemented. This stub will use ECS or EKS for listing deployments.");
    }

    /// <inheritdoc />
    public Task DeleteAsync(
        string deploymentName,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("AWS ContainerPlatform integration is not yet implemented. This stub will use ECS or EKS for deletion operations.");
    }

    /// <inheritdoc />
    public Task<string> GetLogsAsync(
        string deploymentName,
        int? tailLines = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("AWS ContainerPlatform integration is not yet implemented. This stub will use CloudWatch Logs for log retrieval.");
    }
}
