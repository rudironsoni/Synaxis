// <copyright file="MockContainerPlatform.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.TestUtilities.Doubles;

using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Synaxis.Abstractions.Cloud;

/// <summary>
/// In-memory mock implementation of <see cref="IContainerPlatform"/> for testing.
/// Provides thread-safe container orchestration and management capabilities.
/// </summary>
public sealed class MockContainerPlatform : IContainerPlatform
{
    private readonly ConcurrentDictionary<string, DeploymentInfo> _deployments = new();
    private readonly ConcurrentDictionary<string, DeploymentLogs> _logs = new();

    /// <summary>
    /// Deploys a container or container group to the platform.
    /// </summary>
    /// <param name="deploymentName">The name of the deployment.</param>
    /// <param name="image">The container image to deploy.</param>
    /// <param name="configuration">The deployment configuration.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task DeployAsync(
        string deploymentName,
        string image,
        ContainerConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(deploymentName);
        ArgumentException.ThrowIfNullOrEmpty(image);
        ArgumentNullException.ThrowIfNull(configuration);

        cancellationToken.ThrowIfCancellationRequested();

        var info = new DeploymentInfo
        {
            Name = deploymentName,
            Image = image,
            State = DeploymentState.Pending,
            CreatedAt = DateTime.UtcNow,
        };

        _deployments[deploymentName] = info;
        _logs[deploymentName] = new DeploymentLogs();

        // Simulate deployment transitioning to running
        info.State = DeploymentState.Running;

        return Task.CompletedTask;
    }

    /// <summary>
    /// Scales a deployment to the specified number of replicas.
    /// </summary>
    /// <param name="deploymentName">The name of the deployment to scale.</param>
    /// <param name="replicaCount">The desired number of replicas.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the deployment does not exist.</exception>
    public Task ScaleAsync(
        string deploymentName,
        int replicaCount,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(deploymentName);

        cancellationToken.ThrowIfCancellationRequested();

        if (!_deployments.TryGetValue(deploymentName, out var info))
        {
            throw new InvalidOperationException($"Deployment '{deploymentName}' not found.");
        }

        // Update deployment state to reflect scaling
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the status of a deployment.
    /// </summary>
    /// <param name="deploymentName">The name of the deployment.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The deployment status.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the deployment does not exist.</exception>
    public Task<DeploymentStatus> GetStatusAsync(
        string deploymentName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(deploymentName);

        cancellationToken.ThrowIfCancellationRequested();

        if (!_deployments.TryGetValue(deploymentName, out var info))
        {
            throw new InvalidOperationException($"Deployment '{deploymentName}' not found.");
        }

        var status = new DeploymentStatus
        {
            Name = info.Name,
            State = info.State,
            RunningReplicas = info.State == DeploymentState.Running ? 1 : 0,
            DesiredReplicas = 1,
            LastUpdated = DateTime.UtcNow,
        };

        return Task.FromResult(status);
    }

    /// <summary>
    /// Lists all deployments in the platform.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A list of deployment information.</returns>
    public Task<IReadOnlyList<DeploymentInfo>> ListDeploymentsAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult<IReadOnlyList<DeploymentInfo>>(_deployments.Values.ToList());
    }

    /// <summary>
    /// Deletes a deployment from the platform.
    /// </summary>
    /// <param name="deploymentName">The name of the deployment to delete.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task DeleteAsync(
        string deploymentName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(deploymentName);

        cancellationToken.ThrowIfCancellationRequested();

        if (_deployments.TryGetValue(deploymentName, out var info))
        {
            info.State = DeploymentState.Terminating;
            info.State = DeploymentState.Terminated;
        }

        _deployments.TryRemove(deploymentName, out _);
        _logs.TryRemove(deploymentName, out _);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets logs from a deployment.
    /// </summary>
    /// <param name="deploymentName">The name of the deployment.</param>
    /// <param name="tailLines">The number of lines to retrieve from the end of the logs.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The deployment logs.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the deployment does not exist.</exception>
    public Task<string> GetLogsAsync(
        string deploymentName,
        int? tailLines = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(deploymentName);

        cancellationToken.ThrowIfCancellationRequested();

        if (!_logs.TryGetValue(deploymentName, out var deploymentLogs))
        {
            throw new InvalidOperationException($"Deployment '{deploymentName}' not found.");
        }

        var logs = tailLines.HasValue
            ? deploymentLogs.GetLastLines(tailLines.Value)
            : deploymentLogs.GetAllLines();

        return Task.FromResult(string.Join("\n", logs));
    }

    /// <summary>
    /// Adds a log entry to a deployment.
    /// </summary>
    /// <param name="deploymentName">The name of the deployment.</param>
    /// <param name="message">The log message.</param>
    public void AddLog(string deploymentName, string message)
    {
        ArgumentException.ThrowIfNullOrEmpty(deploymentName);
        ArgumentException.ThrowIfNullOrEmpty(message);

        if (_logs.TryGetValue(deploymentName, out var deploymentLogs))
        {
            deploymentLogs.AddLine(message);
        }
    }

    /// <summary>
    /// Updates the state of a deployment.
    /// </summary>
    /// <param name="deploymentName">The name of the deployment.</param>
    /// <param name="state">The new state.</param>
    public void SetState(string deploymentName, DeploymentState state)
    {
        ArgumentException.ThrowIfNullOrEmpty(deploymentName);

        if (_deployments.TryGetValue(deploymentName, out var info))
        {
            info.State = state;
        }
    }

    /// <summary>
    /// Clears all deployments and logs.
    /// </summary>
    public void Clear()
    {
        _deployments.Clear();
        _logs.Clear();
    }

    /// <summary>
    /// Checks if a deployment exists.
    /// </summary>
    /// <param name="deploymentName">The name of the deployment.</param>
    /// <returns>True if the deployment exists; otherwise, false.</returns>
    public bool DeploymentExists(string deploymentName)
    {
        return _deployments.ContainsKey(deploymentName);
    }

    /// <summary>
    /// Gets deployment information.
    /// </summary>
    /// <param name="deploymentName">The name of the deployment.</param>
    /// <returns>The deployment information, or null if not found.</returns>
    public DeploymentInfo? GetDeployment(string deploymentName)
    {
        _deployments.TryGetValue(deploymentName, out var info);
        return info;
    }

    /// <summary>
    /// Gets the number of deployments.
    /// </summary>
    /// <returns>The number of deployments.</returns>
    public int DeploymentCount => _deployments.Count;

    /// <summary>
    /// Gets the names of all deployments.
    /// </summary>
    /// <returns>A collection of deployment names.</returns>
    public IEnumerable<string> GetDeploymentNames()
    {
        return _deployments.Keys.ToList();
    }

    /// <summary>
    /// Simulates a deployment failure.
    /// </summary>
    /// <param name="deploymentName">The name of the deployment.</param>
    public void SimulateFailure(string deploymentName)
    {
        SetState(deploymentName, DeploymentState.Failed);
    }

    /// <summary>
    /// Simulates a deployment suspension.
    /// </summary>
    /// <param name="deploymentName">The name of the deployment.</param>
    public void SimulateTermination(string deploymentName)
    {
        SetState(deploymentName, DeploymentState.Terminated);
    }

    /// <summary>
    /// Simulates a deployment pending state.
    /// </summary>
    /// <param name="deploymentName">The name of the deployment.</param>
    public void SimulatePending(string deploymentName)
    {
        SetState(deploymentName, DeploymentState.Pending);
    }

    private sealed class DeploymentLogs
    {
        private readonly List<string> _lines = new();
        private readonly object _lock = new();

        public void AddLine(string line)
        {
            lock (_lock)
            {
                _lines.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {line}");
            }
        }

        public IReadOnlyList<string> GetAllLines()
        {
            lock (_lock)
            {
                return _lines.ToList();
            }
        }

        public IReadOnlyList<string> GetLastLines(int count)
        {
            lock (_lock)
            {
                return _lines.TakeLast(count).ToList();
            }
        }
    }
}
