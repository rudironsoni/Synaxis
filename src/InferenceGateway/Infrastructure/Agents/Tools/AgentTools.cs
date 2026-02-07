// <copyright file="AgentTools.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Agents.Tools
{
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Registry of tools available to agents.
    /// </summary>
    public class AgentTools : IAgentTools
    {
        /// <summary>
        /// Gets the provider tool.
        /// </summary>
        public IProviderTool Provider { get; }

        /// <summary>
        /// Gets the alert tool.
        /// </summary>
        public IAlertTool Alert { get; }

        /// <summary>
        /// Gets the routing tool.
        /// </summary>
        public IRoutingTool Routing { get; }

        /// <summary>
        /// Gets the health tool.
        /// </summary>
        public IHealthTool Health { get; }

        /// <summary>
        /// Gets the audit tool.
        /// </summary>
        public IAuditTool Audit { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AgentTools"/> class.
        /// </summary>
        /// <param name="provider">The provider tool.</param>
        /// <param name="alert">The alert tool.</param>
        /// <param name="routing">The routing tool.</param>
        /// <param name="health">The health tool.</param>
        /// <param name="audit">The audit tool.</param>
        public AgentTools(
            IProviderTool provider,
            IAlertTool alert,
            IRoutingTool routing,
            IHealthTool health,
            IAuditTool audit)
        {
            this.Provider = provider;
            this.Alert = alert;
            this.Routing = routing;
            this.Health = health;
            this.Audit = audit;
        }
    }
}
