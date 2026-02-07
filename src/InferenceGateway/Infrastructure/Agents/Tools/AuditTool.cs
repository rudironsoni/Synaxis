// <copyright file="AuditTool.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Agents.Tools
{
    using Microsoft.Extensions.Logging;
    using Synaxis.InferenceGateway.Infrastructure.ControlPlane;
    using Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Audit;

    /// <summary>
    /// Tool for logging agent actions and optimizations.
    /// </summary>
    public class AuditTool : IAuditTool
    {
        private readonly ControlPlaneDbContext _db;
        private readonly ILogger<AuditTool> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuditTool"/> class.
        /// </summary>
        /// <param name="db">The database context.</param>
        /// <param name="logger">The logger.</param>
        public AuditTool(ControlPlaneDbContext db, ILogger<AuditTool> logger)
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
                var auditLog = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    Action = $"{agentName}:{action}",
                    UserId = userId,
                    OrganizationId = organizationId,
                    NewValues = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        agent = agentName,
                        action,
                        details,
                        correlationId,
                        timestamp = DateTime.UtcNow,
                    }),
                    CreatedAt = DateTime.UtcNow,
                };

                this._db.AuditLogs.Add(auditLog);
                await this._db.SaveChangesAsync(ct).ConfigureAwait(false);
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
                var auditLog = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    Action = "CostOptimization:ProviderSwitch",
                    OrganizationId = organizationId,
                    NewValues = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        modelId,
                        oldProvider,
                        newProvider,
                        savingsPercent,
                        reason,
                        timestamp = DateTime.UtcNow,
                    }),
                    CreatedAt = DateTime.UtcNow,
                };

                this._db.AuditLogs.Add(auditLog);
                await this._db.SaveChangesAsync(ct).ConfigureAwait(false);

                this._logger.LogInformation(
                    "Optimization logged: OrgId={OrgId}, Model={Model}, {Old}->{New}, Savings={Savings}%",
                    organizationId,
                    modelId,
                    oldProvider,
                    newProvider,
                    savingsPercent);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Failed to log optimization");
            }
        }
    }
}
