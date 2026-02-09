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

namespace Synaxis.Infrastructure.Services
{
    /// <summary>
    /// Service for managing encrypted backups with configurable strategies.
    /// </summary>
    public class BackupService : IBackupService
    {
        private readonly SynaxisDbContext _context;
        private readonly ILogger<BackupService> _logger;

        public BackupService(SynaxisDbContext context, ILogger<BackupService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<BackupResult> ExecuteBackupAsync(Guid organizationId, BackupType backupType)
        {
            _logger.LogInformation("Starting backup for organization {OrganizationId}, type: {BackupType}",
                organizationId, backupType);

            var organization = await _context.Organizations.FindAsync(organizationId);
            if (organization == null)
            {
                throw new InvalidOperationException($"Organization {organizationId} not found");
            }

            var config = await _context.Set<BackupConfig>()
                .FirstOrDefaultAsync(bc => bc.OrganizationId == organizationId && bc.IsActive);

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
                        backupData = await ExecutePostgreSQLBackupAsync(organizationId);
                        break;

                    case BackupType.Redis:
                        backupData = await ExecuteRedisBackupAsync(organizationId);
                        break;

                    case BackupType.Qdrant:
                        backupData = await ExecuteQdrantBackupAsync(organizationId);
                        break;

                    case BackupType.Full:
                        backupData = await ExecuteFullBackupAsync(organizationId, config);
                        break;

                    default:
                        throw new ArgumentException($"Unsupported backup type: {backupType}");
                }

                // Encrypt if enabled
                if (config.EnableEncryption)
                {
                    backupData = await EncryptBackupAsync(backupData, organizationId);
                }

                // Store backup (simulated - in production would save to object storage)
                var storagePath = await StoreBackupAsync(backupId, backupData, organization.PrimaryRegion);

                // Replicate to target regions if cross-region strategy
                var replicatedRegions = new List<string>();
                if (config.Strategy == "cross-region")
                {
                    replicatedRegions = await ReplicateBackupAsync(backupId, backupData, config.TargetRegions);
                }

                // Update config
                config.LastBackupAt = DateTime.UtcNow;
                config.LastBackupStatus = "success";
                config.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Backup completed successfully: {BackupId}", backupId);

                return new BackupResult
                {
                    BackupId = backupId,
                    BackupType = backupType,
                    OrganizationId = organizationId,
                    SizeBytes = backupData.Length,
                    Success = true,
                    CreatedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Backup failed for organization {OrganizationId}", organizationId);

                // Update config
                if (config != null)
                {
                    config.LastBackupAt = DateTime.UtcNow;
                    config.LastBackupStatus = $"failed: {ex.Message}";
                    config.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                return new BackupResult
                {
                    BackupId = null,
                    BackupType = backupType,
                    OrganizationId = organizationId,
                    Success = false,
                    ErrorMessage = ex.Message,
                    CreatedAt = DateTime.UtcNow
                };
            }
        }

        public async Task<bool> RestoreBackupAsync(Guid organizationId, string backupId)
        {
            _logger.LogInformation("Starting restore for organization {OrganizationId}, backup: {BackupId}",
                organizationId, backupId);

            var metadata = await GetBackupMetadataAsync(backupId);
            if (metadata == null)
            {
                throw new InvalidOperationException($"Backup {backupId} not found");
            }

            if (metadata.OrganizationId != organizationId)
            {
                throw new UnauthorizedAccessException("Backup does not belong to this organization");
            }

            // Retrieve backup data (simulated)
            var backupData = await RetrieveBackupAsync(backupId, metadata.PrimaryRegion);

            // Decrypt if encrypted
            if (metadata.IsEncrypted)
            {
                backupData = await DecryptBackupAsync(backupData, organizationId);
            }

            // Restore based on type
            bool success = metadata.BackupType switch
            {
                BackupType.PostgreSQL => await RestorePostgreSQLBackupAsync(organizationId, backupData),
                BackupType.Redis => await RestoreRedisBackupAsync(organizationId, backupData),
                BackupType.Qdrant => await RestoreQdrantBackupAsync(organizationId, backupData),
                BackupType.Full => await RestoreFullBackupAsync(organizationId, backupData),
                _ => throw new ArgumentException($"Unsupported backup type: {metadata.BackupType}")
            };

            _logger.LogInformation("Restore completed for organization {OrganizationId}: {Success}",
                organizationId, success);

            return success;
        }

        public async Task<IList<BackupMetadata>> ListBackupsAsync(Guid organizationId)
        {
            // Simulated - in production would query from metadata store
            await Task.CompletedTask;
            return new List<BackupMetadata>();
        }

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
            var config = await _context.Set<BackupConfig>()
                .FirstOrDefaultAsync(c => c.OrganizationId == orgId);

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
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };
        }

        public async Task<bool> DeleteBackupAsync(string backupId)
        {
            _logger.LogInformation("Deleting backup: {BackupId}", backupId);

            var metadata = await GetBackupMetadataAsync(backupId);
            if (metadata == null)
            {
                return false;
            }

            // Delete from primary region
            await DeleteBackupFromStorageAsync(backupId, metadata.PrimaryRegion);

            // Delete from replicated regions
            foreach (var region in metadata.ReplicatedRegions)
            {
                await DeleteBackupFromStorageAsync(backupId, region);
            }

            return true;
        }

        public async Task<int> EnforceRetentionPoliciesAsync()
        {
            _logger.LogInformation("Enforcing retention policies");

            var configs = await _context.Set<BackupConfig>()
                .Where(bc => bc.IsActive)
                .Include(bc => bc.Organization)
                .ToListAsync();

            int deletedCount = 0;

            foreach (var config in configs)
            {
                var backups = await ListBackupsAsync(config.OrganizationId);
                var cutoffDate = DateTime.UtcNow.AddDays(-config.RetentionDays);

                foreach (var backup in backups.Where(b => b.CreatedAt < cutoffDate))
                {
                    if (await DeleteBackupAsync(backup.BackupId))
                    {
                        deletedCount++;
                        _logger.LogInformation("Deleted expired backup: {BackupId}", backup.BackupId);
                    }
                }
            }

            _logger.LogInformation("Retention policy enforcement complete. Deleted {Count} backups", deletedCount);
            return deletedCount;
        }

        public async Task<byte[]> EncryptBackupAsync(byte[] data, Guid organizationId)
        {
            if (data == null || data.Length == 0)
            {
                throw new ArgumentException("Data cannot be empty", nameof(data));
            }

            // Get or create organization encryption key
            var key = await GetOrCreateEncryptionKeyAsync(organizationId);

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

        public async Task<byte[]> DecryptBackupAsync(byte[] encryptedData, Guid organizationId)
        {
            if (encryptedData == null || encryptedData.Length == 0)
            {
                throw new ArgumentException("Encrypted data cannot be empty", nameof(encryptedData));
            }

            // Get organization encryption key
            var key = await GetOrCreateEncryptionKeyAsync(organizationId);

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
            await Task.CompletedTask;
            var data = $"PostgreSQL backup for org {organizationId} at {DateTime.UtcNow}";
            return Encoding.UTF8.GetBytes(data);
        }

        private async Task<byte[]> ExecuteRedisBackupAsync(Guid organizationId)
        {
            // Simulated - in production would trigger Redis RDB save
            await Task.CompletedTask;
            var data = $"Redis backup for org {organizationId} at {DateTime.UtcNow}";
            return Encoding.UTF8.GetBytes(data);
        }

        private async Task<byte[]> ExecuteQdrantBackupAsync(Guid organizationId)
        {
            // Simulated - in production would call Qdrant snapshot API
            await Task.CompletedTask;
            var data = $"Qdrant backup for org {organizationId} at {DateTime.UtcNow}";
            return Encoding.UTF8.GetBytes(data);
        }

        private async Task<byte[]> ExecuteFullBackupAsync(Guid organizationId, BackupConfig config)
        {
            var backups = new List<byte[]>();

            if (config.EnablePostgresBackup)
            {
                backups.Add(await ExecutePostgreSQLBackupAsync(organizationId));
            }

            if (config.EnableRedisBackup)
            {
                backups.Add(await ExecuteRedisBackupAsync(organizationId));
            }

            if (config.EnableQdrantBackup)
            {
                backups.Add(await ExecuteQdrantBackupAsync(organizationId));
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

        private async Task<string> StoreBackupAsync(string backupId, byte[] data, string region)
        {
            // Simulated - in production would upload to S3/GCS/Azure Blob
            await Task.CompletedTask;
            return $"s3://{region}/backups/{backupId}";
        }

        private async Task<List<string>> ReplicateBackupAsync(string backupId, byte[] data, IList<string> targetRegions)
        {
            var replicatedRegions = new List<string>();

            foreach (var region in targetRegions)
            {
                try
                {
                    await StoreBackupAsync(backupId, data, region);
                    replicatedRegions.Add(region);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to replicate backup to region {Region}", region);
                }
            }

            return replicatedRegions;
        }

        private async Task<byte[]> RetrieveBackupAsync(string backupId, string region)
        {
            // Simulated - in production would download from object storage
            await Task.CompletedTask;
            var data = $"Retrieved backup {backupId} from {region}";
            return Encoding.UTF8.GetBytes(data);
        }

        private async Task<bool> RestorePostgreSQLBackupAsync(Guid organizationId, byte[] backupData)
        {
            // Simulated - in production would execute pg_restore
            await Task.CompletedTask;
            return true;
        }

        private async Task<bool> RestoreRedisBackupAsync(Guid organizationId, byte[] backupData)
        {
            // Simulated - in production would restore Redis RDB
            await Task.CompletedTask;
            return true;
        }

        private async Task<bool> RestoreQdrantBackupAsync(Guid organizationId, byte[] backupData)
        {
            // Simulated - in production would restore Qdrant snapshot
            await Task.CompletedTask;
            return true;
        }

        private async Task<bool> RestoreFullBackupAsync(Guid organizationId, byte[] backupData)
        {
            // Simulated - in production would restore all components
            await Task.CompletedTask;
            return true;
        }

        private async Task DeleteBackupFromStorageAsync(string backupId, string region)
        {
            // Simulated - in production would delete from object storage
            await Task.CompletedTask;
        }

        private async Task<byte[]> GetOrCreateEncryptionKeyAsync(Guid organizationId)
        {
            // Simulated - in production would use KMS (AWS KMS, Azure Key Vault, etc.)
            // For now, generate deterministic key from organization ID
            await Task.CompletedTask;

            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes($"backup-key-{organizationId}"));
                return hash; // 256-bit key
            }
        }
    }
}
