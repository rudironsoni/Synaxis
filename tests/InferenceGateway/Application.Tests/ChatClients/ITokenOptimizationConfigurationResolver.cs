namespace Synaxis.InferenceGateway.Application.Tests.ChatClients;

/// <summary>
/// Interface for token optimization configuration
/// </summary>
public interface ITokenOptimizationConfigurationResolver
{
    bool IsOptimizationEnabled();

    bool IsCachingEnabled();

    bool IsCompressionEnabled();

    bool IsDeduplicationEnabled();

    bool IsSessionAffinityEnabled();

    int GetCompressionThreshold();
}
