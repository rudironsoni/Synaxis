using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Synaxis.InferenceGateway.Infrastructure.Contracts;
using Synaxis.Core.Contracts;
using Synaxis.Core.Models;
using Synaxis.Infrastructure.Data;
using Synaxis.Infrastructure.Services;
using Xunit;

namespace Synaxis.Tests.Unit
{
    public class SuperAdminServiceTests : IDisposable
    {
        private readonly SynaxisDbContext _context;
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly Mock<IAuditService> _auditServiceMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<ILogger<SuperAdminService>> _loggerMock;
        private readonly ISuperAdminService _service;
        private readonly Guid _testOrgId;
        private readonly Guid _testUserId;
        private readonly Guid _superAdminUserId;
        
        public SuperAdminServiceTests()
        {
            var options = new DbContextOptionsBuilder<SynaxisDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
                
            _context = new SynaxisDbContext(options);
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _auditServiceMock = new Mock<IAuditService>();
            _userServiceMock = new Mock<IUserService>();
            _loggerMock = new Mock<ILogger<SuperAdminService>>();
            
            // Create test organization
            _testOrgId = Guid.NewGuid();
            _context.Organizations.Add(new Organization
            {
                Id = _testOrgId,
                Name = "Test Org",
                Slug = "test-org",
                PrimaryRegion = "us-east-1",
                IsActive = true,
                Tier = "pro",
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            });
            
            // Create test user
            _testUserId = Guid.NewGuid();
            _context.Users.Add(new User
            {
                Id = _testUserId,
                OrganizationId = _testOrgId,
                Email = "test@example.com",
                PasswordHash = "hash",
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1",
                IsActive = true,
                Role = "member"
            });
            
            // Create super admin user
            _superAdminUserId = Guid.NewGuid();
            _context.Users.Add(new User
            {
                Id = _superAdminUserId,
                OrganizationId = _testOrgId,
                Email = "superadmin@synaxis.io",
                PasswordHash = "hash",
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1",
                IsActive = true,
                Role = "SuperAdmin",
                MfaEnabled = true,
                MfaSecret = "secret"
            });
            
            _context.SaveChanges();
            
            _service = new SuperAdminService(
                _context,
                _httpClientFactoryMock.Object,
                _auditServiceMock.Object,
                _userServiceMock.Object,
                _loggerMock.Object,
                "us-east-1"
            );
        }
        
        [Fact]
        public async Task GetCrossRegionOrganizationsAsync_ReturnsLocalOrganizations()
        {
            // Arrange
            SetupHttpClientForRemoteRegions(HttpStatusCode.OK, "[]");
            
            // Act
            var result = await _service.GetCrossRegionOrganizationsAsync();
            
            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(_testOrgId, result[0].Id);
            Assert.Equal("Test Org", result[0].Name);
            Assert.Equal("us-east-1", result[0].PrimaryRegion);
        }
        
        [Fact]
        public async Task GenerateImpersonationTokenAsync_ValidRequest_ReturnsToken()
        {
            // Arrange
            var request = new ImpersonationRequest
            {
                UserId = _testUserId,
                OrganizationId = _testOrgId,
                Justification = "Security investigation",
                ApprovedBy = "John Doe",
                DurationMinutes = 15
            };
            
            _auditServiceMock
                .Setup(x => x.LogEventAsync(It.IsAny<AuditEvent>()))
                .ReturnsAsync(new AuditLog());
            
            // Act
            var result = await _service.GenerateImpersonationTokenAsync(request);
            
            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Token);
            Assert.Equal(_testUserId, result.UserId);
            Assert.Equal(_testOrgId, result.OrganizationId);
            Assert.Equal("Security investigation", result.Justification);
            Assert.True(result.ExpiresAt > DateTime.UtcNow);
            Assert.True(result.ExpiresAt <= DateTime.UtcNow.AddMinutes(15));
            
            // Verify audit log was created
            _auditServiceMock.Verify(
                x => x.LogEventAsync(It.Is<AuditEvent>(e => 
                    e.EventType == "SUPER_ADMIN_IMPERSONATION" &&
                    e.UserId == _testUserId)),
                Times.Once);
        }
        
        [Fact]
        public async Task GenerateImpersonationTokenAsync_MissingJustification_ThrowsException()
        {
            // Arrange
            var request = new ImpersonationRequest
            {
                UserId = _testUserId,
                OrganizationId = _testOrgId,
                ApprovedBy = "John Doe"
            };
            
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.GenerateImpersonationTokenAsync(request));
        }
        
        [Fact]
        public async Task GenerateImpersonationTokenAsync_MissingApproval_ThrowsException()
        {
            // Arrange
            var request = new ImpersonationRequest
            {
                UserId = _testUserId,
                OrganizationId = _testOrgId,
                Justification = "Testing"
            };
            
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.GenerateImpersonationTokenAsync(request));
        }
        
        [Fact]
        public async Task GenerateImpersonationTokenAsync_InvalidUser_ThrowsException()
        {
            // Arrange
            var request = new ImpersonationRequest
            {
                UserId = Guid.NewGuid(),
                OrganizationId = _testOrgId,
                Justification = "Testing",
                ApprovedBy = "Admin"
            };
            
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.GenerateImpersonationTokenAsync(request));
        }
        
        [Fact]
        public async Task GetGlobalUsageAnalyticsAsync_ReturnsAggregatedData()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(-30);
            var endDate = DateTime.UtcNow;
            
            // Add some test requests
            _context.Requests.Add(new Request
            {
                Id = Guid.NewGuid(),
                RequestId = Guid.NewGuid(),
                OrganizationId = _testOrgId,
                UserId = _testUserId,
                UserRegion = "us-east-1",
                ProcessedRegion = "us-east-1",
                StoredRegion = "us-east-1",
                Model = "gpt-4",
                Provider = "openai",
                InputTokens = 100,
                OutputTokens = 200,
                Cost = 0.05m,
                StatusCode = 200,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            });
            
            _context.Requests.Add(new Request
            {
                Id = Guid.NewGuid(),
                RequestId = Guid.NewGuid(),
                OrganizationId = _testOrgId,
                UserId = _testUserId,
                UserRegion = "us-east-1",
                ProcessedRegion = "us-east-1",
                StoredRegion = "us-east-1",
                Model = "claude-3",
                Provider = "anthropic",
                InputTokens = 150,
                OutputTokens = 250,
                Cost = 0.08m,
                StatusCode = 200,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            });
            
            await _context.SaveChangesAsync();
            
            SetupHttpClientForRemoteRegions(HttpStatusCode.OK, "{\"region\":\"eu-west-1\",\"requests\":0,\"tokens\":0,\"spend\":0,\"organizations\":0,\"users\":0}");
            
            // Act
            var result = await _service.GetGlobalUsageAnalyticsAsync(startDate, endDate);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalRequests);
            Assert.Equal(700, result.TotalTokens); // 300 + 400
            Assert.Equal(0.13m, result.TotalSpend); // 0.05 + 0.08
            Assert.True(result.UsageByRegion.ContainsKey("us-east-1"));
            Assert.Contains("gpt-4", result.RequestsByModel.Keys);
            Assert.Contains("openai", result.RequestsByProvider.Keys);
        }
        
        [Fact]
        public async Task GetCrossBorderTransfersAsync_ReturnsCrossBorderRequests()
        {
            // Arrange
            var crossBorderRequest = new Request
            {
                Id = Guid.NewGuid(),
                RequestId = Guid.NewGuid(),
                OrganizationId = _testOrgId,
                UserId = _testUserId,
                UserRegion = "eu-west-1",
                ProcessedRegion = "us-east-1",
                StoredRegion = "us-east-1",
                CrossBorderTransfer = true,
                TransferLegalBasis = "SCC",
                TransferPurpose = "API request processing",
                TransferTimestamp = DateTime.UtcNow.AddDays(-1),
                Model = "gpt-4",
                Provider = "openai",
                StatusCode = 200,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };
            
            _context.Requests.Add(crossBorderRequest);
            await _context.SaveChangesAsync();
            
            // Act
            var result = await _service.GetCrossBorderTransfersAsync();
            
            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(_testOrgId, result[0].OrganizationId);
            Assert.Equal("eu-west-1", result[0].FromRegion);
            Assert.Equal("us-east-1", result[0].ToRegion);
            Assert.Equal("SCC", result[0].LegalBasis);
        }
        
        [Fact]
        public async Task GetComplianceStatusAsync_ReturnsComplianceData()
        {
            // Act
            var result = await _service.GetComplianceStatusAsync();
            
            // Assert
            Assert.NotNull(result);
            Assert.True(result.TotalOrganizations > 0);
            Assert.NotNull(result.ComplianceByRegion);
            Assert.NotNull(result.Issues);
            Assert.True(result.CheckedAt <= DateTime.UtcNow);
        }
        
        [Fact]
        public async Task GetSystemHealthOverviewAsync_ReturnsHealthStatus()
        {
            // Arrange
            SetupHttpClientForHealthCheck(HttpStatusCode.OK);
            
            // Act
            var result = await _service.GetSystemHealthOverviewAsync();
            
            // Assert
            Assert.NotNull(result);
            Assert.True(result.TotalRegions > 0);
            Assert.NotNull(result.HealthByRegion);
            Assert.Contains("us-east-1", result.HealthByRegion.Keys);
            Assert.NotNull(result.Alerts);
        }
        
        [Fact]
        public async Task ModifyOrganizationLimitsAsync_ValidRequest_UpdatesLimits()
        {
            // Arrange
            var request = new LimitModificationRequest
            {
                OrganizationId = _testOrgId,
                LimitType = "MaxTeams",
                NewValue = 50,
                Justification = "Customer upgrade",
                ApprovedBy = "Manager"
            };
            
            _auditServiceMock
                .Setup(x => x.LogEventAsync(It.IsAny<AuditEvent>()))
                .ReturnsAsync(new AuditLog());
            
            // Act
            var result = await _service.ModifyOrganizationLimitsAsync(request);
            
            // Assert
            Assert.True(result);
            
            var org = await _context.Organizations.FindAsync(_testOrgId);
            Assert.Equal(50, org.MaxTeams);
            
            // Verify audit log
            _auditServiceMock.Verify(
                x => x.LogEventAsync(It.Is<AuditEvent>(e => 
                    e.EventType == "SUPER_ADMIN_LIMIT_MODIFICATION")),
                Times.Once);
        }
        
        [Fact]
        public async Task ModifyOrganizationLimitsAsync_InvalidLimitType_ThrowsException()
        {
            // Arrange
            var request = new LimitModificationRequest
            {
                OrganizationId = _testOrgId,
                LimitType = "InvalidLimit",
                NewValue = 50,
                Justification = "Testing",
                ApprovedBy = "Admin"
            };
            
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.ModifyOrganizationLimitsAsync(request));
        }
        
        [Fact]
        public async Task ValidateAccessAsync_ValidSuperAdmin_ReturnsSuccess()
        {
            // Arrange
            var context = new SuperAdminAccessContext
            {
                UserId = _superAdminUserId,
                IpAddress = "10.0.0.1",
                MfaCode = "123456",
                Action = "view_analytics",
                RequestTime = DateTime.UtcNow.AddHours(10) // Within business hours
            };
            
            _userServiceMock
                .Setup(x => x.GetUserAsync(_superAdminUserId))
                .ReturnsAsync(await _context.Users.FindAsync(_superAdminUserId));
            
            _userServiceMock
                .Setup(x => x.VerifyMfaCodeAsync(_superAdminUserId, "123456"))
                .ReturnsAsync(true);
            
            _auditServiceMock
                .Setup(x => x.LogEventAsync(It.IsAny<AuditEvent>()))
                .ReturnsAsync(new AuditLog());
            
            // Act
            var result = await _service.ValidateAccessAsync(context);
            
            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsValid);
            Assert.True(result.MfaValid);
            Assert.True(result.IpAllowed);
            Assert.Null(result.FailureReason);
        }
        
        [Fact]
        public async Task ValidateAccessAsync_NonSuperAdmin_ReturnsFailed()
        {
            // Arrange
            var context = new SuperAdminAccessContext
            {
                UserId = _testUserId,
                IpAddress = "10.0.0.1",
                MfaCode = "123456",
                Action = "view_analytics",
                RequestTime = DateTime.UtcNow.AddHours(10)
            };
            
            _userServiceMock
                .Setup(x => x.GetUserAsync(_testUserId))
                .ReturnsAsync(await _context.Users.FindAsync(_testUserId));
            
            // Act
            var result = await _service.ValidateAccessAsync(context);
            
            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsValid);
            Assert.Contains("not a Super Admin", result.FailureReason);
        }
        
        [Fact]
        public async Task ValidateAccessAsync_MfaNotEnabled_ReturnsFailed()
        {
            // Arrange
            var userWithoutMfa = new User
            {
                Id = Guid.NewGuid(),
                OrganizationId = _testOrgId,
                Email = "nomfa@synaxis.io",
                PasswordHash = "hash",
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1",
                IsActive = true,
                Role = "SuperAdmin",
                MfaEnabled = false
            };
            _context.Users.Add(userWithoutMfa);
            await _context.SaveChangesAsync();
            
            var context = new SuperAdminAccessContext
            {
                UserId = userWithoutMfa.Id,
                IpAddress = "10.0.0.1",
                MfaCode = "123456",
                Action = "view_analytics",
                RequestTime = DateTime.UtcNow.AddHours(10)
            };
            
            _userServiceMock
                .Setup(x => x.GetUserAsync(userWithoutMfa.Id))
                .ReturnsAsync(userWithoutMfa);
            
            // Act
            var result = await _service.ValidateAccessAsync(context);
            
            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsValid);
            Assert.False(result.MfaValid);
            Assert.Contains("MFA is not enabled", result.FailureReason);
        }
        
        [Fact]
        public async Task ValidateAccessAsync_InvalidMfaCode_ReturnsFailed()
        {
            // Arrange
            var context = new SuperAdminAccessContext
            {
                UserId = _superAdminUserId,
                IpAddress = "10.0.0.1",
                MfaCode = "wrong",
                Action = "view_analytics",
                RequestTime = DateTime.UtcNow.AddHours(10)
            };
            
            _userServiceMock
                .Setup(x => x.GetUserAsync(_superAdminUserId))
                .ReturnsAsync(await _context.Users.FindAsync(_superAdminUserId));
            
            _userServiceMock
                .Setup(x => x.VerifyMfaCodeAsync(_superAdminUserId, "wrong"))
                .ReturnsAsync(false);
            
            _auditServiceMock
                .Setup(x => x.LogEventAsync(It.IsAny<AuditEvent>()))
                .ReturnsAsync(new AuditLog());
            
            // Act
            var result = await _service.ValidateAccessAsync(context);
            
            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsValid);
            Assert.False(result.MfaValid);
            Assert.Contains("Invalid MFA code", result.FailureReason);
            
            // Verify failed MFA attempt was logged
            _auditServiceMock.Verify(
                x => x.LogEventAsync(It.Is<AuditEvent>(e => 
                    e.EventType == "SUPER_ADMIN_MFA_FAILED")),
                Times.Once);
        }
        
        [Fact]
        public async Task ValidateAccessAsync_UnauthorizedIp_ReturnsFailed()
        {
            // Arrange
            var context = new SuperAdminAccessContext
            {
                UserId = _superAdminUserId,
                IpAddress = "1.2.3.4", // Public IP not in allowlist
                MfaCode = "123456",
                Action = "view_analytics",
                RequestTime = DateTime.UtcNow.AddHours(10)
            };
            
            _userServiceMock
                .Setup(x => x.GetUserAsync(_superAdminUserId))
                .ReturnsAsync(await _context.Users.FindAsync(_superAdminUserId));
            
            _userServiceMock
                .Setup(x => x.VerifyMfaCodeAsync(_superAdminUserId, "123456"))
                .ReturnsAsync(true);
            
            _auditServiceMock
                .Setup(x => x.LogEventAsync(It.IsAny<AuditEvent>()))
                .ReturnsAsync(new AuditLog());
            
            // Act
            var result = await _service.ValidateAccessAsync(context);
            
            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsValid);
            Assert.False(result.IpAllowed);
            Assert.Contains("IP address not in allowlist", result.FailureReason);
            
            // Verify unauthorized IP attempt was logged
            _auditServiceMock.Verify(
                x => x.LogEventAsync(It.Is<AuditEvent>(e => 
                    e.EventType == "SUPER_ADMIN_UNAUTHORIZED_IP")),
                Times.Once);
        }
        
        [Fact]
        public async Task ValidateAccessAsync_OutsideBusinessHours_ReturnsFailed()
        {
            // Arrange
            var context = new SuperAdminAccessContext
            {
                UserId = _superAdminUserId,
                IpAddress = "10.0.0.1",
                MfaCode = "123456",
                Action = "view_analytics",
                RequestTime = DateTime.UtcNow.Date.AddHours(2) // 2 AM UTC
            };
            
            _userServiceMock
                .Setup(x => x.GetUserAsync(_superAdminUserId))
                .ReturnsAsync(await _context.Users.FindAsync(_superAdminUserId));
            
            _userServiceMock
                .Setup(x => x.VerifyMfaCodeAsync(_superAdminUserId, "123456"))
                .ReturnsAsync(true);
            
            _auditServiceMock
                .Setup(x => x.LogEventAsync(It.IsAny<AuditEvent>()))
                .ReturnsAsync(new AuditLog());
            
            // Act
            var result = await _service.ValidateAccessAsync(context);
            
            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsValid);
            Assert.False(result.WithinBusinessHours);
            Assert.Contains("business hours", result.FailureReason);
            
            // Verify off-hours attempt was logged
            _auditServiceMock.Verify(
                x => x.LogEventAsync(It.Is<AuditEvent>(e => 
                    e.EventType == "SUPER_ADMIN_OFF_HOURS")),
                Times.Once);
        }
        
        [Fact]
        public async Task ValidateAccessAsync_SensitiveActionWithoutJustification_ReturnsFailed()
        {
            // Arrange
            var context = new SuperAdminAccessContext
            {
                UserId = _superAdminUserId,
                IpAddress = "10.0.0.1",
                MfaCode = "123456",
                Action = "impersonate",
                Justification = null,
                RequestTime = DateTime.UtcNow.AddHours(10)
            };
            
            _userServiceMock
                .Setup(x => x.GetUserAsync(_superAdminUserId))
                .ReturnsAsync(await _context.Users.FindAsync(_superAdminUserId));
            
            _userServiceMock
                .Setup(x => x.VerifyMfaCodeAsync(_superAdminUserId, "123456"))
                .ReturnsAsync(true);
            
            // Act
            var result = await _service.ValidateAccessAsync(context);
            
            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsValid);
            Assert.Contains("Justification is required", result.FailureReason);
        }
        
        // Helper methods
        
        private void SetupHttpClientForRemoteRegions(HttpStatusCode statusCode, string responseContent)
        {
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(responseContent)
                });
            
            var client = new HttpClient(handlerMock.Object);
            _httpClientFactoryMock
                .Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(client);
        }
        
        private void SetupHttpClientForHealthCheck(HttpStatusCode statusCode)
        {
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.AbsolutePath.Contains("health")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode
                });
            
            var client = new HttpClient(handlerMock.Object);
            _httpClientFactoryMock
                .Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(client);
        }
        
        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
