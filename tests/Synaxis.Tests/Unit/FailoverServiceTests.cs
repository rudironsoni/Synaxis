using System;
using System.Collections.Generic;
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

namespace Synaxis.Tests.Unit
{
    public class FailoverServiceTests : IDisposable
    {
        private readonly Mock<IHealthMonitor> _healthMonitorMock;
        private readonly Mock<ILogger<FailoverService>> _loggerMock;
        private readonly SynaxisDbContext _context;
        private readonly FailoverService _service;
        
        public FailoverServiceTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<SynaxisDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new SynaxisDbContext(options);
            
            _healthMonitorMock = new Mock<IHealthMonitor>();
            _loggerMock = new Mock<ILogger<FailoverService>>();
            
            _service = new FailoverService(
                _context,
                _healthMonitorMock.Object,
                _loggerMock.Object
            );
        }
        
        [Fact]
        public async Task SelectRegionAsync_PrimaryHealthy_ReturnsPrimary()
        {
            // Arrange
            var orgId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var primaryRegion = "eu-west-1";
            
            // Seed organization
            var org = new Organization
            {
                Id = orgId,
                Slug = "test-org",
                Name = "Test Organization",
                PrimaryRegion = primaryRegion,
                AvailableRegions = new List<string> { "eu-west-1", "us-east-1", "sa-east-1" }
            };
            _context.Organizations.Add(org);
            await _context.SaveChangesAsync();
            
            _healthMonitorMock.Setup(h => h.IsRegionHealthyAsync(primaryRegion))
                .ReturnsAsync(true);
            
            // Act
            var decision = await _service.SelectRegionAsync(orgId, userId, primaryRegion);
            
            // Assert
            Assert.Equal(primaryRegion, decision.SelectedRegion);
            Assert.False(decision.IsFailover);
            Assert.False(decision.NeedsCrossBorderConsent);
            Assert.Equal("Primary region healthy", decision.Reason);
        }
        
        [Fact]
        public async Task SelectRegionAsync_PrimaryUnhealthy_FailsOverToNearestHealthy()
        {
            // Arrange
            var orgId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var primaryRegion = "eu-west-1";
            
            // Seed organization and user
            var org = new Organization
            {
                Id = orgId,
                Slug = "test-org",
                Name = "Test Organization",
                PrimaryRegion = primaryRegion,
                AvailableRegions = new List<string> { "eu-west-1", "us-east-1", "sa-east-1" }
            };
            _context.Organizations.Add(org);
            
            var user = new User
            {
                Id = userId,
                OrganizationId = orgId,
                Email = "test@example.com",
                PasswordHash = "hash",
                DataResidencyRegion = "eu-west-1",
                CreatedInRegion = "eu-west-1",
                CrossBorderConsentGiven = true
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            
            _healthMonitorMock.Setup(h => h.IsRegionHealthyAsync(primaryRegion))
                .ReturnsAsync(false);
            
            _healthMonitorMock.Setup(h => h.GetNearestHealthyRegionAsync(
                primaryRegion,
                It.IsAny<List<string>>()))
                .ReturnsAsync("us-east-1");
            
            _healthMonitorMock.Setup(h => h.GetAllRegionHealthAsync())
                .ReturnsAsync(new Dictionary<string, RegionHealth>
                {
                    { "us-east-1", new RegionHealth { Region = "us-east-1", IsHealthy = true, HealthScore = 100 } },
                    { "sa-east-1", new RegionHealth { Region = "sa-east-1", IsHealthy = true, HealthScore = 90 } }
                });
            
            // Act
            var decision = await _service.SelectRegionAsync(orgId, userId, primaryRegion);
            
            // Assert
            Assert.Equal("us-east-1", decision.SelectedRegion);
            Assert.True(decision.IsFailover);
            Assert.False(decision.NeedsCrossBorderConsent); // User has consent
            Assert.Contains("unhealthy", decision.Reason);
        }
        
        [Fact]
        public async Task SelectRegionAsync_NoConsentGiven_NeedsConsent()
        {
            // Arrange
            var orgId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var primaryRegion = "eu-west-1";
            
            var org = new Organization
            {
                Id = orgId,
                Slug = "test-org",
                Name = "Test Organization",
                PrimaryRegion = primaryRegion,
                AvailableRegions = new List<string> { "eu-west-1", "us-east-1" }
            };
            _context.Organizations.Add(org);
            
            var user = new User
            {
                Id = userId,
                OrganizationId = orgId,
                Email = "test@example.com",
                PasswordHash = "hash",
                DataResidencyRegion = "eu-west-1",
                CreatedInRegion = "eu-west-1",
                CrossBorderConsentGiven = false // No consent
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            
            _healthMonitorMock.Setup(h => h.IsRegionHealthyAsync(primaryRegion))
                .ReturnsAsync(false);
            
            _healthMonitorMock.Setup(h => h.GetNearestHealthyRegionAsync(
                primaryRegion,
                It.IsAny<List<string>>()))
                .ReturnsAsync("us-east-1");
            
            _healthMonitorMock.Setup(h => h.GetAllRegionHealthAsync())
                .ReturnsAsync(new Dictionary<string, RegionHealth>
                {
                    { "us-east-1", new RegionHealth { Region = "us-east-1", IsHealthy = true, HealthScore = 100 } }
                });
            
            // Act
            var decision = await _service.SelectRegionAsync(orgId, userId, primaryRegion);
            
            // Assert
            Assert.Equal("us-east-1", decision.SelectedRegion);
            Assert.True(decision.IsFailover);
            Assert.True(decision.NeedsCrossBorderConsent);
        }
        
        [Fact]
        public async Task FailoverAsync_TargetHealthy_WithConsent_Succeeds()
        {
            // Arrange
            var orgId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var fromRegion = "eu-west-1";
            var toRegion = "us-east-1";
            
            var user = new User
            {
                Id = userId,
                OrganizationId = orgId,
                Email = "test@example.com",
                PasswordHash = "hash",
                DataResidencyRegion = "eu-west-1",
                CreatedInRegion = "eu-west-1",
                CrossBorderConsentGiven = true
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            
            _healthMonitorMock.Setup(h => h.IsRegionHealthyAsync(toRegion))
                .ReturnsAsync(true);
            
            // Act
            var result = await _service.FailoverAsync(orgId, userId, fromRegion, toRegion);
            
            // Assert
            Assert.True(result.Success);
            Assert.Equal(toRegion, result.TargetRegion);
            Assert.NotNull(result.Message);
        }
        
        [Fact]
        public async Task FailoverAsync_TargetUnhealthy_Fails()
        {
            // Arrange
            var orgId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var fromRegion = "eu-west-1";
            var toRegion = "us-east-1";
            
            _healthMonitorMock.Setup(h => h.IsRegionHealthyAsync(toRegion))
                .ReturnsAsync(false);
            
            // Act
            var result = await _service.FailoverAsync(orgId, userId, fromRegion, toRegion);
            
            // Assert
            Assert.False(result.Success);
            Assert.Contains("unhealthy", result.Message);
        }
        
        [Fact]
        public async Task FailoverAsync_NoConsent_ReturnsConsentRequired()
        {
            // Arrange
            var orgId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var fromRegion = "eu-west-1";
            var toRegion = "us-east-1";
            
            var user = new User
            {
                Id = userId,
                OrganizationId = orgId,
                Email = "test@example.com",
                PasswordHash = "hash",
                DataResidencyRegion = "eu-west-1",
                CreatedInRegion = "eu-west-1",
                CrossBorderConsentGiven = false
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            
            _healthMonitorMock.Setup(h => h.IsRegionHealthyAsync(toRegion))
                .ReturnsAsync(true);
            
            // Act
            var result = await _service.FailoverAsync(orgId, userId, fromRegion, toRegion);
            
            // Assert
            Assert.False(result.Success);
            Assert.True(result.ConsentRequired);
            Assert.NotNull(result.ConsentUrl);
            Assert.Contains("/consent/cross-border", result.ConsentUrl);
        }
        
        [Fact]
        public async Task HasCrossBorderConsentAsync_UserHasConsent_ReturnsTrue()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                OrganizationId = Guid.NewGuid(),
                Email = "test@example.com",
                PasswordHash = "hash",
                DataResidencyRegion = "eu-west-1",
                CreatedInRegion = "eu-west-1",
                CrossBorderConsentGiven = true
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            
            // Act
            var hasConsent = await _service.HasCrossBorderConsentAsync(userId);
            
            // Assert
            Assert.True(hasConsent);
        }
        
        [Fact]
        public async Task HasCrossBorderConsentAsync_UserNoConsent_ReturnsFalse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                OrganizationId = Guid.NewGuid(),
                Email = "test@example.com",
                PasswordHash = "hash",
                DataResidencyRegion = "eu-west-1",
                CreatedInRegion = "eu-west-1",
                CrossBorderConsentGiven = false
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            
            // Act
            var hasConsent = await _service.HasCrossBorderConsentAsync(userId);
            
            // Assert
            Assert.False(hasConsent);
        }
        
        [Fact]
        public async Task CanRecoverToPrimaryAsync_HighHealthScore_ReturnsTrue()
        {
            // Arrange
            var region = "eu-west-1";
            
            _healthMonitorMock.Setup(h => h.CheckRegionHealthAsync(region))
                .ReturnsAsync(new RegionHealth
                {
                    Region = region,
                    IsHealthy = true,
                    HealthScore = 95
                });
            
            // Act
            var canRecover = await _service.CanRecoverToPrimaryAsync(region);
            
            // Assert
            Assert.True(canRecover);
        }
        
        [Fact]
        public async Task CanRecoverToPrimaryAsync_LowHealthScore_ReturnsFalse()
        {
            // Arrange
            var region = "us-east-1";
            
            _healthMonitorMock.Setup(h => h.CheckRegionHealthAsync(region))
                .ReturnsAsync(new RegionHealth
                {
                    Region = region,
                    IsHealthy = true,
                    HealthScore = 75 // Below 90 threshold
                });
            
            // Act
            var canRecover = await _service.CanRecoverToPrimaryAsync(region);
            
            // Assert
            Assert.False(canRecover);
        }
        
        [Theory]
        [InlineData("eu-west-1", "us-east-1", false)]
        [InlineData("us-east-1", "eu-west-1", false)]
        [InlineData("sa-east-1", "us-east-1", false)]
        [InlineData("eu-west-1", "us-east-1", true)]
        public void GetFailoverNotificationMessage_GeneratesCorrectMessage(
            string fromRegion, 
            string toRegion, 
            bool needsConsent)
        {
            // Act
            var message = _service.GetFailoverNotificationMessage(fromRegion, toRegion, needsConsent);
            
            // Assert
            Assert.NotNull(message);
            Assert.NotEmpty(message);
            
            if (needsConsent)
            {
                Assert.Contains("consent", message.ToLower());
                Assert.Contains("cross-border", message.ToLower());
            }
            else
            {
                Assert.Contains("automatically routed", message.ToLower());
            }
        }
        
        [Fact]
        public async Task RecordCrossBorderTransferAsync_LogsTransfer()
        {
            // Arrange
            var orgId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var fromRegion = "eu-west-1";
            var toRegion = "us-east-1";
            var legalBasis = "consent";
            
            // Act & Assert (should not throw)
            await _service.RecordCrossBorderTransferAsync(orgId, userId, fromRegion, toRegion, legalBasis);
            
            // Verify logging occurred
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Cross-border transfer recorded")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
        
        [Fact]
        public async Task SelectRegionAsync_NoAvailableRegions_ReturnsPrimary()
        {
            // Arrange
            var orgId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var primaryRegion = "eu-west-1";
            
            var org = new Organization
            {
                Id = orgId,
                Slug = "test-org",
                Name = "Test Organization",
                PrimaryRegion = primaryRegion,
                AvailableRegions = new List<string> { primaryRegion } // Only primary available
            };
            _context.Organizations.Add(org);
            await _context.SaveChangesAsync();
            
            _healthMonitorMock.Setup(h => h.IsRegionHealthyAsync(primaryRegion))
                .ReturnsAsync(false);
            
            // Act
            var decision = await _service.SelectRegionAsync(orgId, userId, primaryRegion);
            
            // Assert
            Assert.Equal(primaryRegion, decision.SelectedRegion);
            Assert.False(decision.IsFailover);
            Assert.Contains("No failover regions available", decision.Reason);
        }
        
        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
