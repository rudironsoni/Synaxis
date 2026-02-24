namespace Synaxis.InferenceGateway.Application.Tests.Optimization.Configuration;

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
            Errors = errors,
        };
    }
}
