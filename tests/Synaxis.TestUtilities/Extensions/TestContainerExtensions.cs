// <copyright file="TestContainerExtensions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.TestUtilities.Extensions;

using DotNet.Testcontainers.Containers;

/// <summary>
/// Extension methods for Testcontainers with health checks and proper cleanup.
/// </summary>
public static class TestContainerExtensions
{
    /// <summary>
    /// Default container startup timeout (5 minutes).
    /// </summary>
    public static readonly TimeSpan DefaultStartupTimeout = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Default health check interval.
    /// </summary>
    public static readonly TimeSpan DefaultHealthCheckInterval = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Default health check timeout.
    /// </summary>
    public static readonly TimeSpan DefaultHealthCheckTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Starts a container with health check and proper error handling.
    /// </summary>
    /// <typeparam name="TContainer">The container type.</typeparam>
    /// <param name="container">The container.</param>
    /// <param name="healthCheck">Optional health check function.</param>
    /// <param name="startupTimeout">Optional startup timeout.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The started container.</returns>
    /// <exception cref="TimeoutException">Thrown if container fails to start within timeout.</exception>
    public static async Task<TContainer> StartWithHealthCheckAsync<TContainer>(
        this TContainer container,
        Func<TContainer, CancellationToken, Task<bool>>? healthCheck = null,
        TimeSpan? startupTimeout = null,
        CancellationToken cancellationToken = default)
        where TContainer : IContainer
    {
        var timeout = startupTimeout ?? DefaultStartupTimeout;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        try
        {
            // Start the container
            await container.StartAsync(cts.Token);

            // Run health check if provided
            if (healthCheck != null)
            {
                var isHealthy = await WaitForHealthCheckAsync(
                    container,
                    healthCheck,
                    DefaultHealthCheckInterval,
                    DefaultHealthCheckTimeout,
                    cts.Token);

                if (!isHealthy)
                {
                    throw new TimeoutException(
                        $"Container '{container.Name}' failed health check within {DefaultHealthCheckTimeout.TotalSeconds}s.");
                }
            }

            return container;
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"Container '{container.Name}' failed to start within {timeout.TotalMinutes} minutes.");
        }
    }

    /// <summary>
    /// Waits for a container to become healthy.
    /// </summary>
    /// <typeparam name="TContainer">The container type.</typeparam>
    /// <param name="container">The container.</param>
    /// <param name="healthCheck">The health check function.</param>
    /// <param name="checkInterval">Interval between health checks.</param>
    /// <param name="timeout">Maximum time to wait for health check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if health check passed, false otherwise.</returns>
    private static async Task<bool> WaitForHealthCheckAsync<TContainer>(
        TContainer container,
        Func<TContainer, CancellationToken, Task<bool>> healthCheck,
        TimeSpan checkInterval,
        TimeSpan timeout,
        CancellationToken cancellationToken)
        where TContainer : IContainer
    {
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (await healthCheck(container, cancellationToken))
                {
                    return true;
                }
            }
            catch
            {
                // Health check threw an exception, retry
            }

            await Task.Delay(checkInterval, cancellationToken);
        }

        return false;
    }

    /// <summary>
    /// Safely disposes a container with proper cleanup and error handling.
    /// </summary>
    /// <typeparam name="TContainer">The container type.</typeparam>
    /// <param name="container">The container.</param>
    /// <param name="timeout">Timeout for disposal.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task SafeDisposeAsync<TContainer>(
        this TContainer container,
        TimeSpan? timeout = null)
        where TContainer : IContainer
    {
        var disposeTimeout = timeout ?? TimeSpan.FromMinutes(2);
        using var cts = new CancellationTokenSource(disposeTimeout);

        try
        {
            await container.DisposeAsync().AsTask().WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Container disposal timed out, log and continue
            // The container may still be running, but we can't wait forever
        }
        catch (Exception)
        {
            // Log the exception but don't throw during cleanup
            // We want to continue cleaning up other resources
        }
    }

    /// <summary>
    /// Gets the container health status.
    /// </summary>
    /// <typeparam name="TContainer">The container type.</typeparam>
    /// <param name="container">The container.</param>
    /// <returns>True if container is healthy, false otherwise.</returns>
    public static Task<bool> IsHealthyAsync<TContainer>(this TContainer container)
        where TContainer : IContainer
    {
        // For Testcontainers, healthy state is when container is running
        return Task.FromResult(container.State == TestcontainersStates.Running);
    }
}
