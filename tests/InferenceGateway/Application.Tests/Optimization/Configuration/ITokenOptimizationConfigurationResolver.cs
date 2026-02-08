namespace Synaxis.InferenceGateway.Application.Tests.Optimization.Configuration;

// Mock implementation of ITokenOptimizationConfigurationResolver
public interface ITokenOptimizationConfigurationResolver
{
    Task<TokenOptimizationConfig> ResolveAsync(Guid tenantId, Guid userId);

    Task<TokenOptimizationConfig> GetTenantConfigAsync(Guid tenantId);

    Task<TokenOptimizationConfig> GetUserConfigAsync(Guid userId);

    TokenOptimizationConfig GetSystemConfig();
}
