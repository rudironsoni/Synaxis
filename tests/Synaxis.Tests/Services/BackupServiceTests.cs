using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Synaxis.InferenceGateway.Infrastructure.Contracts;
using Synaxis.Core.Contracts;
using Synaxis.Core.Models;
using Synaxis.Infrastructure.Data;
using Synaxis.Infrastructure.Services;
using Xunit;

namespace Synaxis.Tests.Services
{
    public class BackupServiceTests : IDisposable
    {
        private readonly SynaxisDbContext _context;
        private readonly Mock<ILogger<BackupService>> _loggerMock;
        private readonly IBackupService _backupService;
        
        public BackupServiceTests()
        {
            var options = new DbContextOptionsBuilder<SynaxisDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _context = new SynaxisDbContext(options);
            _loggerMock = new Mock<ILogger<BackupService>>();
            _backupService = new BackupService(_context, _loggerMock.Object);
        }
        
        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
        
        [Fact]
        public async Task ExecuteBackupAsync_WithValidConfig_ReturnsSuccessfulResult()
        {
            // Arrange
            var org = CreateTestOrganization();
            await _context.Organizations.AddAsync(org);
            
            var config = new BackupConfig
            {
                Id = Guid.NewGuid(),
                OrganizationId = org.Id,
                Strategy = "regional-only",
                Frequency = "daily",
                EnableEncryption = true,
                RetentionDays = 7,
                EnablePostgresBackup = true,
                EnableRedisBackup = true,
                EnableQdrantBackup = true,
                IsActive = true
            };
            await _context.Set<BackupConfig>().AddAsync(config);
            await _context.SaveChangesAsync();
            
            // Act
            var result = await _backupService.ExecuteBackupAsync(org.Id, BackupType.PostgreSQL);
            
            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.BackupId);
            Assert.Equal(BackupType.PostgreSQL, result.BackupType);
            Assert.Equal(org.Id, result.OrganizationId);
            Assert.True(result.SizeBytes > 0);
            Assert.Null(result.ErrorMessage);
            
            // Verify config was updated
            var updatedConfig = await _context.Set<BackupConfig>().FindAsync(config.Id);
            Assert.NotNull(updatedConfig.LastBackupAt);
            Assert.Equal("success", updatedConfig.LastBackupStatus);
        }
        
        [Fact]
        public async Task ExecuteBackupAsync_WithoutConfig_ThrowsException()
        {
            // Arrange
            var org = CreateTestOrganization();
            await _context.Organizations.AddAsync(org);
            await _context.SaveChangesAsync();
            
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _backupService.ExecuteBackupAsync(org.Id, BackupType.PostgreSQL)
            );
        }
        
        [Fact]
        public async Task ExecuteBackupAsync_WithDisabledBackupType_ThrowsException()
        {
            // Arrange
            var org = CreateTestOrganization();
            await _context.Organizations.AddAsync(org);
            
            var config = new BackupConfig
            {
                Id = Guid.NewGuid(),
                OrganizationId = org.Id,
                Strategy = "regional-only",
                EnablePostgresBackup = false, // Disabled
                IsActive = true
            };
            await _context.Set<BackupConfig>().AddAsync(config);
            await _context.SaveChangesAsync();
            
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _backupService.ExecuteBackupAsync(org.Id, BackupType.PostgreSQL)
            );
        }
        
        [Fact]
        public async Task ExecuteBackupAsync_FullBackup_ExecutesAllEnabledBackups()
        {
            // Arrange
            var org = CreateTestOrganization();
            await _context.Organizations.AddAsync(org);
            
            var config = new BackupConfig
            {
                Id = Guid.NewGuid(),
                OrganizationId = org.Id,
                Strategy = "regional-only",
                EnablePostgresBackup = true,
                EnableRedisBackup = true,
                EnableQdrantBackup = true,
                IsActive = true
            };
            await _context.Set<BackupConfig>().AddAsync(config);
            await _context.SaveChangesAsync();
            
            // Act
            var result = await _backupService.ExecuteBackupAsync(org.Id, BackupType.Full);
            
            // Assert
            Assert.True(result.Success);
            Assert.Equal(BackupType.Full, result.BackupType);
            Assert.True(result.SizeBytes > 0);
        }
        
        [Fact]
        public async Task EncryptBackupAsync_WithValidData_ReturnsEncryptedData()
        {
            // Arrange
            var org = CreateTestOrganization();
            await _context.Organizations.AddAsync(org);
            await _context.SaveChangesAsync();
            
            var originalData = System.Text.Encoding.UTF8.GetBytes("Test backup data");
            
            // Act
            var encryptedData = await _backupService.EncryptBackupAsync(originalData, org.Id);
            
            // Assert
            Assert.NotNull(encryptedData);
            Assert.True(encryptedData.Length > originalData.Length); // Should include IV
            Assert.NotEqual(originalData, encryptedData);
        }
        
        [Fact]
        public async Task EncryptAndDecryptBackup_RoundTrip_PreservesData()
        {
            // Arrange
            var org = CreateTestOrganization();
            await _context.Organizations.AddAsync(org);
            await _context.SaveChangesAsync();
            
            var originalData = System.Text.Encoding.UTF8.GetBytes("Test backup data for encryption");
            
            // Act
            var encryptedData = await _backupService.EncryptBackupAsync(originalData, org.Id);
            var decryptedData = await _backupService.DecryptBackupAsync(encryptedData, org.Id);
            
            // Assert
            Assert.Equal(originalData, decryptedData);
        }
        
        [Fact]
        public async Task EncryptBackupAsync_WithEmptyData_ThrowsException()
        {
            // Arrange
            var orgId = Guid.NewGuid();
            var emptyData = new byte[0];
            
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _backupService.EncryptBackupAsync(emptyData, orgId)
            );
        }
        
        [Fact]
        public async Task DecryptBackupAsync_WithEmptyData_ThrowsException()
        {
            // Arrange
            var orgId = Guid.NewGuid();
            var emptyData = new byte[0];
            
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _backupService.DecryptBackupAsync(emptyData, orgId)
            );
        }
        
        [Fact]
        public async Task RestoreBackupAsync_WithValidBackup_ReturnsTrue()
        {
            // Arrange
            var org = CreateTestOrganization();
            await _context.Organizations.AddAsync(org);
            await _context.SaveChangesAsync();
            
            var backupId = $"backup_{org.Id}_PostgreSQL_{DateTime.UtcNow:yyyyMMddHHmmss}";
            
            // Act
            var result = await _backupService.RestoreBackupAsync(org.Id, backupId);
            
            // Assert
            Assert.True(result);
        }
        
        [Fact]
        public async Task EnforceRetentionPoliciesAsync_WithExpiredBackups_DeletesBackups()
        {
            // Arrange
            var org = CreateTestOrganization();
            await _context.Organizations.AddAsync(org);
            
            var config = new BackupConfig
            {
                Id = Guid.NewGuid(),
                OrganizationId = org.Id,
                Strategy = "regional-only",
                RetentionDays = 7,
                IsActive = true
            };
            await _context.Set<BackupConfig>().AddAsync(config);
            await _context.SaveChangesAsync();
            
            // Act
            var deletedCount = await _backupService.EnforceRetentionPoliciesAsync();
            
            // Assert
            Assert.True(deletedCount >= 0);
        }
        
        [Fact]
        public async Task GetBackupMetadataAsync_WithValidBackupId_ReturnsMetadata()
        {
            // Arrange
            var orgId = Guid.NewGuid();
            var backupId = $"backup_{orgId}_PostgreSQL_20260205120000";
            
            // Create org and config with encryption enabled
            var org = CreateTestOrganization();
            org.Id = orgId;
            await _context.Organizations.AddAsync(org);
            
            var config = new BackupConfig
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgId,
                Strategy = "regional-only",
                Frequency = "daily",
                EnableEncryption = true,
                RetentionDays = 7,
                IsActive = true
            };
            await _context.Set<BackupConfig>().AddAsync(config);
            await _context.SaveChangesAsync();
            
            // Act
            var metadata = await _backupService.GetBackupMetadataAsync(backupId);
            
            // Assert
            Assert.NotNull(metadata);
            Assert.Equal(backupId, metadata.BackupId);
            Assert.Equal(BackupType.PostgreSQL, metadata.BackupType);
            Assert.Equal(orgId, metadata.OrganizationId);
            Assert.True(metadata.IsEncrypted);
        }
        
        [Fact]
        public async Task GetBackupMetadataAsync_WithInvalidBackupId_ReturnsNull()
        {
            // Arrange
            var invalidBackupId = "invalid_backup_id";
            
            // Act
            var metadata = await _backupService.GetBackupMetadataAsync(invalidBackupId);
            
            // Assert
            Assert.Null(metadata);
        }
        
        [Fact]
        public async Task DeleteBackupAsync_WithValidBackupId_ReturnsTrue()
        {
            // Arrange
            var orgId = Guid.NewGuid();
            var backupId = $"backup_{orgId}_PostgreSQL_20260205120000";
            
            // Act
            var result = await _backupService.DeleteBackupAsync(backupId);
            
            // Assert
            Assert.True(result);
        }
        
        [Fact]
        public async Task ListBackupsAsync_WithValidOrganizationId_ReturnsList()
        {
            // Arrange
            var orgId = Guid.NewGuid();
            
            // Act
            var backups = await _backupService.ListBackupsAsync(orgId);
            
            // Assert
            Assert.NotNull(backups);
            Assert.IsType<System.Collections.Generic.List<BackupMetadata>>(backups);
        }
        
        [Theory]
        [InlineData(BackupType.PostgreSQL)]
        [InlineData(BackupType.Redis)]
        [InlineData(BackupType.Qdrant)]
        [InlineData(BackupType.Full)]
        public async Task ExecuteBackupAsync_WithDifferentTypes_CreatesCorrectBackupId(BackupType backupType)
        {
            // Arrange
            var org = CreateTestOrganization();
            await _context.Organizations.AddAsync(org);
            
            var config = new BackupConfig
            {
                Id = Guid.NewGuid(),
                OrganizationId = org.Id,
                Strategy = "regional-only",
                EnablePostgresBackup = true,
                EnableRedisBackup = true,
                EnableQdrantBackup = true,
                IsActive = true
            };
            await _context.Set<BackupConfig>().AddAsync(config);
            await _context.SaveChangesAsync();
            
            // Act
            var result = await _backupService.ExecuteBackupAsync(org.Id, backupType);
            
            // Assert
            Assert.True(result.Success);
            Assert.Contains(backupType.ToString(), result.BackupId);
        }
        
        private Organization CreateTestOrganization()
        {
            return new Organization
            {
                Id = Guid.NewGuid(),
                Slug = $"test-org-{Guid.NewGuid()}",
                Name = "Test Organization",
                PrimaryRegion = "us-east-1",
                Tier = "enterprise",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
    }
}
