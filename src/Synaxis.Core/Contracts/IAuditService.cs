using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Synaxis.Core.Models;

namespace Synaxis.Core.Contracts
{
    /// <summary>
    /// Service for immutable audit logging with cross-region aggregation
    /// </summary>
    public interface IAuditService
    {
        /// <summary>
        /// Log an audit event (immutable)
        /// </summary>
        Task<AuditLog> LogEventAsync(AuditEvent auditEvent);
        
        /// <summary>
        /// Query audit logs for an organization
        /// </summary>
        Task<List<AuditLog>> QueryAuditLogsAsync(AuditQuery query);
        
        /// <summary>
        /// Get audit log by ID
        /// </summary>
        Task<AuditLog> GetAuditLogAsync(Guid logId);
        
        /// <summary>
        /// Export audit logs for an organization
        /// </summary>
        Task<byte[]> ExportAuditLogsAsync(Guid organizationId, DateTime startDate, DateTime endDate);
        
        /// <summary>
        /// Verify audit log integrity (tamper detection)
        /// </summary>
        Task<bool> VerifyIntegrityAsync(Guid logId);
        
        /// <summary>
        /// Aggregate anonymized audit logs across regions (compliance)
        /// </summary>
        Task<AuditAggregationResult> AggregateAnonymizedLogsAsync(DateTime startDate, DateTime endDate);
    }
    
    public class AuditEvent
    {
        public Guid OrganizationId { get; set; }
        public Guid? UserId { get; set; }
        public string EventType { get; set; }
        public string EventCategory { get; set; }
        public string Action { get; set; }
        public string ResourceType { get; set; }
        public string ResourceId { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public string Region { get; set; }
    }
    
    public class AuditQuery
    {
        public Guid OrganizationId { get; set; }
        public Guid? UserId { get; set; }
        public string EventType { get; set; }
        public string EventCategory { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int PageSize { get; set; } = 50;
        public int PageNumber { get; set; } = 1;
    }
    
    public class AuditAggregationResult
    {
        public int TotalEvents { get; set; }
        public Dictionary<string, int> EventsByType { get; set; }
        public Dictionary<string, int> EventsByCategory { get; set; }
        public Dictionary<string, int> EventsByRegion { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
