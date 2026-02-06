using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Synaxis.InferenceGateway.Infrastructure.Contracts;
using Synaxis.Core.Contracts;
using Synaxis.Core.Models;
using Synaxis.Infrastructure.Data;
using Synaxis.Infrastructure.MultiRegion;
using Xunit;

namespace Synaxis.Tests.Unit
{
    public class RegionRouterTests : IDisposable
    {
        private readonly SynaxisDbContext _context;
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly Mock<ILogger<RegionRouter>> _loggerMock;
        private readonly IRegionRouter _service;
        private readonly Guid _testOrgId;
        private readonly Guid _testUserId;
        
        public RegionRouterTests()
        {
            var options = new DbContextOptionsBuilder<SynaxisDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
                
            _context = new SynaxisDbContext(options);
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _loggerMock = new Mock<ILogger<RegionRouter>>();
            
            // Create test organization
            _testOrgId = Guid.NewGuid();
            _context.Organizations.Add(new Organization
            {
                Id = _testOrgId,
                Name = "Test Org",
                Slug = "test-org",
                PrimaryRegion = "us-east-1",
                IsActive = true
            });
            
            // Create test user
            _testUserId = Guid.NewGuid();
            _context.Users.Add(new User
            {
                Id = _testUserId,
                OrganizationId = _testOrgId,
                Email = "test@example.com",
                PasswordHash = "hash",
                DataResidencyRegion = "eu-west-1",
                CreatedInRegion = "eu-west-1",
                IsActive = true
            });
            
            _context.SaveChanges();
            
            _service = new RegionRouter(_context, _httpClientFactoryMock.Object, _loggerMock.Object, "us-east-1");
        }
        
        [Fact]
        public async Task GetUserRegionAsync_ExistingUser_ReturnsRegion()
        {
            // Act
            var result = await _service.GetUserRegionAsync(_testUserId);
            
            // Assert
            Assert.Equal("eu-west-1", result);
        }
        
        [Fact]
        public async Task GetUserRegionAsync_NonExistentUser_ThrowsException()
        {
            // Arrange
            var nonExistentUserId = Guid.NewGuid();
            
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.GetUserRegionAsync(nonExistentUserId));
        }
        
        [Theory]
        [InlineData("us-east-1", "eu-west-1", true)]
        [InlineData("eu-west-1", "us-east-1", true)]
        [InlineData("us-east-1", "us-east-1", false)]
        [InlineData("eu-west-1", "eu-west-1", false)]
        public async Task IsCrossBorderAsync_ValidRegions_ReturnsExpectedResult(
            string fromRegion, string toRegion, bool expected)
        {
            // Act
            var result = await _service.IsCrossBorderAsync(fromRegion, toRegion);
            
            // Assert
            Assert.Equal(expected, result);
        }
        
        [Fact]
        public async Task RequiresCrossBorderConsentAsync_EuUserToUsRegion_ReturnsTrue()
        {
            // Arrange - User in EU region
            var euUserId = Guid.NewGuid();
            _context.Users.Add(new User
            {
                Id = euUserId,
                OrganizationId = _testOrgId,
                Email = "eu-user@example.com",
                PasswordHash = "hash",
                DataResidencyRegion = "eu-west-1",
                CreatedInRegion = "eu-west-1",
                IsActive = true
            });
            await _context.SaveChangesAsync();
            
            // Act
            var result = await _service.RequiresCrossBorderConsentAsync(euUserId, "us-east-1");
            
            // Assert
            Assert.True(result);
        }
        
        [Fact]
        public async Task RequiresCrossBorderConsentAsync_SameRegion_ReturnsFalse()
        {
            // Arrange - User in US region
            var usUserId = Guid.NewGuid();
            _context.Users.Add(new User
            {
                Id = usUserId,
                OrganizationId = _testOrgId,
                Email = "us-user@example.com",
                PasswordHash = "hash",
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1",
                IsActive = true
            });
            await _context.SaveChangesAsync();
            
            // Act
            var result = await _service.RequiresCrossBorderConsentAsync(usUserId, "us-east-1");
            
            // Assert
            Assert.False(result);
        }
        
        [Fact]
        public async Task RequiresCrossBorderConsentAsync_BrazilUserToUsRegion_ReturnsTrue()
        {
            // Arrange - User in Brazil region
            var brUserId = Guid.NewGuid();
            _context.Users.Add(new User
            {
                Id = brUserId,
                OrganizationId = _testOrgId,
                Email = "br-user@example.com",
                PasswordHash = "hash",
                DataResidencyRegion = "sa-east-1",
                CreatedInRegion = "sa-east-1",
                IsActive = true
            });
            await _context.SaveChangesAsync();
            
            // Act
            var result = await _service.RequiresCrossBorderConsentAsync(brUserId, "us-east-1");
            
            // Assert
            Assert.True(result);
        }
        
        [Fact]
        public async Task GetNearestHealthyRegionAsync_WithLocation_ReturnsNearestRegion()
        {
            // Arrange
            var location = new GeoLocation
            {
                Latitude = 51.5074,  // London
                Longitude = -0.1278,
                CountryCode = "GB"
            };
            
            // Act
            var result = await _service.GetNearestHealthyRegionAsync("us-east-1", location);
            
            // Assert
            Assert.NotNull(result);
            Assert.Contains(result, new[] { "eu-west-1", "us-east-1", "sa-east-1" });
        }
        
        [Fact]
        public async Task LogCrossBorderTransferAsync_ValidContext_LogsTransfer()
        {
            // Arrange
            var context = new CrossBorderTransferContext
            {
                OrganizationId = _testOrgId,
                UserId = _testUserId,
                FromRegion = "us-east-1",
                ToRegion = "eu-west-1",
                LegalBasis = "SCC",
                Purpose = "API request routing",
                DataCategories = new[] { "user_data", "request_data" }
            };
            
            // Act
            await _service.LogCrossBorderTransferAsync(context);
            
            // Assert - Verify logger was called
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }
        
        [Fact]
        public async Task ProcessLocallyAsync_ValidRequest_ThrowsNotImplemented()
        {
            // Arrange
            var request = new { Data = "test" };
            
            // Act & Assert
            await Assert.ThrowsAsync<NotImplementedException>(
                () => _service.ProcessLocallyAsync<object, object>(request));
        }
        
        [Theory]
        [InlineData(40.7128, -74.0060, 51.5074, -0.1278)] // NY to London
        [InlineData(51.5074, -0.1278, -23.5505, -46.6333)] // London to São Paulo
        public async Task GetNearestHealthyRegionAsync_DifferentLocations_ReturnsValidRegion(
            double lat1, double lon1, double lat2, double lon2)
        {
            // Arrange
            var location = new GeoLocation
            {
                Latitude = lat2,
                Longitude = lon2,
                CountryCode = "XX"
            };
            
            // Act
            var result = await _service.GetNearestHealthyRegionAsync("us-east-1", location);
            
            // Assert
            Assert.NotNull(result);
            Assert.True(result == "eu-west-1" || result == "us-east-1" || result == "sa-east-1");
        }
        
        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
