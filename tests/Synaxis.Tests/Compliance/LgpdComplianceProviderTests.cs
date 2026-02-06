using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Synaxis.Common.Tests.Infrastructure;
using Synaxis.InferenceGateway.Infrastructure.Contracts;
using Synaxis.InferenceGateway.Infrastructure.Compliance;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Identity;
using Xunit;

namespace Synaxis.Tests.Compliance
{
    public class LgpdComplianceProviderTests : IDisposable
    {
        private readonly LgpdComplianceProvider _provider;
        private readonly InferenceGateway.Infrastructure.ControlPlane.SynaxisDbContext _dbContext;

        public LgpdComplianceProviderTests()
        {
            _dbContext = InMemorySynaxisDbContext.Create();
            _provider = new LgpdComplianceProvider(_dbContext);
        }

        public void Dispose()
        {
            _dbContext?.Dispose();
        }

        [Fact]
        public void Constructor_ShouldSetCorrectRegulationCode()
        {
            // Assert
            Assert.Equal("LGPD", _provider.RegulationCode);
        }

        [Fact]
        public void Constructor_ShouldSetCorrectRegion()
        {
            // Assert
            Assert.Equal("BR", _provider.Region);
        }

        [Fact]
        public void Constructor_WithNullDbContext_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new LgpdComplianceProvider(null));
        }

        [Theory]
        [InlineData("sa-east-1", "br-south-1", true)]
        [InlineData("br-south-1", "sa-east-1", true)]
        [InlineData("sa-east-1", "sa-saopaulo-1", true)]
        public async Task ValidateTransferAsync_WithinBrazil_ShouldReturnTrue(string fromRegion, string toRegion, bool expected)
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

        [Fact]
        public async Task ValidateTransferAsync_FromBrazilWithAdequacy_ShouldReturnTrue()
        {
            // Arrange
            var context = new TransferContext
            {
                OrganizationId = Guid.NewGuid(),
                FromRegion = "sa-east-1",
                ToRegion = "eu-west-1",
                LegalBasis = "adequacy",
                EncryptionUsed = true
            };

            // Act
            var result = await _provider.ValidateTransferAsync(context);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ValidateTransferAsync_FromBrazilWithSCC_ShouldReturnTrue()
        {
            // Arrange
            var context = new TransferContext
            {
                OrganizationId = Guid.NewGuid(),
                FromRegion = "sa-east-1",
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
        public async Task ValidateTransferAsync_FromBrazilWithContract_ShouldReturnTrue()
        {
            // Arrange
            var context = new TransferContext
            {
                OrganizationId = Guid.NewGuid(),
                FromRegion = "br-south-1",
                ToRegion = "us-east-1",
                LegalBasis = "contract",
                Purpose = "Service delivery",
                EncryptionUsed = true
            };

            // Act
            var result = await _provider.ValidateTransferAsync(context);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ValidateTransferAsync_FromBrazilWithConsent_ShouldReturnTrue()
        {
            // Arrange
            var context = new TransferContext
            {
                OrganizationId = Guid.NewGuid(),
                FromRegion = "sa-east-1",
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
        public async Task ValidateTransferAsync_FromBrazilWithoutEncryption_ShouldReturnFalse()
        {
            // Arrange
            var context = new TransferContext
            {
                OrganizationId = Guid.NewGuid(),
                FromRegion = "sa-east-1",
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
        public async Task ValidateTransferAsync_FromBrazilWithoutPurpose_ShouldReturnFalse()
        {
            // Arrange
            var context = new TransferContext
            {
                OrganizationId = Guid.NewGuid(),
                FromRegion = "sa-east-1",
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
        public async Task ValidateTransferAsync_NullContext_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _provider.ValidateTransferAsync(null));
        }

        [Fact]
        public async Task LogTransferAsync_ShouldCreateAuditLogEntryInPortuguese()
        {
            // Arrange
            var context = new TransferContext
            {
                OrganizationId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                FromRegion = "sa-east-1",
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
            Assert.Equal("transferencia_internacional", auditLog.Action);
            Assert.Equal("transferencia_dados", auditLog.EntityType);
            Assert.Equal(context.OrganizationId, auditLog.OrganizationId);
            Assert.Equal(context.UserId, auditLog.UserId);
            Assert.NotNull(auditLog.NewValues);
            Assert.Contains("regulamento", auditLog.NewValues);
            Assert.Contains("LGPD", auditLog.NewValues);
        }

        [Fact]
        public async Task LogTransferAsync_NullContext_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _provider.LogTransferAsync(null));
        }

        [Fact]
        public async Task ExportUserDataAsync_WithValidUser_ShouldReturnDataExportInPortuguese()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new SynaxisUser
            {
                Id = userId,
                Email = "test@exemplo.com.br",
                FirstName = "João",
                LastName = "Silva",
                UserName = "test@exemplo.com.br",
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
            Assert.Equal("LGPD", result.Metadata["regulamento"]);
            Assert.Equal("portabilidade_dados", result.Metadata["direito"]);
            Assert.Equal("Artigo 18, V", result.Metadata["artigo"]);
            Assert.Equal("pt-BR", result.Metadata["idioma"]);
        }

        [Fact]
        public async Task ExportUserDataAsync_WithNonExistentUser_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _provider.ExportUserDataAsync(userId));
            Assert.Contains("Usuário", exception.Message);
        }

        [Fact]
        public async Task DeleteUserDataAsync_WithValidUser_ShouldDeleteUserAndCreatePortugueseLog()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new SynaxisUser
            {
                Id = userId,
                Email = "test@exemplo.com.br",
                FirstName = "João",
                LastName = "Silva",
                UserName = "test@exemplo.com.br",
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
                .Where(a => a.Action == "eliminacao_dados" && a.UserId == userId)
                .FirstOrDefaultAsync();
            Assert.NotNull(deletionLog);
            Assert.Contains("LGPD", deletionLog.NewValues);
            Assert.Contains("Artigo 18, VI", deletionLog.NewValues);
        }

        [Theory]
        [InlineData("consent", true)]
        [InlineData("legal_obligation", true)]
        [InlineData("public_administration", true)]
        [InlineData("research", true)]
        [InlineData("contract", true)]
        [InlineData("legal_proceedings", true)]
        [InlineData("life_protection", true)]
        [InlineData("health_protection", true)]
        [InlineData("legitimate_interests", true)]
        [InlineData("credit_protection", true)]
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
        [InlineData("medium", 50, true)]
        [InlineData("medium", 30, false)]
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
                AffectedUsersCount = 5,
                RiskLevel = "low",
                DataCategoriesExposed = new[] { "health_data", "email" }
            };

            // Act
            var result = await _provider.IsBreachNotificationRequiredAsync(context);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("racial_ethnic_origin", true)]
        [InlineData("religious_belief", true)]
        [InlineData("political_opinion", true)]
        [InlineData("union_membership", true)]
        [InlineData("philosophical_belief", true)]
        [InlineData("health_data", true)]
        [InlineData("genetic_data", true)]
        [InlineData("biometric_data", true)]
        [InlineData("sexual_life_data", true)]
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
