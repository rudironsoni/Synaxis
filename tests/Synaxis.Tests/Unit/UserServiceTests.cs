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
    public class UserServiceTests : IDisposable
    {
        private readonly SynaxisDbContext _context;
        private readonly IUserService _service;
        private readonly Guid _testOrgId;
        
        public UserServiceTests()
        {
            var options = new DbContextOptionsBuilder<SynaxisDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
                
            _context = new SynaxisDbContext(options);
            _service = new UserService(_context);
            
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
            _context.SaveChanges();
        }
        
        [Fact]
        public async Task CreateUserAsync_ValidRequest_CreatesUser()
        {
            // Arrange
            var request = new CreateUserRequest
            {
                OrganizationId = _testOrgId,
                Email = "test@example.com",
                Password = "SecurePassword123!",
                FirstName = "John",
                LastName = "Doe",
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1"
            };
            
            // Act
            var result = await _service.CreateUserAsync(request);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal("test@example.com", result.Email);
            Assert.Equal("John", result.FirstName);
            Assert.Equal("Doe", result.LastName);
            Assert.Equal("us-east-1", result.DataResidencyRegion);
            Assert.True(result.IsActive);
            Assert.NotNull(result.PasswordHash);
        }
        
        [Fact]
        public async Task CreateUserAsync_DuplicateEmail_ThrowsException()
        {
            // Arrange
            var request1 = new CreateUserRequest
            {
                OrganizationId = _testOrgId,
                Email = "test@example.com",
                Password = "Password123!",
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1"
            };
            
            var request2 = new CreateUserRequest
            {
                OrganizationId = _testOrgId,
                Email = "test@example.com",
                Password = "Password456!",
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1"
            };
            
            await _service.CreateUserAsync(request1);
            
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CreateUserAsync(request2));
        }
        
        [Fact]
        public async Task HashPassword_ValidPassword_ReturnsHash()
        {
            // Arrange
            var password = "SecurePassword123!";
            
            // Act
            var hash = _service.HashPassword(password);
            
            // Assert
            Assert.NotNull(hash);
            Assert.NotEmpty(hash);
            Assert.NotEqual(password, hash);
        }
        
        [Fact]
        public void VerifyPassword_CorrectPassword_ReturnsTrue()
        {
            // Arrange
            var password = "SecurePassword123!";
            var hash = _service.HashPassword(password);
            
            // Act
            var result = _service.VerifyPassword(password, hash);
            
            // Assert
            Assert.True(result);
        }
        
        [Fact]
        public void VerifyPassword_IncorrectPassword_ReturnsFalse()
        {
            // Arrange
            var password = "SecurePassword123!";
            var wrongPassword = "WrongPassword456!";
            var hash = _service.HashPassword(password);
            
            // Act
            var result = _service.VerifyPassword(wrongPassword, hash);
            
            // Assert
            Assert.False(result);
        }
        
        [Fact]
        public async Task AuthenticateAsync_ValidCredentials_ReturnsUser()
        {
            // Arrange
            var password = "SecurePassword123!";
            var request = new CreateUserRequest
            {
                OrganizationId = _testOrgId,
                Email = "test@example.com",
                Password = password,
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1"
            };
            
            await _service.CreateUserAsync(request);
            
            // Act
            var result = await _service.AuthenticateAsync("test@example.com", password);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal("test@example.com", result.Email);
            Assert.NotNull(result.LastLoginAt);
        }
        
        [Fact]
        public async Task AuthenticateAsync_InvalidPassword_ThrowsException()
        {
            // Arrange
            var request = new CreateUserRequest
            {
                OrganizationId = _testOrgId,
                Email = "test@example.com",
                Password = "CorrectPassword123!",
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1"
            };
            
            await _service.CreateUserAsync(request);
            
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _service.AuthenticateAsync("test@example.com", "WrongPassword456!"));
        }
        
        [Fact]
        public async Task AuthenticateAsync_MultipleFailedAttempts_LocksAccount()
        {
            // Arrange
            var request = new CreateUserRequest
            {
                OrganizationId = _testOrgId,
                Email = "test@example.com",
                Password = "CorrectPassword123!",
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1"
            };
            
            await _service.CreateUserAsync(request);
            
            // Act - Make 5 failed attempts
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    await _service.AuthenticateAsync("test@example.com", "WrongPassword!");
                }
                catch (UnauthorizedAccessException)
                {
                    // Expected
                }
            }
            
            // Assert - Account should be locked
            var user = await _service.GetUserByEmailAsync("test@example.com");
            Assert.True(user.LockedUntil.HasValue);
            Assert.True(user.LockedUntil.Value > DateTime.UtcNow);
        }
        
        [Fact]
        public async Task GetUserAsync_ExistingId_ReturnsUser()
        {
            // Arrange
            var request = new CreateUserRequest
            {
                OrganizationId = _testOrgId,
                Email = "test@example.com",
                Password = "Password123!",
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1"
            };
            
            var created = await _service.CreateUserAsync(request);
            
            // Act
            var result = await _service.GetUserAsync(created.Id);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(created.Id, result.Id);
        }
        
        [Fact]
        public async Task GetUserByEmailAsync_ExistingEmail_ReturnsUser()
        {
            // Arrange
            var request = new CreateUserRequest
            {
                OrganizationId = _testOrgId,
                Email = "test@example.com",
                Password = "Password123!",
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1"
            };
            
            await _service.CreateUserAsync(request);
            
            // Act
            var result = await _service.GetUserByEmailAsync("test@example.com");
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal("test@example.com", result.Email);
        }
        
        [Fact]
        public async Task UpdateUserAsync_ValidRequest_UpdatesUser()
        {
            // Arrange
            var createRequest = new CreateUserRequest
            {
                OrganizationId = _testOrgId,
                Email = "test@example.com",
                Password = "Password123!",
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1"
            };
            
            var created = await _service.CreateUserAsync(createRequest);
            
            var updateRequest = new UpdateUserRequest
            {
                FirstName = "Jane",
                LastName = "Smith",
                Timezone = "America/Los_Angeles"
            };
            
            // Act
            var result = await _service.UpdateUserAsync(created.Id, updateRequest);
            
            // Assert
            Assert.Equal("Jane", result.FirstName);
            Assert.Equal("Smith", result.LastName);
            Assert.Equal("America/Los_Angeles", result.Timezone);
        }
        
        [Fact]
        public async Task DeleteUserAsync_ExistingUser_SoftDeletes()
        {
            // Arrange
            var request = new CreateUserRequest
            {
                OrganizationId = _testOrgId,
                Email = "test@example.com",
                Password = "Password123!",
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1"
            };
            
            var created = await _service.CreateUserAsync(request);
            
            // Act
            var result = await _service.DeleteUserAsync(created.Id);
            
            // Assert
            Assert.True(result);
            
            var user = await _context.Users.FindAsync(created.Id);
            Assert.False(user.IsActive);
        }
        
        [Fact]
        public async Task SetupMfaAsync_ValidUser_ReturnsSetupResult()
        {
            // Arrange
            var request = new CreateUserRequest
            {
                OrganizationId = _testOrgId,
                Email = "test@example.com",
                Password = "Password123!",
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1"
            };
            
            var user = await _service.CreateUserAsync(request);
            
            // Act
            var result = await _service.SetupMfaAsync(user.Id);
            
            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Secret);
            Assert.NotNull(result.QrCodeUrl);
            Assert.NotNull(result.ManualEntryKey);
            Assert.Contains("otpauth://totp/", result.QrCodeUrl);
        }
        
        [Fact]
        public async Task UpdateCrossBorderConsentAsync_ValidRequest_UpdatesConsent()
        {
            // Arrange
            var request = new CreateUserRequest
            {
                OrganizationId = _testOrgId,
                Email = "test@example.com",
                Password = "Password123!",
                DataResidencyRegion = "eu-west-1",
                CreatedInRegion = "eu-west-1"
            };
            
            var user = await _service.CreateUserAsync(request);
            
            // Act
            var result = await _service.UpdateCrossBorderConsentAsync(user.Id, true, "1.0");
            
            // Assert
            Assert.True(result);
            
            var updatedUser = await _service.GetUserAsync(user.Id);
            Assert.True(updatedUser.CrossBorderConsentGiven);
            Assert.NotNull(updatedUser.CrossBorderConsentDate);
            Assert.Equal("1.0", updatedUser.CrossBorderConsentVersion);
        }
        
        [Fact]
        public async Task HasCrossBorderConsentAsync_ConsentGiven_ReturnsTrue()
        {
            // Arrange
            var request = new CreateUserRequest
            {
                OrganizationId = _testOrgId,
                Email = "test@example.com",
                Password = "Password123!",
                DataResidencyRegion = "eu-west-1",
                CreatedInRegion = "eu-west-1"
            };
            
            var user = await _service.CreateUserAsync(request);
            await _service.UpdateCrossBorderConsentAsync(user.Id, true, "1.0");
            
            // Act
            var result = await _service.HasCrossBorderConsentAsync(user.Id);
            
            // Assert
            Assert.True(result);
        }
        
        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
