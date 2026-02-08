// <copyright file="RedisInFlightDeduplicationTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Tests.Optimization.Caching;

using Moq;
using StackExchange.Redis;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

/// <summary>
/// Unit tests for IInFlightDeduplication Redis implementation
/// Tests request deduplication to prevent duplicate LLM calls for identical concurrent requests
/// </summary>
public class RedisInFlightDeduplicationTests
{
    private readonly Mock<IConnectionMultiplexer> _mockRedis;
    private readonly Mock<IDatabase> _mockDatabase;
    private readonly CancellationToken _cancellationToken;

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
                It.Is<RedisKey>(k => k.ToString() == lockKey),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(),
                When.NotExists,
                CommandFlags.None))
            .ReturnsAsync(true);

        // Store result
        this._mockDatabase
            .Setup(db => db.StringSetAsync(
                It.Is<RedisKey>(k => k.ToString() == resultKey),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(),
                It.IsAny<When>(),
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
            await Task.Delay(10);
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
                It.IsAny<bool>(),
                When.NotExists,
                CommandFlags.None),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteWithDeduplication_ConcurrentCalls_Deduplicates()
    {
        // Arrange
        var requestHash = "hash-concurrent";
        var lockKey = $"inflight:{requestHash}";
        var resultKey = $"result:{requestHash}";
        var lockTimeout = TimeSpan.FromSeconds(30);

        var lockAttempts = 0;

        // First call acquires lock, subsequent calls fail to acquire
        this._mockDatabase
            .Setup(db => db.StringSetAsync(
                lockKey,
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(),
                When.NotExists,
                CommandFlags.None))
            .ReturnsAsync(() => Interlocked.Increment(ref lockAttempts) == 1);

        // After operation completes, result is available
        var resultAvailable = false;
        this._mockDatabase
            .Setup(db => db.StringGetAsync(resultKey, It.IsAny<CommandFlags>()))
            .ReturnsAsync(() => resultAvailable ? (RedisValue)System.Text.Json.JsonSerializer.Serialize("Result from operation") : RedisValue.Null);

        // Store result (called by first request)
        this._mockDatabase
            .Setup(db => db.StringSetAsync(
                resultKey,
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(),
                It.IsAny<When>(),
                CommandFlags.None))
            .Callback(() => resultAvailable = true)
            .ReturnsAsync(true);

        // Release lock
        this._mockDatabase
            .Setup(db => db.KeyDeleteAsync(lockKey, CommandFlags.None))
            .ReturnsAsync(true);

        var service = new RedisInFlightDeduplication(this._mockRedis.Object);
        var executionCount = 0;

        Func<Task<string>> operation = async () =>
        {
            Interlocked.Increment(ref executionCount);
            await Task.Delay(100); // Simulate work
            return "Result from operation";
        };

        // Act - Simulate 5 concurrent requests
        var tasks = new Task<string>[5];
        for (int i = 0; i < 5; i++)
        {
            tasks[i] = service.ExecuteWithDeduplication(
                requestHash,
                operation,
                lockTimeout,
                this._cancellationToken);
        }

        var results = await Task.WhenAll(tasks);

        // Assert - Only one execution should occur
        Assert.Equal(1, executionCount);
        Assert.All(results, r => Assert.NotNull(r));
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
                It.IsAny<bool>(),
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
            await Task.Delay(10);
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
                It.IsAny<bool>(),
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
                It.IsAny<bool>(),
                It.IsAny<When>(),
                CommandFlags.None))
            .ReturnsAsync(true);

        this._mockDatabase
            .Setup(db => db.KeyDeleteAsync(lockKey, CommandFlags.None))
            .ReturnsAsync(true);

        var service = new RedisInFlightDeduplication(this._mockRedis.Object);

        Func<Task<string>> operation = async () =>
        {
            await Task.Delay(10);
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

        this._mockDatabase.Verify(
            db => db.StringSetAsync(
                It.Is<RedisKey>(k => k.ToString() == resultKey),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(),
                It.IsAny<When>(),
                CommandFlags.None),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteWithDeduplication_Failure_ReleasesLock()
    {
        // Arrange
        var requestHash = "hash-failure";
        var lockKey = $"inflight:{requestHash}";
        var resultKey = $"result:{requestHash}";
        var lockTimeout = TimeSpan.FromSeconds(30);

        this._mockDatabase
            .Setup(db => db.StringSetAsync(
                lockKey,
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(),
                When.NotExists,
                CommandFlags.None))
            .ReturnsAsync(true);

        this._mockDatabase
            .Setup(db => db.StringGetAsync(resultKey, It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        var lockReleased = false;
        this._mockDatabase
            .Setup(db => db.KeyDeleteAsync(lockKey, CommandFlags.None))
            .Callback(() => lockReleased = true)
            .ReturnsAsync(true);

        var service = new RedisInFlightDeduplication(this._mockRedis.Object);

        Func<Task<string>> operation = async () =>
        {
            await Task.Delay(10);
            throw new InvalidOperationException("Operation failed");
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await service.ExecuteWithDeduplication(
                requestHash,
                operation,
                lockTimeout,
                this._cancellationToken);
        });

        // Assert - Lock should be released even on failure
        Assert.True(lockReleased);

        this._mockDatabase.Verify(
            db => db.KeyDeleteAsync(lockKey, CommandFlags.None),
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
                It.IsAny<bool>(),
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
                this._cancellationToken);
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
                It.IsAny<TimeSpan>(),
                When.NotExists))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection failed"));

        var service = new RedisInFlightDeduplication(this._mockRedis.Object);
        var executed = false;

        Func<Task<string>> operation = async () =>
        {
            executed = true;
            await Task.Delay(10);
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
/// Interface for in-flight request deduplication
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
/// Redis implementation of IInFlightDeduplication
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
            var lockAcquired = await db.StringSetAsync(lockKey, "locked", lockTimeout, when: When.NotExists);

            if (lockAcquired)
            {
                try
                {
                    // We got the lock - execute the operation
                    var result = await operation();

                    // Store the result for other waiters
                    var serialized = System.Text.Json.JsonSerializer.Serialize(result);
                    await db.StringSetAsync(resultKey, serialized, TimeSpan.FromMinutes(5));

                    return result;
                }
                finally
                {
                    // Always release the lock
                    await db.KeyDeleteAsync(lockKey);
                }
            }
            else
            {
                // Someone else is executing - wait for result
                var deadline = DateTime.UtcNow.Add(lockTimeout);

                while (DateTime.UtcNow < deadline)
                {
                    var cachedResult = await db.StringGetAsync(resultKey);
                    if (cachedResult.HasValue)
                    {
                        return System.Text.Json.JsonSerializer.Deserialize<T>(cachedResult.ToString())!;
                    }

                    await Task.Delay(100, cancellationToken); // Poll interval
                }

                // Timeout - execute as fallback
                return await operation();
            }
        }
        catch (RedisException)
        {
            // Fail open - execute directly if Redis is unavailable
            return await operation();
        }
    }

    public async Task<bool> IsInFlightAsync(string requestHash, CancellationToken cancellationToken)
    {
        var db = this._redis.GetDatabase();
        var lockKey = $"inflight:{requestHash}";
        return await db.KeyExistsAsync(lockKey);
    }
}
