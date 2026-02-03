using Microsoft.EntityFrameworkCore;

namespace Synaxis.InferenceGateway.Application.Tests.Optimization.Configuration;

// Mock entity classes for Token Optimization configuration testing
public class TenantTokenOptimizationConfig
{
    public Guid TenantId { get; set; }
    public double? SimilarityThreshold { get; set; }
    public int? CacheTtlSeconds { get; set; }
    public string? CompressionStrategy { get; set; }
    public bool? EnableCaching { get; set; }
}

public class UserTokenOptimizationConfig
{
    public Guid UserId { get; set; }
    public double? SimilarityThreshold { get; set; }
    public int? CacheTtlSeconds { get; set; }
    public bool? EnableCaching { get; set; }
}

public class TokenOptimizationConfig
{
    public double SimilarityThreshold { get; set; }
    public int? CacheTtlSeconds { get; set; }
    public string? CompressionStrategy { get; set; }
    public bool EnableCaching { get; set; }
    public bool EnableCompression { get; set; }
    public int? MaxConcurrentRequests { get; set; }
    public int? MaxTokensPerRequest { get; set; }
}

public enum ConfigurationLevel
{
    System,
    Tenant,
    User
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}

// Test-specific DbContext for Token Optimization
public class TestTokenOptimizationDbContext : DbContext
{
    public TestTokenOptimizationDbContext(DbContextOptions<TestTokenOptimizationDbContext> options)
        : base(options)
    {
    }

    public DbSet<TenantTokenOptimizationConfig> TenantTokenOptimizationConfigs => Set<TenantTokenOptimizationConfig>();
    public DbSet<UserTokenOptimizationConfig> UserTokenOptimizationConfigs => Set<UserTokenOptimizationConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TenantTokenOptimizationConfig>(entity =>
        {
            entity.HasKey(t => t.TenantId);
        });

        modelBuilder.Entity<UserTokenOptimizationConfig>(entity =>
        {
            entity.HasKey(u => u.UserId);
        });
    }
}

// Mock implementation of ITokenOptimizationConfigurationResolver
public interface ITokenOptimizationConfigurationResolver
{
    Task<TokenOptimizationConfig> ResolveAsync(Guid tenantId, Guid userId);
    Task<TokenOptimizationConfig> GetTenantConfigAsync(Guid tenantId);
    Task<TokenOptimizationConfig> GetUserConfigAsync(Guid userId);
    TokenOptimizationConfig GetSystemConfig();
}

public class TokenOptimizationConfigurationResolver : ITokenOptimizationConfigurationResolver
{
    private readonly IDbContextFactory<TestTokenOptimizationDbContext> _dbContextFactory;

    private readonly TokenOptimizationConfig _systemDefaults = new()
    {
        SimilarityThreshold = 0.85,
        CacheTtlSeconds = 3600,
        CompressionStrategy = "gzip",
        EnableCaching = true,
        EnableCompression = true,
        MaxConcurrentRequests = 100,
        MaxTokensPerRequest = 4096
    };

    public TokenOptimizationConfigurationResolver(
        IDbContextFactory<TestTokenOptimizationDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<TokenOptimizationConfig> ResolveAsync(Guid tenantId, Guid userId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        // Start with system defaults
        var config = CloneConfig(_systemDefaults);

        // Apply tenant overrides
        var tenantConfig = await context.TenantTokenOptimizationConfigs
            .FirstOrDefaultAsync(t => t.TenantId == tenantId);
        
        if (tenantConfig != null)
        {
            ApplyTenantOverrides(config, tenantConfig);
        }

        // Apply user overrides (highest priority)
        var userConfig = await context.UserTokenOptimizationConfigs
            .FirstOrDefaultAsync(u => u.UserId == userId);
        
        if (userConfig != null)
        {
            ApplyUserOverrides(config, userConfig);
        }

        return config;
    }

    public async Task<TokenOptimizationConfig> GetTenantConfigAsync(Guid tenantId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var config = CloneConfig(_systemDefaults);
        var tenantConfig = await context.TenantTokenOptimizationConfigs
            .FirstOrDefaultAsync(t => t.TenantId == tenantId);
        
        if (tenantConfig != null)
        {
            ApplyTenantOverrides(config, tenantConfig);
        }

        return config;
    }

    public async Task<TokenOptimizationConfig> GetUserConfigAsync(Guid userId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var config = CloneConfig(_systemDefaults);
        var userConfig = await context.UserTokenOptimizationConfigs
            .FirstOrDefaultAsync(u => u.UserId == userId);
        
        if (userConfig != null)
        {
            ApplyUserOverrides(config, userConfig);
        }

        return config;
    }

    public TokenOptimizationConfig GetSystemConfig()
    {
        return CloneConfig(_systemDefaults);
    }

    private void ApplyTenantOverrides(TokenOptimizationConfig config, TenantTokenOptimizationConfig tenantConfig)
    {
        if (tenantConfig.SimilarityThreshold.HasValue)
            config.SimilarityThreshold = tenantConfig.SimilarityThreshold.Value;
        
        if (tenantConfig.CacheTtlSeconds.HasValue)
            config.CacheTtlSeconds = tenantConfig.CacheTtlSeconds.Value;
        
        if (!string.IsNullOrEmpty(tenantConfig.CompressionStrategy))
            config.CompressionStrategy = tenantConfig.CompressionStrategy;
        
        if (tenantConfig.EnableCaching.HasValue)
            config.EnableCaching = tenantConfig.EnableCaching.Value;
    }

    private void ApplyUserOverrides(TokenOptimizationConfig config, UserTokenOptimizationConfig userConfig)
    {
        if (userConfig.SimilarityThreshold.HasValue)
            config.SimilarityThreshold = userConfig.SimilarityThreshold.Value;
        
        if (userConfig.CacheTtlSeconds.HasValue)
            config.CacheTtlSeconds = userConfig.CacheTtlSeconds.Value;
        
        if (userConfig.EnableCaching.HasValue)
            config.EnableCaching = userConfig.EnableCaching.Value;
    }

    private TokenOptimizationConfig CloneConfig(TokenOptimizationConfig source)
    {
        return new TokenOptimizationConfig
        {
            SimilarityThreshold = source.SimilarityThreshold,
            CacheTtlSeconds = source.CacheTtlSeconds,
            CompressionStrategy = source.CompressionStrategy,
            EnableCaching = source.EnableCaching,
            EnableCompression = source.EnableCompression,
            MaxConcurrentRequests = source.MaxConcurrentRequests,
            MaxTokensPerRequest = source.MaxTokensPerRequest
        };
    }
}

// Mock implementation of ITokenOptimizationConfigValidator
public interface ITokenOptimizationConfigValidator
{
    ValidationResult Validate(TokenOptimizationConfig config, ConfigurationLevel level);
}

public class TokenOptimizationConfigValidator : ITokenOptimizationConfigValidator
{
    public ValidationResult Validate(TokenOptimizationConfig config, ConfigurationLevel level)
    {
        var errors = new List<string>();

        // Validate SimilarityThreshold range
        if (config.SimilarityThreshold < 0.5 || config.SimilarityThreshold > 1.0)
        {
            errors.Add("SimilarityThreshold must be between 0.5 and 1.0");
        }

        // User-specific minimum threshold
        if (level == ConfigurationLevel.User && config.SimilarityThreshold < 0.7)
        {
            errors.Add("SimilarityThreshold for user must be at least 0.7");
        }

        // Validate CacheTtlSeconds range
        if (config.CacheTtlSeconds.HasValue && 
            (config.CacheTtlSeconds < 60 || config.CacheTtlSeconds > 86400))
        {
            errors.Add("CacheTtlSeconds must be between 60 and 86400");
        }

        // Validate CompressionStrategy
        if (!string.IsNullOrEmpty(config.CompressionStrategy) &&
            config.CompressionStrategy != "gzip" && 
            config.CompressionStrategy != "brotli" && 
            config.CompressionStrategy != "none")
        {
            errors.Add("CompressionStrategy must be one of: gzip, brotli, none");
        }

        // User cannot set CompressionStrategy
        if (level == ConfigurationLevel.User && !string.IsNullOrEmpty(config.CompressionStrategy))
        {
            errors.Add("CompressionStrategy cannot be set at user level");
        }

        // User cannot set MaxConcurrentRequests
        if (level == ConfigurationLevel.User && config.MaxConcurrentRequests.HasValue)
        {
            errors.Add("MaxConcurrentRequests is a system-only setting");
        }

        // Only system can set MaxConcurrentRequests
        if (level != ConfigurationLevel.System && config.MaxConcurrentRequests.HasValue)
        {
            errors.Add("MaxConcurrentRequests can only be set at system level");
        }

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
}
