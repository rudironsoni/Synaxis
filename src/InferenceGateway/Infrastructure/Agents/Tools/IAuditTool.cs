// <copyright file="IAuditTool.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Agents.Tools
{
    /// <summary>
    /// Tool for logging agent actions and optimizations.
    /// </summary>
    public interface IAuditTool
    {
        /// <summary>
        /// Logs an agent action to the audit log.
        /// </summary>
        /// <param name="agentName">The agent name.</param>
        /// <param name="action">The action performed.</param>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <param name="details">Action details.</param>
        /// <param name="correlationId">The correlation ID.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task LogActionAsync(string agentName, string action, Guid? organizationId, Guid? userId, string details, string correlationId, CancellationToken ct = default);

        /// <summary>
        /// Logs a cost optimization action.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="modelId">The model ID.</param>
        /// <param name="oldProvider">The old provider.</param>
        /// <param name="newProvider">The new provider.</param>
        /// <param name="savingsPercent">The savings percentage.</param>
        /// <param name="reason">The optimization reason.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task LogOptimizationAsync(Guid organizationId, string modelId, string oldProvider, string newProvider, decimal savingsPercent, string reason, CancellationToken ct = default);
    }
}
