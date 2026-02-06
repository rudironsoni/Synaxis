using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Synaxis.Common.Tests.Infrastructure;
using Synaxis.InferenceGateway.Infrastructure.Contracts;
using Synaxis.InferenceGateway.Infrastructure.Compliance;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Identity;
using Xunit;

namespace Synaxis.Tests.Integration
{
    public class ComplianceProvidersIntegrationTests : IDisposable
    {
        private readonly InferenceGateway.Infrastructure.ControlPlane.SynaxisDbContext _dbContext;
        private readonly GdprComplianceProvider _gdprProvider;
        private readonly LgpdComplianceProvider _lgpdProvider;
        private readonly ComplianceProviderFactory _factory;

        public ComplianceProvidersIntegrationTests()
        {
            _dbContext = InMemorySynaxisDbContext.Create();
            _gdprProvider = new GdprComplianceProvider(_dbContext);
            _lgpdProvider = new LgpdComplianceProvider(_dbContext);
            _factory = new ComplianceProviderFactory(_dbContext);
        }

        public void Dispose()
        {
            _dbContext?.Dispose();
        }

        [Fact]
        public async Task FullUserDataLifecycle_GDPR_ShouldWorkEndToEnd()
        {
            // Arrange - Create a user with related data
            var userId = Guid.NewGuid();
            var orgId = Guid.NewGuid();

            var organization = new Organization
            {
                Id = orgId,
                LegalName = "Test Organization",
                DisplayName = "Test Org",
                Slug = "test-org",
                Status = "Active",
                PlanTier = "Professional",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var user = new SynaxisUser
            {
                Id = userId,
                Email = "gdpr.user@example.com",
                FirstName = "Test",
                LastName = "User",
                UserName = "gdpr.user@example.com",
                Status = "Active",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Organizations.Add(organization);
            _dbContext.Users.Add(user);

            var membership = new UserOrganizationMembership
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                OrganizationId = orgId,
                OrganizationRole = "Member",
                Status = "Active",
                JoinedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.UserOrganizationMemberships.Add(membership);
            await _dbContext.SaveChangesAsync();

            // Act 1: Export user data
            var exportResult = await _gdprProvider.ExportUserDataAsync(userId);

            // Assert export
            Assert.NotNull(exportResult);
            Assert.Equal(userId, exportResult.UserId);
            Assert.Equal("json", exportResult.Format);
            Assert.NotNull(exportResult.Data);
            Assert.True(exportResult.Data.Length > 0);

            var jsonString = System.Text.Encoding.UTF8.GetString(exportResult.Data);
            Assert.Contains("profile", jsonString);
            Assert.Contains("organization_memberships", jsonString);

            // Act 2: Delete user data
            var deleteResult = await _gdprProvider.DeleteUserDataAsync(userId);

            // Assert deletion
            Assert.True(deleteResult);

            var deletedUser = await _dbContext.Users.FindAsync(userId);
            Assert.Null(deletedUser);

            var deletedMembership = await _dbContext.UserOrganizationMemberships
                .FirstOrDefaultAsync(m => m.UserId == userId);
            Assert.Null(deletedMembership);

            // Verify audit log was created
            var auditLog = await _dbContext.AuditLogs
                .FirstOrDefaultAsync(a => a.Action == "data_erasure" && a.UserId == userId);
            Assert.NotNull(auditLog);
        }

        [Fact]
        public async Task FullUserDataLifecycle_LGPD_ShouldWorkEndToEnd()
        {
            // Arrange - Create a user with related data
            var userId = Guid.NewGuid();
            var orgId = Guid.NewGuid();

            var organization = new Organization
            {
                Id = orgId,
                LegalName = "Organização de Teste",
                DisplayName = "Org Teste",
                Slug = "org-teste",
                Status = "Active",
                PlanTier = "Professional",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var user = new SynaxisUser
            {
                Id = userId,
                Email = "lgpd.usuario@exemplo.com.br",
                FirstName = "João",
                LastName = "Silva",
                UserName = "lgpd.usuario@exemplo.com.br",
                Status = "Active",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Organizations.Add(organization);
            _dbContext.Users.Add(user);

            var membership = new UserOrganizationMembership
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                OrganizationId = orgId,
                OrganizationRole = "Member",
                Status = "Active",
                JoinedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.UserOrganizationMemberships.Add(membership);
            await _dbContext.SaveChangesAsync();

            // Act 1: Export user data
            var exportResult = await _lgpdProvider.ExportUserDataAsync(userId);

            // Assert export with Portuguese metadata
            Assert.NotNull(exportResult);
            Assert.Equal(userId, exportResult.UserId);
            Assert.Equal("json", exportResult.Format);
            
            var jsonString = System.Text.Encoding.UTF8.GetString(exportResult.Data);
            Assert.Contains("perfil", jsonString);
            Assert.Contains("vinculos_organizacao", jsonString);
            
            Assert.Equal("LGPD", exportResult.Metadata["regulamento"]);
            Assert.Equal("pt-BR", exportResult.Metadata["idioma"]);

            // Act 2: Delete user data
            var deleteResult = await _lgpdProvider.DeleteUserDataAsync(userId);

            // Assert deletion
            Assert.True(deleteResult);

            var deletedUser = await _dbContext.Users.FindAsync(userId);
            Assert.Null(deletedUser);

            // Verify Portuguese audit log was created
            var auditLog = await _dbContext.AuditLogs
                .FirstOrDefaultAsync(a => a.Action == "eliminacao_dados" && a.UserId == userId);
            Assert.NotNull(auditLog);
            Assert.Contains("LGPD", auditLog.NewValues);
        }

        [Fact]
        public async Task CrossBorderTransferValidationAndLogging_GDPR_ShouldWorkEndToEnd()
        {
            // Arrange
            var context = new TransferContext
            {
                OrganizationId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                FromRegion = "eu-west-1",
                ToRegion = "us-east-1",
                LegalBasis = "SCC",
                Purpose = "API request processing",
                DataCategories = new[] { "request_logs", "usage_metrics" },
                EncryptionUsed = true,
                UserConsentObtained = false
            };

            // Act 1: Validate transfer
            var isValid = await _gdprProvider.ValidateTransferAsync(context);

            // Assert validation
            Assert.True(isValid);

            // Act 2: Log transfer
            await _gdprProvider.LogTransferAsync(context);

            // Assert logging
            var auditLog = await _dbContext.AuditLogs
                .FirstOrDefaultAsync(a => a.Action == "cross_border_transfer");
            
            Assert.NotNull(auditLog);
            Assert.Equal(context.OrganizationId, auditLog.OrganizationId);
            Assert.Equal(context.UserId, auditLog.UserId);
            Assert.NotNull(auditLog.NewValues);
            
            var logData = JsonSerializer.Deserialize<JsonElement>(auditLog.NewValues);
            Assert.Equal("eu-west-1", logData.GetProperty("FromRegion").GetString());
            Assert.Equal("us-east-1", logData.GetProperty("ToRegion").GetString());
            Assert.Equal("SCC", logData.GetProperty("LegalBasis").GetString());
        }

        [Fact]
        public async Task CrossBorderTransferValidationAndLogging_LGPD_ShouldWorkEndToEnd()
        {
            // Arrange
            var context = new TransferContext
            {
                OrganizationId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                FromRegion = "sa-east-1",
                ToRegion = "us-east-1",
                LegalBasis = "contract",
                Purpose = "Processamento de dados",
                DataCategories = new[] { "dados_pessoais", "metricas_uso" },
                EncryptionUsed = true,
                UserConsentObtained = false
            };

            // Act 1: Validate transfer
            var isValid = await _lgpdProvider.ValidateTransferAsync(context);

            // Assert validation
            Assert.True(isValid);

            // Act 2: Log transfer
            await _lgpdProvider.LogTransferAsync(context);

            // Assert logging with Portuguese fields
            var auditLog = await _dbContext.AuditLogs
                .FirstOrDefaultAsync(a => a.Action == "transferencia_internacional");
            
            Assert.NotNull(auditLog);
            Assert.Equal(context.OrganizationId, auditLog.OrganizationId);
            Assert.Equal(context.UserId, auditLog.UserId);
            Assert.NotNull(auditLog.NewValues);
            Assert.Contains("regulamento", auditLog.NewValues);
            Assert.Contains("LGPD", auditLog.NewValues);
        }

        [Fact]
        public async Task FactoryProviderSelection_ShouldReturnCorrectProviderForRegion()
        {
            // Act
            var euProvider = _factory.GetProvider("eu-west-1");
            var brProvider = _factory.GetProvider("sa-east-1");

            // Assert
            Assert.Equal("GDPR", euProvider.RegulationCode);
            Assert.Equal("LGPD", brProvider.RegulationCode);

            // Test processing with correct providers
            var euContext = new ProcessingContext
            {
                OrganizationId = Guid.NewGuid(),
                ProcessingPurpose = "Marketing",
                LegalBasis = "consent",
                DataCategories = new[] { "email" }
            };

            var brContext = new ProcessingContext
            {
                OrganizationId = Guid.NewGuid(),
                ProcessingPurpose = "Marketing",
                LegalBasis = "consent",
                DataCategories = new[] { "email" }
            };

            var euResult = await euProvider.IsProcessingAllowedAsync(euContext);
            var brResult = await brProvider.IsProcessingAllowedAsync(brContext);

            Assert.True(euResult);
            Assert.True(brResult);
        }

        [Fact]
        public async Task BreachNotification_GDPR_ShouldEvaluateCorrectly()
        {
            // Arrange
            var highRiskBreach = new BreachContext
            {
                OrganizationId = Guid.NewGuid(),
                BreachType = "Data exfiltration",
                AffectedUsersCount = 1000,
                RiskLevel = "high",
                DataCategoriesExposed = new[] { "email", "password_hash" }
            };

            var lowRiskBreach = new BreachContext
            {
                OrganizationId = Guid.NewGuid(),
                BreachType = "Minor configuration error",
                AffectedUsersCount = 5,
                RiskLevel = "low",
                DataCategoriesExposed = new[] { "email" }
            };

            // Act
            var highRiskResult = await _gdprProvider.IsBreachNotificationRequiredAsync(highRiskBreach);
            var lowRiskResult = await _gdprProvider.IsBreachNotificationRequiredAsync(lowRiskBreach);

            // Assert
            Assert.True(highRiskResult);
            Assert.False(lowRiskResult);
        }

        [Fact]
        public async Task BreachNotification_LGPD_ShouldEvaluateCorrectly()
        {
            // Arrange - LGPD has lower threshold (50 users vs GDPR's 100)
            var mediumRiskBreach = new BreachContext
            {
                OrganizationId = Guid.NewGuid(),
                BreachType = "Vazamento de dados",
                AffectedUsersCount = 50,
                RiskLevel = "medium",
                DataCategoriesExposed = new[] { "email", "nome" }
            };

            var lowRiskBreach = new BreachContext
            {
                OrganizationId = Guid.NewGuid(),
                BreachType = "Erro de configuração menor",
                AffectedUsersCount = 30,
                RiskLevel = "medium",
                DataCategoriesExposed = new[] { "email" }
            };

            // Act
            var mediumRiskResult = await _lgpdProvider.IsBreachNotificationRequiredAsync(mediumRiskBreach);
            var lowRiskResult = await _lgpdProvider.IsBreachNotificationRequiredAsync(lowRiskBreach);

            // Assert
            Assert.True(mediumRiskResult);  // 50 users triggers notification
            Assert.False(lowRiskResult);     // 30 users doesn't trigger
        }
    }
}
