using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Synaxis.InferenceGateway.Infrastructure.Contracts;
using Synaxis.Core.Contracts;
using Synaxis.Core.Models;
using Synaxis.Infrastructure.Data;
using Synaxis.Infrastructure.Services;
using Xunit;

namespace Synaxis.Tests.Unit
{
    public class TenantServiceTests : IDisposable
    {
        private readonly SynaxisDbContext _context;
        private readonly ITenantService _service;
        
        public TenantServiceTests()
        {
            var options = new DbContextOptionsBuilder<SynaxisDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
                
            _context = new SynaxisDbContext(options);
            _service = new TenantService(_context);
            
            SeedSubscriptionPlans();
        }
        
        private void SeedSubscriptionPlans()
        {
            _context.SubscriptionPlans.Add(new SubscriptionPlan
            {
                Slug = "free",
                Name = "Free",
                LimitsConfig = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "max_teams", 1 },
                    { "max_users_per_team", 3 },
                    { "max_keys_per_user", 2 },
                    { "max_concurrent_requests", 10 },
                    { "monthly_request_limit", 10000L },
                    { "monthly_token_limit", 100000L },
                    { "data_retention_days", 30 }
                }
            });
            
            _context.SubscriptionPlans.Add(new SubscriptionPlan
            {
                Slug = "pro",
                Name = "Pro",
                LimitsConfig = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "max_teams", 5 },
                    { "max_users_per_team", 20 },
                    { "max_keys_per_user", 10 },
                    { "max_concurrent_requests", 100 },
                    { "monthly_request_limit", 100000L },
                    { "monthly_token_limit", 10000000L },
                    { "data_retention_days", 90 }
                }
            });
            
            _context.SaveChanges();
        }
        
        [Fact]
        public async Task CreateOrganizationAsync_ValidRequest_CreatesOrganization()
        {
            // Arrange
            var request = new CreateOrganizationRequest
            {
                Name = "Test Org",
                Slug = "test-org",
                PrimaryRegion = "us-east-1",
                BillingCurrency = "USD"
            };
            
            // Act
            var result = await _service.CreateOrganizationAsync(request);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Org", result.Name);
            Assert.Equal("test-org", result.Slug);
            Assert.Equal("us-east-1", result.PrimaryRegion);
            Assert.Equal("USD", result.BillingCurrency);
            Assert.Equal("free", result.Tier);
            Assert.True(result.IsActive);
            Assert.False(result.IsVerified);
        }
        
        [Fact]
        public async Task CreateOrganizationAsync_NullRequest_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _service.CreateOrganizationAsync(null));
        }
        
        [Fact]
        public async Task CreateOrganizationAsync_EmptyName_ThrowsArgumentException()
        {
            // Arrange
            var request = new CreateOrganizationRequest
            {
                Name = "",
                Slug = "test-org",
                PrimaryRegion = "us-east-1"
            };
            
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateOrganizationAsync(request));
        }
        
        [Fact]
        public async Task CreateOrganizationAsync_EmptySlug_ThrowsArgumentException()
        {
            // Arrange
            var request = new CreateOrganizationRequest
            {
                Name = "Test Org",
                Slug = "",
                PrimaryRegion = "us-east-1"
            };
            
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateOrganizationAsync(request));
        }
        
        [Fact]
        public async Task CreateOrganizationAsync_EmptyPrimaryRegion_ThrowsArgumentException()
        {
            // Arrange
            var request = new CreateOrganizationRequest
            {
                Name = "Test Org",
                Slug = "test-org",
                PrimaryRegion = ""
            };
            
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateOrganizationAsync(request));
        }
        
        [Fact]
        public async Task CreateOrganizationAsync_DuplicateSlug_ThrowsInvalidOperationException()
        {
            // Arrange
            var request1 = new CreateOrganizationRequest
            {
                Name = "Test Org 1",
                Slug = "test-org",
                PrimaryRegion = "us-east-1"
            };
            
            var request2 = new CreateOrganizationRequest
            {
                Name = "Test Org 2",
                Slug = "test-org",
                PrimaryRegion = "us-east-1"
            };
            
            await _service.CreateOrganizationAsync(request1);
            
            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CreateOrganizationAsync(request2));
            Assert.Contains("already exists", exception.Message);
        }
        
        [Fact]
        public async Task GetOrganizationAsync_ExistingId_ReturnsOrganization()
        {
            // Arrange
            var request = new CreateOrganizationRequest
            {
                Name = "Test Org",
                Slug = "test-org",
                PrimaryRegion = "us-east-1"
            };
            
            var created = await _service.CreateOrganizationAsync(request);
            
            // Act
            var result = await _service.GetOrganizationAsync(created.Id);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(created.Id, result.Id);
            Assert.Equal("Test Org", result.Name);
        }
        
        [Fact]
        public async Task GetOrganizationAsync_NonExistingId_ThrowsInvalidOperationException()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();
            
            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.GetOrganizationAsync(nonExistingId));
            Assert.Contains("not found", exception.Message);
        }
        
        [Fact]
        public async Task GetOrganizationBySlugAsync_ExistingSlug_ReturnsOrganization()
        {
            // Arrange
            var request = new CreateOrganizationRequest
            {
                Name = "Test Org",
                Slug = "test-org",
                PrimaryRegion = "us-east-1"
            };
            
            await _service.CreateOrganizationAsync(request);
            
            // Act
            var result = await _service.GetOrganizationBySlugAsync("test-org");
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal("test-org", result.Slug);
        }
        
        [Fact]
        public async Task GetOrganizationBySlugAsync_EmptySlug_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.GetOrganizationBySlugAsync(""));
        }
        
        [Fact]
        public async Task GetOrganizationBySlugAsync_NonExistingSlug_ThrowsInvalidOperationException()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.GetOrganizationBySlugAsync("non-existing"));
            Assert.Contains("not found", exception.Message);
        }
        
        [Fact]
        public async Task UpdateOrganizationAsync_ValidRequest_UpdatesOrganization()
        {
            // Arrange
            var createRequest = new CreateOrganizationRequest
            {
                Name = "Test Org",
                Slug = "test-org",
                PrimaryRegion = "us-east-1"
            };
            
            var created = await _service.CreateOrganizationAsync(createRequest);
            
            var updateRequest = new UpdateOrganizationRequest
            {
                Name = "Updated Org",
                Description = "Updated description"
            };
            
            // Act
            var result = await _service.UpdateOrganizationAsync(created.Id, updateRequest);
            
            // Assert
            Assert.Equal("Updated Org", result.Name);
            Assert.Equal("Updated description", result.Description);
        }
        
        [Fact]
        public async Task UpdateOrganizationAsync_NullRequest_ThrowsArgumentNullException()
        {
            // Arrange
            var orgId = Guid.NewGuid();
            
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _service.UpdateOrganizationAsync(orgId, null));
        }
        
        [Fact]
        public async Task UpdateOrganizationAsync_NonExistingId_ThrowsInvalidOperationException()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();
            var updateRequest = new UpdateOrganizationRequest
            {
                Name = "Updated Org"
            };
            
            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.UpdateOrganizationAsync(nonExistingId, updateRequest));
            Assert.Contains("not found", exception.Message);
        }
        
        [Fact]
        public async Task DeleteOrganizationAsync_ExistingOrg_SoftDeletes()
        {
            // Arrange
            var request = new CreateOrganizationRequest
            {
                Name = "Test Org",
                Slug = "test-org",
                PrimaryRegion = "us-east-1"
            };
            
            var created = await _service.CreateOrganizationAsync(request);
            
            // Act
            var result = await _service.DeleteOrganizationAsync(created.Id);
            
            // Assert
            Assert.True(result);
            
            var org = await _context.Organizations.FindAsync(created.Id);
            Assert.False(org.IsActive);
        }
        
        [Fact]
        public async Task DeleteOrganizationAsync_NonExistingOrg_ReturnsFalse()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();
            
            // Act
            var result = await _service.DeleteOrganizationAsync(nonExistingId);
            
            // Assert
            Assert.False(result);
        }
        
        [Fact]
        public async Task GetOrganizationLimitsAsync_FreeTier_ReturnsDefaultLimits()
        {
            // Arrange
            var request = new CreateOrganizationRequest
            {
                Name = "Test Org",
                Slug = "test-org",
                PrimaryRegion = "us-east-1"
            };
            
            var created = await _service.CreateOrganizationAsync(request);
            
            // Act
            var limits = await _service.GetOrganizationLimitsAsync(created.Id);
            
            // Assert
            Assert.Equal(1, limits.MaxTeams);
            Assert.Equal(3, limits.MaxUsersPerTeam);
            Assert.Equal(2, limits.MaxKeysPerUser);
            Assert.Equal(10, limits.MaxConcurrentRequests);
            Assert.Equal(10000, limits.MonthlyRequestLimit);
            Assert.Equal(100000, limits.MonthlyTokenLimit);
            Assert.Equal(30, limits.DataRetentionDays);
        }
        
        [Fact]
        public async Task GetOrganizationLimitsAsync_ProTier_ReturnsProLimits()
        {
            // Arrange
            var request = new CreateOrganizationRequest
            {
                Name = "Test Org",
                Slug = "test-org",
                PrimaryRegion = "us-east-1"
            };
            
            var created = await _service.CreateOrganizationAsync(request);
            created.Tier = "pro";
            await _context.SaveChangesAsync();
            
            // Act
            var limits = await _service.GetOrganizationLimitsAsync(created.Id);
            
            // Assert
            Assert.Equal(5, limits.MaxTeams);
            Assert.Equal(20, limits.MaxUsersPerTeam);
            Assert.Equal(10, limits.MaxKeysPerUser);
            Assert.Equal(100, limits.MaxConcurrentRequests);
            Assert.Equal(100000, limits.MonthlyRequestLimit);
            Assert.Equal(10000000, limits.MonthlyTokenLimit);
        }
        
        [Fact]
        public async Task GetOrganizationLimitsAsync_WithOverrides_ReturnsOverriddenLimits()
        {
            // Arrange
            var request = new CreateOrganizationRequest
            {
                Name = "Test Org",
                Slug = "test-org",
                PrimaryRegion = "us-east-1"
            };
            
            var created = await _service.CreateOrganizationAsync(request);
            
            // Apply overrides
            created.MaxTeams = 10;
            created.MaxUsersPerTeam = 50;
            created.MonthlyRequestLimit = 500000;
            await _context.SaveChangesAsync();
            
            // Act
            var limits = await _service.GetOrganizationLimitsAsync(created.Id);
            
            // Assert
            Assert.Equal(10, limits.MaxTeams);
            Assert.Equal(50, limits.MaxUsersPerTeam);
            Assert.Equal(500000, limits.MonthlyRequestLimit);
        }
        
        [Fact]
        public async Task GetOrganizationLimitsAsync_NonExistingOrg_ThrowsInvalidOperationException()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();
            
            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.GetOrganizationLimitsAsync(nonExistingId));
            Assert.Contains("not found", exception.Message);
        }
        
        [Fact]
        public async Task CanAddTeamAsync_UnderLimit_ReturnsTrue()
        {
            // Arrange
            var request = new CreateOrganizationRequest
            {
                Name = "Test Org",
                Slug = "test-org",
                PrimaryRegion = "us-east-1"
            };
            
            var created = await _service.CreateOrganizationAsync(request);
            
            // Act
            var result = await _service.CanAddTeamAsync(created.Id);
            
            // Assert
            Assert.True(result);
        }
        
        [Fact]
        public async Task CanAddTeamAsync_AtLimit_ReturnsFalse()
        {
            // Arrange
            var request = new CreateOrganizationRequest
            {
                Name = "Test Org",
                Slug = "test-org",
                PrimaryRegion = "us-east-1"
            };
            
            var created = await _service.CreateOrganizationAsync(request);
            
            // Add team up to limit (free tier has 1 team limit)
            _context.Teams.Add(new Team 
            { 
                OrganizationId = created.Id, 
                Name = "Team 1", 
                Slug = "team-1",
                IsActive = true 
            });
            await _context.SaveChangesAsync();
            
            // Act
            var result = await _service.CanAddTeamAsync(created.Id);
            
            // Assert
            Assert.False(result);
        }
        
        [Fact]
        public async Task CanAddTeamAsync_NonExistingOrg_ReturnsFalse()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();
            
            // Act
            var result = await _service.CanAddTeamAsync(nonExistingId);
            
            // Assert
            Assert.False(result);
        }
        
        [Fact]
        public async Task IsUnderConcurrentLimitAsync_AlwaysReturnsTrue()
        {
            // Arrange - this is a placeholder implementation
            var request = new CreateOrganizationRequest
            {
                Name = "Test Org",
                Slug = "test-org",
                PrimaryRegion = "us-east-1"
            };
            
            var created = await _service.CreateOrganizationAsync(request);
            
            // Act
            var result = await _service.IsUnderConcurrentLimitAsync(created.Id);
            
            // Assert
            Assert.True(result); // Current implementation always returns true
        }
        
        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
