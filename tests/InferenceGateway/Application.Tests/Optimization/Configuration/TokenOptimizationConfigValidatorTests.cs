using Xunit;

namespace Synaxis.InferenceGateway.Application.Tests.Optimization.Configuration;

public class TokenOptimizationConfigValidatorTests
{
    private readonly TokenOptimizationConfigValidator _validator;

    public TokenOptimizationConfigValidatorTests()
    {
        this._validator = new TokenOptimizationConfigValidator();
    }

    [Fact]
    public void Validate_SimilarityThresholdInRange_Passes()
    {
        // Arrange
        var config = new TokenOptimizationConfig
        {
            SimilarityThreshold = 0.85,
            CacheTtlSeconds = 3600,
            CompressionStrategy = "gzip",
            EnableCaching = true,
            EnableCompression = true,
        };

        // Act
        var result = this._validator.Validate(config, ConfigurationLevel.System);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_SimilarityThresholdOutOfRange_Fails()
    {
        // Arrange - Below minimum
        var configLow = new TokenOptimizationConfig
        {
            SimilarityThreshold = 0.49, // Below minimum of 0.5
            CacheTtlSeconds = 3600,
            CompressionStrategy = "gzip",
        };

        // Act
        var resultLow = this._validator.Validate(configLow, ConfigurationLevel.System);

        // Assert
        Assert.False(resultLow.IsValid);
        Assert.Contains(resultLow.Errors, e => e.Contains("SimilarityThreshold") && e.Contains("0.5") && e.Contains("1.0"));

        // Arrange - Above maximum
        var configHigh = new TokenOptimizationConfig
        {
            SimilarityThreshold = 1.01, // Above maximum of 1.0
            CacheTtlSeconds = 3600,
            CompressionStrategy = "gzip",
        };

        // Act
        var resultHigh = this._validator.Validate(configHigh, ConfigurationLevel.System);

        // Assert
        Assert.False(resultHigh.IsValid);
        Assert.Contains(resultHigh.Errors, e => e.Contains("SimilarityThreshold") && e.Contains("0.5") && e.Contains("1.0"));
    }

    [Fact]
    public void Validate_UserLowThreshold_Fails()
    {
        // Arrange - User attempting to set threshold below 0.7
        var config = new TokenOptimizationConfig
        {
            SimilarityThreshold = 0.65, // Below user minimum of 0.7
            CacheTtlSeconds = 3600,
        };

        // Act
        var result = this._validator.Validate(config, ConfigurationLevel.User);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.Contains("SimilarityThreshold") &&
            e.Contains("0.7") &&
            e.Contains("user"));
    }

    [Fact]
    public void Validate_CacheTtlInRange_Passes()
    {
        // Arrange
        var config = new TokenOptimizationConfig
        {
            SimilarityThreshold = 0.85,
            CacheTtlSeconds = 7200, // Within valid range (60-86400)
            CompressionStrategy = "gzip",
        };

        // Act
        var result = this._validator.Validate(config, ConfigurationLevel.Tenant);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_InvalidCompressionStrategy_Fails()
    {
        // Arrange
        var config = new TokenOptimizationConfig
        {
            SimilarityThreshold = 0.85,
            CacheTtlSeconds = 3600,
            CompressionStrategy = "invalid-compression", // Invalid strategy
        };

        // Act
        var result = this._validator.Validate(config, ConfigurationLevel.System);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.Contains("CompressionStrategy") &&
            e.Contains("gzip") &&
            e.Contains("brotli"));
    }

    [Fact]
    public void Validate_UserRestrictedProperties_Fails()
    {
        // Arrange - User attempting to set system-only properties
        var config = new TokenOptimizationConfig
        {
            SimilarityThreshold = 0.85,
            CacheTtlSeconds = 3600,
            CompressionStrategy = "gzip", // System-only setting
            MaxConcurrentRequests = 100, // System-only setting
        };

        // Act
        var result = this._validator.Validate(config, ConfigurationLevel.User);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.Contains("CompressionStrategy") &&
            e.Contains("user"));
        Assert.Contains(result.Errors, e =>
            e.Contains("MaxConcurrentRequests") &&
            e.Contains("system"));
    }

    [Fact]
    public void Validate_TenantAllowedProperties_Passes()
    {
        // Arrange - Tenant can set more properties than users but not all system properties
        var config = new TokenOptimizationConfig
        {
            SimilarityThreshold = 0.85,
            CacheTtlSeconds = 7200,
            CompressionStrategy = "brotli", // Tenants can set this
            EnableCaching = true,
            EnableCompression = true,
        };

        // Act
        var result = this._validator.Validate(config, ConfigurationLevel.Tenant);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_AllValid_Passes()
    {
        // Arrange - All properties valid at system level
        var config = new TokenOptimizationConfig
        {
            SimilarityThreshold = 0.90,
            CacheTtlSeconds = 3600,
            CompressionStrategy = "gzip",
            EnableCaching = true,
            EnableCompression = true,
            MaxConcurrentRequests = 50,
            MaxTokensPerRequest = 4096,
        };

        // Act
        var result = this._validator.Validate(config, ConfigurationLevel.System);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
}
