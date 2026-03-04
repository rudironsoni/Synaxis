// <copyright file="AsyncSynchronizationHelper.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.TestUtilities.Helpers;

/// <summary>
/// Helper class for proper async synchronization in tests.
/// Avoids using Task.Delay for timing-dependent operations.
/// </summary>
public static class AsyncSynchronizationHelper
{
    /// <summary>
    /// Waits for a condition to become true using polling instead of fixed delays.
    /// </summary>
    /// <param name="condition">The condition to check.</param>
    /// <param name="timeout">Maximum time to wait.</param>
    /// <param name="pollInterval">Interval between polls.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if condition was met, false if timed out.</returns>
    public static async Task<bool> WaitForAsync(
        Func<bool> condition,
        TimeSpan timeout,
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default)
    {
        var interval = pollInterval ?? TimeSpan.FromMilliseconds(100);
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (condition())
            {
                return true;
            }

            await Task.Delay(interval, cancellationToken);
        }

        return false;
    }

    /// <summary>
    /// Waits for an async condition to become true using polling instead of fixed delays.
    /// </summary>
    /// <param name="condition">The async condition to check.</param>
    /// <param name="timeout">Maximum time to wait.</param>
    /// <param name="pollInterval">Interval between polls.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if condition was met, false if timed out.</returns>
    public static async Task<bool> WaitForAsync(
        Func<Task<bool>> condition,
        TimeSpan timeout,
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default)
    {
        var interval = pollInterval ?? TimeSpan.FromMilliseconds(100);
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await condition())
            {
                return true;
            }

            await Task.Delay(interval, cancellationToken);
        }

        return false;
    }

    /// <summary>
    /// Waits for an async condition to become true using polling instead of fixed delays.
    /// </summary>
    /// <typeparam name="T">The state type.</typeparam>
    /// <param name="condition">The async condition to check with state.</param>
    /// <param name="state">The state to pass to the condition.</param>
    /// <param name="timeout">Maximum time to wait.</param>
    /// <param name="pollInterval">Interval between polls.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if condition was met, false if timed out.</returns>
    public static async Task<bool> WaitForAsync<T>(
        Func<T, Task<bool>> condition,
        T state,
        TimeSpan timeout,
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default)
    {
        var interval = pollInterval ?? TimeSpan.FromMilliseconds(100);
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await condition(state))
            {
                return true;
            }

            await Task.Delay(interval, cancellationToken);
        }

        return false;
    }

    /// <summary>
    /// Waits for an action to complete with a timeout.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <param name="timeout">Maximum time to wait.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    /// <exception cref="TimeoutException">Thrown if the action times out.</exception>
    public static async Task WaitWithTimeoutAsync(
        Func<Task> action,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        try
        {
            await action().WaitAsync(cts.Token);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            throw new TimeoutException($"Operation timed out after {timeout.TotalSeconds}s");
        }
    }

    /// <summary>
    /// Waits for a task to complete with a timeout.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="task">The task to wait for.</param>
    /// <param name="timeout">Maximum time to wait.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The task result.</returns>
    /// <exception cref="TimeoutException">Thrown if the task times out.</exception>
    public static async Task<T> WaitWithTimeoutAsync<T>(
        Task<T> task,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        try
        {
            return await task.WaitAsync(cts.Token);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            throw new TimeoutException($"Operation timed out after {timeout.TotalSeconds}s");
        }
    }
}
