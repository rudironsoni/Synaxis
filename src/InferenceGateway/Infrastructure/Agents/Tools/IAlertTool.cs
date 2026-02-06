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
        Task SendAdminAlertAsync(string subject, string message, AlertSeverity severity, CancellationToken ct = default);

        Task SendNotificationAsync(Guid? userId, Guid? organizationId, string message, CancellationToken ct = default);
    }

    public enum AlertSeverity
    {
        Info,
        Warning,
        Critical,
    }
}