using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Synaxis.Core.Models
{
    /// <summary>
    /// Immutable audit log entry for compliance and security
    /// </summary>
    public class AuditLog
    {
        public Guid Id { get; set; }
        
        [Required]
        public Guid OrganizationId { get; set; }
        
        public Guid? UserId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string EventType { get; set; }
        
        [Required]
        [StringLength(100)]
        public string EventCategory { get; set; }
        
        [Required]
        [StringLength(255)]
        public string Action { get; set; }
        
        [StringLength(100)]
        public string ResourceType { get; set; }
        
        [StringLength(255)]
        public string ResourceId { get; set; }
        
        /// <summary>
        /// Additional event metadata as JSON
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        
        [StringLength(45)]
        public string IpAddress { get; set; }
        
        [StringLength(500)]
        public string UserAgent { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Region { get; set; }
        
        /// <summary>
        /// Cryptographic hash for tamper detection
        /// </summary>
        [Required]
        [StringLength(128)]
        public string IntegrityHash { get; set; }
        
        /// <summary>
        /// Hash of previous log entry for chain verification
        /// </summary>
        [StringLength(128)]
        public string PreviousHash { get; set; }
        
        /// <summary>
        /// Timestamp when event occurred (immutable)
        /// </summary>
        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual Organization Organization { get; set; }
        public virtual User User { get; set; }
    }
}
