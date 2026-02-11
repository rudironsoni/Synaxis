// <copyright file="UsersControllerTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Tests.Controllers
{
using System;
using System.Collections.Generic;
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
using Synaxis.InferenceGateway.Application.Interfaces;
using Synaxis.InferenceGateway.WebApi.Controllers;
using Xunit;

[Trait("Category", "Unit")]
public sealed class UsersControllerTests : IDisposable
{
        private readonly SynaxisDbContext _dbContext;
        private readonly TestOrganizationUserContext _userContext;
        private readonly TestPasswordService _passwordService;
        private readonly UsersController _controller;
        private readonly Guid _testUserId = Guid.NewGuid();
        private readonly Guid _testOrganizationId = Guid.NewGuid();
        private readonly Guid _testTeamId = Guid.NewGuid();
        private bool _disposed;

        public UsersControllerTests()
        {
            var options = new DbContextOptionsBuilder<SynaxisDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;
            _dbContext = new SynaxisDbContext(options);
            _userContext = new TestOrganizationUserContext(_testUserId, _testOrganizationId);
            _passwordService = new TestPasswordService();

            _controller = new UsersController(
                _dbContext,
                _userContext,
                _passwordService);

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
        public async Task GetMe_WithAuthenticatedUser_ReturnsUserProfile()
        {
            // Arrange
            var testUser = CreateTestUser();
            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.GetMe(CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();

            var response = okResult.Value!;
            var responseType = response.GetType();
            responseType.GetProperty("id")?.GetValue(response).Should().Be(_testUserId);
            responseType.GetProperty("email")?.GetValue(response).Should().Be(testUser.Email);
            responseType.GetProperty("firstName")?.GetValue(response).Should().Be(testUser.FirstName);
            responseType.GetProperty("lastName")?.GetValue(response).Should().Be(testUser.LastName);
            responseType.GetProperty("role")?.GetValue(response).Should().Be(testUser.Role);
            responseType.GetProperty("mfaEnabled")?.GetValue(response).Should().Be(testUser.MfaEnabled);
        }

        [Fact]
        public async Task GetMe_WithAuthenticatedUser_IncludesOrganizationMembership()
        {
            // Arrange
            var testUser = CreateTestUser();
            var testOrganization = CreateTestOrganization();
            var testTeam = CreateTestTeam(testOrganization);
            var testTeamMembership = CreateTestTeamMembership(testUser, testTeam, testOrganization);

            _dbContext.Organizations.Add(testOrganization);
            _dbContext.Teams.Add(testTeam);
            _dbContext.Users.Add(testUser);
            _dbContext.TeamMemberships.Add(testTeamMembership);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.GetMe(CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();

            var response = okResult.Value!;
            var responseType = response.GetType();
            responseType.GetProperty("organizationId")?.GetValue(response).Should().Be(_testOrganizationId);
        }

        [Fact]
        public async Task GetMe_WithAuthenticatedUser_IncludesMfaStatus()
        {
            // Arrange
            var testUser = CreateTestUser();
            testUser.MfaEnabled = true;
            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.GetMe(CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();

            var response = okResult.Value!;
            var responseType = response.GetType();
            responseType.GetProperty("mfaEnabled")?.GetValue(response).Should().Be(true);
        }

        [Fact]
        public async Task GetMe_WithUnauthenticatedUser_ReturnsUnauthorized()
        {
            // Arrange
            _userContext.SetAuthenticated(false);

            // Act
            var result = await _controller.GetMe(CancellationToken.None);

            // Assert
            result.Should().BeOfType<UnauthorizedResult>();
        }

        [Fact]
        public async Task GetMe_WithNonExistentUser_ReturnsNotFound()
        {
            // Arrange
            // User not added to database

            // Act
            var result = await _controller.GetMe(CancellationToken.None);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task GetMe_WithBlacklistedToken_ReturnsUnauthorized()
        {
            // Arrange
            var testUser = CreateTestUser();
            _dbContext.Users.Add(testUser);

            var blacklistedToken = new JwtBlacklist
            {
                Id = Guid.NewGuid(),
                TokenId = "test-jti-123",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
            };
            _dbContext.JwtBlacklists.Add(blacklistedToken);
            await _dbContext.SaveChangesAsync();

            // Setup Authorization header with blacklisted token
            _controller.ControllerContext.HttpContext.Request.Headers["Authorization"] =
                "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJqdGkiOiJ0ZXN0LWp0aS0xMjMifQ.test";

            // Act
            var result = await _controller.GetMe(CancellationToken.None);

            // Assert
            result.Should().BeOfType<UnauthorizedResult>();
        }

        [Fact]
        public async Task GetMe_WithValidToken_ReturnsUserProfile()
        {
            // Arrange
            var testUser = CreateTestUser();
            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            // Setup Authorization header with valid token (not blacklisted)
            _controller.ControllerContext.HttpContext.Request.Headers["Authorization"] =
                "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJqdGkiOiJ2YWxpZC1qdGkifQ.test";

            // Act
            var result = await _controller.GetMe(CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task GetMe_ReturnsAllRequiredFields()
        {
            // Arrange
            var testUser = CreateTestUser();
            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.GetMe(CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();

            var response = okResult.Value!;
            var responseType = response.GetType();
            // Verify all required fields are present
            responseType.GetProperty("id")?.GetValue(response).Should().NotBeNull();
            responseType.GetProperty("email")?.GetValue(response).Should().NotBeNull();
            responseType.GetProperty("firstName")?.GetValue(response).Should().NotBeNull();
            responseType.GetProperty("lastName")?.GetValue(response).Should().NotBeNull();
            responseType.GetProperty("role")?.GetValue(response).Should().NotBeNull();
            responseType.GetProperty("mfaEnabled")?.GetValue(response).Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateMe_WithAuthenticatedUser_UpdatesProfileSuccessfully()
        {
            // Arrange
            var testUser = CreateTestUser();
            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            var request = new Synaxis.Core.Contracts.UpdateUserRequest
            {
                FirstName = "Updated",
                LastName = "Name",
                Timezone = "America/New_York",
            };

            // Act
            var result = await _controller.UpdateMe(request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();

            var response = okResult.Value!;
            var responseType = response.GetType();
            responseType.GetProperty("firstName")?.GetValue(response).Should().Be("Updated");
            responseType.GetProperty("lastName")?.GetValue(response).Should().Be("Name");
            responseType.GetProperty("timezone")?.GetValue(response).Should().Be("America/New_York");

            // Verify database was updated
            var updatedUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == _testUserId);
            updatedUser.Should().NotBeNull();
            updatedUser!.FirstName.Should().Be("Updated");
            updatedUser.LastName.Should().Be("Name");
            updatedUser.Timezone.Should().Be("America/New_York");
        }

        [Fact]
        public async Task UpdateMe_WithUnauthenticatedUser_ReturnsUnauthorized()
        {
            // Arrange
            _userContext.SetAuthenticated(false);
            var request = new Synaxis.Core.Contracts.UpdateUserRequest { FirstName = "Updated" };

            // Act
            var result = await _controller.UpdateMe(request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<UnauthorizedResult>();
        }

        [Fact]
        public async Task UpdateMe_WithInvalidFirstName_ReturnsBadRequest()
        {
            // Arrange
            var testUser = CreateTestUser();
            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            var request = new Synaxis.Core.Contracts.UpdateUserRequest { FirstName = new string('A', 101) }; // Exceeds max length

            // Act
            var result = await _controller.UpdateMe(request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateMe_WithInvalidLastName_ReturnsBadRequest()
        {
            // Arrange
            var testUser = CreateTestUser();
            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            var request = new Synaxis.Core.Contracts.UpdateUserRequest { LastName = new string('A', 101) }; // Exceeds max length

            // Act
            var result = await _controller.UpdateMe(request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateMe_WithInvalidTimezone_ReturnsBadRequest()
        {
            // Arrange
            var testUser = CreateTestUser();
            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            var request = new Synaxis.Core.Contracts.UpdateUserRequest { Timezone = "Invalid" }; // Single part, no slash

            // Act
            var result = await _controller.UpdateMe(request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateMe_WithNonExistentUser_ReturnsNotFound()
        {
            // Arrange
            // User not added to database
            var request = new Synaxis.Core.Contracts.UpdateUserRequest { FirstName = "Updated" };

            // Act
            var result = await _controller.UpdateMe(request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task UpdateMe_WithBlacklistedToken_ReturnsUnauthorized()
        {
            // Arrange
            var testUser = CreateTestUser();
            _dbContext.Users.Add(testUser);

            var blacklistedToken = new JwtBlacklist
            {
                Id = Guid.NewGuid(),
                TokenId = "test-jti-123",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
            };
            _dbContext.JwtBlacklists.Add(blacklistedToken);
            await _dbContext.SaveChangesAsync();

            // Setup Authorization header with blacklisted token
            _controller.ControllerContext.HttpContext.Request.Headers["Authorization"] =
                "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJqdGkiOiJ0ZXN0LWp0aS0xMjMifQ.test";

            var request = new Synaxis.Core.Contracts.UpdateUserRequest { FirstName = "Updated" };

            // Act
            var result = await _controller.UpdateMe(request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<UnauthorizedResult>();
        }

        [Fact]
        public async Task UpdateMe_WithPartialUpdate_UpdatesOnlyProvidedFields()
        {
            // Arrange
            var testUser = CreateTestUser();
            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            var request = new Synaxis.Core.Contracts.UpdateUserRequest { FirstName = "Updated" };

            // Act
            var result = await _controller.UpdateMe(request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();

            var response = okResult.Value!;
            var responseType = response.GetType();
            responseType.GetProperty("firstName")?.GetValue(response).Should().Be("Updated");
            responseType.GetProperty("lastName")?.GetValue(response).Should().Be(testUser.LastName); // Unchanged
            responseType.GetProperty("timezone")?.GetValue(response).Should().Be(testUser.Timezone); // Unchanged

            // Verify database was updated
            var updatedUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == _testUserId);
            updatedUser.Should().NotBeNull();
            updatedUser!.FirstName.Should().Be("Updated");
            updatedUser.LastName.Should().Be(testUser.LastName);
            updatedUser.Timezone.Should().Be(testUser.Timezone);
        }

        [Fact]
        public async Task UpdateMe_WithEmptyFields_DoesNotUpdateFields()
        {
            // Arrange
            var testUser = CreateTestUser();
            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            var request = new Synaxis.Core.Contracts.UpdateUserRequest { FirstName = string.Empty, LastName = string.Empty, Timezone = string.Empty };

            // Act
            var result = await _controller.UpdateMe(request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();

            var response = okResult.Value!;
            var responseType = response.GetType();
            responseType.GetProperty("firstName")?.GetValue(response).Should().Be(testUser.FirstName);
            responseType.GetProperty("lastName")?.GetValue(response).Should().Be(testUser.LastName);
            responseType.GetProperty("timezone")?.GetValue(response).Should().Be(testUser.Timezone);
        }

        [Fact]
        public async Task UpdateMe_WithNullFields_DoesNotUpdateFields()
        {
            // Arrange
            var testUser = CreateTestUser();
            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            var request = new Synaxis.Core.Contracts.UpdateUserRequest { FirstName = null, LastName = null, Timezone = null };

            // Act
            var result = await _controller.UpdateMe(request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();

            var response = okResult.Value!;
            var responseType = response.GetType();
            responseType.GetProperty("firstName")?.GetValue(response).Should().Be(testUser.FirstName);
            responseType.GetProperty("lastName")?.GetValue(response).Should().Be(testUser.LastName);
            responseType.GetProperty("timezone")?.GetValue(response).Should().Be(testUser.Timezone);
        }

        [Fact]
        public async Task UpdateMe_UpdatesUpdatedAtTimestamp()
        {
            // Arrange
            var testUser = CreateTestUser();
            var originalUpdatedAt = testUser.UpdatedAt;
            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            var request = new Synaxis.Core.Contracts.UpdateUserRequest { FirstName = "Updated" };

            // Act
            var result = await _controller.UpdateMe(request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();

            var response = okResult.Value!;
            var responseType = response.GetType();
            var updatedAt = responseType.GetProperty("updatedAt")?.GetValue(response);
            updatedAt.Should().NotBeNull();
            updatedAt.Should().BeOfType<DateTime>();

            var updatedUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == _testUserId);
            updatedUser.Should().NotBeNull();
            updatedUser!.UpdatedAt.Should().BeAfter(originalUpdatedAt);
        }

        private void SetupControllerContext()
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString()),
                new Claim("organization_id", _testOrganizationId.ToString()),
            }));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        private User CreateTestUser()
        {
            return new User
            {
                Id = _testUserId,
                OrganizationId = _testOrganizationId,
                Email = "test@example.com",
                PasswordHash = "hashedpassword",
                FirstName = "Test",
                LastName = "User",
                Role = "member",
                MfaEnabled = false,
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
        }

        private Organization CreateTestOrganization()
        {
            return new Organization
            {
                Id = _testOrganizationId,
                Slug = "test-org",
                Name = "Test Organization",
                PrimaryRegion = "us-east-1",
                Tier = "free",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
        }

        private Team CreateTestTeam(Organization organization)
        {
            return new Team
            {
                Id = _testTeamId,
                OrganizationId = organization.Id,
                Slug = "test-team",
                Name = "Test Team",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
        }

        private TeamMembership CreateTestTeamMembership(User user, Team team, Organization organization)
        {
            return new TeamMembership
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TeamId = team.Id,
                OrganizationId = organization.Id,
                Role = "member",
                JoinedAt = DateTime.UtcNow,
            };
        }

        /// <summary>
        /// Test implementation of IOrganizationUserContext.
        /// </summary>
        private class TestOrganizationUserContext : IOrganizationUserContext
        {
            private readonly Guid _userId;
            private readonly Guid? _organizationId;
            private bool _isAuthenticated;

            public TestOrganizationUserContext(Guid userId, Guid? organizationId = null)
            {
                _userId = userId;
                _organizationId = organizationId;
                _isAuthenticated = true;
            }

            public Guid UserId => _userId;

            public Guid? OrganizationId => _organizationId;

            public bool IsAuthenticated => _isAuthenticated;

            public void SetAuthenticated(bool isAuthenticated)
            {
                _isAuthenticated = isAuthenticated;
            }
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
}
