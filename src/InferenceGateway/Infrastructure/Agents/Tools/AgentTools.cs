using Microsoft.Extensions.Logging;

namespace Synaxis.InferenceGateway.Infrastructure.Agents.Tools;

public class AgentTools : IAgentTools
{
    public IProviderTool Provider { get; }
    public IAlertTool Alert { get; }
    public IRoutingTool Routing { get; }
    public IHealthTool Health { get; }
    public IAuditTool Audit { get; }

    public AgentTools(
        IProviderTool provider,
        IAlertTool alert,
        IRoutingTool routing,
        IHealthTool health,
        IAuditTool audit)
    {
        Provider = provider;
        Alert = alert;
        Routing = routing;
        Health = health;
        Audit = audit;
    }
}
