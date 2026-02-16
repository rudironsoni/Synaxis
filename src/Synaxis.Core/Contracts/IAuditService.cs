// <copyright file="IAuditService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Synaxis.Core.Models;

    /// <summary>
    /// Service for immutable audit logging with cross-region aggregation.
    /// </summary>
    public interface IAuditService
    {
        /// <summary>
        /// Log an audit event (immutable).
        /// </summary>
        /// <param name="auditEvent">The audit event to log.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the audit log.</returns>
        Task<AuditLog> LogEventAsync(AuditEvent auditEvent);

        /// <summary>
        /// Query audit logs for an organization.
        /// </summary>
        /// <param name="query">The query parameters for filtering audit logs.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the list of audit logs.</returns>
        Task<IList<AuditLog>> QueryAuditLogsAsync(AuditQuery query);

        /// <summary>
        /// Get audit log by ID.
        /// </summary>
        /// <param name="logId">The unique identifier of the audit log.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the audit log.</returns>
        Task<AuditLog> GetAuditLogAsync(Guid logId);

        /// <summary>
        /// Export audit logs for an organization.
        /// </summary>
        /// <param name="organizationId">The unique identifier of the organization.</param>
        /// <param name="startDate">The start date of the export range.</param>
        /// <param name="endDate">The end date of the export range.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the exported audit logs as byte array.</returns>
        Task<byte[]> ExportAuditLogsAsync(Guid organizationId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Verify audit log integrity (tamper detection).
        /// </summary>
        /// <param name="logId">The unique identifier of the audit log to verify.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether the log is valid.</returns>
        Task<bool> VerifyIntegrityAsync(Guid logId);

        /// <summary>
        /// Aggregate anonymized audit logs across regions (compliance).
        /// </summary>
        /// <param name="startDate">The start date of the aggregation range.</param>
        /// <param name="endDate">The end date of the aggregation range.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the aggregation result.</returns>
        Task<AuditAggregationResult> AggregateAnonymizedLogsAsync(DateTime startDate, DateTime endDate);
    }

    /// <summary>
    /// Represents an audit event to be logged.
    /// </summary>
    public class AuditEvent
    {
        /// <summary>
        /// Gets or sets the organization identifier.
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Gets or sets the event type.
        /// </summary>
        public string EventType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the event category.
        /// </summary>
        public string EventCategory { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the action performed.
        /// </summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the resource type.
        /// </summary>
        public string ResourceType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the resource identifier.
        /// </summary>
        public string ResourceId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the event metadata.
        /// </summary>
        public IDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>(StringComparer.Ordinal);

        /// <summary>
        /// Gets or sets the IP address.
        /// </summary>
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user agent.
        /// </summary>
        public string UserAgent { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the region.
        /// </summary>
        public string Region { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents query parameters for filtering audit logs.
    /// </summary>
    public class AuditQuery
    {
        /// <summary>
        /// Gets or sets the organization identifier.
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Gets or sets the event type filter.
        /// </summary>
        public string EventType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the event category filter.
        /// </summary>
        public string EventCategory { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the start date filter.
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Gets or sets the end date filter.
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Gets or sets the page size.
        /// </summary>
        public int PageSize { get; set; } = 50;

        /// <summary>
        /// Gets or sets the page number.
        /// </summary>
        public int PageNumber { get; set; } = 1;
    }

    /// <summary>
    /// Represents the result of audit log aggregation.
    /// </summary>
    public class AuditAggregationResult
    {
        /// <summary>
        /// Gets or sets the total number of events.
        /// </summary>
        public int TotalEvents { get; set; }

        /// <summary>
        /// Gets or sets events grouped by type.
        /// </summary>
        public IDictionary<string, int> EventsByType { get; set; } = new Dictionary<string, int>(StringComparer.Ordinal);

        /// <summary>
        /// Gets or sets events grouped by category.
        /// </summary>
        public IDictionary<string, int> EventsByCategory { get; set; } = new Dictionary<string, int>(StringComparer.Ordinal);

        /// <summary>
        /// Gets or sets events grouped by region.
        /// </summary>
        public IDictionary<string, int> EventsByRegion { get; set; } = new Dictionary<string, int>(StringComparer.Ordinal);

        /// <summary>
        /// Gets or sets the start date of the aggregation.
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Gets or sets the end date of the aggregation.
        /// </summary>
        public DateTime EndDate { get; set; }
    }
}
