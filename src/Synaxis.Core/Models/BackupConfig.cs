using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Synaxis.Core.Models
{
    /// <summary>
    /// Organization backup configuration and settings
    /// </summary>
    public class BackupConfig
    {
        public Guid Id { get; set; }
        
        [Required]
        public Guid OrganizationId { get; set; }
        
        /// <summary>
        /// Backup strategy: regional-only, cross-region
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Strategy { get; set; } = "regional-only";
        
        /// <summary>
        /// Backup frequency: hourly, daily, weekly
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Frequency { get; set; } = "daily";
        
        /// <summary>
        /// Hour of day for scheduled backups (0-23)
        /// </summary>
        public int ScheduleHour { get; set; } = 2;
        
        /// <summary>
        /// Target regions for cross-region backups
        /// </summary>
        public List<string> TargetRegions { get; set; } = new List<string>();
        
        /// <summary>
        /// Enable encryption for backups
        /// </summary>
        public bool EnableEncryption { get; set; } = true;
        
        /// <summary>
        /// Encryption key ID (reference to key management service)
        /// </summary>
        public string EncryptionKeyId { get; set; }
        
        /// <summary>
        /// Retention period in days
        /// </summary>
        public int RetentionDays { get; set; } = 7;
        
        /// <summary>
        /// Enable PostgreSQL backups
        /// </summary>
        public bool EnablePostgresBackup { get; set; } = true;
        
        /// <summary>
        /// Enable Redis backups
        /// </summary>
        public bool EnableRedisBackup { get; set; } = true;
        
        /// <summary>
        /// Enable Qdrant backups
        /// </summary>
        public bool EnableQdrantBackup { get; set; } = true;
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? LastBackupAt { get; set; }
        
        public string LastBackupStatus { get; set; }
        
        // Navigation property
        public virtual Organization Organization { get; set; }
    }
}
