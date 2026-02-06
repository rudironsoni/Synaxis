using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Synaxis.InferenceGateway.Application.Tests.Optimization.Configuration;

public class TokenOptimizationConfigResolverTests : IDisposable
{
    private readonly Mock<IDbContextFactory<TestTokenOptimizationDbContext>> _dbContextFactoryMock;
    private readonly TestTokenOptimizationDbContext _dbContext;
    private readonly TokenOptimizationConfigurationResolver _resolver;

    public TokenOptimizationConfigResolverTests()
    {
        var options = new DbContextOptionsBuilder<TestTokenOptimizationDbContext>()
            .UseInMemoryDatabase($"TokenOptimizationTestDb_{Guid.NewGuid()}")
            .Options;

        _dbContext = new TestTokenOptimizationDbContext(options);
        _dbContextFactoryMock = new Mock<IDbContextFactory<TestTokenOptimizationDbContext>>();
        _dbContextFactoryMock.Setup(x => x.CreateDbContext()).Returns(_dbContext);
        _dbContextFactoryMock.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_dbContext);

        _resolver = new TokenOptimizationConfigurationResolver(_dbContextFactoryMock.Object);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

    [Fact]
    public async Task ResolveAsync_NoOverrides_ReturnsSystemDefaults()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Act
        var config = await _resolver.ResolveAsync(tenantId, userId);

        // Assert
        Assert.NotNull(config);
        Assert.Equal(0.85, config.SimilarityThreshold); // Expected system default
        Assert.Equal(3600, config.CacheTtlSeconds); // Expected system default
        Assert.Equal("gzip", config.CompressionStrategy); // Expected system default
        Assert.True(config.EnableCaching); // Expected system default
    }

    [Fact]
    public async Task ResolveAsync_TenantOverride_AppliesTenantSettings()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        // Seed tenant configuration
        var tenantConfig = new TenantTokenOptimizationConfig
        {
            TenantId = tenantId,
            SimilarityThreshold = 0.90,
            CacheTtlSeconds = 7200,
            EnableCaching = true
        };
        _dbContext.TenantTokenOptimizationConfigs.Add(tenantConfig);
        await _dbContext.SaveChangesAsync();

        // Act
        var config = await _resolver.ResolveAsync(tenantId, userId);

        // Assert
        Assert.NotNull(config);
        Assert.Equal(0.90, config.SimilarityThreshold);
        Assert.Equal(7200, config.CacheTtlSeconds);
        Assert.True(config.EnableCaching);
    }

    [Fact]
    public async Task ResolveAsync_UserOverride_AppliesUserSettings()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        // Seed user configuration
        var userConfig = new UserTokenOptimizationConfig
        {
            UserId = userId,
            SimilarityThreshold = 0.95,
            EnableCaching = false
        };
        _dbContext.UserTokenOptimizationConfigs.Add(userConfig);
        await _dbContext.SaveChangesAsync();

        // Act
        var config = await _resolver.ResolveAsync(tenantId, userId);

        // Assert
        Assert.NotNull(config);
        Assert.Equal(0.95, config.SimilarityThreshold);
        Assert.False(config.EnableCaching);
    }

    [Fact]
    public async Task ResolveAsync_UserAndTenant_UserWins()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        // Seed tenant configuration
        var tenantConfig = new TenantTokenOptimizationConfig
        {
            TenantId = tenantId,
            SimilarityThreshold = 0.80,
            CacheTtlSeconds = 5400,
            EnableCaching = true
        };
        _dbContext.TenantTokenOptimizationConfigs.Add(tenantConfig);

        // Seed user configuration (should override tenant)
        var userConfig = new UserTokenOptimizationConfig
        {
            UserId = userId,
            SimilarityThreshold = 0.92,
            EnableCaching = false
        };
        _dbContext.UserTokenOptimizationConfigs.Add(userConfig);
        await _dbContext.SaveChangesAsync();

        // Act
        var config = await _resolver.ResolveAsync(tenantId, userId);

        // Assert
        Assert.NotNull(config);
        Assert.Equal(0.92, config.SimilarityThreshold); // User override wins
        Assert.False(config.EnableCaching); // User override wins
        Assert.Equal(5400, config.CacheTtlSeconds); // Tenant value (not overridden by user)
    }

    [Fact]
    public async Task ResolveAsync_NullableFields_InheritFromParent()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        // Seed tenant configuration with some values
        var tenantConfig = new TenantTokenOptimizationConfig
        {
            TenantId = tenantId,
            SimilarityThreshold = 0.88,
            CacheTtlSeconds = 4800,
            CompressionStrategy = "brotli",
            EnableCaching = true
        };
        _dbContext.TenantTokenOptimizationConfigs.Add(tenantConfig);

        // Seed user configuration with only partial overrides (nulls should inherit)
        var userConfig = new UserTokenOptimizationConfig
        {
            UserId = userId,
            SimilarityThreshold = null, // Should inherit from tenant
            EnableCaching = false // Override
            // CacheTtlSeconds not set, should inherit from tenant
        };
        _dbContext.UserTokenOptimizationConfigs.Add(userConfig);
        await _dbContext.SaveChangesAsync();

        // Act
        var config = await _resolver.ResolveAsync(tenantId, userId);

        // Assert
        Assert.NotNull(config);
        Assert.Equal(0.88, config.SimilarityThreshold); // Inherited from tenant
        Assert.False(config.EnableCaching); // User override
        Assert.Equal(4800, config.CacheTtlSeconds); // Inherited from tenant
        Assert.Equal("brotli", config.CompressionStrategy); // Inherited from tenant
    }

    [Fact]
    public async Task GetTenantConfigAsync_ReturnsTenantEffectiveConfig()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        
        var tenantConfig = new TenantTokenOptimizationConfig
        {
            TenantId = tenantId,
            SimilarityThreshold = 0.87,
            CacheTtlSeconds = 7200
        };
        _dbContext.TenantTokenOptimizationConfigs.Add(tenantConfig);
        await _dbContext.SaveChangesAsync();

        // Act
        var config = await _resolver.GetTenantConfigAsync(tenantId);

        // Assert
        Assert.NotNull(config);
        Assert.Equal(0.87, config.SimilarityThreshold);
        Assert.Equal(7200, config.CacheTtlSeconds);
    }

    [Fact]
    public async Task GetUserConfigAsync_ReturnsUserEffectiveConfig()
    {
        // Arrange
        var userId = Guid.NewGuid();
        
        var userConfig = new UserTokenOptimizationConfig
        {
            UserId = userId,
            SimilarityThreshold = 0.93,
            EnableCaching = true
        };
        _dbContext.UserTokenOptimizationConfigs.Add(userConfig);
        await _dbContext.SaveChangesAsync();

        // Act
        var config = await _resolver.GetUserConfigAsync(userId);

        // Assert
        Assert.NotNull(config);
        Assert.Equal(0.93, config.SimilarityThreshold);
        Assert.True(config.EnableCaching);
    }

    [Fact]
    public void GetSystemConfig_ReturnsRawConfiguration()
    {
        // Act
        var config = _resolver.GetSystemConfig();

        // Assert
        Assert.NotNull(config);
        Assert.Equal(0.85, config.SimilarityThreshold); // System default
        Assert.Equal(3600, config.CacheTtlSeconds); // System default
        Assert.Equal("gzip", config.CompressionStrategy); // System default
        Assert.True(config.EnableCaching); // System default
        Assert.True(config.EnableCompression); // System default
    }

    [Fact]
    public async Task SystemOnlySettings_NeverOverridden()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        // Seed tenant configuration (should not be able to override system-only settings)
        var tenantConfig = new TenantTokenOptimizationConfig
        {
            TenantId = tenantId,
            SimilarityThreshold = 0.90
            // MaxConcurrentRequests is system-only and cannot be set here
        };
        _dbContext.TenantTokenOptimizationConfigs.Add(tenantConfig);
        await _dbContext.SaveChangesAsync();

        // Act
        var config = await _resolver.ResolveAsync(tenantId, userId);
        var systemConfig = _resolver.GetSystemConfig();

        // Assert
        Assert.NotNull(config);
        Assert.Equal(systemConfig.MaxConcurrentRequests, config.MaxConcurrentRequests); // System-only setting
        Assert.Equal(0.90, config.SimilarityThreshold); // Tenant override applied
    }

    [Fact]
    public async Task ConcurrentResolution_ThreadSafe()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var userId3 = Guid.NewGuid();
        
        // Seed configurations
        var tenantConfig = new TenantTokenOptimizationConfig
        {
            TenantId = tenantId,
            SimilarityThreshold = 0.88
        };
        _dbContext.TenantTokenOptimizationConfigs.Add(tenantConfig);

        var userConfig1 = new UserTokenOptimizationConfig
        {
            UserId = userId1,
            SimilarityThreshold = 0.90
        };
        var userConfig2 = new UserTokenOptimizationConfig
        {
            UserId = userId2,
            SimilarityThreshold = 0.95
        };
        var userConfig3 = new UserTokenOptimizationConfig
        {
            UserId = userId3,
            SimilarityThreshold = 0.80
        };
        _dbContext.UserTokenOptimizationConfigs.Add(userConfig1);
        _dbContext.UserTokenOptimizationConfigs.Add(userConfig2);
        _dbContext.UserTokenOptimizationConfigs.Add(userConfig3);
        await _dbContext.SaveChangesAsync();

        // Act - Resolve concurrently
        var tasks = new[]
        {
            Task.Run(async () => await _resolver.ResolveAsync(tenantId, userId1)),
            Task.Run(async () => await _resolver.ResolveAsync(tenantId, userId2)),
            Task.Run(async () => await _resolver.ResolveAsync(tenantId, userId3)),
            Task.Run(async () => await _resolver.ResolveAsync(tenantId, userId1)),
            Task.Run(async () => await _resolver.ResolveAsync(tenantId, userId2))
        };

        var configs = await Task.WhenAll(tasks);

        // Assert - All resolutions should succeed and return correct values
        Assert.All(configs, c => Assert.NotNull(c));
        Assert.Equal(0.90, configs[0].SimilarityThreshold);
        Assert.Equal(0.95, configs[1].SimilarityThreshold);
        Assert.Equal(0.80, configs[2].SimilarityThreshold);
        Assert.Equal(0.90, configs[3].SimilarityThreshold); // Same as first
        Assert.Equal(0.95, configs[4].SimilarityThreshold); // Same as second
    }
}
