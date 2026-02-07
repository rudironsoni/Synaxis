using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;
using Synaxis.InferenceGateway.Application.Configuration;
using Synaxis.InferenceGateway.Infrastructure.Routing;
using System.Collections.Generic;

namespace Synaxis.InferenceGateway.Infrastructure.Tests.Routing;

public class RedisQuotaTrackerTests
{
    private readonly Mock<IConnectionMultiplexer> _mockRedis;
    private readonly Mock<IDatabase> _mockDatabase;
    private readonly Mock<ILogger<RedisQuotaTracker>> _mockLogger;
    private readonly Mock<IOptions<SynaxisConfiguration>> _mockConfig;
    private readonly SynaxisConfiguration _config;

    public RedisQuotaTrackerTests()
    {
        this._mockRedis = new Mock<IConnectionMultiplexer>();
        this._mockDatabase = new Mock<IDatabase>();
        this._mockLogger = new Mock<ILogger<RedisQuotaTracker>>();
        this._mockConfig = new Mock<IOptions<SynaxisConfiguration>>();

        this._config = new SynaxisConfiguration
        {
            // TODO: Re-enable provider configuration tests once ProviderConfiguration type is available
            // Providers = new Dictionary<string, ProviderConfiguration>
            // {
            //     ["Groq"] = new ProviderConfiguration
            //     {
            //         RateLimitRPM = 100,
            //         RateLimitTPM = 10000
            //     }
            // }
        };

        this._mockConfig.Setup(c => c.Value).Returns(this._config);
        this._mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(this._mockDatabase.Object);
    }

    [Fact]
    public async Task CheckQuotaAsync_WithNonExistentProvider_ReturnsTrue()
    {
        // Arrange
        var tracker = new RedisQuotaTracker(this._mockRedis.Object, this._mockLogger.Object, this._mockConfig.Object);

        // Act
        var result = await tracker.CheckQuotaAsync("NonExistent");

        // Assert
        result.Should().BeTrue();
    }

    // TODO: Re-enable once ProviderConfiguration type is available
    // [Fact]
    // public async Task CheckQuotaAsync_WithNoRateLimits_ReturnsTrue()
    // {
    //     // Arrange
    //     _config.Providers["NoLimit"] = new ProviderConfiguration
    //     {
    //         RateLimitRPM = null,
    //         RateLimitTPM = null
    //     };
    //
    //     var tracker = new RedisQuotaTracker(_mockRedis.Object, _mockLogger.Object, _mockConfig.Object);
    //
    //     // Act
    //     var result = await tracker.CheckQuotaAsync("NoLimit");
    //
    //     // Assert
    //     result.Should().BeTrue();
    // }

    [Fact]
    public async Task CheckQuotaAsync_WithinLimits_ReturnsTrue()
    {
        // Arrange
        this._mockDatabase
            .Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create(1L));

        var tracker = new RedisQuotaTracker(this._mockRedis.Object, this._mockLogger.Object, this._mockConfig.Object);

        // Act
        var result = await tracker.CheckQuotaAsync("Groq");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CheckQuotaAsync_ExceedsLimit_ReturnsFalse()
    {
        // Arrange
        this._mockDatabase
            .Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create(0L));

        this._mockDatabase
            .Setup(db => db.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)101L);

        var tracker = new RedisQuotaTracker(this._mockRedis.Object, this._mockLogger.Object, this._mockConfig.Object);

        // Act
        var result = await tracker.CheckQuotaAsync("Groq");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CheckQuotaAsync_OnRedisError_ReturnsTrueAsFailsafe()
    {
        // Arrange
        this._mockDatabase
            .Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisException("Connection failed"));

        var tracker = new RedisQuotaTracker(this._mockRedis.Object, this._mockLogger.Object, this._mockConfig.Object);

        // Act
        var result = await tracker.CheckQuotaAsync("Groq");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task RecordUsageAsync_IncrementsTokenCounters()
    {
        // Arrange
        var tracker = new RedisQuotaTracker(this._mockRedis.Object, this._mockLogger.Object, this._mockConfig.Object);

        // Act
        await tracker.RecordUsageAsync("Groq", 100, 50);

        // Assert
        this._mockDatabase.Verify(
            db => db.StringIncrementAsync(
                It.Is<RedisKey>(k => k.ToString().Contains("tpm")),
                150,
                It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Fact]
    public async Task RecordUsageAsync_SetsExpiration()
    {
        // Arrange
        var tracker = new RedisQuotaTracker(this._mockRedis.Object, this._mockLogger.Object, this._mockConfig.Object);

        // Act
        await tracker.RecordUsageAsync("Groq", 100, 50);

        // Assert
        this._mockDatabase.Verify(
            db => db.KeyExpireAsync(
                It.Is<RedisKey>(k => k.ToString().Contains("tpm")),
                TimeSpan.FromMinutes(1),
                It.IsAny<ExpireWhen>(),
                It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckHierarchicalQuotaAsync_WithinAllLimits_ReturnsTrue()
    {
        // Arrange
        this._mockDatabase
            .Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create(1L));

        var tracker = new RedisQuotaTracker(this._mockRedis.Object, this._mockLogger.Object, this._mockConfig.Object);

        // Act
        var result = await tracker.CheckHierarchicalQuotaAsync(
            "org1", "group1", "user1",
            orgMaxRpm: 1000, orgMaxTpm: 100000,
            groupMaxRpm: 500, groupMaxTpm: 50000,
            userMaxRpm: 100, userMaxTpm: 10000);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CheckHierarchicalQuotaAsync_ExceedsAnyLimit_ReturnsFalse()
    {
        // Arrange
        this._mockDatabase
            .Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create(0L));

        var tracker = new RedisQuotaTracker(this._mockRedis.Object, this._mockLogger.Object, this._mockConfig.Object);

        // Act
        var result = await tracker.CheckHierarchicalQuotaAsync(
            "org1", "group1", "user1",
            orgMaxRpm: 1000, orgMaxTpm: 100000,
            groupMaxRpm: 500, groupMaxTpm: 50000,
            userMaxRpm: 100, userMaxTpm: 10000);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CheckHierarchicalQuotaAsync_UsesCorrectKeyStructure()
    {
        // Arrange
        RedisKey[]? capturedKeys = null;

        this._mockDatabase
            .Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .Callback<string, RedisKey[], RedisValue[], CommandFlags>((script, keys, values, flags) =>
            {
                capturedKeys = keys;
            })
            .ReturnsAsync(RedisResult.Create(1L));

        var tracker = new RedisQuotaTracker(this._mockRedis.Object, this._mockLogger.Object, this._mockConfig.Object);

        // Act
        await tracker.CheckHierarchicalQuotaAsync(
            "org1", "group1", "user1",
            userMaxRpm: 100);

        // Assert
        capturedKeys.Should().NotBeNull();
        capturedKeys.Should().HaveCount(6);
        capturedKeys![0].ToString().Should().Contain("org1:group1:user1:rpm");
        capturedKeys[2].ToString().Should().Contain("org1:group1:rpm");
        capturedKeys[4].ToString().Should().Contain("org1:rpm");
    }

    [Fact]
    public async Task CheckHierarchicalQuotaAsync_OnRedisError_ReturnsTrueAsFailsafe()
    {
        // Arrange
        this._mockDatabase
            .Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisException("Connection failed"));

        var tracker = new RedisQuotaTracker(this._mockRedis.Object, this._mockLogger.Object, this._mockConfig.Object);

        // Act
        var result = await tracker.CheckHierarchicalQuotaAsync(
            "org1", "group1", "user1",
            userMaxRpm: 100);

        // Assert
        result.Should().BeTrue();
    }
}
