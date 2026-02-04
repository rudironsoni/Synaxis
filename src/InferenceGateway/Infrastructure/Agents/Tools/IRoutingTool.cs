namespace Synaxis.InferenceGateway.Infrastructure.Agents.Tools;

/// <summary>
/// Tool for managing routing decisions.
/// </summary>
public interface IRoutingTool
{
    Task<bool> SwitchProviderAsync(Guid organizationId, string modelId, string fromProvider, string toProvider, string reason, CancellationToken ct = default);
    Task<RoutingMetrics> GetRoutingMetricsAsync(Guid organizationId, string modelId, CancellationToken ct = default);
}

public record RoutingMetrics(int TotalRequests, Dictionary<string, int> ProviderDistribution, decimal AverageCost);
