
namespace Synaxis.InferenceGateway.Infrastructure.Tests.Routing;

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using Synaxis.InferenceGateway.Infrastructure.Services;
using Xunit;

/// <summary>
/// Unit tests for RedisRateLimitingService.
/// Tests rate limit checking, hierarchical rate limiting, token usage tracking, and Lua script execution.
/// </summary>
public class RedisRateLimitingServiceTests
{
    private readonly Mock<IConnectionMultiplexer> _mockRedis;
    private readonly Mock<IDatabase> _mockDatabase;
    private readonly Mock<ILogger<RedisRateLimitingService>> _mockLogger;
    private readonly RedisRateLimitingService _service;

    public RedisRateLimitingServiceTests()
    {
        this._mockRedis = new Mock<IConnectionMultiplexer>();
        this._mockDatabase = new Mock<IDatabase>();
        this._mockLogger = new Mock<ILogger<RedisRateLimitingService>>();

        this._mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(this._mockDatabase.Object);

        this._service = new RedisRateLimitingService(this._mockRedis.Object, this._mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullRedis_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new RedisRateLimitingService(null!, this._mockLogger.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("redis");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new RedisRateLimitingService(this._mockRedis.Object, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region CheckRateLimitAsync Tests

    [Fact]
    public async Task CheckRateLimitAsync_WithinLimit_ShouldReturnAllowed()
    {
        // Arrange
        var key = "ratelimit:user:123:rpm";
        var limit = 100;
        var window = TimeSpan.FromMinutes(1);

        // Simulate Lua script returning [current_count, ttl_seconds]
        var luaResult = new RedisValue[] { 50, 60 };
        this._mockDatabase
            .Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create(luaResult));

        // Act
        var result = await this._service.CheckRateLimitAsync(key, limit, window).ConfigureAwait(false);

        // Assert
        result.Should().NotBeNull();
        result.IsAllowed.Should().BeTrue();
        result.Current.Should().Be(50);
        result.Limit.Should().Be(limit);
        result.Remaining.Should().Be(50);
        result.ResetAfter.Should().Be(60);
    }

    [Fact]
    public async Task CheckRateLimitAsync_ExceedsLimit_ShouldReturnDenied()
    {
        // Arrange
        var key = "ratelimit:user:123:rpm";
        var limit = 100;
        var window = TimeSpan.FromMinutes(1);

        // Simulate exceeding the limit
        var luaResult = new RedisValue[] { 101, 45 };
        this._mockDatabase
            .Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create(luaResult));

        // Act
        var result = await this._service.CheckRateLimitAsync(key, limit, window).ConfigureAwait(false);

        // Assert
        result.Should().NotBeNull();
        result.IsAllowed.Should().BeFalse();
        result.Current.Should().Be(101);
        result.Limit.Should().Be(limit);
        result.Remaining.Should().Be(0);
        result.ResetAfter.Should().Be(45);
    }

    [Fact]
    public async Task CheckRateLimitAsync_WithCustomIncrement_ShouldUseCorrectValue()
    {
        // Arrange
        var key = "ratelimit:user:123:tpm";
        var limit = 10000;
        var window = TimeSpan.FromMinutes(1);
        var increment = 500; // Increment by 500 tokens

        var luaResult = new RedisValue[] { 5500, 60 };
        this._mockDatabase
            .Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.Is<RedisKey[]>(keys => keys[0] == key),
                It.Is<RedisValue[]>(args => (int)args[0] == limit &&
                                            (int)args[1] == 60 &&
                                            (int)args[2] == increment),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create(luaResult));

        // Act
        var result = await this._service.CheckRateLimitAsync(key, limit, window, increment).ConfigureAwait(false);

        // Assert
        result.IsAllowed.Should().BeTrue();
        result.Current.Should().Be(5500);
    }

    [Fact]
    public async Task CheckRateLimitAsync_WithNullOrEmptyKey_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act1 = async () => await this._service.CheckRateLimitAsync(null!, 100, TimeSpan.FromMinutes(1)).ConfigureAwait(false);
        await act1.Should().ThrowAsync<ArgumentException>().WithParameterName("key").ConfigureAwait(false);

        var act2 = async () => await this._service.CheckRateLimitAsync("", 100, TimeSpan.FromMinutes(1)).ConfigureAwait(false);
        await act2.Should().ThrowAsync<ArgumentException>().WithParameterName("key").ConfigureAwait(false);

        var act3 = async () => await this._service.CheckRateLimitAsync("   ", 100, TimeSpan.FromMinutes(1)).ConfigureAwait(false);
        await act3.Should().ThrowAsync<ArgumentException>().WithParameterName("key").ConfigureAwait(false);
    }

    [Fact]
    public async Task CheckRateLimitAsync_WithZeroOrNegativeLimit_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act1 = async () => await this._service.CheckRateLimitAsync("key", 0, TimeSpan.FromMinutes(1)).ConfigureAwait(false);
        await act1.Should().ThrowAsync<ArgumentException>().WithParameterName("limit").ConfigureAwait(false);

        var act2 = async () => await this._service.CheckRateLimitAsync("key", -10, TimeSpan.FromMinutes(1)).ConfigureAwait(false);
        await act2.Should().ThrowAsync<ArgumentException>().WithParameterName("limit").ConfigureAwait(false);
    }

    [Fact]
    public async Task CheckRateLimitAsync_WhenRedisThrows_ShouldFailOpen()
    {
        // Arrange
        var key = "ratelimit:user:123:rpm";
        var limit = 100;
        var window = TimeSpan.FromMinutes(1);

        this._mockDatabase
            .Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection failed"));

        // Act
        var result = await this._service.CheckRateLimitAsync(key, limit, window).ConfigureAwait(false);

        // Assert
        result.Should().NotBeNull();
        result.IsAllowed.Should().BeTrue(); // Fail open: allow request
        result.Current.Should().Be(0);
        result.Limit.Should().Be(limit);
    }

    [Fact]
    public async Task CheckRateLimitAsync_ShouldExecuteLuaScriptAtomically()
    {
        // Arrange
        var key = "ratelimit:user:123:rpm";
        var limit = 100;
        var window = TimeSpan.FromMinutes(1);

        var luaResult = new RedisValue[] { 1, 60 };
        this._mockDatabase
            .Setup(db => db.ScriptEvaluateAsync(
                It.Is<string>(script => script.Contains("redis.call('GET', key, StringComparison.Ordinal)") &&
                                        script.Contains("redis.call('INCRBY', key, increment, StringComparison.Ordinal)")),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create(luaResult));

        // Act
        await this._service.CheckRateLimitAsync(key, limit, window).ConfigureAwait(false);

        // Assert
        this._mockDatabase.Verify(db => db.ScriptEvaluateAsync(
            It.IsAny<string>(),
            It.IsAny<RedisKey[]>(),
            It.IsAny<RedisValue[]>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    #endregion

    #region CheckHierarchicalRateLimitAsync Tests

    [Fact]
    public async Task CheckHierarchicalRateLimitAsync_WithinAllLimits_ShouldReturnAllowed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        var config = new RateLimitConfig
        {
            UserRpm = 100,
            UserTpm = 10000,
            GroupRpm = 500,
            GroupTpm = 50000,
            OrganizationRpm = 1000,
            OrganizationTpm = 100000,
            Window = TimeSpan.FromMinutes(1),
        };

        // All checks pass
        var luaResult = new RedisValue[] { 50, 60 };
        this._mockDatabase
            .Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create(luaResult));

        // Act
        var result = await this._service.CheckHierarchicalRateLimitAsync(
            userId, groupId, organizationId, config);

        // Assert
        result.Should().NotBeNull();
        result.IsAllowed.Should().BeTrue();
        result.LimitedBy.Should().NotBeNullOrEmpty();
        result.LimitType.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CheckHierarchicalRateLimitAsync_UserLimitExceeded_ShouldReturnDenied()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        var config = new RateLimitConfig
        {
            UserRpm = 100,
            GroupRpm = 500,
            OrganizationRpm = 1000,
            Window = TimeSpan.FromMinutes(1),
        };

        var callCount = 0;
        this._mockDatabase
            .Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                // First call (User RPM) exceeds limit
                if (callCount == 1)
                {
                    return RedisResult.Create(new RedisValue[] { 101, 45 });
                }
                // Other calls are within limits
                return RedisResult.Create(new RedisValue[] { 50, 60 });
            });

        // Act
        var result = await this._service.CheckHierarchicalRateLimitAsync(
            userId, groupId, organizationId, config);

        // Assert
        result.Should().NotBeNull();
        result.IsAllowed.Should().BeFalse();
        result.LimitedBy.Should().Be("User");
        result.LimitType.Should().Be("RPM");
    }

    [Fact]
    public async Task CheckHierarchicalRateLimitAsync_GroupLimitExceeded_ShouldReturnDenied()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        var config = new RateLimitConfig
        {
            UserRpm = 100,
            GroupRpm = 500,
            OrganizationRpm = 1000,
            Window = TimeSpan.FromMinutes(1),
        };

        var callCount = 0;
        this._mockDatabase
            .Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                // Group RPM check (assume it's the 2nd call) exceeds limit
                if (callCount == 2)
                {
                    return RedisResult.Create(new RedisValue[] { 501, 45 });
                }
                // Other calls are within limits
                return RedisResult.Create(new RedisValue[] { 50, 60 });
            });

        // Act
        var result = await this._service.CheckHierarchicalRateLimitAsync(
            userId, groupId, organizationId, config);

        // Assert
        result.Should().NotBeNull();
        result.IsAllowed.Should().BeFalse();
        result.LimitedBy.Should().Be("Group");
    }

    [Fact]
    public async Task CheckHierarchicalRateLimitAsync_OrganizationLimitExceeded_ShouldReturnDenied()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        var config = new RateLimitConfig
        {
            UserRpm = 100,
            OrganizationRpm = 1000,
            Window = TimeSpan.FromMinutes(1),
        };

        var callCount = 0;
        this._mockDatabase
            .Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                // Organization RPM check exceeds limit
                if (callCount == 2)
                {
                    return RedisResult.Create(new RedisValue[] { 1001, 30 });
                }
                // Other calls are within limits
                return RedisResult.Create(new RedisValue[] { 50, 60 });
            });

        // Act
        var result = await this._service.CheckHierarchicalRateLimitAsync(
            userId, groupId, organizationId, config);

        // Assert
        result.Should().NotBeNull();
        result.IsAllowed.Should().BeFalse();
        result.LimitedBy.Should().Be("Organization");
    }

    [Fact]
    public async Task CheckHierarchicalRateLimitAsync_WithNoLimits_ShouldReturnAllowed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        var config = RateLimitConfig.NoLimits();

        // Act
        var result = await this._service.CheckHierarchicalRateLimitAsync(
            userId, groupId, organizationId, config);

        // Assert
        result.Should().NotBeNull();
        result.IsAllowed.Should().BeTrue();
        this._mockDatabase.Verify(db => db.ScriptEvaluateAsync(
            It.IsAny<string>(),
            It.IsAny<RedisKey[]>(),
            It.IsAny<RedisValue[]>(),
            It.IsAny<CommandFlags>()), Times.Never);
    }

    [Fact]
    public async Task CheckHierarchicalRateLimitAsync_WithoutGroup_ShouldSkipGroupChecks()
    {
        // Arrange
        var userId = Guid.NewGuid();
        Guid? groupId = null; // No group
        var organizationId = Guid.NewGuid();

        var config = new RateLimitConfig
        {
            UserRpm = 100,
            GroupRpm = 500, // This should be ignored
            OrganizationRpm = 1000,
            Window = TimeSpan.FromMinutes(1),
        };

        var luaResult = new RedisValue[] { 50, 60 };
        this._mockDatabase
            .Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create(luaResult));

        // Act
        var result = await this._service.CheckHierarchicalRateLimitAsync(
            userId, groupId, organizationId, config);

        // Assert
        result.Should().NotBeNull();
        result.IsAllowed.Should().BeTrue();

        // Should only check User and Organization (not Group)
        this._mockDatabase.Verify(db => db.ScriptEvaluateAsync(
            It.IsAny<string>(),
            It.Is<RedisKey[]>(keys => keys.Any(k => k.ToString().Contains(":group:", StringComparison.Ordinal))),
            It.IsAny<RedisValue[]>(),
            It.IsAny<CommandFlags>()), Times.Never);
    }

    [Fact]
    public async Task CheckHierarchicalRateLimitAsync_ShouldUseCorrectKeyStructure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        var config = RateLimitConfig.UserLevel(rpm: 100);

        var capturedKeys = new List<RedisKey[]>();
        this._mockDatabase
            .Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .Callback<string, RedisKey[], RedisValue[], CommandFlags>((_, keys, _, _) =>
            {
                capturedKeys.Add(keys);
            })
            .ReturnsAsync(RedisResult.Create(new RedisValue[] { 50, 60 }));

        // Act
        await this._service.CheckHierarchicalRateLimitAsync(
            userId, groupId, organizationId, config);

        // Assert
        capturedKeys.Should().NotBeEmpty();
        capturedKeys[0][0].ToString().Should().Contain($"ratelimit:user:{userId}:rpm");
    }

    [Fact]
    public async Task CheckHierarchicalRateLimitAsync_WithNullConfig_ShouldThrowArgumentNullException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        // Act & Assert
        var act = async () => await this._service.CheckHierarchicalRateLimitAsync(
            userId, null, organizationId, null!);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("config").ConfigureAwait(false);
    }

    [Fact]
    public async Task CheckHierarchicalRateLimitAsync_ShouldCheckAllLevelsInParallel()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        var config = new RateLimitConfig
        {
            UserRpm = 100,
            UserTpm = 10000,
            GroupRpm = 500,
            GroupTpm = 50000,
            OrganizationRpm = 1000,
            OrganizationTpm = 100000,
            Window = TimeSpan.FromMinutes(1),
        };

        var executionTimes = new List<DateTime>();
        this._mockDatabase
            .Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .Callback(() => executionTimes.Add(DateTime.UtcNow))
            .ReturnsAsync(RedisResult.Create(new RedisValue[] { 50, 60 }));

        // Act
        await this._service.CheckHierarchicalRateLimitAsync(
            userId, groupId, organizationId, config);

        // Assert - All checks should execute within a short time window (parallel execution)
        executionTimes.Should().HaveCount(6); // 3 levels * 2 types (RPM + TPM)
        var timeSpan = executionTimes.Max() - executionTimes.Min();
        timeSpan.Should().BeLessThan(TimeSpan.FromMilliseconds(500)); // All within 500ms suggests parallel execution
    }

    #endregion

    #region IncrementTokenUsageAsync Tests

    [Fact]
    public async Task IncrementTokenUsageAsync_ShouldIncrementSuccessfully()
    {
        // Arrange
        var key = "ratelimit:user:123:tpm";
        var tokenCount = 500;
        var window = TimeSpan.FromMinutes(1);

        var luaResult = new RedisValue[] { 5500, 60 };
        this._mockDatabase
            .Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create(luaResult));

        // Act
        var result = await this._service.IncrementTokenUsageAsync(key, tokenCount, window).ConfigureAwait(false);

        // Assert
        result.Should().Be(5500);
        this._mockDatabase.Verify(db => db.ScriptEvaluateAsync(
            It.IsAny<string>(),
            It.Is<RedisKey[]>(keys => keys[0] == key),
            It.Is<RedisValue[]>(args => (int)args[0] == tokenCount && (int)args[1] == 60),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task IncrementTokenUsageAsync_WithNullOrEmptyKey_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act1 = async () => await this._service.IncrementTokenUsageAsync(null!, 100, TimeSpan.FromMinutes(1)).ConfigureAwait(false);
        await act1.Should().ThrowAsync<ArgumentException>().WithParameterName("key").ConfigureAwait(false);

        var act2 = async () => await this._service.IncrementTokenUsageAsync("", 100, TimeSpan.FromMinutes(1)).ConfigureAwait(false);
        await act2.Should().ThrowAsync<ArgumentException>().WithParameterName("key").ConfigureAwait(false);
    }

    [Fact]
    public async Task IncrementTokenUsageAsync_WithNegativeTokenCount_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = async () => await this._service.IncrementTokenUsageAsync("key", -10, TimeSpan.FromMinutes(1)).ConfigureAwait(false);
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("tokenCount").ConfigureAwait(false);
    }

    [Fact]
    public async Task IncrementTokenUsageAsync_WhenRedisThrows_ShouldReturn0()
    {
        // Arrange
        var key = "ratelimit:user:123:tpm";
        var tokenCount = 500;
        var window = TimeSpan.FromMinutes(1);

        this._mockDatabase
            .Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisException("Connection failed"));

        // Act
        var result = await this._service.IncrementTokenUsageAsync(key, tokenCount, window).ConfigureAwait(false);

        // Assert
        result.Should().Be(0); // Graceful degradation
    }

    [Fact]
    public async Task IncrementTokenUsageAsync_ShouldExecuteLuaScript()
    {
        // Arrange
        var key = "ratelimit:user:123:tpm";
        var tokenCount = 500;
        var window = TimeSpan.FromMinutes(1);

        var luaResult = new RedisValue[] { 500, 60 };
        this._mockDatabase
            .Setup(db => db.ScriptEvaluateAsync(
                It.Is<string>(script => script.Contains("redis.call('INCRBY', key, tokens, StringComparison.Ordinal)")),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create(luaResult));

        // Act
        await this._service.IncrementTokenUsageAsync(key, tokenCount, window).ConfigureAwait(false);

        // Assert
        this._mockDatabase.Verify(db => db.ScriptEvaluateAsync(
            It.IsAny<string>(),
            It.IsAny<RedisKey[]>(),
            It.IsAny<RedisValue[]>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    #endregion

    #region IncrementHierarchicalTokenUsageAsync Tests

    [Fact]
    public async Task IncrementHierarchicalTokenUsageAsync_ShouldIncrementAllLevels()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var tokenCount = 500;
        var window = TimeSpan.FromMinutes(1);

        var luaResult = new RedisValue[] { 5500, 60 };
        this._mockDatabase
            .Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create(luaResult));

        // Act
        await this._service.IncrementHierarchicalTokenUsageAsync(
            userId, groupId, organizationId, tokenCount, window);

        // Assert
        this._mockDatabase.Verify(db => db.ScriptEvaluateAsync(
            It.IsAny<string>(),
            It.IsAny<RedisKey[]>(),
            It.IsAny<RedisValue[]>(),
            It.IsAny<CommandFlags>()), Times.Exactly(3)); // User, Group, Organization
    }

    [Fact]
    public async Task IncrementHierarchicalTokenUsageAsync_WithoutGroup_ShouldSkipGroupIncrement()
    {
        // Arrange
        var userId = Guid.NewGuid();
        Guid? groupId = null;
        var organizationId = Guid.NewGuid();
        var tokenCount = 500;
        var window = TimeSpan.FromMinutes(1);

        var luaResult = new RedisValue[] { 5500, 60 };
        this._mockDatabase
            .Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create(luaResult));

        // Act
        await this._service.IncrementHierarchicalTokenUsageAsync(
            userId, groupId, organizationId, tokenCount, window);

        // Assert
        this._mockDatabase.Verify(db => db.ScriptEvaluateAsync(
            It.IsAny<string>(),
            It.IsAny<RedisKey[]>(),
            It.IsAny<RedisValue[]>(),
            It.IsAny<CommandFlags>()), Times.Exactly(2)); // User and Organization only
    }

    #endregion

    #region GetCurrentUsageAsync Tests

    [Fact]
    public async Task GetCurrentUsageAsync_ShouldReturnCurrentValue()
    {
        // Arrange
        var key = "ratelimit:user:123:rpm";
        this._mockDatabase
            .Setup(db => db.StringGetAsync(key, It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)75);

        // Act
        var result = await this._service.GetCurrentUsageAsync(key).ConfigureAwait(false);

        // Assert
        result.Should().Be(75);
    }

    [Fact]
    public async Task GetCurrentUsageAsync_WithNonExistentKey_ShouldReturn0()
    {
        // Arrange
        var key = "ratelimit:user:123:rpm";
        this._mockDatabase
            .Setup(db => db.StringGetAsync(key, It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        // Act
        var result = await this._service.GetCurrentUsageAsync(key).ConfigureAwait(false);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task GetCurrentUsageAsync_WhenRedisThrows_ShouldReturn0()
    {
        // Arrange
        var key = "ratelimit:user:123:rpm";
        this._mockDatabase
            .Setup(db => db.StringGetAsync(key, It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisException("Connection failed"));

        // Act
        var result = await this._service.GetCurrentUsageAsync(key).ConfigureAwait(false);

        // Assert
        result.Should().Be(0);
    }

    #endregion

    #region ResetRateLimitAsync Tests

    [Fact]
    public async Task ResetRateLimitAsync_ShouldDeleteKey()
    {
        // Arrange
        var key = "ratelimit:user:123:rpm";
        this._mockDatabase
            .Setup(db => db.KeyDeleteAsync(key, It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        var result = await this._service.ResetRateLimitAsync(key).ConfigureAwait(false);

        // Assert
        result.Should().BeTrue();
        this._mockDatabase.Verify(db => db.KeyDeleteAsync(key, It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task ResetRateLimitAsync_WithNonExistentKey_ShouldReturnFalse()
    {
        // Arrange
        var key = "ratelimit:user:123:rpm";
        this._mockDatabase
            .Setup(db => db.KeyDeleteAsync(key, It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);

        // Act
        var result = await this._service.ResetRateLimitAsync(key).ConfigureAwait(false);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ResetRateLimitAsync_WhenRedisThrows_ShouldReturnFalse()
    {
        // Arrange
        var key = "ratelimit:user:123:rpm";
        this._mockDatabase
            .Setup(db => db.KeyDeleteAsync(key, It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisException("Connection failed"));

        // Act
        var result = await this._service.ResetRateLimitAsync(key).ConfigureAwait(false);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
