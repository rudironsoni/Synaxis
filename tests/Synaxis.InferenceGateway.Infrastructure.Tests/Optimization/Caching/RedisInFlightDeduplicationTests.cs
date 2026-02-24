// <copyright file="RedisInFlightDeduplicationTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Tests.Optimization.Caching;

using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using StackExchange.Redis;
using Synaxis.InferenceGateway.Infrastructure.Optimization.Caching;
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
                When.NotExists,
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

        // Release lock using Lua script
        this._mockDatabase
            .Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                CommandFlags.None))
            .ReturnsAsync(() => RedisResult.Create(1));

        var service = new RedisInFlightDeduplication(this._mockRedis.Object);
        var executed = false;
        Func<Task<string>> operation = async () =>
        {
            executed = true;
            await Task.Yield();
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
                When.NotExists,
                It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteWithDeduplication_ConcurrentCalls_Deduplicates()
    {
        // Arrange
        var requestHash = "hash-concurrent";
        var lockKey = $"inflight:{requestHash}";
        var resultKey = $"result:{requestHash}";
        var lockTimeout = TimeSpan.FromMilliseconds(500);
        var resultStored = false;
        var lockReleased = false;
        var lockAcquired = false;
        var concurrentCallers = 5;

        // Use thread-safe mechanisms for state tracking
        var lockObj = new object();

        // Lock acquisition: only first caller succeeds
        this._mockDatabase
            .Setup(db => db.StringSetAsync(
                It.Is<RedisKey>(k => k.ToString() == lockKey),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                When.NotExists,
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(() =>
            {
                lock (lockObj)
                {
                    if (!lockAcquired)
                    {
                        lockAcquired = true;
                        return true;
                    }
                    return false;
                }
            });

        // Result polling: initially null, then available after stored
        this._mockDatabase
            .Setup(db => db.StringGetAsync(
                It.Is<RedisKey>(k => k.ToString() == resultKey),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(() =>
            {
                lock (lockObj)
                {
                    return resultStored ? "\"Result from operation\"" : RedisValue.Null;
                }
            });

        // Store result
        this._mockDatabase
            .Setup(db => db.StringSetAsync(
                It.Is<RedisKey>(k => k.ToString() == resultKey),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                When.Always,
                It.IsAny<CommandFlags>()))
            .Callback(() =>
            {
                lock (lockObj)
                {
                    resultStored = true;
                }
            })
            .ReturnsAsync(true);

        // Release lock using Lua script
        this._mockDatabase
            .Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                CommandFlags.None))
            .ReturnsAsync(() =>
            {
                lockReleased = true;
                return RedisResult.Create(1);
            });

        var service = new RedisInFlightDeduplication(this._mockRedis.Object);
        var executionCount = 0;

        Func<Task<string>> operation = async () =>
        {
            Interlocked.Increment(ref executionCount);
            await Task.Yield();
            return "Result from operation";
        };

        // Act - Launch 5 concurrent requests with timeout protection
        var tasks = new List<Task<string>>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        for (int i = 0; i < concurrentCallers; i++)
        {
            tasks.Add(service.ExecuteWithDeduplication(
                requestHash,
                operation,
                lockTimeout,
                cts.Token));
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
            await Task.Yield();
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
            .Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                CommandFlags.None))
            .ReturnsAsync(() => RedisResult.Create(1));

        var service = new RedisInFlightDeduplication(this._mockRedis.Object);

        Func<Task<string>> operation = async () =>
        {
            await Task.Yield();
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
        var lockAcquisitionCount = 0;

        // Lock acquisition succeeds on first call, then can be acquired again after release
        this._mockDatabase
            .Setup(db => db.StringSetAsync(
                It.Is<RedisKey>(k => k.ToString() == lockKey),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                When.NotExists,
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(() =>
            {
                using var scope = this._lock.EnterScope();
                lockAcquisitionCount++;
                return true;
            });

        // No cached result
        this._mockDatabase
            .Setup(db => db.StringGetAsync(
                It.Is<RedisKey>(k => k.ToString() == resultKey),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        // Lock release using Lua script (called in finally block even on failure)
        this._mockDatabase
            .Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                CommandFlags.None))
            .Callback(() =>
            {
                using var scope = this._lock.EnterScope();
                lockReleased = true;
            })
            .ReturnsAsync(() => RedisResult.Create(1));

        var service = new RedisInFlightDeduplication(this._mockRedis.Object);

        Func<Task<string>> operation = async () =>
        {
            await Task.Yield();
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

        // Verify lock was acquired exactly once
        Assert.Equal(1, lockAcquisitionCount);

        this._mockDatabase.Verify(
            db => db.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                CommandFlags.None),
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
            .Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                CommandFlags.None))
            .ReturnsAsync(() => RedisResult.Create(1));

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
            await Task.Yield();
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
