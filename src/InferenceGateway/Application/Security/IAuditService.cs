// <copyright file="IAuditService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Security
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides audit logging services.
    /// </summary>
    public interface IAuditService
    {
        /// <summary>
        /// Logs an audit event.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="userId">The user identifier, if applicable.</param>
        /// <param name="action">The action being audited.</param>
        /// <param name="payload">Additional data associated with the action.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task LogAsync(Guid tenantId, Guid? userId, string action, object? payload, CancellationToken cancellationToken = default);
    }
}