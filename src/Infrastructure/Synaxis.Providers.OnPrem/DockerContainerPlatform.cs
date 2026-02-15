// <copyright file="DockerContainerPlatform.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.OnPrem;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using Synaxis.Abstractions.Cloud;

/// <summary>
/// Docker-based implementation of IContainerPlatform for on-premise deployments.
/// </summary>
#pragma warning disable SA1101 // Prefix local calls with this - Fields are prefixed with underscore, not this
#pragma warning disable MA0002 // Use an overload that has a IEqualityComparer - Using default comparer for simplicity
#pragma warning disable IDISP006 // Implement IDisposable - DockerClient manages its own lifecycle
public class DockerContainerPlatform : IContainerPlatform
{
    private readonly DockerClient _dockerClient;
    private readonly ILogger<DockerContainerPlatform> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DockerContainerPlatform"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public DockerContainerPlatform(ILogger<DockerContainerPlatform> logger)
    {
        _dockerClient = new DockerClientConfiguration().CreateClient();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task DeployAsync(
        string deploymentName,
        string image,
        ContainerConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        // Pull the image if not present
        await PullImageAsync(image, cancellationToken).ConfigureAwait(false);

        // Create container
        var envVars = configuration.EnvironmentVariables
            .Select(kv => $"{kv.Key}={kv.Value}")
            .ToList();

        var portBindings = configuration.PortMappings
            .ToDictionary(
                kv => $"{kv.Value}/tcp",
                kv => (IList<PortBinding>)new List<PortBinding> { new() { HostPort = kv.Key.ToString(CultureInfo.InvariantCulture) } },
                StringComparer.Ordinal);

        var createParameters = new CreateContainerParameters
        {
            Name = deploymentName,
            Image = image,
            Env = envVars,
            HostConfig = new HostConfig
            {
                PortBindings = portBindings,
                RestartPolicy = new RestartPolicy { Name = RestartPolicyKind.UnlessStopped },
            },
            Labels = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "synaxis.deployment", deploymentName },
                { "synaxis.managed", "true" },
            },
        };

        var response = await _dockerClient.Containers.CreateContainerAsync(createParameters, cancellationToken).ConfigureAwait(false);
        await _dockerClient.Containers.StartContainerAsync(response.ID, new ContainerStartParameters(), cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Deployed container {DeploymentName} with image {Image}", deploymentName, image);
    }

    /// <inheritdoc />
    public async Task ScaleAsync(
        string deploymentName,
        int replicaCount,
        CancellationToken cancellationToken = default)
    {
        // Docker doesn't support scaling like Kubernetes
        // For simplicity, we'll log a warning
        _logger.LogWarning(
            "Docker does not support scaling like Kubernetes. To scale {DeploymentName} to {ReplicaCount} replicas, use Docker Swarm or Kubernetes.",
            deploymentName,
            replicaCount);
    }

    /// <inheritdoc />
    public async Task<DeploymentStatus> GetStatusAsync(
        string deploymentName,
        CancellationToken cancellationToken = default)
    {
        var containers = await _dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters { All = true },
            cancellationToken).ConfigureAwait(false);

        var container = containers.FirstOrDefault(c => c.Names.Any(n => string.Equals(n.TrimStart('/'), deploymentName, StringComparison.Ordinal)));

        if (container == null)
        {
            return new DeploymentStatus
            {
                Name = deploymentName,
                State = DeploymentState.Terminated,
                RunningReplicas = 0,
                DesiredReplicas = 0,
                LastUpdated = DateTime.UtcNow,
            };
        }

        var state = MapContainerStateToDeploymentState(container.State);
        var runningReplicas = state == DeploymentState.Running ? 1 : 0;

        return new DeploymentStatus
        {
            Name = deploymentName,
            State = state,
            RunningReplicas = runningReplicas,
            DesiredReplicas = 1,
            LastUpdated = container.Created,
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DeploymentInfo>> ListDeploymentsAsync(
        CancellationToken cancellationToken = default)
    {
        var containers = await _dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters { All = true },
            cancellationToken).ConfigureAwait(false);

        var deployments = containers
            .Where(c => c.Labels.ContainsKey("synaxis.managed") && string.Equals(c.Labels["synaxis.managed"], "true", StringComparison.Ordinal))
            .Select(c => new DeploymentInfo
            {
                Name = c.Names.FirstOrDefault()?.TrimStart('/') ?? c.ID.Substring(0, 12),
                Image = c.Image,
                State = MapContainerStateToDeploymentState(c.State),
                CreatedAt = c.Created,
            })
            .ToList();

        return deployments.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(
        string deploymentName,
        CancellationToken cancellationToken = default)
    {
        var containers = await _dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters { All = true },
            cancellationToken).ConfigureAwait(false);

        var container = containers.FirstOrDefault(c => c.Names.Any(n => string.Equals(n.TrimStart('/'), deploymentName, StringComparison.Ordinal)));

        if (container != null)
        {
            await _dockerClient.Containers.StopContainerAsync(container.ID, new ContainerStopParameters(), cancellationToken).ConfigureAwait(false);
            await _dockerClient.Containers.RemoveContainerAsync(container.ID, new ContainerRemoveParameters { Force = true }, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Deleted deployment {DeploymentName}", deploymentName);
        }
    }

    /// <inheritdoc />
    public async Task<string> GetLogsAsync(
        string deploymentName,
        int? tailLines = null,
        CancellationToken cancellationToken = default)
    {
        var containers = await _dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters { All = true },
            cancellationToken).ConfigureAwait(false);

        var container = containers.FirstOrDefault(c => c.Names.Any(n => string.Equals(n.TrimStart('/'), deploymentName, StringComparison.Ordinal)));

        if (container == null)
        {
            return $"Container {deploymentName} not found";
        }

        var logsParameters = new ContainerLogsParameters
        {
            ShowStdout = true,
            ShowStderr = true,
            Tail = tailLines?.ToString(CultureInfo.InvariantCulture),
        };

        using var stream = await _dockerClient.Containers.GetContainerLogsAsync(container.ID, false, logsParameters, cancellationToken).ConfigureAwait(false);
        var (stdout, stderr) = await stream.ReadOutputToEndAsync(cancellationToken).ConfigureAwait(false);
        var logs = $"{stdout}{stderr}";

        return logs;
    }

    private async Task PullImageAsync(string image, CancellationToken cancellationToken)
    {
        try
        {
            await _dockerClient.Images.CreateImageAsync(
                new ImagesCreateParameters { FromImage = image },
                null,
                new Progress<JSONMessage>(message =>
                {
                    if (!string.IsNullOrEmpty(message.Status))
                    {
                        _logger.LogDebug("Pulling image {Image}: {Status}", image, message.Status);
                    }
                }),
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to pull image {Image}, assuming it exists locally", image);
        }
    }

    private static DeploymentState MapContainerStateToDeploymentState(string containerState)
    {
        return containerState.ToLowerInvariant() switch
        {
            "running" => DeploymentState.Running,
            "created" => DeploymentState.Pending,
            "restarting" => DeploymentState.Pending,
            "exited" => DeploymentState.Terminated,
            "dead" => DeploymentState.Failed,
            "paused" => DeploymentState.Terminating,
            _ => DeploymentState.Failed,
        };
    }
}
#pragma warning restore IDISP006 // Implement IDisposable
#pragma warning restore MA0002 // Use an overload that has a IEqualityComparer
#pragma warning restore SA1101 // Prefix local calls with this
