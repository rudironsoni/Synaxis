// <copyright file="BackupConfig.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Organization backup configuration and settings.
    /// </summary>
    public class BackupConfig
    {
        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the organization identifier.
        /// </summary>
        [Required]
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the backup strategy: regional-only, cross-region.
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Strategy { get; set; } = "regional-only";

        /// <summary>
        /// Gets or sets the backup frequency: hourly, daily, weekly.
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Frequency { get; set; } = "daily";

        /// <summary>
        /// Gets or sets the hour of day for scheduled backups (0-23).
        /// </summary>
        public int ScheduleHour { get; set; } = 2;

        /// <summary>
        /// Gets or sets the target regions for cross-region backups.
        /// </summary>
        public IList<string> TargetRegions { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets a value indicating whether encryption for backups is enabled.
        /// </summary>
        public bool EnableEncryption { get; set; } = true;

        /// <summary>
        /// Gets or sets the encryption key ID (reference to key management service).
        /// </summary>
        public string EncryptionKeyId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the retention period in days.
        /// </summary>
        public int RetentionDays { get; set; } = 7;

        /// <summary>
        /// Gets or sets a value indicating whether PostgreSQL backups are enabled.
        /// </summary>
        public bool EnablePostgresBackup { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether Redis backups are enabled.
        /// </summary>
        public bool EnableRedisBackup { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether Qdrant backups are enabled.
        /// </summary>
        public bool EnableQdrantBackup { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the backup configuration is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the last update timestamp.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the last backup timestamp.
        /// </summary>
        public DateTime? LastBackupAt { get; set; }

        /// <summary>
        /// Gets or sets the last backup status.
        /// </summary>
        public string LastBackupStatus { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the organization navigation property.
        /// </summary>
        public virtual Organization Organization { get; set; }
    }
}
