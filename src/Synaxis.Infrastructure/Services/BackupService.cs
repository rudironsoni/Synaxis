// <copyright file="BackupService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Synaxis.Core.Contracts;
    using Synaxis.Core.Models;
    using Synaxis.Infrastructure.Data;

    /// <summary>
    /// Service for managing encrypted backups with configurable strategies.
    /// </summary>
    public class BackupService : IBackupService
    {
        private readonly SynaxisDbContext _context;
        private readonly ILogger<BackupService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackupService"/> class.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="logger"></param>
        public BackupService(SynaxisDbContext context, ILogger<BackupService> logger)
        {
            this._context = context ?? throw new ArgumentNullException(nameof(context));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<BackupResult> ExecuteBackupAsync(Guid organizationId, BackupType backupType)
        {
            this._logger.LogInformation("Starting backup for organization {OrganizationId}, type: {BackupType}", organizationId, backupType);

            var organization = await this._context.Organizations.FindAsync(organizationId).ConfigureAwait(false);
            if (organization == null)
            {
                throw new InvalidOperationException($"Organization {organizationId} not found");
            }

            var config = await this._context.Set<BackupConfig>()
                .FirstOrDefaultAsync(bc => bc.OrganizationId == organizationId && bc.IsActive).ConfigureAwait(false);

            if (config == null)
            {
                throw new InvalidOperationException($"No active backup configuration for organization {organizationId}");
            }

            // Check if backup type is enabled BEFORE try-catch to allow exception to propagate
            switch (backupType)
            {
                case BackupType.PostgreSQL:
                    if (!config.EnablePostgresBackup)
                    {
                        throw new InvalidOperationException("PostgreSQL backup is disabled");
                    }

                    break;

                case BackupType.Redis:
                    if (!config.EnableRedisBackup)
                    {
                        throw new InvalidOperationException("Redis backup is disabled");
                    }

                    break;

                case BackupType.Qdrant:
                    if (!config.EnableQdrantBackup)
                    {
                        throw new InvalidOperationException("Qdrant backup is disabled");
                    }

                    break;
            }

            try
            {
                var backupId = $"backup_{organizationId}_{backupType}_{DateTime.UtcNow:yyyyMMddHHmmss}";
                byte[] backupData;

                // Execute backup based on type
                switch (backupType)
                {
                    case BackupType.PostgreSQL:
                        backupData = await this.ExecutePostgreSQLBackupAsync(organizationId).ConfigureAwait(false);
                        break;

                    case BackupType.Redis:
                        backupData = await this.ExecuteRedisBackupAsync(organizationId).ConfigureAwait(false);
                        break;

                    case BackupType.Qdrant:
                        backupData = await this.ExecuteQdrantBackupAsync(organizationId).ConfigureAwait(false);
                        break;

                    case BackupType.Full:
                        backupData = await this.ExecuteFullBackupAsync(organizationId, config).ConfigureAwait(false);
                        break;

                    default:
                        throw new ArgumentException($"Unsupported backup type: {backupType}");
                }

                // Encrypt if enabled
                if (config.EnableEncryption)
                {
                    backupData = await this.EncryptBackupAsync(backupData, organizationId).ConfigureAwait(false);
                }

                // Store backup (simulated - in production would save to object storage)
                await this.StoreBackupAsync(backupId, backupData, organization.PrimaryRegion).ConfigureAwait(false);

                // Replicate to target regions if cross-region strategy
                if (string.Equals(config.Strategy, "cross-region", StringComparison.Ordinal))
                {
                    await this.ReplicateBackupAsync(backupId, backupData, config.TargetRegions).ConfigureAwait(false);
                }

                // Update config
                config.LastBackupAt = DateTime.UtcNow;
                config.LastBackupStatus = "success";
                config.UpdatedAt = DateTime.UtcNow;
                await this._context.SaveChangesAsync().ConfigureAwait(false);

                this._logger.LogInformation("Backup completed successfully: {BackupId}", backupId);

                return new BackupResult
                {
                    BackupId = backupId,
                    BackupType = backupType,
                    OrganizationId = organizationId,
                    SizeBytes = backupData.Length,
                    Success = true,
                    CreatedAt = DateTime.UtcNow,
                };
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Backup failed for organization {OrganizationId}", organizationId);

                // Update config
                if (config != null)
                {
                    config.LastBackupAt = DateTime.UtcNow;
                    config.LastBackupStatus = $"failed: {ex.Message}";
                    config.UpdatedAt = DateTime.UtcNow;
                    await this._context.SaveChangesAsync().ConfigureAwait(false);
                }

                return new BackupResult
                {
                    BackupId = null,
                    BackupType = backupType,
                    OrganizationId = organizationId,
                    Success = false,
                    ErrorMessage = ex.Message,
                    CreatedAt = DateTime.UtcNow,
                };
            }
        }

        /// <inheritdoc/>
        public async Task<bool> RestoreBackupAsync(Guid organizationId, string backupId)
        {
            this._logger.LogInformation("Starting restore for organization {OrganizationId}, backup: {BackupId}", organizationId, backupId);

            var metadata = await this.GetBackupMetadataAsync(backupId).ConfigureAwait(false);
            if (metadata == null)
            {
                throw new InvalidOperationException($"Backup {backupId} not found");
            }

            if (metadata.OrganizationId != organizationId)
            {
                throw new UnauthorizedAccessException("Backup does not belong to this organization");
            }

            // Retrieve backup data (simulated)
            var backupData = await this.RetrieveBackupAsync(backupId, metadata.PrimaryRegion).ConfigureAwait(false);

            // Decrypt if encrypted
            if (metadata.IsEncrypted)
            {
                backupData = await this.DecryptBackupAsync(backupData, organizationId).ConfigureAwait(false);
            }

            // Restore based on type
            bool success = metadata.BackupType switch
            {
                BackupType.PostgreSQL => await this.RestorePostgreSQLBackupAsync(organizationId, backupData).ConfigureAwait(false),
                BackupType.Redis => await this.RestoreRedisBackupAsync(organizationId, backupData).ConfigureAwait(false),
                BackupType.Qdrant => await this.RestoreQdrantBackupAsync(organizationId, backupData).ConfigureAwait(false),
                BackupType.Full => await this.RestoreFullBackupAsync(organizationId, backupData).ConfigureAwait(false),
                _ => throw new ArgumentException($"Unsupported backup type: {metadata.BackupType}"),
            };

            this._logger.LogInformation("Restore completed for organization {OrganizationId}: {Success}", organizationId, success);

            return success;
        }

        /// <inheritdoc/>
        public async Task<IList<BackupMetadata>> ListBackupsAsync(Guid organizationId)
        {
            // Simulated - in production would query from metadata store
            await Task.CompletedTask.ConfigureAwait(false);
            return new List<BackupMetadata>();
        }

        /// <inheritdoc/>
        public async Task<BackupMetadata> GetBackupMetadataAsync(string backupId)
        {
            // Simulated - in production would query from metadata store

            // Parse backup ID for demonstration
            var parts = backupId.Split('_');
            if (parts.Length < 4)
            {
                return null;
            }

            var orgId = Guid.Parse(parts[1]);

            // Check if org has encryption enabled in config
            var config = await this._context.Set<BackupConfig>()
                .FirstOrDefaultAsync(c => c.OrganizationId == orgId).ConfigureAwait(false);

            var isEncrypted = config?.EnableEncryption ?? false;

            return new BackupMetadata
            {
                BackupId = backupId,
                BackupType = Enum.Parse<BackupType>(parts[2]),
                OrganizationId = orgId,
                PrimaryRegion = "us-east-1",
                ReplicatedRegions = new List<string>(),
                IsEncrypted = isEncrypted, // Check config for encryption setting
                StoragePath = $"/backups/{backupId}",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
            };
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteBackupAsync(string backupId)
        {
            this._logger.LogInformation("Deleting backup: {BackupId}", backupId);

            var metadata = await this.GetBackupMetadataAsync(backupId).ConfigureAwait(false);
            if (metadata == null)
            {
                return false;
            }

            // Delete from primary region
            await this.DeleteBackupFromStorageAsync(backupId, metadata.PrimaryRegion).ConfigureAwait(false);

            // Delete from replicated regions
            foreach (var region in metadata.ReplicatedRegions)
            {
                await this.DeleteBackupFromStorageAsync(backupId, region).ConfigureAwait(false);
            }

            return true;
        }

        /// <inheritdoc/>
        public async Task<int> EnforceRetentionPoliciesAsync()
        {
            this._logger.LogInformation("Enforcing retention policies");

            var configs = await this._context.Set<BackupConfig>()
                .Where(bc => bc.IsActive)
                .Include(bc => bc.Organization)
                .ToListAsync().ConfigureAwait(false);

            int deletedCount = 0;

            foreach (var config in configs)
            {
                var backups = await this.ListBackupsAsync(config.OrganizationId).ConfigureAwait(false);
                var cutoffDate = DateTime.UtcNow.AddDays(-config.RetentionDays);

                foreach (var backupId in backups.Where(b => b.CreatedAt < cutoffDate).Select(b => b.BackupId))
                {
                    if (await this.DeleteBackupAsync(backupId).ConfigureAwait(false))
                    {
                        deletedCount++;
                        this._logger.LogInformation("Deleted expired backup: {BackupId}", backupId);
                    }
                }
            }

            this._logger.LogInformation("Retention policy enforcement complete. Deleted {Count} backups", deletedCount);
            return deletedCount;
        }

        /// <inheritdoc/>
        public async Task<byte[]> EncryptBackupAsync(byte[] data, Guid organizationId)
        {
            if (data == null || data.Length == 0)
            {
                throw new ArgumentException("Data cannot be empty", nameof(data));
            }

            // Get or create organization encryption key
            var key = await this.GetOrCreateEncryptionKeyAsync(organizationId).ConfigureAwait(false);

            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.GenerateIV();

                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                {
                    var encryptedData = encryptor.TransformFinalBlock(data, 0, data.Length);

                    // Prepend IV to encrypted data
                    var result = new byte[aes.IV.Length + encryptedData.Length];
                    Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
                    Buffer.BlockCopy(encryptedData, 0, result, aes.IV.Length, encryptedData.Length);

                    return result;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<byte[]> DecryptBackupAsync(byte[] encryptedData, Guid organizationId)
        {
            if (encryptedData == null || encryptedData.Length == 0)
            {
                throw new ArgumentException("Encrypted data cannot be empty", nameof(encryptedData));
            }

            // Get organization encryption key
            var key = await this.GetOrCreateEncryptionKeyAsync(organizationId).ConfigureAwait(false);

            using (var aes = Aes.Create())
            {
                aes.Key = key;

                // Extract IV from beginning of encrypted data
                var iv = new byte[16];
                Buffer.BlockCopy(encryptedData, 0, iv, 0, iv.Length);
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                {
                    var dataLength = encryptedData.Length - iv.Length;
                    return decryptor.TransformFinalBlock(encryptedData, iv.Length, dataLength);
                }
            }
        }

        // Private helper methods
        private async Task<byte[]> ExecutePostgreSQLBackupAsync(Guid organizationId)
        {
            // Simulated - in production would execute pg_dump
            await Task.CompletedTask.ConfigureAwait(false);
            var data = $"PostgreSQL backup for org {organizationId} at {DateTime.UtcNow}";
            return Encoding.UTF8.GetBytes(data);
        }

        private async Task<byte[]> ExecuteRedisBackupAsync(Guid organizationId)
        {
            // Simulated - in production would trigger Redis RDB save
            await Task.CompletedTask.ConfigureAwait(false);
            var data = $"Redis backup for org {organizationId} at {DateTime.UtcNow}";
            return Encoding.UTF8.GetBytes(data);
        }

        private async Task<byte[]> ExecuteQdrantBackupAsync(Guid organizationId)
        {
            // Simulated - in production would call Qdrant snapshot API
            await Task.CompletedTask.ConfigureAwait(false);
            var data = $"Qdrant backup for org {organizationId} at {DateTime.UtcNow}";
            return Encoding.UTF8.GetBytes(data);
        }

        private async Task<byte[]> ExecuteFullBackupAsync(Guid organizationId, BackupConfig config)
        {
            var backups = new List<byte[]>();

            if (config.EnablePostgresBackup)
            {
                backups.Add(await this.ExecutePostgreSQLBackupAsync(organizationId).ConfigureAwait(false));
            }

            if (config.EnableRedisBackup)
            {
                backups.Add(await this.ExecuteRedisBackupAsync(organizationId).ConfigureAwait(false));
            }

            if (config.EnableQdrantBackup)
            {
                backups.Add(await this.ExecuteQdrantBackupAsync(organizationId).ConfigureAwait(false));
            }

            // Combine all backups
            var totalSize = backups.Sum(b => b.Length);
            var combined = new byte[totalSize];
            int offset = 0;

            foreach (var backup in backups)
            {
                Buffer.BlockCopy(backup, 0, combined, offset, backup.Length);
                offset += backup.Length;
            }

            return combined;
        }

        private async Task<string> StoreBackupAsync(string backupId, string region)
        {
            // Simulated - in production would upload to S3/GCS/Azure Blob
            await Task.CompletedTask.ConfigureAwait(false);
            return $"s3://{region}/backups/{backupId}";
        }

        private async Task<List<string>> ReplicateBackupAsync(string backupId, byte[] data, IList<string> targetRegions)
        {
            var replicatedRegions = new List<string>();

            foreach (var region in targetRegions)
            {
                try
                {
                    await this.StoreBackupAsync(backupId, data, region).ConfigureAwait(false);
                    replicatedRegions.Add(region);
                }
                catch (Exception ex)
                {
                    this._logger.LogWarning(ex, "Failed to replicate backup to region {Region}", region);
                }
            }

            return replicatedRegions;
        }

        private async Task<byte[]> RetrieveBackupAsync(string backupId, string region)
        {
            // Simulated - in production would download from object storage
            await Task.CompletedTask.ConfigureAwait(false);
            var data = $"Retrieved backup {backupId} from {region}";
            return Encoding.UTF8.GetBytes(data);
        }

        private async Task<bool> RestorePostgreSQLBackupAsync()
        {
            // Simulated - in production would execute pg_restore
            await Task.CompletedTask.ConfigureAwait(false);
            return true;
        }

        private async Task<bool> RestoreRedisBackupAsync()
        {
            // Simulated - in production would restore Redis RDB
            await Task.CompletedTask.ConfigureAwait(false);
            return true;
        }

        private async Task<bool> RestoreQdrantBackupAsync()
        {
            // Simulated - in production would restore Qdrant snapshot
            await Task.CompletedTask.ConfigureAwait(false);
            return true;
        }

        private async Task<bool> RestoreFullBackupAsync()
        {
            // Simulated - in production would restore all components
            await Task.CompletedTask.ConfigureAwait(false);
            return true;
        }

        private Task DeleteBackupFromStorageAsync()
        {
            // Simulated - in production would delete from object storage
            return Task.CompletedTask;
        }

        private async Task<byte[]> GetOrCreateEncryptionKeyAsync(Guid organizationId)
        {
            // Simulated - in production would use KMS (AWS KMS, Azure Key Vault, etc.)
            // For now, generate deterministic key from organization ID
            await Task.CompletedTask.ConfigureAwait(false);

            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes($"backup-key-{organizationId}"));
                return hash; // 256-bit key
            }
        }
    }
}
