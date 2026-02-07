using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace Synaxis.InferenceGateway.Infrastructure.Tests.Optimization.Caching;

/// <summary>
/// Unit tests for IConversationStore Redis implementation
/// Tests conversation history storage and compression for token optimization
/// </summary>
public class RedisConversationStoreTests
{
    private readonly Mock<IConnectionMultiplexer> _mockRedis;
    private readonly Mock<IDatabase> _mockDatabase;
    private readonly CancellationToken _cancellationToken;

    public RedisConversationStoreTests()
    {
        this._mockRedis = new Mock<IConnectionMultiplexer>();
        this._mockDatabase = new Mock<IDatabase>();
        this._mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(this._mockDatabase.Object);
        this._cancellationToken = CancellationToken.None;
    }

    [Fact]
    public async Task GetFullHistoryAsync_Existing_ReturnsMessages()
    {
        // Arrange
        var sessionId = "session-123";
        var key = $"conversation:{sessionId}:messages";

        var messages = new List<ConversationMessage>
        {
            new () { Role = "user", Content = "Hello", Timestamp = DateTimeOffset.UtcNow.AddMinutes(-10) },
            new () { Role = "assistant", Content = "Hi there!", Timestamp = DateTimeOffset.UtcNow.AddMinutes(-9) },
            new () { Role = "user", Content = "How are you?", Timestamp = DateTimeOffset.UtcNow.AddMinutes(-8) },
        };

        var redisValues = messages.Select(m => (RedisValue)JsonSerializer.Serialize(m)).ToArray();

        this._mockDatabase
            .Setup(db => db.ListRangeAsync(key, 0, -1, It.IsAny<CommandFlags>()))
            .ReturnsAsync(redisValues);

        var store = new RedisConversationStore(this._mockRedis.Object);

        // Act
        var result = await store.GetFullHistoryAsync(sessionId, this._cancellationToken).ConfigureAwait(false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("Hello", result[0].Content);
        Assert.Equal("Hi there!", result[1].Content);
        Assert.Equal("How are you?", result[2].Content);

        this._mockDatabase.Verify(
            db => db.ListRangeAsync(key, 0, -1, It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Fact]
    public async Task GetFullHistoryAsync_Missing_ReturnsEmpty()
    {
        // Arrange
        var sessionId = "session-nonexistent";
        var key = $"conversation:{sessionId}:messages";

        this._mockDatabase
            .Setup(db => db.ListRangeAsync(key, 0, -1, It.IsAny<CommandFlags>()))
            .ReturnsAsync(Array.Empty<RedisValue>());

        var store = new RedisConversationStore(this._mockRedis.Object);

        // Act
        var result = await store.GetFullHistoryAsync(sessionId, this._cancellationToken).ConfigureAwait(false);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);

        this._mockDatabase.Verify(
            db => db.ListRangeAsync(key, 0, -1, It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Fact]
    public async Task AppendMessageAsync_AddsToList()
    {
        // Arrange
        var sessionId = "session-123";
        var key = $"conversation:{sessionId}:messages";
        var ttl = TimeSpan.FromHours(24);

        var message = new ConversationMessage
        {
            Role = "user",
            Content = "New message",
            Timestamp = DateTimeOffset.UtcNow,
        };

        this._mockDatabase
            .Setup(db => db.ListRightPushAsync(
                key,
                It.IsAny<RedisValue>(),
                When.Always,
                CommandFlags.None))
            .ReturnsAsync(1);

        this._mockDatabase
            .Setup(db => db.KeyExpireAsync(key, ttl, ExpireWhen.Always, CommandFlags.None))
            .ReturnsAsync(true);

        var store = new RedisConversationStore(this._mockRedis.Object);

        // Act
        await store.AppendMessageAsync(sessionId, message, ttl, this._cancellationToken).ConfigureAwait(false);

        // Assert
        this._mockDatabase.Verify(
            db => db.ListRightPushAsync(
                key,
                It.Is<RedisValue>(v => v.ToString().Contains("New message", StringComparison.Ordinal)),
                When.Always,
                CommandFlags.None),
            Times.Once);

        this._mockDatabase.Verify(
            db => db.KeyExpireAsync(key, ttl, ExpireWhen.Always, CommandFlags.None),
            Times.Once);
    }

    [Fact]
    public async Task GetCompressedForProviderAsync_AppliesStrategy()
    {
        // Arrange
        var sessionId = "session-123";
        var providerStrategy = "sliding-window";
        var maxTokens = 1000;
        var key = $"conversation:{sessionId}:messages";

        var messages = new List<ConversationMessage>();
        for (int i = 0; i < 20; i++)
        {
            messages.Add(new ConversationMessage
            {
                Role = i % 2 == 0 ? "user" : "assistant",
                Content = $"Message {i}",
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-20 + i),
            });
        }

        var redisValues = messages.Select(m => (RedisValue)JsonSerializer.Serialize(m)).ToArray();

        this._mockDatabase
            .Setup(db => db.ListRangeAsync(key, 0, -1, It.IsAny<CommandFlags>()))
            .ReturnsAsync(redisValues);

        var store = new RedisConversationStore(this._mockRedis.Object);

        // Act
        var result = await store.GetCompressedForProviderAsync(
            sessionId,
            providerStrategy,
            maxTokens,
            this._cancellationToken).ConfigureAwait(false);

        // Assert
        Assert.NotNull(result);
        // With sliding window strategy, should return most recent messages within token limit
        Assert.True(result.Count <= messages.Count);
        Assert.True(result.Count > 0);

        this._mockDatabase.Verify(
            db => db.ListRangeAsync(key, 0, -1, It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Fact]
    public async Task ClearSessionAsync_RemovesAllData()
    {
        // Arrange
        var sessionId = "session-123";
        var messageKey = $"conversation:{sessionId}:messages";
        var metadataKey = $"conversation:{sessionId}:metadata";

        this._mockDatabase
            .Setup(db => db.KeyDeleteAsync(
                It.Is<RedisKey[]>(keys =>
                    keys.Any(k => k.ToString() == messageKey) &&
                    keys.Any(k => k.ToString() == metadataKey)),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(2);

        var store = new RedisConversationStore(this._mockRedis.Object);

        // Act
        await store.ClearSessionAsync(sessionId, this._cancellationToken).ConfigureAwait(false);

        // Assert
        this._mockDatabase.Verify(
            db => db.KeyDeleteAsync(
                It.Is<RedisKey[]>(keys =>
                    keys.Any(k => k.ToString() == messageKey) &&
                    keys.Any(k => k.ToString() == metadataKey)),
                It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Fact]
    public async Task LargeConversation_HandlesEfficiently()
    {
        // Arrange
        var sessionId = "session-large";
        var key = $"conversation:{sessionId}:messages";

        // Create a large conversation (100 messages)
        var messages = new List<ConversationMessage>();
        for (int i = 0; i < 100; i++)
        {
            messages.Add(new ConversationMessage
            {
                Role = i % 2 == 0 ? "user" : "assistant",
                Content = $"This is message number {i} with some substantial content to simulate real conversations.",
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-100 + i),
            });
        }

        var redisValues = messages.Select(m => (RedisValue)JsonSerializer.Serialize(m)).ToArray();

        this._mockDatabase
            .Setup(db => db.ListRangeAsync(key, 0, -1, It.IsAny<CommandFlags>()))
            .ReturnsAsync(redisValues);

        var store = new RedisConversationStore(this._mockRedis.Object);

        // Act
        var result = await store.GetFullHistoryAsync(sessionId, this._cancellationToken).ConfigureAwait(false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(100, result.Count);
        Assert.Equal("This is message number 0 with some substantial content to simulate real conversations.", result[0].Content);
        Assert.Equal("This is message number 99 with some substantial content to simulate real conversations.", result[99].Content);
    }

    [Fact]
    public async Task RedisFailure_FailsOpen_ReturnsEmpty()
    {
        // Arrange
        var sessionId = "session-123";
        var key = $"conversation:{sessionId}:messages";

        this._mockDatabase
            .Setup(db => db.ListRangeAsync(key, 0, -1, It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection failed"));

        var store = new RedisConversationStore(this._mockRedis.Object);

        // Act
        var result = await store.GetFullHistoryAsync(sessionId, this._cancellationToken).ConfigureAwait(false);

        // Assert - Should fail open and return empty list rather than throwing
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task MessageSerialization_RoundTripsCorrectly()
    {
        // Arrange
        var sessionId = "session-serialize";
        var key = $"conversation:{sessionId}:messages";
        var ttl = TimeSpan.FromHours(24);

        var originalMessage = new ConversationMessage
        {
            Role = "user",
            Content = "Test message with special chars: ñ, 中文, 🎉",
            Timestamp = DateTimeOffset.UtcNow,
            Metadata = new Dictionary<string, string>
            {
                { "model", "gpt-4" },
                { "temperature", "0.7" }
            },
        };

        RedisValue capturedValue = RedisValue.Null;

        this._mockDatabase
            .Setup(db => db.ListRightPushAsync(
                key,
                It.IsAny<RedisValue>(),
                When.Always,
                CommandFlags.None))
            .Callback<RedisKey, RedisValue, When, CommandFlags>((k, v, w, f) => capturedValue = v)
            .ReturnsAsync(1);

        this._mockDatabase
            .Setup(db => db.KeyExpireAsync(key, ttl, ExpireWhen.Always, CommandFlags.None))
            .ReturnsAsync(true);

        this._mockDatabase
            .Setup(db => db.ListRangeAsync(key, 0, -1, It.IsAny<CommandFlags>()))
            .ReturnsAsync(() => new[] { capturedValue });

        var store = new RedisConversationStore(this._mockRedis.Object);

        // Act - Append and retrieve
        await store.AppendMessageAsync(sessionId, originalMessage, ttl, this._cancellationToken).ConfigureAwait(false);
        var result = await store.GetFullHistoryAsync(sessionId, this._cancellationToken).ConfigureAwait(false);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);

        var retrievedMessage = result[0];
        Assert.Equal(originalMessage.Role, retrievedMessage.Role);
        Assert.Equal(originalMessage.Content, retrievedMessage.Content);
        Assert.NotNull(retrievedMessage.Metadata);
        Assert.Equal("gpt-4", retrievedMessage.Metadata["model"]);
        Assert.Equal("0.7", retrievedMessage.Metadata["temperature"]);
    }
}

/// <summary>
/// Interface for conversation history storage
/// </summary>
public interface IConversationStore
{
    Task<List<ConversationMessage>> GetFullHistoryAsync(string sessionId, CancellationToken cancellationToken);

    Task AppendMessageAsync(string sessionId, ConversationMessage message, TimeSpan ttl, CancellationToken cancellationToken);

    Task<List<ConversationMessage>> GetCompressedForProviderAsync(string sessionId, string strategy, int maxTokens, CancellationToken cancellationToken);

    Task ClearSessionAsync(string sessionId, CancellationToken cancellationToken);
}

/// <summary>
/// Redis implementation of IConversationStore
/// </summary>
public class RedisConversationStore : IConversationStore
{
    private readonly IConnectionMultiplexer _redis;

    public RedisConversationStore(IConnectionMultiplexer redis)
    {
        this._redis = redis;
    }

    public async Task<List<ConversationMessage>> GetFullHistoryAsync(string sessionId, CancellationToken cancellationToken)
    {
        try
        {
            var db = this._redis.GetDatabase();
            var key = $"conversation:{sessionId}:messages";
            var values = await db.ListRangeAsync(key).ConfigureAwait(false);

            var messages = new List<ConversationMessage>();
            foreach (var value in values)
            {
                if (value.HasValue)
                {
                    var message = JsonSerializer.Deserialize<ConversationMessage>(value.ToString());
                    if (message != null)
                    {
                        messages.Add(message);
                    }
                }
            }

            return messages;
        }
        catch (RedisException)
        {
            // Fail open - return empty list on Redis errors
            return new List<ConversationMessage>();
        }
    }

    public async Task AppendMessageAsync(string sessionId, ConversationMessage message, TimeSpan ttl, CancellationToken cancellationToken)
    {
        var db = this._redis.GetDatabase();
        var key = $"conversation:{sessionId}:messages";
        var serialized = JsonSerializer.Serialize(message);

        await db.ListRightPushAsync(key, serialized).ConfigureAwait(false);
        await db.KeyExpireAsync(key, ttl).ConfigureAwait(false);
    }

    public async Task<List<ConversationMessage>> GetCompressedForProviderAsync(
        string sessionId,
        string strategy,
        int maxTokens,
        CancellationToken cancellationToken)
    {
        var fullHistory = await this.GetFullHistoryAsync(sessionId, cancellationToken).ConfigureAwait(false);

        // Simple sliding window compression for testing
        // In production, this would use actual token counting and sophisticated strategies
        if (strategy == "sliding-window")
        {
            // Return last N messages that fit within token limit
            // Assume ~4 tokens per message for simplification
            var maxMessages = Math.Min(maxTokens / 4, fullHistory.Count);
            return fullHistory.Skip(Math.Max(0, fullHistory.Count - maxMessages)).ToList();
        }

        return fullHistory;
    }

    public async Task ClearSessionAsync(string sessionId, CancellationToken cancellationToken)
    {
        var db = this._redis.GetDatabase();
        var keys = new RedisKey[]
        {
            $"conversation:{sessionId}:messages",
            $"conversation:{sessionId}:metadata",
        };

        await db.KeyDeleteAsync(keys).ConfigureAwait(false);
    }
}

/// <summary>
/// Represents a message in a conversation
/// </summary>
public class ConversationMessage
{
    public string Role { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTimeOffset Timestamp { get; set; }

    public Dictionary<string, string>? Metadata { get; set; }
}
