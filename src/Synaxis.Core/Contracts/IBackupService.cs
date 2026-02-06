using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Synaxis.Core.Models;

namespace Synaxis.Core.Contracts
{
    /// <summary>
    /// Service for managing encrypted backups with configurable strategies
    /// </summary>
    public interface IBackupService
    {
        /// <summary>
        /// Execute backup for an organization
        /// </summary>
        Task<BackupResult> ExecuteBackupAsync(Guid organizationId, BackupType backupType);
        
        /// <summary>
        /// Restore backup for an organization
        /// </summary>
        Task<bool> RestoreBackupAsync(Guid organizationId, string backupId);
        
        /// <summary>
        /// List backups for an organization
        /// </summary>
        Task<List<BackupMetadata>> ListBackupsAsync(Guid organizationId);
        
        /// <summary>
        /// Get backup metadata
        /// </summary>
        Task<BackupMetadata> GetBackupMetadataAsync(string backupId);
        
        /// <summary>
        /// Delete backup
        /// </summary>
        Task<bool> DeleteBackupAsync(string backupId);
        
        /// <summary>
        /// Enforce retention policies (delete expired backups)
        /// </summary>
        Task<int> EnforceRetentionPoliciesAsync();
        
        /// <summary>
        /// Encrypt backup data with organization key
        /// </summary>
        Task<byte[]> EncryptBackupAsync(byte[] data, Guid organizationId);
        
        /// <summary>
        /// Decrypt backup data with organization key
        /// </summary>
        Task<byte[]> DecryptBackupAsync(byte[] encryptedData, Guid organizationId);
    }
    
    public enum BackupType
    {
        PostgreSQL,
        Redis,
        Qdrant,
        Full
    }
    
    public class BackupResult
    {
        public string BackupId { get; set; }
        public BackupType BackupType { get; set; }
        public Guid OrganizationId { get; set; }
        public long SizeBytes { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    
    public class BackupMetadata
    {
        public string BackupId { get; set; }
        public BackupType BackupType { get; set; }
        public Guid OrganizationId { get; set; }
        public string PrimaryRegion { get; set; }
        public List<string> ReplicatedRegions { get; set; }
        public long SizeBytes { get; set; }
        public bool IsEncrypted { get; set; }
        public string StoragePath { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
