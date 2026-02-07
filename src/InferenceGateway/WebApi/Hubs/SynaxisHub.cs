// <copyright file="SynaxisHub.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Hubs
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.SignalR;

    /// <summary>
    /// SignalR hub for real-time Synaxis updates.
    /// Enables real-time notifications for provider health, cost optimization,
    /// model discovery, security alerts, and audit events.
    /// </summary>
    [Authorize]
    public class SynaxisHub : Hub
    {
        private readonly ILogger<SynaxisHub> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SynaxisHub"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public SynaxisHub(ILogger<SynaxisHub> logger)
        {
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Join organization group for targeted updates.
        /// </summary>
        /// <param name="organizationId">The organization ID to join.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task JoinOrganization(string organizationId)
        {
            if (string.IsNullOrWhiteSpace(organizationId))
            {
                this._logger.LogWarning("Connection {ConnectionId} attempted to join with empty organization ID", this.Context.ConnectionId);
                throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));
            }

            // NOTE: Validate user belongs to organization. Implementation pending.
            await this.Groups.AddToGroupAsync(this.Context.ConnectionId, organizationId).ConfigureAwait(false);
            this._logger.LogInformation("Connection {ConnectionId} joined organization {OrganizationId}", this.Context.ConnectionId, organizationId);
        }

        /// <summary>
        /// Leave organization group.
        /// </summary>
        /// <param name="organizationId">The organization ID to leave.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task LeaveOrganization(string organizationId)
        {
            if (string.IsNullOrWhiteSpace(organizationId))
            {
                this._logger.LogWarning("Connection {ConnectionId} attempted to leave with empty organization ID", this.Context.ConnectionId);
                return;
            }

            await this.Groups.RemoveFromGroupAsync(this.Context.ConnectionId, organizationId).ConfigureAwait(false);
            this._logger.LogInformation("Connection {ConnectionId} left organization {OrganizationId}", this.Context.ConnectionId, organizationId);
        }

        /// <summary>
        /// Authentication check on connection.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public override async Task OnConnectedAsync()
        {
            var user = this.Context.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                this._logger.LogWarning("Unauthenticated connection attempt: {ConnectionId}", this.Context.ConnectionId);
                this.Context.Abort();
                return;
            }

            this._logger.LogInformation("Client connected: {ConnectionId}, User: {UserName}", this.Context.ConnectionId, user.Identity.Name);
            await base.OnConnectedAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Handle disconnection.
        /// </summary>
        /// <param name="exception">The exception that caused the disconnection, if any.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (exception != null)
            {
                this._logger.LogError(exception, "Client disconnected with error: {ConnectionId}", this.Context.ConnectionId);
            }
            else
            {
                this._logger.LogInformation("Client disconnected: {ConnectionId}", this.Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
        }
    }
}
