// <copyright file="RedisInFlightDeduplication.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Optimization.Caching;

using System;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

/// <summary>
/// Interface for in-flight request deduplication.
/// </summary>
public interface IInFlightDeduplication
{
    /// <summary>
    /// Executes an operation with deduplication to prevent duplicate concurrent executions.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="requestHash">A unique hash identifying the request.</param>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="lockTimeout">The timeout for acquiring the lock.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    Task<T> ExecuteWithDeduplication<T>(
        string requestHash,
        Func<Task<T>> operation,
        TimeSpan lockTimeout,
        CancellationToken cancellationToken);

    /// <summary>
    /// Checks if a request with the given hash is currently in flight.
    /// </summary>
    /// <param name="requestHash">A unique hash identifying the request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the request is in flight, false otherwise.</returns>
    Task<bool> IsInFlightAsync(string requestHash, CancellationToken cancellationToken);
}

/// <summary>
/// Redis implementation of IInFlightDeduplication.
/// Uses owner tokens with Lua scripts for safe lock release to prevent deleting another caller's lock.
/// </summary>
public class RedisInFlightDeduplication : IInFlightDeduplication
{
    // Lua script for safe lock release: only delete if the lock value matches our token
    private const string ReleaseLockScript = @"
        if redis.call('get', KEYS[1]) == ARGV[1] then
            return redis.call('del', KEYS[1])
        else
            return 0
        end";

    private readonly IConnectionMultiplexer _redis;
    private readonly TimeSpan _resultTtl = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _pollInterval = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisInFlightDeduplication"/> class.
    /// </summary>
    /// <param name="redis">The Redis connection multiplexer.</param>
    public RedisInFlightDeduplication(IConnectionMultiplexer redis)
    {
        this._redis = redis;
    }

    /// <inheritdoc />
    public async Task<T> ExecuteWithDeduplication<T>(
        string requestHash,
        Func<Task<T>> operation,
        TimeSpan lockTimeout,
        CancellationToken cancellationToken)
    {
        try
        {
            var db = this._redis.GetDatabase();
            var lockKey = $"inflight:{requestHash}";
            var resultKey = $"result:{requestHash}";
            var ownerToken = Guid.NewGuid().ToString();

            var lockAcquired = await db.StringSetAsync(
                lockKey,
                ownerToken,
                lockTimeout,
                when: When.NotExists,
                flags: CommandFlags.None).ConfigureAwait(false);

            if (lockAcquired)
            {
                return await this.ExecuteWithLockAsync(db, lockKey, resultKey, ownerToken, operation).ConfigureAwait(false);
            }

            return await this.WaitForResultOrExecuteFallbackAsync(db, resultKey, lockTimeout, operation, cancellationToken).ConfigureAwait(false);
        }
        catch (RedisException)
        {
            // Fail open - execute directly if Redis is unavailable
            return await operation().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Executes the operation while holding the lock.
    /// </summary>
    private async Task<T> ExecuteWithLockAsync<T>(
        IDatabase db,
        string lockKey,
        string resultKey,
        string ownerToken,
        Func<Task<T>> operation)
    {
        try
        {
            var result = await operation().ConfigureAwait(false);

            // Store the result for other waiters
            var serialized = System.Text.Json.JsonSerializer.Serialize(result);
            await db.StringSetAsync(
                resultKey,
                serialized,
                this._resultTtl,
                when: When.Always,
                flags: CommandFlags.None).ConfigureAwait(false);

            return result;
        }
        finally
        {
            // Always release the lock using Lua script to ensure we only delete our own lock
            await ReleaseLockAsync(db, lockKey, ownerToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Waits for a cached result or executes the operation as a fallback after timeout.
    /// </summary>
    private async Task<T> WaitForResultOrExecuteFallbackAsync<T>(
        IDatabase db,
        string resultKey,
        TimeSpan lockTimeout,
        Func<Task<T>> operation,
        CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.Add(lockTimeout);

        while (DateTime.UtcNow < deadline && !cancellationToken.IsCancellationRequested)
        {
            var cachedResult = await db.StringGetAsync(resultKey).ConfigureAwait(false);
            if (cachedResult.HasValue)
            {
                return System.Text.Json.JsonSerializer.Deserialize<T>(cachedResult.ToString())!;
            }

            await Task.Delay(this._pollInterval, cancellationToken).ConfigureAwait(false);
        }

        // Timeout - execute as fallback
        return await operation().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<bool> IsInFlightAsync(string requestHash, CancellationToken cancellationToken)
    {
        var db = this._redis.GetDatabase();
        var lockKey = $"inflight:{requestHash}";
        return db.KeyExistsAsync(lockKey);
    }

    /// <summary>
    /// Releases a lock using a Lua script to ensure we only delete our own lock.
    /// This prevents deleting another caller's lock after TTL expiry.
    /// </summary>
    /// <param name="db">The Redis database.</param>
    /// <param name="lockKey">The lock key.</param>
    /// <param name="ownerToken">The owner token that was stored when acquiring the lock.</param>
    /// <returns>True if the lock was released, false otherwise.</returns>
    private static async Task<bool> ReleaseLockAsync(IDatabase db, string lockKey, string ownerToken)
    {
        var result = await db.ScriptEvaluateAsync(
            ReleaseLockScript,
            new RedisKey[] { lockKey },
            new RedisValue[] { ownerToken }).ConfigureAwait(false);

        return (long)result == 1;
    }
}
