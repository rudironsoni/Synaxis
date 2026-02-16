// <copyright file="IBackupService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Synaxis.Core.Models;

    /// <summary>
    /// Service for managing encrypted backups with configurable strategies.
    /// </summary>
    public interface IBackupService
    {
        /// <summary>
        /// Execute backup for an organization.
        /// </summary>
        /// <param name="organizationId">The unique identifier of the organization.</param>
        /// <param name="backupType">The type of backup to execute.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the backup result.</returns>
        Task<BackupResult> ExecuteBackupAsync(Guid organizationId, BackupType backupType);

        /// <summary>
        /// Restore backup for an organization.
        /// </summary>
        /// <param name="organizationId">The unique identifier of the organization.</param>
        /// <param name="backupId">The unique identifier of the backup to restore.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether the restore was successful.</returns>
        Task<bool> RestoreBackupAsync(Guid organizationId, string backupId);

        /// <summary>
        /// List backups for an organization.
        /// </summary>
        /// <param name="organizationId">The unique identifier of the organization.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the list of backup metadata.</returns>
        Task<IList<BackupMetadata>> ListBackupsAsync(Guid organizationId);

        /// <summary>
        /// Get backup metadata.
        /// </summary>
        /// <param name="backupId">The unique identifier of the backup.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the backup metadata.</returns>
        Task<BackupMetadata> GetBackupMetadataAsync(string backupId);

        /// <summary>
        /// Delete backup.
        /// </summary>
        /// <param name="backupId">The unique identifier of the backup to delete.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether the deletion was successful.</returns>
        Task<bool> DeleteBackupAsync(string backupId);

        /// <summary>
        /// Enforce retention policies (delete expired backups).
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the number of backups deleted.</returns>
        Task<int> EnforceRetentionPoliciesAsync();

        /// <summary>
        /// Encrypt backup data with organization key.
        /// </summary>
        /// <param name="data">The data to encrypt.</param>
        /// <param name="organizationId">The unique identifier of the organization.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the encrypted data.</returns>
        Task<byte[]> EncryptBackupAsync(byte[] data, Guid organizationId);

        /// <summary>
        /// Decrypt backup data with organization key.
        /// </summary>
        /// <param name="encryptedData">The encrypted data to decrypt.</param>
        /// <param name="organizationId">The unique identifier of the organization.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the decrypted data.</returns>
        Task<byte[]> DecryptBackupAsync(byte[] encryptedData, Guid organizationId);
    }

    /// <summary>
    /// Represents the type of backup to execute.
    /// </summary>
    public enum BackupType
    {
        /// <summary>
        /// PostgreSQL database backup.
        /// </summary>
        PostgreSQL,

        /// <summary>
        /// Redis cache backup.
        /// </summary>
        Redis,

        /// <summary>
        /// Qdrant vector database backup.
        /// </summary>
        Qdrant,

        /// <summary>
        /// Full backup of all data stores.
        /// </summary>
        Full,
    }

    /// <summary>
    /// Represents the result of a backup operation.
    /// </summary>
    public class BackupResult
    {
        /// <summary>
        /// Gets or sets the backup identifier.
        /// </summary>
        public required string BackupId { get; set; }

        /// <summary>
        /// Gets or sets the backup type.
        /// </summary>
        public BackupType BackupType { get; set; }

        /// <summary>
        /// Gets or sets the organization identifier.
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the backup size in bytes.
        /// </summary>
        public long SizeBytes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the backup was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the error message if backup failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the backup was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Represents metadata for a backup.
    /// </summary>
    public class BackupMetadata
    {
        /// <summary>
        /// Gets or sets the backup identifier.
        /// </summary>
        public required string BackupId { get; set; }

        /// <summary>
        /// Gets or sets the backup type.
        /// </summary>
        public BackupType BackupType { get; set; }

        /// <summary>
        /// Gets or sets the organization identifier.
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the primary region.
        /// </summary>
        public required string PrimaryRegion { get; set; }

        /// <summary>
        /// Gets or sets the list of replicated regions.
        /// </summary>
        public required IList<string> ReplicatedRegions { get; set; }

        /// <summary>
        /// Gets or sets the backup size in bytes.
        /// </summary>
        public long SizeBytes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the backup is encrypted.
        /// </summary>
        public bool IsEncrypted { get; set; }

        /// <summary>
        /// Gets or sets the storage path of the backup.
        /// </summary>
        public required string StoragePath { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the backup was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the backup expires.
        /// </summary>
        public DateTime ExpiresAt { get; set; }
    }
}
