namespace Synaxis.InferenceGateway.Infrastructure.Agents.Tools;

/// <summary>
/// Tool for managing provider configurations.
/// </summary>
public interface IProviderTool
{
    Task<bool> UpdateProviderConfigAsync(Guid organizationId, Guid providerId, string key, object value, CancellationToken ct = default);
    Task<ProviderStatus> GetProviderStatusAsync(Guid organizationId, Guid providerId, CancellationToken ct = default);
    Task<List<ProviderInfo>> GetAllProvidersAsync(Guid organizationId, CancellationToken ct = default);
}

public record ProviderStatus(bool IsEnabled, bool IsHealthy, DateTime? LastChecked);
public record ProviderInfo(Guid Id, string Name, bool IsEnabled, decimal? InputCost, decimal? OutputCost);
