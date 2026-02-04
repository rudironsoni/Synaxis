namespace Synaxis.InferenceGateway.Infrastructure.Agents.Tools;

/// <summary>
/// Registry of tools available to agents.
/// </summary>
public interface IAgentTools
{
    IProviderTool Provider { get; }
    IAlertTool Alert { get; }
    IRoutingTool Routing { get; }
    IHealthTool Health { get; }
    IAuditTool Audit { get; }
}
