// <copyright file="AuditTool.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Agents.Tools
{
    using Microsoft.Extensions.Logging;
    using Synaxis.Core.Models;
    using Synaxis.Infrastructure.Data;

    /// <summary>
    /// Tool for logging agent actions and optimizations.
    /// </summary>
    public class AuditTool : IAuditTool
    {
        private readonly SynaxisDbContext _db;
        private readonly ILogger<AuditTool> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuditTool"/> class.
        /// </summary>
        /// <param name="db">The database context.</param>
        /// <param name="logger">The logger.</param>
        public AuditTool(SynaxisDbContext db, ILogger<AuditTool> logger)
        {
            this._db = db;
            this._logger = logger;
        }

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
        public async Task LogActionAsync(string agentName, string action, Guid? organizationId, Guid? userId, string details, string correlationId, CancellationToken ct = default)
        {
            try
            {
                var timestamp = DateTime.UtcNow;
                var auditLog = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId ?? Guid.Empty,
                    UserId = userId,
                    EventType = $"{agentName}:{action}",
                    EventCategory = "agent",
                    Action = action,
                    ResourceType = "agent",
                    ResourceId = agentName,
                    Metadata = new Dictionary<string, object>
                    {
                        { "agent", agentName },
                        { "action", action },
                        { "details", details },
                        { "correlationId", correlationId },
                        { "timestamp", timestamp },
                    },
                    Region = "unknown",
                    IntegrityHash = string.Empty,
                    Timestamp = timestamp,
                };

                this._db.AuditLogs.Add(auditLog);
                await this._db.SaveChangesAsync(ct).ConfigureAwait(false);

                this._logger.LogInformation(
                    "Agent action logged: ToolName={ToolName}, Action={Action}, OrganizationId={OrganizationId}, UserId={UserId}, Timestamp={Timestamp}, Success={Success}",
                    agentName,
                    action,
                    organizationId,
                    userId,
                    timestamp,
                    true);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Failed to log action to audit");
            }
        }

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
        public async Task LogOptimizationAsync(Guid organizationId, string modelId, string oldProvider, string newProvider, decimal savingsPercent, string reason, CancellationToken ct = default)
        {
            try
            {
                var timestamp = DateTime.UtcNow;
                var auditLog = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    UserId = null,
                    EventType = "CostOptimization:ProviderSwitch",
                    EventCategory = "cost_optimization",
                    Action = "ProviderSwitch",
                    ResourceType = "model",
                    ResourceId = modelId,
                    Metadata = new Dictionary<string, object>
                    {
                        { "modelId", modelId },
                        { "oldProvider", oldProvider },
                        { "newProvider", newProvider },
                        { "savingsPercent", savingsPercent },
                        { "reason", reason },
                        { "timestamp", timestamp },
                    },
                    Region = "unknown",
                    IntegrityHash = string.Empty,
                    Timestamp = timestamp,
                };

                this._db.AuditLogs.Add(auditLog);
                await this._db.SaveChangesAsync(ct).ConfigureAwait(false);

                this._logger.LogInformation(
                    "Optimization logged: ToolName={ToolName}, Action={Action}, OrganizationId={OrganizationId}, Model={Model}, OldProvider={OldProvider}, NewProvider={NewProvider}, Savings={Savings}%, Timestamp={Timestamp}, Success={Success}",
                    "CostOptimization",
                    "ProviderSwitch",
                    organizationId,
                    modelId,
                    oldProvider,
                    newProvider,
                    savingsPercent,
                    timestamp,
                    true);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Failed to log optimization");
            }
        }
    }
}
