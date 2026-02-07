
namespace Synaxis.InferenceGateway.Infrastructure.Tests.Optimization.Configuration;

using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

/// <summary>
/// Integration tests for TokenOptimizationConfigurationResolver with EF Core
/// Tests database persistence and retrieval of token optimization configurations
/// </summary>
public class TokenOptimizationConfigurationResolverTests : IAsyncLifetime
{
    private DbContextOptions<OptimizationDbContext> _dbOptions = null!;
    private OptimizationDbContext _dbContext = null!;

    public Task InitializeAsync()
    {
        // Setup in-memory database for testing
        this._dbOptions = new DbContextOptionsBuilder<OptimizationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TokenOptimizationTests_{Guid.NewGuid()}")
            .Options;

        this._dbContext = new OptimizationDbContext(this._dbOptions);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (this._dbContext != null)
        {
            await this._dbContext.Database.EnsureDeletedAsync().ConfigureAwait(false);
            await this._dbContext.DisposeAsync().ConfigureAwait(false);
        }
    }

    [Fact]
    public async Task GetConfigurationAsync_ExistingProvider_ReturnsConfiguration()
    {
        // Arrange
        var providerId = "openai-gpt4";
        var config = new TokenOptimizationConfiguration
        {
            ProviderId = providerId,
            EnablePromptCaching = true,
            EnableConversationCompression = true,
            CompressionStrategy = "sliding-window",
            MaxContextTokens = 8000,
            TargetCompressionRatio = 0.7,
            EnableSemanticCache = false,
            SemanticCacheSimilarityThreshold = 0.85,
            CacheTtlMinutes = 60,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        await this._dbContext.TokenOptimizationConfigurations.AddAsync(config).ConfigureAwait(false);
        await this._dbContext.SaveChangesAsync().ConfigureAwait(false);

        var resolver = new TokenOptimizationConfigurationResolver(this._dbContext);

        // Act
        var result = await resolver.GetConfigurationAsync(providerId, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(providerId, result.ProviderId);
        Assert.True(result.EnablePromptCaching);
        Assert.True(result.EnableConversationCompression);
        Assert.Equal("sliding-window", result.CompressionStrategy);
        Assert.Equal(8000, result.MaxContextTokens);
        Assert.Equal(0.7, result.TargetCompressionRatio);
    }

    [Fact]
    public async Task GetConfigurationAsync_MissingProvider_ReturnsNull()
    {
        // Arrange
        var resolver = new TokenOptimizationConfigurationResolver(this._dbContext);

        // Act
        var result = await resolver.GetConfigurationAsync("nonexistent-provider", CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateConfigurationAsync_NewProvider_CreatesSuccessfully()
    {
        // Arrange
        var resolver = new TokenOptimizationConfigurationResolver(this._dbContext);
        var config = new TokenOptimizationConfiguration
        {
            ProviderId = "anthropic-claude",
            EnablePromptCaching = true,
            EnableConversationCompression = false,
            CompressionStrategy = "none",
            MaxContextTokens = 100000,
            TargetCompressionRatio = 0.5,
            EnableSemanticCache = true,
            SemanticCacheSimilarityThreshold = 0.9,
            CacheTtlMinutes = 120,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        // Act
        await resolver.CreateConfigurationAsync(config, CancellationToken.None).ConfigureAwait(false);

        // Assert
        var saved = await this._dbContext.TokenOptimizationConfigurations
            .FirstOrDefaultAsync(c => c.ProviderId == "anthropic-claude");

        Assert.NotNull(saved);
        Assert.Equal("anthropic-claude", saved.ProviderId);
        Assert.True(saved.EnablePromptCaching);
        Assert.False(saved.EnableConversationCompression);
        Assert.True(saved.EnableSemanticCache);
        Assert.Equal(100000, saved.MaxContextTokens);
    }

    [Fact]
    public async Task UpdateConfigurationAsync_ExistingProvider_UpdatesSuccessfully()
    {
        // Arrange
        var providerId = "openai-gpt35";
        var original = new TokenOptimizationConfiguration
        {
            ProviderId = providerId,
            EnablePromptCaching = false,
            MaxContextTokens = 4000,
            CompressionStrategy = "none",
            UpdatedAt = DateTimeOffset.UtcNow.AddHours(-1),
        };

        await this._dbContext.TokenOptimizationConfigurations.AddAsync(original).ConfigureAwait(false);
        await this._dbContext.SaveChangesAsync().ConfigureAwait(false);
        this._dbContext.Entry(original).State = EntityState.Detached;

        var resolver = new TokenOptimizationConfigurationResolver(this._dbContext);

        // Act
        var updated = new TokenOptimizationConfiguration
        {
            ProviderId = providerId,
            EnablePromptCaching = true,
            MaxContextTokens = 16000,
            CompressionStrategy = "sliding-window",
            EnableConversationCompression = true,
            TargetCompressionRatio = 0.6,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        await resolver.UpdateConfigurationAsync(updated, CancellationToken.None).ConfigureAwait(false);

        // Assert
        var saved = await this._dbContext.TokenOptimizationConfigurations
            .FirstOrDefaultAsync(c => c.ProviderId == providerId);

        Assert.NotNull(saved);
        Assert.True(saved.EnablePromptCaching);
        Assert.Equal(16000, saved.MaxContextTokens);
        Assert.Equal("sliding-window", saved.CompressionStrategy);
        Assert.True(saved.EnableConversationCompression);
    }

    [Fact]
    public async Task DeleteConfigurationAsync_ExistingProvider_DeletesSuccessfully()
    {
        // Arrange
        var providerId = "test-provider";
        var config = new TokenOptimizationConfiguration
        {
            ProviderId = providerId,
            EnablePromptCaching = true,
            MaxContextTokens = 8000,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        await this._dbContext.TokenOptimizationConfigurations.AddAsync(config).ConfigureAwait(false);
        await this._dbContext.SaveChangesAsync().ConfigureAwait(false);

        var resolver = new TokenOptimizationConfigurationResolver(this._dbContext);

        // Act
        await resolver.DeleteConfigurationAsync(providerId, CancellationToken.None).ConfigureAwait(false);

        // Assert
        var deleted = await this._dbContext.TokenOptimizationConfigurations
            .FirstOrDefaultAsync(c => c.ProviderId == providerId);

        Assert.Null(deleted);
    }

    [Fact]
    public async Task GetAllConfigurationsAsync_MultipleProviders_ReturnsAll()
    {
        // Arrange
        var config1 = new TokenOptimizationConfiguration
        {
            ProviderId = "provider-1",
            EnablePromptCaching = true,
            MaxContextTokens = 8000,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        var config2 = new TokenOptimizationConfiguration
        {
            ProviderId = "provider-2",
            EnablePromptCaching = false,
            MaxContextTokens = 4000,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        var config3 = new TokenOptimizationConfiguration
        {
            ProviderId = "provider-3",
            EnablePromptCaching = true,
            MaxContextTokens = 16000,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        await this._dbContext.TokenOptimizationConfigurations.AddAsync(config1).ConfigureAwait(false);
        await this._dbContext.TokenOptimizationConfigurations.AddAsync(config2).ConfigureAwait(false);
        await this._dbContext.TokenOptimizationConfigurations.AddAsync(config3).ConfigureAwait(false);
        await this._dbContext.SaveChangesAsync().ConfigureAwait(false);

        var resolver = new TokenOptimizationConfigurationResolver(this._dbContext);

        // Act
        var result = await resolver.GetAllConfigurationsAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Contains(result, c => c.ProviderId == "provider-1", StringComparison.Ordinal);
        Assert.Contains(result, c => c.ProviderId == "provider-2", StringComparison.Ordinal);
        Assert.Contains(result, c => c.ProviderId == "provider-3", StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetConfigurationsByStrategyAsync_FiltersCorrectly()
    {
        // Arrange
        var config1 = new TokenOptimizationConfiguration
        {
            ProviderId = "provider-sw-1",
            CompressionStrategy = "sliding-window",
            MaxContextTokens = 8000,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        var config2 = new TokenOptimizationConfiguration
        {
            ProviderId = "provider-sw-2",
            CompressionStrategy = "sliding-window",
            MaxContextTokens = 4000,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        var config3 = new TokenOptimizationConfiguration
        {
            ProviderId = "provider-none",
            CompressionStrategy = "none",
            MaxContextTokens = 16000,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        await this._dbContext.TokenOptimizationConfigurations.AddAsync(config1).ConfigureAwait(false);
        await this._dbContext.TokenOptimizationConfigurations.AddAsync(config2).ConfigureAwait(false);
        await this._dbContext.TokenOptimizationConfigurations.AddAsync(config3).ConfigureAwait(false);
        await this._dbContext.SaveChangesAsync().ConfigureAwait(false);

        var resolver = new TokenOptimizationConfigurationResolver(this._dbContext);

        // Act
        var result = await resolver.GetConfigurationsByStrategyAsync("sliding-window", CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, c => Assert.Equal("sliding-window", c.CompressionStrategy));
    }

    [Fact]
    public async Task GetConfigurationsWithCachingEnabledAsync_FiltersCorrectly()
    {
        // Arrange
        var config1 = new TokenOptimizationConfiguration
        {
            ProviderId = "provider-cache-1",
            EnablePromptCaching = true,
            MaxContextTokens = 8000,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        var config2 = new TokenOptimizationConfiguration
        {
            ProviderId = "provider-cache-2",
            EnablePromptCaching = true,
            MaxContextTokens = 4000,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        var config3 = new TokenOptimizationConfiguration
        {
            ProviderId = "provider-no-cache",
            EnablePromptCaching = false,
            MaxContextTokens = 16000,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        await this._dbContext.TokenOptimizationConfigurations.AddAsync(config1).ConfigureAwait(false);
        await this._dbContext.TokenOptimizationConfigurations.AddAsync(config2).ConfigureAwait(false);
        await this._dbContext.TokenOptimizationConfigurations.AddAsync(config3).ConfigureAwait(false);
        await this._dbContext.SaveChangesAsync().ConfigureAwait(false);

        var resolver = new TokenOptimizationConfigurationResolver(this._dbContext);

        // Act
        var result = await resolver.GetConfigurationsWithCachingEnabledAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, c => Assert.True(c.EnablePromptCaching));
    }

    [Fact]
    public async Task ConcurrentUpdates_HandledCorrectly()
    {
        // Arrange
        var providerId = "concurrent-provider";
        var config = new TokenOptimizationConfiguration
        {
            ProviderId = providerId,
            EnablePromptCaching = false,
            MaxContextTokens = 4000,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        await this._dbContext.TokenOptimizationConfigurations.AddAsync(config).ConfigureAwait(false);
        await this._dbContext.SaveChangesAsync().ConfigureAwait(false);

        // Create two separate contexts to simulate concurrent access
        var context1 = new OptimizationDbContext(this._dbOptions);
        var context2 = new OptimizationDbContext(this._dbOptions);

        var resolver1 = new TokenOptimizationConfigurationResolver(context1);
        var resolver2 = new TokenOptimizationConfigurationResolver(context2);

        // Act - Simulate concurrent updates
        var update1 = new TokenOptimizationConfiguration
        {
            ProviderId = providerId,
            EnablePromptCaching = true,
            MaxContextTokens = 8000,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        var update2 = new TokenOptimizationConfiguration
        {
            ProviderId = providerId,
            EnablePromptCaching = true,
            MaxContextTokens = 16000,
            UpdatedAt = DateTimeOffset.UtcNow.AddSeconds(1),
        };

        await resolver1.UpdateConfigurationAsync(update1, CancellationToken.None).ConfigureAwait(false);
        await resolver2.UpdateConfigurationAsync(update2, CancellationToken.None).ConfigureAwait(false);

        // Assert - Last write wins
        var final = await this._dbContext.TokenOptimizationConfigurations
            .FirstOrDefaultAsync(c => c.ProviderId == providerId);

        Assert.NotNull(final);
        Assert.Equal(16000, final.MaxContextTokens);

        await context1.DisposeAsync().ConfigureAwait(false);
        await context2.DisposeAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task BulkOperations_PerformEfficiently()
    {
        // Arrange
        var resolver = new TokenOptimizationConfigurationResolver(this._dbContext);
        var configs = new List<TokenOptimizationConfiguration>();

        for (int i = 0; i < 100; i++)
        {
            configs.Add(new TokenOptimizationConfiguration
            {
                ProviderId = $"bulk-provider-{i}",
                EnablePromptCaching = i % 2 == 0,
                MaxContextTokens = 4000 + (i * 100),
                CompressionStrategy = i % 3 == 0 ? "sliding-window" : "none",
                UpdatedAt = DateTimeOffset.UtcNow,
            });
        }

        // Act
        foreach (var config in configs)
        {
            await this._dbContext.TokenOptimizationConfigurations.AddAsync(config).ConfigureAwait(false);
        }
        await this._dbContext.SaveChangesAsync().ConfigureAwait(false);

        var result = await resolver.GetAllConfigurationsAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.Equal(100, result.Count);
        Assert.Equal(50, result.Count(c => c.EnablePromptCaching));
        Assert.True(result.Count(c => c.CompressionStrategy == "sliding-window") >= 33);
    }

    [Fact]
    public async Task ConfigurationValidation_EnforcesConstraints()
    {
        // Arrange
        var resolver = new TokenOptimizationConfigurationResolver(this._dbContext);

        // Act & Assert - Null ProviderId should fail
        var invalidConfig = new TokenOptimizationConfiguration
        {
            ProviderId = null!,
            MaxContextTokens = 8000,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        await Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            await resolver.CreateConfigurationAsync(invalidConfig, CancellationToken.None).ConfigureAwait(false);
        });
    }

    [Fact]
    public async Task QueryPerformance_OptimizedForCommonPatterns()
    {
        // Arrange
        var configs = new List<TokenOptimizationConfiguration>();
        for (int i = 0; i < 1000; i++)
        {
            configs.Add(new TokenOptimizationConfiguration
            {
                ProviderId = $"perf-provider-{i}",
                EnablePromptCaching = true,
                MaxContextTokens = 8000,
                CompressionStrategy = "sliding-window",
                UpdatedAt = DateTimeOffset.UtcNow,
            });
        }

        foreach (var config in configs)
        {
            await this._dbContext.TokenOptimizationConfigurations.AddAsync(config).ConfigureAwait(false);
        }
        await this._dbContext.SaveChangesAsync().ConfigureAwait(false);

        var resolver = new TokenOptimizationConfigurationResolver(this._dbContext);

        // Act - Query should complete quickly even with large dataset
        var startTime = DateTime.UtcNow;
        var result = await resolver.GetConfigurationAsync("perf-provider-500", CancellationToken.None).ConfigureAwait(false);
        var duration = DateTime.UtcNow - startTime;

        // Assert
        Assert.NotNull(result);
        Assert.True(duration.TotalMilliseconds < 100, $"Query took {duration.TotalMilliseconds}ms");
    }
}

/// <summary>
/// EF Core DbContext for token optimization configurations
/// </summary>
public class OptimizationDbContext : DbContext
{
    public OptimizationDbContext(DbContextOptions<OptimizationDbContext> options)
        : base(options)
    {
    }

    public DbSet<TokenOptimizationConfiguration> TokenOptimizationConfigurations => this.Set<TokenOptimizationConfiguration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TokenOptimizationConfiguration>(entity =>
        {
            entity.HasKey(e => e.ProviderId);
            entity.Property(e => e.ProviderId).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
        });
    }
}

/// <summary>
/// Configuration resolver for token optimization settings
/// </summary>
public class TokenOptimizationConfigurationResolver
{
    private readonly OptimizationDbContext _dbContext;

    public TokenOptimizationConfigurationResolver(OptimizationDbContext dbContext)
    {
        this._dbContext = dbContext;
    }

    public async Task<TokenOptimizationConfiguration?> GetConfigurationAsync(
        string providerId,
        CancellationToken cancellationToken)
    {
        return await this._dbContext.TokenOptimizationConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ProviderId == providerId, cancellationToken);
    }

    public async Task CreateConfigurationAsync(
        TokenOptimizationConfiguration configuration,
        CancellationToken cancellationToken)
    {
        await this._dbContext.TokenOptimizationConfigurations.AddAsync(configuration, cancellationToken).ConfigureAwait(false);
        await this._dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateConfigurationAsync(
        TokenOptimizationConfiguration configuration,
        CancellationToken cancellationToken)
    {
        this._dbContext.TokenOptimizationConfigurations.Update(configuration);
        await this._dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteConfigurationAsync(
        string providerId,
        CancellationToken cancellationToken)
    {
        var config = await this._dbContext.TokenOptimizationConfigurations
            .FirstOrDefaultAsync(c => c.ProviderId == providerId, cancellationToken);

        if (config != null)
        {
            this._dbContext.TokenOptimizationConfigurations.Remove(config);
            await this._dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<List<TokenOptimizationConfiguration>> GetAllConfigurationsAsync(
        CancellationToken cancellationToken)
    {
        return await this._dbContext.TokenOptimizationConfigurations
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<TokenOptimizationConfiguration>> GetConfigurationsByStrategyAsync(
        string strategy,
        CancellationToken cancellationToken)
    {
        return await this._dbContext.TokenOptimizationConfigurations
            .AsNoTracking()
            .Where(c => c.CompressionStrategy == strategy)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<TokenOptimizationConfiguration>> GetConfigurationsWithCachingEnabledAsync(
        CancellationToken cancellationToken)
    {
        return await this._dbContext.TokenOptimizationConfigurations
            .AsNoTracking()
            .Where(c => c.EnablePromptCaching)
            .ToListAsync(cancellationToken);
    }
}

/// <summary>
/// Entity representing token optimization configuration for a provider
/// </summary>
public class TokenOptimizationConfiguration
{
    public string ProviderId { get; set; } = string.Empty;

    public bool EnablePromptCaching { get; set; }

    public bool EnableConversationCompression { get; set; }

    public string? CompressionStrategy { get; set; }

    public int MaxContextTokens { get; set; }

    public double TargetCompressionRatio { get; set; }

    public bool EnableSemanticCache { get; set; }

    public double SemanticCacheSimilarityThreshold { get; set; }

    public int CacheTtlMinutes { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
