// <copyright file="OrganizationsControllerTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Tests.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Synaxis.Core.Contracts;
    using Synaxis.Core.Models;
    using Synaxis.Infrastructure.Data;
    using Synaxis.InferenceGateway.WebApi.Controllers;
    using Synaxis.InferenceGateway.WebApi.Controllers.Organizations;
    using Xunit;
    using OrganizationSettingsResponse = Synaxis.Core.Contracts.OrganizationSettingsResponse;

    [Trait("Category", "Unit")]
    public class OrganizationsControllerTests : IDisposable
    {
        private readonly SynaxisDbContext _dbContext;
        private readonly OrganizationsController _controller;
        private readonly Guid _testUserId = Guid.NewGuid();
        private readonly Guid _testOrganizationId = Guid.NewGuid();
        private bool _disposed;

        public OrganizationsControllerTests()
        {
            var options = new DbContextOptionsBuilder<SynaxisDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;
            _dbContext = new SynaxisDbContext(options);

            _controller = new OrganizationsController(_dbContext);
            SetupControllerContext();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _dbContext.Dispose();
                _disposed = true;
            }
        }

        private Organization CreateTestOrganization()
        {
            return new Organization
            {
                Id = _testOrganizationId,
                Name = "Test Organization",
                Slug = "test-org",
                Description = "Test organization",
                PrimaryRegion = "us-east-1",
                Tier = "free",
                BillingCurrency = "USD",
                CreditBalance = 0.00m,
                CreditCurrency = "USD",
                SubscriptionStatus = "active",
                IsTrial = false,
                DataRetentionDays = 30,
                RequireSso = false,
                IsActive = true,
                IsVerified = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                AllowedEmailDomains = new List<string>(),
                AvailableRegions = new List<string>(),
                PrivacyConsent = new Dictionary<string, object>()
            };
        }

        private TeamMembership CreateTestTeamMembership(Guid userId, Guid teamId, Guid organizationId, string role = "Member")
        {
            return new TeamMembership
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TeamId = teamId,
                OrganizationId = organizationId,
                Role = role,
                JoinedAt = DateTime.UtcNow
            };
        }

        private void SetupControllerContext()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString()),
                new Claim("sub", _testUserId.ToString())
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = claimsPrincipal
                }
            };
        }
    }

    [Trait("Category", "Unit")]
    public class OrganizationSettingsControllerTests : IDisposable
    {
        private readonly SynaxisDbContext _dbContext;
        private readonly OrganizationSettingsController _controller;
        private readonly Guid _testUserId = Guid.NewGuid();
        private readonly Guid _testOrganizationId = Guid.NewGuid();
        private bool _disposed;

        public OrganizationSettingsControllerTests()
        {
            var options = new DbContextOptionsBuilder<SynaxisDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;
            _dbContext = new SynaxisDbContext(options);

            var passwordService = new TestPasswordService();
            _controller = new OrganizationSettingsController(_dbContext, passwordService);
            SetupControllerContext();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _dbContext.Dispose();
                _disposed = true;
            }
        }

        [Fact]
        public async Task GetOrganizationSettings_WhenOrganizationDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var nonExistentOrgId = Guid.NewGuid();

            // Act
            var result = await _controller.GetOrganizationSettings(nonExistentOrgId, CancellationToken.None);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task GetOrganizationSettings_WhenUserIsNotMember_ReturnsForbid()
        {
            // Arrange
            var organization = CreateTestOrganization();
            _dbContext.Organizations.Add(organization);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.GetOrganizationSettings(_testOrganizationId, CancellationToken.None);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task GetOrganizationSettings_WhenUserIsMember_ReturnsOkWithSettings()
        {
            // Arrange
            var organization = CreateTestOrganization();
            var membership = CreateTestTeamMembership(_testUserId, Guid.NewGuid(), _testOrganizationId);

            _dbContext.Organizations.Add(organization);
            _dbContext.TeamMemberships.Add(membership);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.GetOrganizationSettings(_testOrganizationId, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        private Organization CreateTestOrganization()
        {
            return new Organization
            {
                Id = _testOrganizationId,
                Name = "Test Organization",
                Slug = "test-org",
                Description = "Test organization",
                PrimaryRegion = "us-east-1",
                Tier = "free",
                BillingCurrency = "USD",
                CreditBalance = 0.00m,
                CreditCurrency = "USD",
                SubscriptionStatus = "active",
                IsTrial = false,
                DataRetentionDays = 30,
                RequireSso = false,
                IsActive = true,
                IsVerified = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                AllowedEmailDomains = new List<string>(),
                AvailableRegions = new List<string>(),
                PrivacyConsent = new Dictionary<string, object>()
            };
        }

        private TeamMembership CreateTestTeamMembership(Guid userId, Guid teamId, Guid organizationId, string role = "Member")
        {
            return new TeamMembership
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TeamId = teamId,
                OrganizationId = organizationId,
                Role = role,
                JoinedAt = DateTime.UtcNow
            };
        }

        private void SetupControllerContext()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString()),
                new Claim("sub", _testUserId.ToString())
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = claimsPrincipal
                }
            };
        }

        /// <summary>
        /// Test implementation of IPasswordService.
        /// </summary>
        private class TestPasswordService : IPasswordService
        {
            public Task<ChangePasswordResult> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
            {
                return Task.FromResult(new ChangePasswordResult { Success = true });
            }

            public Task<PasswordExpirationStatus> CheckPasswordExpirationAsync(Guid userId)
            {
                return Task.FromResult(new PasswordExpirationStatus { IsExpired = false, IsExpiringSoon = false, DaysUntilExpiration = 90 });
            }

            public int GetPasswordStrength(string password)
            {
                return 80;
            }

            public Task<PasswordPolicy> GetPasswordPolicyAsync(Guid organizationId)
            {
                return Task.FromResult(new PasswordPolicy());
            }

            public Task<ResetPasswordResult> ResetPasswordAsync(Guid userId, string newPassword)
            {
                return Task.FromResult(new ResetPasswordResult { Success = true });
            }

            public Task<PasswordValidationResult> ValidatePasswordAsync(Guid userId, string password)
            {
                return Task.FromResult(new PasswordValidationResult { IsValid = true, StrengthScore = 80, StrengthLevel = "Strong" });
            }

            public Task<PasswordPolicy> UpdatePasswordPolicyAsync(Guid organizationId, PasswordPolicy policy)
            {
                return Task.FromResult(policy);
            }
        }
    }

    [Trait("Category", "Unit")]
    public class OrganizationApiKeysControllerTests : IDisposable
    {
        private readonly SynaxisDbContext _dbContext;
        private readonly OrganizationApiKeysController _controller;
        private readonly Guid _testUserId = Guid.NewGuid();
        private readonly Guid _testOrganizationId = Guid.NewGuid();
        private bool _disposed;

        public OrganizationApiKeysControllerTests()
        {
            var options = new DbContextOptionsBuilder<SynaxisDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;
            _dbContext = new SynaxisDbContext(options);

            _controller = new OrganizationApiKeysController(_dbContext);
            SetupControllerContext();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _dbContext.Dispose();
                _disposed = true;
            }
        }

        [Fact]
        public async Task GetApiKey_WhenOrganizationNotFound_ReturnsNotFound()
        {
            // Arrange
            var nonExistentOrgId = Guid.NewGuid();
            var apiKeyId = Guid.NewGuid();

            // Act
            var result = await _controller.GetApiKey(nonExistentOrgId, apiKeyId, CancellationToken.None);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult!.Value.Should().Be("Organization not found");
        }

        [Fact]
        public async Task GetApiKey_WhenApiKeyNotFound_ReturnsNotFound()
        {
            // Arrange
            var organization = CreateTestOrganization();
            var membership = CreateTestTeamMembership(_testUserId, Guid.NewGuid(), _testOrganizationId, "Member");

            _dbContext.Organizations.Add(organization);
            _dbContext.TeamMemberships.Add(membership);
            await _dbContext.SaveChangesAsync();

            var nonExistentKeyId = Guid.NewGuid();

            // Act
            var result = await _controller.GetApiKey(_testOrganizationId, nonExistentKeyId, CancellationToken.None);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult!.Value.Should().Be("API key not found");
        }

        [Fact]
        public async Task UpdateApiKey_WithValidRequest_ReturnsOk()
        {
            // Arrange
            var organization = CreateTestOrganization();
            var membership = CreateTestTeamMembership(_testUserId, Guid.NewGuid(), _testOrganizationId, "OrgAdmin");

            _dbContext.Organizations.Add(organization);
            _dbContext.TeamMemberships.Add(membership);

            var apiKey = new OrganizationApiKey
            {
                Id = Guid.NewGuid(),
                OrganizationId = _testOrganizationId,
                CreatedBy = _testUserId,
                Name = "Original Name",
                KeyHash = "test-hash",
                KeyPrefix = "testpref",
                Permissions = new Dictionary<string, object>(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.OrganizationApiKeys.Add(apiKey);
            await _dbContext.SaveChangesAsync();

            var request = new UpdateOrganizationApiKeyRequest
            {
                Name = "Updated Name",
                Permissions = new Dictionary<string, object> { { "write", true } }
            };

            // Act
            var result = await _controller.UpdateApiKey(_testOrganizationId, apiKey.Id, request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var response = okResult!.Value as OrganizationApiKeyResponse;
            response.Should().NotBeNull();
            response!.Name.Should().Be("Updated Name");

            // Verify API key was updated
            var updatedApiKey = await _dbContext.OrganizationApiKeys.FindAsync(apiKey.Id);
            updatedApiKey!.Name.Should().Be("Updated Name");
        }

        [Fact]
        public async Task UpdateApiKey_WhenRevokeIsTrue_SetsRevokedAt()
        {
            // Arrange
            var organization = CreateTestOrganization();
            var membership = CreateTestTeamMembership(_testUserId, Guid.NewGuid(), _testOrganizationId, "OrgAdmin");

            _dbContext.Organizations.Add(organization);
            _dbContext.TeamMemberships.Add(membership);

            var apiKey = new OrganizationApiKey
            {
                Id = Guid.NewGuid(),
                OrganizationId = _testOrganizationId,
                CreatedBy = _testUserId,
                Name = "Test API Key",
                KeyHash = "test-hash",
                KeyPrefix = "testpref",
                Permissions = new Dictionary<string, object>(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.OrganizationApiKeys.Add(apiKey);
            await _dbContext.SaveChangesAsync();

            var request = new UpdateOrganizationApiKeyRequest
            {
                Revoke = true,
                RevokedReason = "Security concern"
            };

            // Act
            var result = await _controller.UpdateApiKey(_testOrganizationId, apiKey.Id, request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var response = okResult!.Value as OrganizationApiKeyResponse;
            response.Should().NotBeNull();
            response!.IsActive.Should().BeFalse();
            response.RevokedAt.Should().NotBeNull();

            // Verify API key was revoked
            var revokedApiKey = await _dbContext.OrganizationApiKeys.FindAsync(apiKey.Id);
            revokedApiKey!.IsActive.Should().BeFalse();
            revokedApiKey.RevokedAt.Should().NotBeNull();
            revokedApiKey.RevokedReason.Should().Be("Security concern");
        }

        [Fact]
        public async Task UpdateApiKey_WhenNotAdmin_ReturnsForbid()
        {
            // Arrange
            var organization = CreateTestOrganization();
            var membership = CreateTestTeamMembership(_testUserId, Guid.NewGuid(), _testOrganizationId, "Member");

            _dbContext.Organizations.Add(organization);
            _dbContext.TeamMemberships.Add(membership);

            var apiKey = new OrganizationApiKey
            {
                Id = Guid.NewGuid(),
                OrganizationId = _testOrganizationId,
                CreatedBy = _testUserId,
                Name = "Test API Key",
                KeyHash = "test-hash",
                KeyPrefix = "testpref",
                Permissions = new Dictionary<string, object>(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.OrganizationApiKeys.Add(apiKey);
            await _dbContext.SaveChangesAsync();

            var request = new UpdateOrganizationApiKeyRequest { Name = "Updated Name" };

            // Act
            var result = await _controller.UpdateApiKey(_testOrganizationId, apiKey.Id, request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task UpdateApiKey_WhenApiKeyNotFound_ReturnsNotFound()
        {
            // Arrange
            var organization = CreateTestOrganization();
            var membership = CreateTestTeamMembership(_testUserId, Guid.NewGuid(), _testOrganizationId, "OrgAdmin");

            _dbContext.Organizations.Add(organization);
            _dbContext.TeamMemberships.Add(membership);
            await _dbContext.SaveChangesAsync();

            var nonExistentKeyId = Guid.NewGuid();
            var request = new UpdateOrganizationApiKeyRequest { Name = "Updated Name" };

            // Act
            var result = await _controller.UpdateApiKey(_testOrganizationId, nonExistentKeyId, request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult!.Value.Should().Be("API key not found");
        }

        [Fact]
        public async Task DeleteApiKey_WithValidRequest_ReturnsNoContent()
        {
            // Arrange
            var organization = CreateTestOrganization();
            var membership = CreateTestTeamMembership(_testUserId, Guid.NewGuid(), _testOrganizationId, "OrgAdmin");

            _dbContext.Organizations.Add(organization);
            _dbContext.TeamMemberships.Add(membership);

            var apiKey = new OrganizationApiKey
            {
                Id = Guid.NewGuid(),
                OrganizationId = _testOrganizationId,
                CreatedBy = _testUserId,
                Name = "Test API Key",
                KeyHash = "test-hash",
                KeyPrefix = "testpref",
                Permissions = new Dictionary<string, object>(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.OrganizationApiKeys.Add(apiKey);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.DeleteApiKey(_testOrganizationId, apiKey.Id, CancellationToken.None);

            // Assert
            result.Should().BeOfType<NoContentResult>();

            // Verify API key was deleted
            var deletedApiKey = await _dbContext.OrganizationApiKeys.FindAsync(apiKey.Id);
            deletedApiKey.Should().BeNull();
        }

        [Fact]
        public async Task DeleteApiKey_WhenOrganizationNotFound_ReturnsNotFound()
        {
            // Arrange
            var nonExistentOrgId = Guid.NewGuid();
            var apiKeyId = Guid.NewGuid();

            // Act
            var result = await _controller.DeleteApiKey(nonExistentOrgId, apiKeyId, CancellationToken.None);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult!.Value.Should().Be("Organization not found");
        }

        [Fact]
        public async Task DeleteApiKey_WhenApiKeyNotFound_ReturnsNotFound()
        {
            // Arrange
            var organization = CreateTestOrganization();
            var membership = CreateTestTeamMembership(_testUserId, Guid.NewGuid(), _testOrganizationId, "OrgAdmin");

            _dbContext.Organizations.Add(organization);
            _dbContext.TeamMemberships.Add(membership);
            await _dbContext.SaveChangesAsync();

            var nonExistentKeyId = Guid.NewGuid();

            // Act
            var result = await _controller.DeleteApiKey(_testOrganizationId, nonExistentKeyId, CancellationToken.None);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult!.Value.Should().Be("API key not found");
        }

        [Fact]
        public async Task DeleteApiKey_WhenNotAdmin_ReturnsForbid()
        {
            // Arrange
            var organization = CreateTestOrganization();
            var membership = CreateTestTeamMembership(_testUserId, Guid.NewGuid(), _testOrganizationId, "Member");

            _dbContext.Organizations.Add(organization);
            _dbContext.TeamMemberships.Add(membership);

            var apiKey = new OrganizationApiKey
            {
                Id = Guid.NewGuid(),
                OrganizationId = _testOrganizationId,
                CreatedBy = _testUserId,
                Name = "Test API Key",
                KeyHash = "test-hash",
                KeyPrefix = "testpref",
                Permissions = new Dictionary<string, object>(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.OrganizationApiKeys.Add(apiKey);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.DeleteApiKey(_testOrganizationId, apiKey.Id, CancellationToken.None);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task DeleteApiKey_WhenKeyAlreadyRevoked_ReturnsGone()
        {
            // Arrange
            var organization = CreateTestOrganization();
            var membership = CreateTestTeamMembership(_testUserId, Guid.NewGuid(), _testOrganizationId, "OrgAdmin");

            _dbContext.Organizations.Add(organization);
            _dbContext.TeamMemberships.Add(membership);

            var apiKey = new OrganizationApiKey
            {
                Id = Guid.NewGuid(),
                OrganizationId = _testOrganizationId,
                CreatedBy = _testUserId,
                Name = "Test API Key",
                KeyHash = "test-hash",
                KeyPrefix = "testpref",
                Permissions = new Dictionary<string, object>(),
                IsActive = false,
                RevokedAt = DateTime.UtcNow.AddDays(-1),
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };
            _dbContext.OrganizationApiKeys.Add(apiKey);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.DeleteApiKey(_testOrganizationId, apiKey.Id, CancellationToken.None);

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(410);
            objectResult.Value.Should().Be("API key has already been revoked");
        }

        [Fact]
        public async Task RotateApiKey_WithValidRequest_ReturnsOkWithNewKey()
        {
            // Arrange
            var organization = CreateTestOrganization();
            var membership = CreateTestTeamMembership(_testUserId, Guid.NewGuid(), _testOrganizationId, "OrgAdmin");

            _dbContext.Organizations.Add(organization);
            _dbContext.TeamMemberships.Add(membership);

            var apiKey = new OrganizationApiKey
            {
                Id = Guid.NewGuid(),
                OrganizationId = _testOrganizationId,
                CreatedBy = _testUserId,
                Name = "Test API Key",
                KeyHash = "old-hash",
                KeyPrefix = "oldpref",
                Permissions = new Dictionary<string, object>(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.OrganizationApiKeys.Add(apiKey);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.RotateApiKey(_testOrganizationId, apiKey.Id, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var response = okResult!.Value as RotateOrganizationApiKeyResponse;
            response.Should().NotBeNull();
            response!.Id.Should().Be(apiKey.Id);
            response.Name.Should().Be("Test API Key");
            response.Key.Should().NotBeNullOrEmpty();
            response.Key.Should().StartWith("sk-");
            response.KeyPrefix.Should().NotBe("oldpref");
            response.RotatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

            // Verify API key was updated with new hash
            var updatedApiKey = await _dbContext.OrganizationApiKeys.FindAsync(apiKey.Id);
            updatedApiKey!.KeyHash.Should().NotBe("old-hash");
            updatedApiKey.KeyPrefix.Should().NotBe("oldpref");
        }

        [Fact]
        public async Task RotateApiKey_WhenOrganizationNotFound_ReturnsNotFound()
        {
            // Arrange
            var nonExistentOrgId = Guid.NewGuid();
            var apiKeyId = Guid.NewGuid();

            // Act
            var result = await _controller.RotateApiKey(nonExistentOrgId, apiKeyId, CancellationToken.None);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult!.Value.Should().Be("Organization not found");
        }

        [Fact]
        public async Task RotateApiKey_WhenApiKeyNotFound_ReturnsNotFound()
        {
            // Arrange
            var organization = CreateTestOrganization();
            var membership = CreateTestTeamMembership(_testUserId, Guid.NewGuid(), _testOrganizationId, "OrgAdmin");

            _dbContext.Organizations.Add(organization);
            _dbContext.TeamMemberships.Add(membership);
            await _dbContext.SaveChangesAsync();

            var nonExistentKeyId = Guid.NewGuid();

            // Act
            var result = await _controller.RotateApiKey(_testOrganizationId, nonExistentKeyId, CancellationToken.None);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult!.Value.Should().Be("API key not found");
        }

        [Fact]
        public async Task RotateApiKey_WhenNotAdmin_ReturnsForbid()
        {
            // Arrange
            var organization = CreateTestOrganization();
            var membership = CreateTestTeamMembership(_testUserId, Guid.NewGuid(), _testOrganizationId, "Member");

            _dbContext.Organizations.Add(organization);
            _dbContext.TeamMemberships.Add(membership);

            var apiKey = new OrganizationApiKey
            {
                Id = Guid.NewGuid(),
                OrganizationId = _testOrganizationId,
                CreatedBy = _testUserId,
                Name = "Test API Key",
                KeyHash = "test-hash",
                KeyPrefix = "testpref",
                Permissions = new Dictionary<string, object>(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.OrganizationApiKeys.Add(apiKey);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.RotateApiKey(_testOrganizationId, apiKey.Id, CancellationToken.None);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task RotateApiKey_WhenKeyAlreadyRevoked_ReturnsGone()
        {
            // Arrange
            var organization = CreateTestOrganization();
            var membership = CreateTestTeamMembership(_testUserId, Guid.NewGuid(), _testOrganizationId, "OrgAdmin");

            _dbContext.Organizations.Add(organization);
            _dbContext.TeamMemberships.Add(membership);

            var apiKey = new OrganizationApiKey
            {
                Id = Guid.NewGuid(),
                OrganizationId = _testOrganizationId,
                CreatedBy = _testUserId,
                Name = "Test API Key",
                KeyHash = "test-hash",
                KeyPrefix = "testpref",
                Permissions = new Dictionary<string, object>(),
                IsActive = false,
                RevokedAt = DateTime.UtcNow.AddDays(-1),
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };
            _dbContext.OrganizationApiKeys.Add(apiKey);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.RotateApiKey(_testOrganizationId, apiKey.Id, CancellationToken.None);

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(410);
            objectResult.Value.Should().Be("API key has already been revoked");
        }

        [Fact]
        public async Task GetApiKeyUsage_WithValidRequest_ReturnsOkWithUsageData()
        {
            // Arrange
            var organization = CreateTestOrganization();
            var membership = CreateTestTeamMembership(_testUserId, Guid.NewGuid(), _testOrganizationId, "Member");

            _dbContext.Organizations.Add(organization);
            _dbContext.TeamMemberships.Add(membership);

            var apiKey = new OrganizationApiKey
            {
                Id = Guid.NewGuid(),
                OrganizationId = _testOrganizationId,
                CreatedBy = _testUserId,
                Name = "Test API Key",
                KeyHash = "test-hash",
                KeyPrefix = "testpref",
                Permissions = new Dictionary<string, object>(),
                IsActive = true,
                TotalRequests = 100,
                ErrorCount = 5,
                LastUsedAt = DateTime.UtcNow.AddMinutes(-10),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.OrganizationApiKeys.Add(apiKey);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.GetApiKeyUsage(_testOrganizationId, apiKey.Id, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var response = okResult!.Value as OrganizationApiKeyUsageResponse;
            response.Should().NotBeNull();
            response!.ApiKeyId.Should().Be(apiKey.Id);
            response.TotalRequests.Should().Be(100);
            response.ErrorCount.Should().Be(5);
            response.ErrorRate.Should().Be(0.05);
            response.LastUsedAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(-10), TimeSpan.FromSeconds(5));
            response.RequestsByHour.Should().NotBeNull();
            response.RequestsByDay.Should().NotBeNull();
            response.RequestsByWeek.Should().NotBeNull();
        }

        [Fact]
        public async Task GetApiKeyUsage_WhenOrganizationNotFound_ReturnsNotFound()
        {
            // Arrange
            var nonExistentOrgId = Guid.NewGuid();
            var apiKeyId = Guid.NewGuid();

            // Act
            var result = await _controller.GetApiKeyUsage(nonExistentOrgId, apiKeyId, CancellationToken.None);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult!.Value.Should().Be("Organization not found");
        }

        [Fact]
        public async Task GetApiKeyUsage_WhenApiKeyNotFound_ReturnsNotFound()
        {
            // Arrange
            var organization = CreateTestOrganization();
            var membership = CreateTestTeamMembership(_testUserId, Guid.NewGuid(), _testOrganizationId, "Member");

            _dbContext.Organizations.Add(organization);
            _dbContext.TeamMemberships.Add(membership);
            await _dbContext.SaveChangesAsync();

            var nonExistentKeyId = Guid.NewGuid();

            // Act
            var result = await _controller.GetApiKeyUsage(_testOrganizationId, nonExistentKeyId, CancellationToken.None);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult!.Value.Should().Be("API key not found");
        }

        [Fact]
        public async Task GetApiKeyUsage_WhenNotMember_ReturnsForbid()
        {
            // Arrange
            var organization = CreateTestOrganization();
            // Don't add membership - user is not a member

            _dbContext.Organizations.Add(organization);
            await _dbContext.SaveChangesAsync();

            var apiKeyId = Guid.NewGuid();

            // Act
            var result = await _controller.GetApiKeyUsage(_testOrganizationId, apiKeyId, CancellationToken.None);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        private Organization CreateTestOrganization()
        {
            return new Organization
            {
                Id = _testOrganizationId,
                Name = "Test Organization",
                Slug = "test-org",
                Description = "Test organization",
                PrimaryRegion = "us-east-1",
                Tier = "free",
                BillingCurrency = "USD",
                CreditBalance = 0.00m,
                CreditCurrency = "USD",
                SubscriptionStatus = "active",
                IsTrial = false,
                DataRetentionDays = 30,
                RequireSso = false,
                IsActive = true,
                IsVerified = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                AllowedEmailDomains = new List<string>(),
                AvailableRegions = new List<string>(),
                PrivacyConsent = new Dictionary<string, object>()
            };
        }

        private TeamMembership CreateTestTeamMembership(Guid userId, Guid teamId, Guid organizationId, string role = "Member")
        {
            return new TeamMembership
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TeamId = teamId,
                OrganizationId = organizationId,
                Role = role,
                JoinedAt = DateTime.UtcNow
            };
        }

        private void SetupControllerContext()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString()),
                new Claim("sub", _testUserId.ToString())
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = claimsPrincipal
                }
            };
        }
    }
}
