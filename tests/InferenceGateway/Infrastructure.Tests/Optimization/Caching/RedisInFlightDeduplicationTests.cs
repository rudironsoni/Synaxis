// <copyright file="RedisInFlightDeduplicationTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Tests.Optimization.Caching;

using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using StackExchange.Redis;
using Xunit;

/// <summary>
/// Unit tests for IInFlightDeduplication Redis implementation
/// Tests request deduplication to prevent duplicate LLM calls for identical concurrent requests.
/// </summary>
public class RedisInFlightDeduplicationTests
{
    private readonly Mock<IConnectionMultiplexer> _mockRedis;
    private readonly Mock<IDatabase> _mockDatabase;
    private readonly CancellationToken _cancellationToken;
    private readonly Lock _lock = new();

    public RedisInFlightDeduplicationTests()
    {
        this._mockRedis = new Mock<IConnectionMultiplexer>();
        this._mockDatabase = new Mock<IDatabase>();
        this._mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(this._mockDatabase.Object);
        this._cancellationToken = CancellationToken.None;
    }

    [Fact]
    public async Task ExecuteWithDeduplication_FirstCall_AcquiresLock()
    {
        // Arrange
        var requestHash = "hash-123";
        var lockKey = $"inflight:{requestHash}";
        var resultKey = $"result:{requestHash}";
        var lockTimeout = TimeSpan.FromSeconds(30);

        // Lock acquired (first call)
        this._mockDatabase
            .Setup(db => db.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                When.Always,
                CommandFlags.None))
            .ReturnsAsync(true);

        // Store result
        this._mockDatabase
            .Setup(db => db.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                When.Always,
                CommandFlags.None))
            .ReturnsAsync(true);

        // Release lock
        this._mockDatabase
            .Setup(db => db.KeyDeleteAsync(It.Is<RedisKey>(k => k.ToString() == lockKey), CommandFlags.None))
            .ReturnsAsync(true);

        var service = new RedisInFlightDeduplication(this._mockRedis.Object);
        var executed = false;
        Func<Task<string>> operation = async () =>
        {
            executed = true;
            await Task.Delay(10).ConfigureAwait(false);
            return "Result from operation";
        };

        // Act
        var result = await service.ExecuteWithDeduplication(
            requestHash,
            operation,
            lockTimeout,
            this._cancellationToken);

        // Assert
        Assert.True(executed);
        Assert.Equal("Result from operation", result);

        this._mockDatabase.Verify(
            db => db.StringSetAsync(
                It.Is<RedisKey>(k => k.ToString() == lockKey),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                When.NotExists),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteWithDeduplication_ConcurrentCalls_Deduplicates()
    {
        // Arrange
        var requestHash = "hash-concurrent";
        var lockKey = $"inflight:{requestHash}";
        var resultKey = $"result:{requestHash}";
        var lockTimeout = TimeSpan.FromSeconds(5);
        var resultStored = false;
        var lockReleased = false;
        var lockAcquired = false;
        var lockObj = new System.Threading.Lock();

        // Lock acquisition: use a lock to ensure thread-safe behavior
        this._mockDatabase
            .Setup(db => db.StringSetAsync(
                It.Is<RedisKey>(k => k.ToString() == lockKey),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                When.NotExists))
            .ReturnsAsync(() =>
            {
                using var scope = this._lock.EnterScope();
                if (!lockAcquired)
                {
                    lockAcquired = true;
                    return true;
                }

                return false;
            });

        // Result polling: initially null, then available after stored
        this._mockDatabase
            .Setup(db => db.StringGetAsync(
                It.Is<RedisKey>(k => k.ToString() == resultKey),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(() =>
            {
                using var scope = this._lock.EnterScope();
                return resultStored ? "\"Result from operation\"" : RedisValue.Null;
            });

        // Store result
        this._mockDatabase
            .Setup(db => db.StringSetAsync(
                It.Is<RedisKey>(k => k.ToString() == resultKey),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                When.Always))
            .Callback(() =>
            {
                using var scope = lockObj.EnterScope();
                resultStored = true;
            })
            .ReturnsAsync(true);

        // Release lock
        this._mockDatabase
            .Setup(db => db.KeyDeleteAsync(
                It.Is<RedisKey>(k => k.ToString() == lockKey),
                It.IsAny<CommandFlags>()))
            .Callback(() => lockReleased = true)
            .ReturnsAsync(true);

        var service = new RedisInFlightDeduplication(this._mockRedis.Object);
        var executionCount = 0;

        Func<Task<string>> operation = async () =>
        {
            Interlocked.Increment(ref executionCount);

            // Simulate operation taking some time to allow other calls to start
            await Task.Delay(200).ConfigureAwait(false);
            return "Result from operation";
        };

        // Act - Launch 5 concurrent requests
        var tasks = new List<Task<string>>();
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(service.ExecuteWithDeduplication(
                requestHash,
                operation,
                lockTimeout,
                this._cancellationToken));
        }

        var results = await Task.WhenAll(tasks);

        // Assert - Operation should execute only ONCE despite 5 concurrent calls
        Assert.Equal(1, executionCount);

        // All callers should get the same result
        Assert.All(results, r => Assert.Equal("Result from operation", r));

        // Verify lock was acquired and released
        Assert.True(resultStored);
        Assert.True(lockReleased);
    }

    [Fact]
    public async Task ExecuteWithDeduplication_LockTimeout_ExecutesFallback()
    {
        // Arrange
        var requestHash = "hash-timeout";
        var lockKey = $"inflight:{requestHash}";
        var resultKey = $"result:{requestHash}";
        var lockTimeout = TimeSpan.FromMilliseconds(50);

        // Lock cannot be acquired (someone else holds it)
        this._mockDatabase
            .Setup(db => db.StringSetAsync(
                lockKey,
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                When.NotExists,
                CommandFlags.None))
            .ReturnsAsync(false);

        // No result available yet
        this._mockDatabase
            .Setup(db => db.StringGetAsync(resultKey, It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        var service = new RedisInFlightDeduplication(this._mockRedis.Object);
        var executed = false;

        Func<Task<string>> operation = async () =>
        {
            executed = true;
            await Task.Delay(10).ConfigureAwait(false);
            return "Fallback result";
        };

        // Act - Wait for timeout, then execute fallback
        var result = await service.ExecuteWithDeduplication(
            requestHash,
            operation,
            lockTimeout,
            this._cancellationToken);

        // Assert - Should execute operation as fallback after timeout
        Assert.True(executed);
        Assert.Equal("Fallback result", result);
    }

    [Fact]
    public async Task ExecuteWithDeduplication_Success_StoresResult()
    {
        // Arrange
        var requestHash = "hash-store";
        var lockKey = $"inflight:{requestHash}";
        var resultKey = $"result:{requestHash}";
        var lockTimeout = TimeSpan.FromSeconds(30);
        var resultTtl = TimeSpan.FromMinutes(5);

        this._mockDatabase
            .Setup(db => db.StringSetAsync(
                lockKey,
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                When.NotExists,
                CommandFlags.None))
            .ReturnsAsync(true);

        this._mockDatabase
            .Setup(db => db.StringGetAsync(resultKey, It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        this._mockDatabase
            .Setup(db => db.StringSetAsync(
                It.Is<RedisKey>(k => k.ToString() == resultKey),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                When.Always,
                CommandFlags.None))
            .ReturnsAsync(true);

        this._mockDatabase
            .Setup(db => db.KeyDeleteAsync(lockKey, CommandFlags.None))
            .ReturnsAsync(true);

        var service = new RedisInFlightDeduplication(this._mockRedis.Object);

        Func<Task<string>> operation = async () =>
        {
            await Task.Delay(10).ConfigureAwait(false);
            return "Operation result";
        };

        // Act
        var result = await service.ExecuteWithDeduplication(
            requestHash,
            operation,
            lockTimeout,
            this._cancellationToken);

        // Assert
        Assert.Equal("Operation result", result);
    }

    [Fact]
    public async Task ExecuteWithDeduplication_Failure_ReleasesLock()
    {
        // Arrange
        var requestHash = "hash-failure";
        var lockKey = $"inflight:{requestHash}";
        var resultKey = $"result:{requestHash}";
        var lockTimeout = TimeSpan.FromSeconds(5);
        var lockReleased = false;

        // Lock acquisition succeeds (specific to lock key)
        this._mockDatabase
            .Setup(db => db.StringSetAsync(
                It.Is<RedisKey>(k => k.ToString() == lockKey),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                When.NotExists,
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // No cached result
        this._mockDatabase
            .Setup(db => db.StringGetAsync(
                It.Is<RedisKey>(k => k.ToString() == resultKey),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        // Lock release (called in finally block even on failure)
        this._mockDatabase
            .Setup(db => db.KeyDeleteAsync(
                It.Is<RedisKey>(k => k.ToString() == lockKey),
                It.IsAny<CommandFlags>()))
            .Callback(() => lockReleased = true)
            .ReturnsAsync(true);

        var service = new RedisInFlightDeduplication(this._mockRedis.Object);

        Func<Task<string>> operation = async () =>
        {
            await Task.Delay(10).ConfigureAwait(false);
            throw new InvalidOperationException("Operation failed");
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await service.ExecuteWithDeduplication(
                requestHash,
                operation,
                lockTimeout,
                this._cancellationToken).ConfigureAwait(false);
        });

        Assert.Equal("Operation failed", exception.Message);

        // Assert - Lock should be released even on failure (finally block guarantees this)
        Assert.True(lockReleased, "Lock should be released even when operation fails");

        this._mockDatabase.Verify(
            db => db.KeyDeleteAsync(
                It.Is<RedisKey>(k => k.ToString() == lockKey),
                It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteWithDeduplication_Exception_Propagates()
    {
        // Arrange
        var requestHash = "hash-exception";
        var lockKey = $"inflight:{requestHash}";
        var resultKey = $"result:{requestHash}";
        var lockTimeout = TimeSpan.FromSeconds(30);

        this._mockDatabase
            .Setup(db => db.StringSetAsync(
                lockKey,
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                When.NotExists,
                CommandFlags.None))
            .ReturnsAsync(true);

        this._mockDatabase
            .Setup(db => db.StringGetAsync(resultKey, It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        this._mockDatabase
            .Setup(db => db.KeyDeleteAsync(lockKey, CommandFlags.None))
            .ReturnsAsync(true);

        var service = new RedisInFlightDeduplication(this._mockRedis.Object);

        Func<Task<string>> operation = () => throw new ArgumentException("Invalid argument");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await service.ExecuteWithDeduplication(
                requestHash,
                operation,
                lockTimeout,
                this._cancellationToken).ConfigureAwait(false);
        });

        Assert.Equal("Invalid argument", exception.Message);
    }

    [Fact]
    public async Task IsInFlightAsync_Locked_ReturnsTrue()
    {
        // Arrange
        var requestHash = "hash-locked";
        var lockKey = $"inflight:{requestHash}";

        this._mockDatabase
            .Setup(db => db.KeyExistsAsync(lockKey, It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var service = new RedisInFlightDeduplication(this._mockRedis.Object);

        // Act
        var result = await service.IsInFlightAsync(requestHash, this._cancellationToken);

        // Assert
        Assert.True(result);

        this._mockDatabase.Verify(
            db => db.KeyExistsAsync(lockKey, It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Fact]
    public async Task IsInFlightAsync_Unlocked_ReturnsFalse()
    {
        // Arrange
        var requestHash = "hash-unlocked";
        var lockKey = $"inflight:{requestHash}";

        this._mockDatabase
            .Setup(db => db.KeyExistsAsync(lockKey, It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);

        var service = new RedisInFlightDeduplication(this._mockRedis.Object);

        // Act
        var result = await service.IsInFlightAsync(requestHash, this._cancellationToken);

        // Assert
        Assert.False(result);

        this._mockDatabase.Verify(
            db => db.KeyExistsAsync(lockKey, It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Fact]
    public async Task RedisFailure_FailsOpen_ExecutesDirectly()
    {
        // Arrange
        var requestHash = "hash-redis-failure";
        var lockKey = $"inflight:{requestHash}";

        this._mockDatabase
            .Setup(db => db.StringSetAsync(
                lockKey,
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                When.NotExists,
                CommandFlags.None))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection failed"));

        var service = new RedisInFlightDeduplication(this._mockRedis.Object);
        var executed = false;

        Func<Task<string>> operation = async () =>
        {
            executed = true;
            await Task.Delay(10).ConfigureAwait(false);
            return "Direct execution result";
        };

        // Act - Should execute directly when Redis fails
        var result = await service.ExecuteWithDeduplication(
            requestHash,
            operation,
            TimeSpan.FromSeconds(30),
            this._cancellationToken);

        // Assert
        Assert.True(executed);
        Assert.Equal("Direct execution result", result);
    }
}

/// <summary>
/// Interface for in-flight request deduplication.
/// </summary>
public interface IInFlightDeduplication
{
    Task<T> ExecuteWithDeduplication<T>(
        string requestHash,
        Func<Task<T>> operation,
        TimeSpan lockTimeout,
        CancellationToken cancellationToken);

    Task<bool> IsInFlightAsync(string requestHash, CancellationToken cancellationToken);
}

/// <summary>
/// Redis implementation of IInFlightDeduplication.
/// </summary>
public class RedisInFlightDeduplication : IInFlightDeduplication
{
    private readonly IConnectionMultiplexer _redis;

    public RedisInFlightDeduplication(IConnectionMultiplexer redis)
    {
        this._redis = redis;
    }

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

            // Try to acquire lock
            var lockAcquired = await db.StringSetAsync(lockKey, "locked", lockTimeout, when: When.NotExists).ConfigureAwait(false);

            if (lockAcquired)
            {
                try
                {
                    // We got the lock - execute the operation
                    var result = await operation().ConfigureAwait(false);

                    // Store the result for other waiters
                    var serialized = System.Text.Json.JsonSerializer.Serialize(result);
                    await db.StringSetAsync(resultKey, serialized, TimeSpan.FromMinutes(5)).ConfigureAwait(false);

                    return result;
                }
                finally
                {
                    // Always release the lock
                    await db.KeyDeleteAsync(lockKey).ConfigureAwait(false);
                }
            }
            else
            {
                // Someone else is executing - wait for result
                var deadline = DateTime.UtcNow.Add(lockTimeout);

                while (DateTime.UtcNow < deadline)
                {
                    var cachedResult = await db.StringGetAsync(resultKey).ConfigureAwait(false);
                    if (cachedResult.HasValue)
                    {
                        return System.Text.Json.JsonSerializer.Deserialize<T>(cachedResult.ToString())!;
                    }

                    await Task.Delay(100, cancellationToken).ConfigureAwait(false); // Poll interval
                }

                // Timeout - execute as fallback
                return await operation().ConfigureAwait(false);
            }
        }
        catch (RedisException)
        {
            // Fail open - execute directly if Redis is unavailable
            return await operation().ConfigureAwait(false);
        }
    }

    public Task<bool> IsInFlightAsync(string requestHash, CancellationToken cancellationToken)
    {
        var db = this._redis.GetDatabase();
        var lockKey = $"inflight:{requestHash}";
        return db.KeyExistsAsync(lockKey);
    }
}
