// <copyright file="SynaxisAgent.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Agents
{
    using System;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Base class for all Synaxis agents with tenant context and tool access.
    /// </summary>
    public abstract class SynaxisAgent : ISynaxisAgent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SynaxisAgent"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        protected SynaxisAgent(ILogger logger)
        {
            this.Logger = logger;
        }

        /// <summary>
        /// Gets or sets the organization ID.
        /// </summary>
        public Guid? OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Gets or sets the group ID.
        /// </summary>
        public Guid? GroupId { get; set; }

        /// <summary>
        /// Gets the agent name.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Gets the logger instance.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// Get a correlation ID for logging agent actions.
        /// </summary>
        /// <returns>A correlation ID string.</returns>
        protected static string GetCorrelationId() => Guid.NewGuid().ToString("N")[..8];

        /// <summary>
        /// Log agent action with tenant context.
        /// </summary>
        /// <param name="action">The action being performed.</param>
        /// <param name="details">Details about the action.</param>
        /// <param name="correlationId">The correlation ID.</param>
        protected void LogAction(string action, string details, string correlationId)
        {
            this.Logger.LogInformation(
                "[{Agent}][{CorrelationId}] Action: {Action}, OrgId: {OrgId}, UserId: {UserId}, Details: {Details}",
                this.Name,
                correlationId,
                action,
                this.OrganizationId,
                this.UserId,
                details);
        }

        /// <summary>
        /// Log agent error with tenant context.
        /// </summary>
        /// <param name="ex">The exception that occurred.</param>
        /// <param name="action">The action that failed.</param>
        /// <param name="correlationId">The correlation ID.</param>
        protected void LogError(Exception ex, string action, string correlationId)
        {
            this.Logger.LogError(
                ex,
                "[{Agent}][{CorrelationId}] Error during {Action}, OrgId: {OrgId}, UserId: {UserId}",
                this.Name,
                correlationId,
                action,
                this.OrganizationId,
                this.UserId);
        }
    }
}
