// <copyright file="IAlertTool.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Agents.Tools
{
    /// <summary>
    /// Tool for sending alerts and notifications.
    /// </summary>
    public interface IAlertTool
    {
        /// <summary>
        /// Sends an admin alert.
        /// </summary>
        /// <param name="subject">The alert subject.</param>
        /// <param name="message">The alert message.</param>
        /// <param name="severity">The alert severity.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task SendAdminAlertAsync(string subject, string message, AlertSeverity severity, CancellationToken ct = default);

        /// <summary>
        /// Sends a notification to a user or organization.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="message">The notification message.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task SendNotificationAsync(Guid? userId, Guid? organizationId, string message, CancellationToken ct = default);
    }

    /// <summary>
    /// Alert severity levels.
    /// </summary>
    public enum AlertSeverity
    {
        /// <summary>
        /// Informational alert.
        /// </summary>
        Info,

        /// <summary>
        /// Warning alert.
        /// </summary>
        Warning,

        /// <summary>
        /// Critical alert.
        /// </summary>
        Critical,
    }
}
