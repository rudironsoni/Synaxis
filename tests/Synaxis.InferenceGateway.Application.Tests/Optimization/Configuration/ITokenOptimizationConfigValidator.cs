namespace Synaxis.InferenceGateway.Application.Tests.Optimization.Configuration;

// Mock implementation of ITokenOptimizationConfigValidator
public interface ITokenOptimizationConfigValidator
{
    ValidationResult Validate(TokenOptimizationConfig config, ConfigurationLevel level);
}
