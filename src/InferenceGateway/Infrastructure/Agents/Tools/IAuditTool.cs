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
        Task LogActionAsync(string agentName, string action, Guid? organizationId, Guid? userId, string details, string correlationId, CancellationToken ct = default);

        Task LogOptimizationAsync(Guid organizationId, string modelId, string oldProvider, string newProvider, decimal savingsPercent, string reason, CancellationToken ct = default);
    }
}