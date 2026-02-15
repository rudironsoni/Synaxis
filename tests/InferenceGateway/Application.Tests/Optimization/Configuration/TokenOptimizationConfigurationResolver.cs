using Microsoft.EntityFrameworkCore;

namespace Synaxis.InferenceGateway.Application.Tests.Optimization.Configuration;

public class TokenOptimizationConfigurationResolver(
    IDbContextFactory<TestTokenOptimizationDbContext> dbContextFactory) : ITokenOptimizationConfigurationResolver
{
    private readonly IDbContextFactory<TestTokenOptimizationDbContext> _dbContextFactory = dbContextFactory;

    private readonly TokenOptimizationConfig _systemDefaults = new()
    {
        SimilarityThreshold = 0.85,
        CacheTtlSeconds = 3600,
        CompressionStrategy = "gzip",
        EnableCaching = true,
        EnableCompression = true,
        MaxConcurrentRequests = 100,
        MaxTokensPerRequest = 4096,
    };

    public async Task<TokenOptimizationConfig> ResolveAsync(Guid tenantId, Guid userId)
    {
        await using var context = await this._dbContextFactory.CreateDbContextAsync();

        // Start with system defaults
        var config = this.CloneConfig(this._systemDefaults);

        // Apply tenant overrides
        var tenantConfig = await context.TenantTokenOptimizationConfigs
            .FirstOrDefaultAsync(t => t.TenantId == tenantId);

        if (tenantConfig != null)
        {
            this.ApplyTenantOverrides(config, tenantConfig);
        }

        // Apply user overrides (highest priority)
        var userConfig = await context.UserTokenOptimizationConfigs
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (userConfig != null)
        {
            this.ApplyUserOverrides(config, userConfig);
        }

        return config;
    }

    public async Task<TokenOptimizationConfig> GetTenantConfigAsync(Guid tenantId)
    {
        await using var context = await this._dbContextFactory.CreateDbContextAsync();

        var config = this.CloneConfig(this._systemDefaults);
        var tenantConfig = await context.TenantTokenOptimizationConfigs
            .FirstOrDefaultAsync(t => t.TenantId == tenantId);

        if (tenantConfig != null)
        {
            this.ApplyTenantOverrides(config, tenantConfig);
        }

        return config;
    }

    public async Task<TokenOptimizationConfig> GetUserConfigAsync(Guid userId)
    {
        await using var context = await this._dbContextFactory.CreateDbContextAsync();

        var config = this.CloneConfig(this._systemDefaults);
        var userConfig = await context.UserTokenOptimizationConfigs
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (userConfig != null)
        {
            this.ApplyUserOverrides(config, userConfig);
        }

        return config;
    }

    public TokenOptimizationConfig GetSystemConfig()
    {
        return this.CloneConfig(this._systemDefaults);
    }

    private void ApplyTenantOverrides(TokenOptimizationConfig config, TenantTokenOptimizationConfig tenantConfig)
    {
        if (tenantConfig.SimilarityThreshold.HasValue)
        {
            config.SimilarityThreshold = tenantConfig.SimilarityThreshold.Value;
        }

        if (tenantConfig.CacheTtlSeconds.HasValue)
        {
            config.CacheTtlSeconds = tenantConfig.CacheTtlSeconds.Value;
        }

        if (!string.IsNullOrEmpty(tenantConfig.CompressionStrategy))
        {
            config.CompressionStrategy = tenantConfig.CompressionStrategy;
        }

        if (tenantConfig.EnableCaching.HasValue)
        {
            config.EnableCaching = tenantConfig.EnableCaching.Value;
        }
    }

    private void ApplyUserOverrides(TokenOptimizationConfig config, UserTokenOptimizationConfig userConfig)
    {
        if (userConfig.SimilarityThreshold.HasValue)
        {
            config.SimilarityThreshold = userConfig.SimilarityThreshold.Value;
        }

        if (userConfig.CacheTtlSeconds.HasValue)
        {
            config.CacheTtlSeconds = userConfig.CacheTtlSeconds.Value;
        }

        if (userConfig.EnableCaching.HasValue)
        {
            config.EnableCaching = userConfig.EnableCaching.Value;
        }
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
            MaxTokensPerRequest = source.MaxTokensPerRequest,
        };
    }
}
