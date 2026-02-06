using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Synaxis.Common.Tests.Infrastructure;
using Synaxis.InferenceGateway.Infrastructure.Contracts;
using Synaxis.InferenceGateway.Infrastructure.Compliance;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Identity;
using Xunit;

namespace Synaxis.Tests.Compliance
{
    public class GdprComplianceProviderTests : IDisposable
    {
        private readonly GdprComplianceProvider _provider;
        private readonly InferenceGateway.Infrastructure.ControlPlane.SynaxisDbContext _dbContext;

        public GdprComplianceProviderTests()
        {
            _dbContext = InMemorySynaxisDbContext.Create();
            _provider = new GdprComplianceProvider(_dbContext);
        }

        public void Dispose()
        {
            _dbContext?.Dispose();
        }

        [Fact]
        public void Constructor_ShouldSetCorrectRegulationCode()
        {
            // Assert
            Assert.Equal("GDPR", _provider.RegulationCode);
        }

        [Fact]
        public void Constructor_ShouldSetCorrectRegion()
        {
            // Assert
            Assert.Equal("EU", _provider.Region);
        }

        [Fact]
        public void Constructor_WithNullDbContext_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new GdprComplianceProvider(null));
        }

        [Theory]
        [InlineData("eu-west-1", "eu-central-1", true)]
        [InlineData("eu-central-1", "eu-west-1", true)]
        [InlineData("eu-north-1", "eu-south-1", true)]
        public async Task ValidateTransferAsync_WithinEU_ShouldReturnTrue(string fromRegion, string toRegion, bool expected)
        {
            // Arrange
            var context = new TransferContext
            {
                OrganizationId = Guid.NewGuid(),
                FromRegion = fromRegion,
                ToRegion = toRegion,
                EncryptionUsed = true
            };

            // Act
            var result = await _provider.ValidateTransferAsync(context);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("eu-west-1", "uk-west-1", "adequacy", true)]
        [InlineData("eu-west-1", "jp-east-1", "adequacy", true)]
        [InlineData("eu-west-1", "nz-south-1", "adequacy", true)]
        public async Task ValidateTransferAsync_ToAdequateCountry_ShouldReturnTrue(string fromRegion, string toRegion, string legalBasis, bool expected)
        {
            // Arrange
            var context = new TransferContext
            {
                OrganizationId = Guid.NewGuid(),
                FromRegion = fromRegion,
                ToRegion = toRegion,
                LegalBasis = legalBasis,
                EncryptionUsed = true
            };

            // Act
            var result = await _provider.ValidateTransferAsync(context);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task ValidateTransferAsync_FromEUWithSCC_ShouldReturnTrue()
        {
            // Arrange
            var context = new TransferContext
            {
                OrganizationId = Guid.NewGuid(),
                FromRegion = "eu-west-1",
                ToRegion = "us-east-1",
                LegalBasis = "SCC",
                Purpose = "Data processing",
                EncryptionUsed = true
            };

            // Act
            var result = await _provider.ValidateTransferAsync(context);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ValidateTransferAsync_FromEUWithConsent_ShouldReturnTrue()
        {
            // Arrange
            var context = new TransferContext
            {
                OrganizationId = Guid.NewGuid(),
                FromRegion = "eu-west-1",
                ToRegion = "us-east-1",
                LegalBasis = "consent",
                Purpose = "Data processing",
                UserConsentObtained = true,
                EncryptionUsed = true
            };

            // Act
            var result = await _provider.ValidateTransferAsync(context);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ValidateTransferAsync_FromEUWithoutEncryption_ShouldReturnFalse()
        {
            // Arrange
            var context = new TransferContext
            {
                OrganizationId = Guid.NewGuid(),
                FromRegion = "eu-west-1",
                ToRegion = "us-east-1",
                LegalBasis = "consent",
                Purpose = "Data processing",
                UserConsentObtained = true,
                EncryptionUsed = false
            };

            // Act
            var result = await _provider.ValidateTransferAsync(context);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ValidateTransferAsync_FromEUWithoutPurpose_ShouldReturnFalse()
        {
            // Arrange
            var context = new TransferContext
            {
                OrganizationId = Guid.NewGuid(),
                FromRegion = "eu-west-1",
                ToRegion = "us-east-1",
                LegalBasis = "consent",
                Purpose = "",
                UserConsentObtained = true,
                EncryptionUsed = true
            };

            // Act
            var result = await _provider.ValidateTransferAsync(context);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ValidateTransferAsync_FromEUWithoutLegalBasis_ShouldReturnFalse()
        {
            // Arrange
            var context = new TransferContext
            {
                OrganizationId = Guid.NewGuid(),
                FromRegion = "eu-west-1",
                ToRegion = "us-east-1",
                LegalBasis = null,
                Purpose = "Data processing",
                EncryptionUsed = true
            };

            // Act
            var result = await _provider.ValidateTransferAsync(context);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ValidateTransferAsync_NullContext_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _provider.ValidateTransferAsync(null));
        }

        [Fact]
        public async Task LogTransferAsync_ShouldCreateAuditLogEntry()
        {
            // Arrange
            var context = new TransferContext
            {
                OrganizationId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                FromRegion = "eu-west-1",
                ToRegion = "us-east-1",
                LegalBasis = "SCC",
                Purpose = "Data processing",
                DataCategories = new[] { "personal_data", "usage_data" },
                EncryptionUsed = true,
                UserConsentObtained = false
            };

            // Act
            await _provider.LogTransferAsync(context);

            // Assert
            var auditLog = await _dbContext.AuditLogs.FirstOrDefaultAsync();
            Assert.NotNull(auditLog);
            Assert.Equal("cross_border_transfer", auditLog.Action);
            Assert.Equal("data_transfer", auditLog.EntityType);
            Assert.Equal(context.OrganizationId, auditLog.OrganizationId);
            Assert.Equal(context.UserId, auditLog.UserId);
            Assert.NotNull(auditLog.NewValues);
        }

        [Fact]
        public async Task LogTransferAsync_NullContext_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _provider.LogTransferAsync(null));
        }

        [Fact]
        public async Task ExportUserDataAsync_WithValidUser_ShouldReturnDataExport()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var orgId = Guid.NewGuid();
            
            var user = new SynaxisUser
            {
                Id = userId,
                Email = "test@example.com",
                FirstName = "John",
                LastName = "Doe",
                UserName = "test@example.com",
                Status = "Active",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _provider.ExportUserDataAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result.UserId);
            Assert.Equal("json", result.Format);
            Assert.NotNull(result.Data);
            Assert.True(result.Data.Length > 0);
            Assert.NotNull(result.Metadata);
            Assert.Equal("GDPR", result.Metadata["regulation"]);
            Assert.Equal("data_portability", result.Metadata["right"]);
        }

        [Fact]
        public async Task ExportUserDataAsync_WithNonExistentUser_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _provider.ExportUserDataAsync(userId));
        }

        [Fact]
        public async Task DeleteUserDataAsync_WithValidUser_ShouldDeleteUserAndRelatedData()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new SynaxisUser
            {
                Id = userId,
                Email = "test@example.com",
                FirstName = "John",
                LastName = "Doe",
                UserName = "test@example.com",
                Status = "Active",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _provider.DeleteUserDataAsync(userId);

            // Assert
            Assert.True(result);
            
            var deletedUser = await _dbContext.Users.FindAsync(userId);
            Assert.Null(deletedUser);

            var deletionLog = await _dbContext.AuditLogs
                .Where(a => a.Action == "data_erasure" && a.UserId == userId)
                .FirstOrDefaultAsync();
            Assert.NotNull(deletionLog);
        }

        [Fact]
        public async Task DeleteUserDataAsync_WithNonExistentUser_ShouldReturnTrue()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var result = await _provider.DeleteUserDataAsync(userId);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("consent", true)]
        [InlineData("contract", true)]
        [InlineData("legal_obligation", true)]
        [InlineData("vital_interests", true)]
        [InlineData("public_task", true)]
        [InlineData("legitimate_interests", true)]
        [InlineData("invalid_basis", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public async Task IsProcessingAllowedAsync_WithLegalBasis_ShouldReturnExpectedResult(string legalBasis, bool expected)
        {
            // Arrange
            var context = new ProcessingContext
            {
                OrganizationId = Guid.NewGuid(),
                ProcessingPurpose = "Marketing",
                LegalBasis = legalBasis,
                DataCategories = new[] { "email", "name" }
            };

            // Act
            var result = await _provider.IsProcessingAllowedAsync(context);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task IsProcessingAllowedAsync_NullContext_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _provider.IsProcessingAllowedAsync(null));
        }

        [Fact]
        public void GetDataRetentionDays_ShouldReturnNull()
        {
            // Act
            var result = _provider.GetDataRetentionDays();

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData("high", 10, true)]
        [InlineData("medium", 100, true)]
        [InlineData("medium", 50, false)]
        [InlineData("low", 10, false)]
        public async Task IsBreachNotificationRequiredAsync_WithRiskLevel_ShouldReturnExpectedResult(
            string riskLevel, int affectedCount, bool expected)
        {
            // Arrange
            var context = new BreachContext
            {
                OrganizationId = Guid.NewGuid(),
                BreachType = "Data leak",
                AffectedUsersCount = affectedCount,
                RiskLevel = riskLevel,
                DataCategoriesExposed = new[] { "email", "name" }
            };

            // Act
            var result = await _provider.IsBreachNotificationRequiredAsync(context);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task IsBreachNotificationRequiredAsync_WithSensitiveData_ShouldReturnTrue()
        {
            // Arrange
            var context = new BreachContext
            {
                OrganizationId = Guid.NewGuid(),
                BreachType = "Data leak",
                AffectedUsersCount = 10,
                RiskLevel = "low",
                DataCategoriesExposed = new[] { "health_data", "email" }
            };

            // Act
            var result = await _provider.IsBreachNotificationRequiredAsync(context);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("financial_data", true)]
        [InlineData("biometric_data", true)]
        [InlineData("genetic_data", true)]
        [InlineData("political_opinions", true)]
        [InlineData("religious_beliefs", true)]
        public async Task IsBreachNotificationRequiredAsync_WithVariousSensitiveCategories_ShouldReturnTrue(
            string dataCategory, bool expected)
        {
            // Arrange
            var context = new BreachContext
            {
                OrganizationId = Guid.NewGuid(),
                BreachType = "Data leak",
                AffectedUsersCount = 1,
                RiskLevel = "low",
                DataCategoriesExposed = new[] { dataCategory }
            };

            // Act
            var result = await _provider.IsBreachNotificationRequiredAsync(context);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task IsBreachNotificationRequiredAsync_NullContext_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _provider.IsBreachNotificationRequiredAsync(null));
        }
    }
}
