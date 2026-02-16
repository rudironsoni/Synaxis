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
        /// <param name="context">The database context.</param>
        /// <param name="logger">The logger.</param>
        public BackupService(SynaxisDbContext context, ILogger<BackupService> logger)
        {
            this._context = context ?? throw new ArgumentNullException(nameof(context));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<BackupResult> ExecuteBackupAsync(Guid organizationId, BackupType backupType)
        {
            this._logger.LogInformation(
                "Starting backup for organization {OrganizationId}, type: {BackupType}",
                organizationId,
                backupType);

            var organization = await this._context.Organizations.FindAsync(organizationId).ConfigureAwait(false);
            if (organization == null)
            {
                throw new InvalidOperationException($"Organization {organizationId} not found");
            }

            var config = await this._context.Set<BackupConfig>()
                .FirstOrDefaultAsync(bc => bc.OrganizationId == organizationId && bc.IsActive)
                .ConfigureAwait(false);

            if (config == null)
            {
                throw new InvalidOperationException($"No active backup configuration for organization {organizationId}");
            }

            ValidateBackupTypeEnabled(backupType, config);

            try
            {
                return await this.ExecuteBackupSuccessAsync(organizationId, backupType, organization, config).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return await this.ExecuteBackupFailureAsync(organizationId, backupType, config, ex).ConfigureAwait(false);
            }
        }

        private async Task<BackupResult> ExecuteBackupSuccessAsync(Guid organizationId, BackupType type, Organization organization, BackupConfig config)
        {
            var backupId = $"backup_{organizationId}_{type}_{DateTime.UtcNow:yyyyMMddHHmmss}";
            var backupData = await ExecuteBackupByTypeAsync(organizationId, type, config).ConfigureAwait(false);

            if (config.EnableEncryption)
            {
                backupData = await this.EncryptBackupAsync(backupData, organizationId).ConfigureAwait(false);
            }

            await BackupService.StoreBackupAsync(backupId, organization.PrimaryRegion).ConfigureAwait(false);

            if (string.Equals(config.Strategy, "cross-region", StringComparison.Ordinal))
            {
                await this.ReplicateBackupAsync(backupId, config.TargetRegions).ConfigureAwait(false);
            }

            config.LastBackupAt = DateTime.UtcNow;
            config.LastBackupStatus = "success";
            config.UpdatedAt = DateTime.UtcNow;
            await this._context.SaveChangesAsync().ConfigureAwait(false);

            this._logger.LogInformation("Backup completed successfully: {BackupId}", backupId);

            return new BackupResult
            {
                BackupId = backupId,
                BackupType = type,
                OrganizationId = organizationId,
                SizeBytes = backupData.Length,
                Success = true,
                CreatedAt = DateTime.UtcNow,
            };
        }

        private async Task<BackupResult> ExecuteBackupFailureAsync(Guid organizationId, BackupType type, BackupConfig config, Exception ex)
        {
            this._logger.LogError(ex, "Backup failed for organization {OrganizationId}", organizationId);

            if (config != null)
            {
                config.LastBackupAt = DateTime.UtcNow;
                config.LastBackupStatus = $"failed: {ex.Message}";
                config.UpdatedAt = DateTime.UtcNow;
                await this._context.SaveChangesAsync().ConfigureAwait(false);
            }

            return new BackupResult
            {
                BackupId = string.Empty,
                BackupType = type,
                OrganizationId = organizationId,
                Success = false,
                ErrorMessage = ex.Message,
                CreatedAt = DateTime.UtcNow,
            };
        }

        private static void ValidateBackupTypeEnabled(BackupType backupType, BackupConfig config)
        {
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
        }

        private static async Task<byte[]> ExecuteBackupByTypeAsync(Guid organizationId, BackupType type, BackupConfig config)
        {
            return type switch
            {
                BackupType.PostgreSQL => await ExecutePostgreSQLBackupAsync(organizationId).ConfigureAwait(false),
                BackupType.Redis => await ExecuteRedisBackupAsync(organizationId).ConfigureAwait(false),
                BackupType.Qdrant => await ExecuteQdrantBackupAsync(organizationId).ConfigureAwait(false),
                BackupType.Full => await ExecuteFullBackupAsync(organizationId, config).ConfigureAwait(false),
                _ => throw new ArgumentException($"Unsupported backup type: {type}", nameof(type)),
            };
        }

        /// <inheritdoc/>
        public async Task<bool> RestoreBackupAsync(Guid organizationId, string backupId)
        {
            this._logger.LogInformation(
                "Starting restore for organization {OrganizationId}, backup: {BackupId}",
                organizationId,
                backupId);

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
            await RetrieveBackupAsync(backupId, metadata.PrimaryRegion).ConfigureAwait(false);

            // Decrypt if encrypted
            if (metadata.IsEncrypted)
            {
                await this.DecryptBackupAsync(Array.Empty<byte>(), organizationId).ConfigureAwait(false);
            }

            // Restore based on type
            bool success = metadata.BackupType switch
            {
                BackupType.PostgreSQL => await RestorePostgreSQLBackupAsync().ConfigureAwait(false),
                BackupType.Redis => await RestoreRedisBackupAsync().ConfigureAwait(false),
                BackupType.Qdrant => await RestoreQdrantBackupAsync().ConfigureAwait(false),
                BackupType.Full => await RestoreFullBackupAsync().ConfigureAwait(false),
                _ => throw new ArgumentException($"Unsupported backup type: {metadata.BackupType}", nameof(backupId)),
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
                throw new InvalidOperationException($"Backup {backupId} not found");
            }

            var orgId = Guid.Parse(parts[1]);

            // Check if org has encryption enabled in config
            var config = await this._context.Set<BackupConfig>()
                .FirstOrDefaultAsync(c => c.OrganizationId == orgId)
                .ConfigureAwait(false);

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
            await DeleteBackupFromStorageAsync().ConfigureAwait(false);

            // Delete from replicated regions
            foreach (var region in metadata.ReplicatedRegions)
            {
                await DeleteBackupFromStorageAsync().ConfigureAwait(false);
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
                .ToListAsync()
                .ConfigureAwait(false);

            int deletedCount = 0;

            foreach (var config in configs)
            {
                var backups = await this.ListBackupsAsync(config.OrganizationId).ConfigureAwait(false);
                var cutoffDate = DateTime.UtcNow.AddDays(-config.RetentionDays);

                var expiredBackupIds = backups.Where(b => b.CreatedAt < cutoffDate).Select(b => b.BackupId).ToList();
                foreach (var backupId in expiredBackupIds)
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
            var key = await GetOrCreateEncryptionKeyAsync(organizationId).ConfigureAwait(false);

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
            var key = await GetOrCreateEncryptionKeyAsync(organizationId).ConfigureAwait(false);

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
        private static async Task<byte[]> ExecutePostgreSQLBackupAsync(Guid organizationId)
        {
            // Simulated - in production would execute pg_dump
            await Task.CompletedTask.ConfigureAwait(false);
            var data = $"PostgreSQL backup for org {organizationId} at {DateTime.UtcNow}";
            return Encoding.UTF8.GetBytes(data);
        }

        private static async Task<byte[]> ExecuteRedisBackupAsync(Guid organizationId)
        {
            // Simulated - in production would trigger Redis RDB save
            await Task.CompletedTask.ConfigureAwait(false);
            var data = $"Redis backup for org {organizationId} at {DateTime.UtcNow}";
            return Encoding.UTF8.GetBytes(data);
        }

        private static async Task<byte[]> ExecuteQdrantBackupAsync(Guid organizationId)
        {
            // Simulated - in production would call Qdrant snapshot API
            await Task.CompletedTask.ConfigureAwait(false);
            var data = $"Qdrant backup for org {organizationId} at {DateTime.UtcNow}";
            return Encoding.UTF8.GetBytes(data);
        }

        private static async Task<byte[]> ExecuteFullBackupAsync(Guid organizationId, BackupConfig config)
        {
            var backups = new List<byte[]>();

            if (config.EnablePostgresBackup)
            {
                backups.Add(await ExecutePostgreSQLBackupAsync(organizationId).ConfigureAwait(false));
            }

            if (config.EnableRedisBackup)
            {
                backups.Add(await ExecuteRedisBackupAsync(organizationId).ConfigureAwait(false));
            }

            if (config.EnableQdrantBackup)
            {
                backups.Add(await ExecuteQdrantBackupAsync(organizationId).ConfigureAwait(false));
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

        private static async Task<string> StoreBackupAsync(string backupId, string region)
        {
            // Simulated - in production would upload to S3/GCS/Azure Blob
            await Task.CompletedTask.ConfigureAwait(false);
            return $"s3://{region}/backups/{backupId}";
        }

        private async Task ReplicateBackupAsync(string backupId, IList<string> targetRegions)
        {
            foreach (var region in targetRegions)
            {
                try
                {
                    await StoreBackupAsync(backupId, region).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    this._logger.LogWarning(ex, "Failed to replicate backup to region {Region}", region);
                }
            }
        }

        private static async Task<byte[]> RetrieveBackupAsync(string backupId, string region)
        {
            // Simulated - in production would download from object storage
            await Task.CompletedTask.ConfigureAwait(false);
            var data = $"Retrieved backup {backupId} from {region}";
            return Encoding.UTF8.GetBytes(data);
        }

        private static async Task<bool> RestoreBackupInternalAsync()
        {
            // Simulated - in production would execute restore based on backup type
            await Task.CompletedTask.ConfigureAwait(false);
            return true;
        }

        private static Task<bool> RestorePostgreSQLBackupAsync()
        {
            // Simulated - in production would execute pg_restore
            return RestoreBackupInternalAsync();
        }

        private static Task<bool> RestoreRedisBackupAsync()
        {
            // Simulated - in production would restore Redis RDB
            return RestoreBackupInternalAsync();
        }

        private static Task<bool> RestoreQdrantBackupAsync()
        {
            // Simulated - in production would restore Qdrant snapshot
            return RestoreBackupInternalAsync();
        }

        private static Task<bool> RestoreFullBackupAsync()
        {
            // Simulated - in production would restore all components
            return RestoreBackupInternalAsync();
        }

        private static Task DeleteBackupFromStorageAsync()
        {
            // Simulated - in production would delete from object storage
            return Task.CompletedTask;
        }

        private static async Task<byte[]> GetOrCreateEncryptionKeyAsync(Guid organizationId)
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
