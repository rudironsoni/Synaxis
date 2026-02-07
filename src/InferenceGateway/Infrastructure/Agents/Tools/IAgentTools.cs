// <copyright file="IAgentTools.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Agents.Tools
{
    /// <summary>
    /// Registry of tools available to agents.
    /// </summary>
    public interface IAgentTools
    {
        /// <summary>
        /// Gets the provider tool.
        /// </summary>
        IProviderTool Provider { get; }

        /// <summary>
        /// Gets the alert tool.
        /// </summary>
        IAlertTool Alert { get; }

        /// <summary>
        /// Gets the routing tool.
        /// </summary>
        IRoutingTool Routing { get; }

        /// <summary>
        /// Gets the health tool.
        /// </summary>
        IHealthTool Health { get; }

        /// <summary>
        /// Gets the audit tool.
        /// </summary>
        IAuditTool Audit { get; }
    }
}
