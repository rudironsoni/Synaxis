using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace Synaxis.InferenceGateway.Infrastructure.Tests.Optimization.Caching;

/// <summary>
/// Unit tests for ISessionStore Redis implementation
/// Tests session affinity and activity tracking for token optimization
/// </summary>
public class RedisSessionStoreTests
{
    private readonly Mock<IConnectionMultiplexer> _mockRedis;
    private readonly Mock<IDatabase> _mockDatabase;
    private readonly CancellationToken _cancellationToken;

    public RedisSessionStoreTests()
    {
        _mockRedis = new Mock<IConnectionMultiplexer>();
        _mockDatabase = new Mock<IDatabase>();
        _mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_mockDatabase.Object);
        _cancellationToken = CancellationToken.None;
    }

    [Fact]
    public async Task GetProviderAffinityAsync_Existing_ReturnsValue()
    {
        // Arrange
        var sessionId = "session-123";
        var expectedProvider = "openai-gpt4";
        var key = $"session:{sessionId}:affinity";

        _mockDatabase
            .Setup(db => db.StringGetAsync(key, It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)expectedProvider);

        var store = new RedisSessionStore(_mockRedis.Object);

        // Act
        var result = await store.GetProviderAffinityAsync(sessionId, _cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedProvider, result);

        _mockDatabase.Verify(
            db => db.StringGetAsync(key, It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Fact]
    public async Task GetProviderAffinityAsync_Missing_ReturnsNull()
    {
        // Arrange
        var sessionId = "session-nonexistent";
        var key = $"session:{sessionId}:affinity";

        _mockDatabase
            .Setup(db => db.StringGetAsync(key, It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        var store = new RedisSessionStore(_mockRedis.Object);

        // Act
        var result = await store.GetProviderAffinityAsync(sessionId, _cancellationToken);

        // Assert
        Assert.Null(result);

        _mockDatabase.Verify(
            db => db.StringGetAsync(key, It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Fact]
    public async Task SetProviderAffinityAsync_StoresValue()
    {
        // Arrange
        var sessionId = "session-123";
        var providerId = "anthropic-claude";
        var ttl = TimeSpan.FromHours(1);
        var key = $"session:{sessionId}:affinity";

        _mockDatabase
            .Setup(db => db.StringSetAsync(key, providerId, ttl, false, When.Always, CommandFlags.None))
            .ReturnsAsync(true);

        var store = new RedisSessionStore(_mockRedis.Object);

        // Act
        await store.SetProviderAffinityAsync(sessionId, providerId, ttl, _cancellationToken);

        // Assert
        _mockDatabase.Verify(
            db => db.StringSetAsync(key, providerId, ttl, false, When.Always, CommandFlags.None),
            Times.Once);
    }

    [Fact]
    public async Task GetLastActivityAsync_Existing_ReturnsTimestamp()
    {
        // Arrange
        var sessionId = "session-123";
        var expectedTimestamp = DateTimeOffset.UtcNow.AddMinutes(-5);
        var key = $"session:{sessionId}:lastactivity";

        _mockDatabase
            .Setup(db => db.StringGetAsync(key, It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)expectedTimestamp.ToUnixTimeSeconds());

        var store = new RedisSessionStore(_mockRedis.Object);

        // Act
        var result = await store.GetLastActivityAsync(sessionId, _cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.HasValue);
        Assert.InRange(result.Value, expectedTimestamp.AddSeconds(-1), expectedTimestamp.AddSeconds(1));

        _mockDatabase.Verify(
            db => db.StringGetAsync(key, It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateLastActivityAsync_UpdatesTimestamp()
    {
        // Arrange
        var sessionId = "session-123";
        var timestamp = DateTimeOffset.UtcNow;
        var ttl = TimeSpan.FromHours(2);
        var key = $"session:{sessionId}:lastactivity";

        _mockDatabase
            .Setup(db => db.StringSetAsync(
                key,
                It.IsAny<RedisValue>(),
                ttl,
                false,
                When.Always,
                CommandFlags.None))
            .ReturnsAsync(true);

        var store = new RedisSessionStore(_mockRedis.Object);

        // Act
        await store.UpdateLastActivityAsync(sessionId, timestamp, ttl, _cancellationToken);

        // Assert
        _mockDatabase.Verify(
            db => db.StringSetAsync(
                key,
                It.Is<RedisValue>(v => Math.Abs((long)v - timestamp.ToUnixTimeSeconds()) <= 1),
                ttl,
                false,
                When.Always,
                CommandFlags.None),
            Times.Once);
    }

    [Fact]
    public async Task SessionExistsAsync_Existing_ReturnsTrue()
    {
        // Arrange
        var sessionId = "session-123";
        var key = $"session:{sessionId}:affinity";

        _mockDatabase
            .Setup(db => db.KeyExistsAsync(key, It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var store = new RedisSessionStore(_mockRedis.Object);

        // Act
        var result = await store.SessionExistsAsync(sessionId, _cancellationToken);

        // Assert
        Assert.True(result);

        _mockDatabase.Verify(
            db => db.KeyExistsAsync(key, It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Fact]
    public async Task SessionExistsAsync_Missing_ReturnsFalse()
    {
        // Arrange
        var sessionId = "session-nonexistent";
        var key = $"session:{sessionId}:affinity";

        _mockDatabase
            .Setup(db => db.KeyExistsAsync(key, It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);

        var store = new RedisSessionStore(_mockRedis.Object);

        // Act
        var result = await store.SessionExistsAsync(sessionId, _cancellationToken);

        // Assert
        Assert.False(result);

        _mockDatabase.Verify(
            db => db.KeyExistsAsync(key, It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Fact]
    public async Task GetActiveSessionsAsync_ReturnsList()
    {
        // Arrange
        var pattern = "session:*:affinity";
        var sessionKeys = new RedisKey[]
        {
            "session:session-1:affinity",
            "session:session-2:affinity",
            "session:session-3:affinity"
        };

        var mockServer = new Mock<IServer>();
        mockServer
            .Setup(s => s.KeysAsync(
                It.IsAny<int>(),
                It.Is<RedisValue>(v => v.ToString() == pattern),
                It.IsAny<int>(),
                It.IsAny<long>(),
                It.IsAny<int>(),
                It.IsAny<CommandFlags>()))
            .Returns(sessionKeys.ToAsyncEnumerable());

        _mockRedis
            .Setup(r => r.GetEndPoints(It.IsAny<bool>()))
            .Returns(new System.Net.EndPoint[] { new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 6379) });

        _mockRedis
            .Setup(r => r.GetServer(It.IsAny<System.Net.EndPoint>(), It.IsAny<object>()))
            .Returns(mockServer.Object);

        var store = new RedisSessionStore(_mockRedis.Object);

        // Act
        var result = await store.GetActiveSessionsAsync(_cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Contains("session-1", result);
        Assert.Contains("session-2", result);
        Assert.Contains("session-3", result);
    }

    [Fact]
    public async Task RedisFailure_FailsOpen_ReturnsNull()
    {
        // Arrange
        var sessionId = "session-123";
        var key = $"session:{sessionId}:affinity";

        _mockDatabase
            .Setup(db => db.StringGetAsync(key, It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection failed"));

        var store = new RedisSessionStore(_mockRedis.Object);

        // Act
        var result = await store.GetProviderAffinityAsync(sessionId, _cancellationToken);

        // Assert - Should fail open and return null rather than throwing
        Assert.Null(result);
    }

    [Fact]
    public async Task ConcurrentAccess_HandlesSafely()
    {
        // Arrange
        var sessionId = "session-concurrent";
        var providerId = "openai-gpt4";
        var key = $"session:{sessionId}:affinity";
        var ttl = TimeSpan.FromHours(1);

        _mockDatabase
            .Setup(db => db.StringSetAsync(key, providerId, ttl, false, When.Always, CommandFlags.None))
            .ReturnsAsync(true);

        _mockDatabase
            .Setup(db => db.StringGetAsync(key, It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)providerId);

        var store = new RedisSessionStore(_mockRedis.Object);

        // Act - Simulate concurrent access
        var setTasks = new Task[10];
        var getTasks = new Task<string?>[10];

        for (int i = 0; i < 10; i++)
        {
            setTasks[i] = store.SetProviderAffinityAsync(sessionId, providerId, ttl, _cancellationToken);
            getTasks[i] = store.GetProviderAffinityAsync(sessionId, _cancellationToken);
        }

        await Task.WhenAll(setTasks);
        var results = await Task.WhenAll(getTasks);

        // Assert
        Assert.All(results, r => Assert.Equal(providerId, r));

        _mockDatabase.Verify(
            db => db.StringSetAsync(key, providerId, ttl, false, When.Always, CommandFlags.None),
            Times.Exactly(10));

        _mockDatabase.Verify(
            db => db.StringGetAsync(key, It.IsAny<CommandFlags>()),
            Times.Exactly(10));
    }
}

/// <summary>
/// Mock implementation of ISessionStore for testing
/// </summary>
public interface ISessionStore
{
    Task<string?> GetProviderAffinityAsync(string sessionId, CancellationToken cancellationToken);
    Task SetProviderAffinityAsync(string sessionId, string providerId, TimeSpan ttl, CancellationToken cancellationToken);
    Task<DateTimeOffset?> GetLastActivityAsync(string sessionId, CancellationToken cancellationToken);
    Task UpdateLastActivityAsync(string sessionId, DateTimeOffset timestamp, TimeSpan ttl, CancellationToken cancellationToken);
    Task<bool> SessionExistsAsync(string sessionId, CancellationToken cancellationToken);
    Task<List<string>> GetActiveSessionsAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Redis implementation of ISessionStore
/// </summary>
public class RedisSessionStore : ISessionStore
{
    private readonly IConnectionMultiplexer _redis;

    public RedisSessionStore(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<string?> GetProviderAffinityAsync(string sessionId, CancellationToken cancellationToken)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = $"session:{sessionId}:affinity";
            var value = await db.StringGetAsync(key);
            return value.HasValue ? value.ToString() : null;
        }
        catch (RedisException)
        {
            // Fail open - return null on Redis errors
            return null;
        }
    }

    public async Task SetProviderAffinityAsync(string sessionId, string providerId, TimeSpan ttl, CancellationToken cancellationToken)
    {
        var db = _redis.GetDatabase();
        var key = $"session:{sessionId}:affinity";
        await db.StringSetAsync(key, providerId, ttl);
    }

    public async Task<DateTimeOffset?> GetLastActivityAsync(string sessionId, CancellationToken cancellationToken)
    {
        var db = _redis.GetDatabase();
        var key = $"session:{sessionId}:lastactivity";
        var value = await db.StringGetAsync(key);

        if (value.HasValue && long.TryParse(value.ToString(), out var unixTimestamp))
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);
        }

        return null;
    }

    public async Task UpdateLastActivityAsync(string sessionId, DateTimeOffset timestamp, TimeSpan ttl, CancellationToken cancellationToken)
    {
        var db = _redis.GetDatabase();
        var key = $"session:{sessionId}:lastactivity";
        await db.StringSetAsync(key, timestamp.ToUnixTimeSeconds(), ttl);
    }

    public async Task<bool> SessionExistsAsync(string sessionId, CancellationToken cancellationToken)
    {
        var db = _redis.GetDatabase();
        var key = $"session:{sessionId}:affinity";
        return await db.KeyExistsAsync(key);
    }

    public async Task<List<string>> GetActiveSessionsAsync(CancellationToken cancellationToken)
    {
        var sessions = new List<string>();
        var endpoints = _redis.GetEndPoints();

        foreach (var endpoint in endpoints)
        {
            var server = _redis.GetServer(endpoint);
            var keys = server.KeysAsync(pattern: "session:*:affinity");

            await foreach (var key in keys)
            {
                var keyStr = key.ToString();
                // Extract session ID from "session:{sessionId}:affinity"
                var parts = keyStr.Split(':');
                if (parts.Length == 3)
                {
                    sessions.Add(parts[1]);
                }
            }
        }

        return sessions;
    }
}

// Extension method for testing
internal static class AsyncEnumerableExtensions
{
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
    {
        foreach (var item in source)
        {
            yield return item;
        }
        await Task.CompletedTask;
    }
}
