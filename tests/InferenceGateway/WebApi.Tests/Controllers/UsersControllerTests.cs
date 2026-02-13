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
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
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

        [Fact]
        public async Task DeleteMe_WithAuthenticatedUser_SoftDeletesAccount()
        {
            // Arrange
            var testUser = CreateTestUser();
            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.DeleteMe(CancellationToken.None);

            // Assert
            result.Should().BeOfType<NoContentResult>();

            // Verify soft delete in database
            var deletedUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == _testUserId);
            deletedUser.Should().NotBeNull();
            deletedUser!.IsActive.Should().BeFalse();
            deletedUser.DeletedAt.Should().NotBeNull();
            deletedUser.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task DeleteMe_WithUnauthenticatedUser_ReturnsUnauthorized()
        {
            // Arrange
            _userContext.SetAuthenticated(false);

            // Act
            var result = await _controller.DeleteMe(CancellationToken.None);

            // Assert
            result.Should().BeOfType<UnauthorizedResult>();
        }

        [Fact]
        public async Task DeleteMe_WithNonExistentUser_ReturnsNotFound()
        {
            // Arrange
            // User not added to database

            // Act
            var result = await _controller.DeleteMe(CancellationToken.None);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task DeleteMe_WithBlacklistedToken_ReturnsUnauthorized()
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
            var result = await _controller.DeleteMe(CancellationToken.None);

            // Assert
            result.Should().BeOfType<UnauthorizedResult>();
        }

        [Fact]
        public async Task DeleteMe_WithActiveRefreshTokens_RevokesAllTokens()
        {
            // Arrange
            var testUser = CreateTestUser();
            _dbContext.Users.Add(testUser);

            var activeRefreshToken1 = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                TokenHash = "hash1",
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow,
            };

            var activeRefreshToken2 = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                TokenHash = "hash2",
                ExpiresAt = DateTime.UtcNow.AddDays(14),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow,
            };

            _dbContext.RefreshTokens.Add(activeRefreshToken1);
            _dbContext.RefreshTokens.Add(activeRefreshToken2);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.DeleteMe(CancellationToken.None);

            // Assert
            result.Should().BeOfType<NoContentResult>();

            // Verify all refresh tokens are revoked
            var revokedTokens = await _dbContext.RefreshTokens
                .Where(rt => rt.UserId == _testUserId)
                .ToListAsync();

            revokedTokens.Should().HaveCount(2);
            revokedTokens.Should().OnlyContain(rt => rt.IsRevoked);
            revokedTokens.Should().OnlyContain(rt => rt.RevokedAt.HasValue);
        }

        [Fact]
        public async Task DeleteMe_WithActiveOrganizationMemberships_DeletesMemberships()
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
            var result = await _controller.DeleteMe(CancellationToken.None);

            // Assert
            result.Should().BeOfType<NoContentResult>();

            // Verify user is soft deleted
            var deletedUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == _testUserId);
            deletedUser.Should().NotBeNull();
            deletedUser!.IsActive.Should().BeFalse();
            deletedUser.DeletedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task DeleteMe_UpdatesUpdatedAtTimestamp()
        {
            // Arrange
            var testUser = CreateTestUser();
            var originalUpdatedAt = testUser.UpdatedAt;
            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.DeleteMe(CancellationToken.None);

            // Assert
            result.Should().BeOfType<NoContentResult>();

            var deletedUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == _testUserId);
            deletedUser.Should().NotBeNull();
            deletedUser!.UpdatedAt.Should().BeAfter(originalUpdatedAt);
        }

        [Fact]
        public async Task DeleteMe_Returns204NoContent()
        {
            // Arrange
            var testUser = CreateTestUser();
            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.DeleteMe(CancellationToken.None);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            var noContentResult = result as NoContentResult;
            noContentResult!.StatusCode.Should().Be(204);
        }

        [Fact]
        public async Task DeleteMe_ClearsSensitiveData()
        {
            // Arrange
            var testUser = CreateTestUser();
            testUser.PasswordHash = "hashedpassword";
            testUser.MfaSecret = "mfa-secret";
            testUser.MfaBackupCodes = "backup-codes";
            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.DeleteMe(CancellationToken.None);

            // Assert
            result.Should().BeOfType<NoContentResult>();

            var deletedUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == _testUserId);
            deletedUser.Should().NotBeNull();
            deletedUser!.PasswordHash.Should().BeNull();
            deletedUser.MfaSecret.Should().BeNull();
            deletedUser.MfaBackupCodes.Should().BeNull();
            deletedUser.Email.Should().StartWith("deleted_");
            deletedUser.Email.Should().EndWith("@deleted.local");
        }

        [Fact]
        public async Task DeleteMe_WithVirtualKeys_RevokesAllKeys()
        {
            // Arrange
            var testUser = CreateTestUser();
            var testOrganization = CreateTestOrganization();
            var testTeam = CreateTestTeam(testOrganization);

            var virtualKey1 = new VirtualKey
            {
                Id = Guid.NewGuid(),
                KeyHash = "hash1",
                OrganizationId = _testOrganizationId,
                TeamId = _testTeamId,
                CreatedBy = _testUserId,
                Name = "Key 1",
                IsActive = true,
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow,
            };

            var virtualKey2 = new VirtualKey
            {
                Id = Guid.NewGuid(),
                KeyHash = "hash2",
                OrganizationId = _testOrganizationId,
                TeamId = _testTeamId,
                CreatedBy = _testUserId,
                Name = "Key 2",
                IsActive = true,
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow,
            };

            _dbContext.Organizations.Add(testOrganization);
            _dbContext.Teams.Add(testTeam);
            _dbContext.Users.Add(testUser);
            _dbContext.VirtualKeys.Add(virtualKey1);
            _dbContext.VirtualKeys.Add(virtualKey2);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.DeleteMe(CancellationToken.None);

            // Assert
            result.Should().BeOfType<NoContentResult>();

            var revokedKeys = await _dbContext.VirtualKeys
                .Where(vk => vk.CreatedBy == _testUserId)
                .ToListAsync();

            revokedKeys.Should().HaveCount(2);
            revokedKeys.Should().OnlyContain(vk => !vk.IsActive);
            revokedKeys.Should().OnlyContain(vk => vk.IsRevoked);
            revokedKeys.Should().OnlyContain(vk => vk.RevokedAt.HasValue);
            revokedKeys.Should().OnlyContain(vk => vk.RevokedReason == "User account deleted");
        }

        [Fact]
        public async Task DeleteMe_RemovesTeamMemberships()
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
            var result = await _controller.DeleteMe(CancellationToken.None);

            // Assert
            result.Should().BeOfType<NoContentResult>();

            var memberships = await _dbContext.TeamMemberships
                .Where(tm => tm.UserId == _testUserId)
                .ToListAsync();

            memberships.Should().BeEmpty();
        }

        [Fact]
        public async Task DeleteMe_WithCollectionMemberships_RemovesMemberships()
        {
            // Arrange
            var testUser = CreateTestUser();
            var testOrganization = CreateTestOrganization();
            var testTeam = CreateTestTeam(testOrganization);

            var collection = new Collection
            {
                Id = Guid.NewGuid(),
                OrganizationId = _testOrganizationId,
                TeamId = _testTeamId,
                Slug = "test-collection",
                Name = "Test Collection",
                Type = "general",
                Visibility = "private",
                IsActive = true,
                CreatedBy = _testUserId,
                CreatedAt = DateTime.UtcNow,
            };

            var collectionMembership = new CollectionMembership
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                CollectionId = collection.Id,
                OrganizationId = _testOrganizationId,
                Role = "Admin",
                JoinedAt = DateTime.UtcNow,
                AddedBy = _testUserId,
            };

            _dbContext.Organizations.Add(testOrganization);
            _dbContext.Teams.Add(testTeam);
            _dbContext.Users.Add(testUser);
            _dbContext.Collections.Add(collection);
            _dbContext.CollectionMemberships.Add(collectionMembership);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.DeleteMe(CancellationToken.None);

            // Assert
            result.Should().BeOfType<NoContentResult>();

            var memberships = await _dbContext.CollectionMemberships
                .Where(cm => cm.UserId == _testUserId)
                .ToListAsync();

            memberships.Should().BeEmpty();
        }

        [Fact]
        public async Task DeleteMe_CreatesAuditLogEntry()
        {
            // Arrange
            var testUser = CreateTestUser();
            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.DeleteMe(CancellationToken.None);

            // Assert
            result.Should().BeOfType<NoContentResult>();

            var auditLogs = await _dbContext.AuditLogs
                .Where(al => al.UserId == _testUserId && al.EventType == "UserDeleted")
                .ToListAsync();

            auditLogs.Should().HaveCount(1);
            var auditLog = auditLogs[0];
            auditLog.EventCategory.Should().Be("Account");
            auditLog.Action.Should().Be("Delete");
            auditLog.ResourceType.Should().Be("User");
            auditLog.ResourceId.Should().Be(_testUserId.ToString());
        }

        [Fact]
        public async Task DeleteMe_WithOnlyOwnerRole_ReturnsBadRequest()
        {
            // Arrange
            var testUser = CreateTestUser();
            var testOrganization = CreateTestOrganization();
            var testTeam = CreateTestTeam(testOrganization);

            // Create user as OrgAdmin
            var testTeamMembership = new TeamMembership
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                TeamId = _testTeamId,
                OrganizationId = _testOrganizationId,
                Role = "OrgAdmin",
                JoinedAt = DateTime.UtcNow,
            };

            _dbContext.Organizations.Add(testOrganization);
            _dbContext.Teams.Add(testTeam);
            _dbContext.Users.Add(testUser);
            _dbContext.TeamMemberships.Add(testTeamMembership);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.DeleteMe(CancellationToken.None);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();

            var errorResponse = badRequestResult!.Value!;
            var responseType = errorResponse.GetType();
            responseType.GetProperty("error")?.GetValue(errorResponse).Should().Be("Cannot delete account");
            responseType.GetProperty("organizationId")?.GetValue(errorResponse).Should().Be(_testOrganizationId);
        }

        [Fact]
        public async Task DeleteMe_WithMultipleOwners_AllowsDeletion()
        {
            // Arrange
            var testUser = CreateTestUser();
            var testOrganization = CreateTestOrganization();
            var testTeam = CreateTestTeam(testOrganization);

            var otherUserId = Guid.NewGuid();
            var otherUser = new User
            {
                Id = otherUserId,
                OrganizationId = _testOrganizationId,
                Email = "other@example.com",
                PasswordHash = "hashedpassword",
                FirstName = "Other",
                LastName = "User",
                Role = "member",
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            // Create both users as OrgAdmin
            var testTeamMembership = new TeamMembership
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                TeamId = _testTeamId,
                OrganizationId = _testOrganizationId,
                Role = "OrgAdmin",
                JoinedAt = DateTime.UtcNow,
            };

            var otherTeamMembership = new TeamMembership
            {
                Id = Guid.NewGuid(),
                UserId = otherUserId,
                TeamId = _testTeamId,
                OrganizationId = _testOrganizationId,
                Role = "OrgAdmin",
                JoinedAt = DateTime.UtcNow,
            };

            _dbContext.Organizations.Add(testOrganization);
            _dbContext.Teams.Add(testTeam);
            _dbContext.Users.Add(testUser);
            _dbContext.Users.Add(otherUser);
            _dbContext.TeamMemberships.Add(testTeamMembership);
            _dbContext.TeamMemberships.Add(otherTeamMembership);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.DeleteMe(CancellationToken.None);

            // Assert
            result.Should().BeOfType<NoContentResult>();

            var deletedUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == _testUserId);
            deletedUser.Should().NotBeNull();
            deletedUser!.IsActive.Should().BeFalse();
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

        [Fact]
        public async Task UploadAvatar_WithAuthenticatedUser_UploadsAvatarSuccessfully()
        {
            // Arrange
            var testUser = CreateTestUser();
            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            var testImage = CreateTestImageFile(200, 200, "image/jpeg");
            var formFile = CreateFormFile(testImage, "avatar.jpg", "image/jpeg");

            // Act
            var result = await _controller.UploadAvatar(formFile, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();

            var response = okResult.Value!;
            var responseType = response.GetType();
            var avatarUrl = responseType.GetProperty("avatarUrl")?.GetValue(response);
            avatarUrl.Should().NotBeNull();
            avatarUrl.Should().BeOfType<string>();
            var avatarUrlString = (string)avatarUrl!;
            avatarUrlString.Should().StartWith("/uploads/avatars/");
            avatarUrlString.Should().Contain(_testUserId.ToString());

            // Verify database was updated
            var updatedUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == _testUserId);
            updatedUser.Should().NotBeNull();
            updatedUser!.AvatarUrl.Should().Be(avatarUrlString);
        }

        [Fact]
        public async Task UploadAvatar_WithUnauthenticatedUser_ReturnsUnauthorized()
        {
            // Arrange
            _userContext.SetAuthenticated(false);
            var formFile = CreateFormFile(Array.Empty<byte>(), "avatar.jpg", "image/jpeg");

            // Act
            var result = await _controller.UploadAvatar(formFile, CancellationToken.None);

            // Assert
            result.Should().BeOfType<UnauthorizedResult>();
        }

        [Fact]
        public async Task UploadAvatar_WithNonExistentUser_ReturnsNotFound()
        {
            // Arrange
            // User not added to database
            var formFile = CreateFormFile(Array.Empty<byte>(), "avatar.jpg", "image/jpeg");

            // Act
            var result = await _controller.UploadAvatar(formFile, CancellationToken.None);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task UploadAvatar_WithNoFile_ReturnsBadRequest()
        {
            // Arrange
            var testUser = CreateTestUser();
            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            IFormFile? formFile = null;

            // Act
            var result = await _controller.UploadAvatar(formFile, CancellationToken.None);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().Be("Avatar file is required");
        }

        [Fact]
        public async Task UploadAvatar_WithInvalidFileType_ReturnsBadRequest()
        {
            // Arrange
            var testUser = CreateTestUser();
            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            var testFile = System.Text.Encoding.UTF8.GetBytes("not an image");
            var formFile = CreateFormFile(testFile, "document.pdf", "application/pdf");

            // Act
            var result = await _controller.UploadAvatar(formFile, CancellationToken.None);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().Be("Invalid file type. Only JPEG, PNG, GIF, and WebP are allowed");
        }

        [Fact]
        public async Task UploadAvatar_WithFileTooLarge_ReturnsBadRequest()
        {
            // Arrange
            var testUser = CreateTestUser();
            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            var largeFile = new byte[6 * 1024 * 1024]; // 6MB
            var formFile = CreateFormFile(largeFile, "avatar.jpg", "image/jpeg");

            // Act
            var result = await _controller.UploadAvatar(formFile, CancellationToken.None);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().Be("File size exceeds maximum limit of 5MB");
        }

        [Fact]
        public async Task UploadAvatar_WithImageTooSmall_ReturnsBadRequest()
        {
            // Arrange
            var testUser = CreateTestUser();
            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            var testImage = CreateTestImageFile(32, 32, "image/jpeg");
            var formFile = CreateFormFile(testImage, "avatar.jpg", "image/jpeg");

            // Act
            var result = await _controller.UploadAvatar(formFile, CancellationToken.None);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().Be("Image dimensions must be at least 64x64 pixels");
        }

        [Fact]
        public async Task UploadAvatar_WithImageTooLarge_ReturnsBadRequest()
        {
            // Arrange
            var testUser = CreateTestUser();
            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            var testImage = CreateTestImageFile(1200, 1200, "image/jpeg");
            var formFile = CreateFormFile(testImage, "avatar.jpg", "image/jpeg");

            // Act
            var result = await _controller.UploadAvatar(formFile, CancellationToken.None);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().Be("Image dimensions must not exceed 1024x1024 pixels");
        }

        [Fact]
        public async Task UploadAvatar_WithValidJpeg_UploadsSuccessfully()
        {
            // Arrange
            var testUser = CreateTestUser();
            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            var testImage = CreateTestImageFile(200, 200, "image/jpeg");
            var formFile = CreateFormFile(testImage, "avatar.jpg", "image/jpeg");

            // Act
            var result = await _controller.UploadAvatar(formFile, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();

            var response = okResult.Value!;
            var responseType = response.GetType();
            var avatarUrl = responseType.GetProperty("avatarUrl")?.GetValue(response);
            avatarUrl.Should().NotBeNull();
        }

        [Fact]
        public async Task UploadAvatar_WithValidPng_UploadsSuccessfully()
        {
            // Arrange
            var testUser = CreateTestUser();
            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            var testImage = CreateTestImageFile(200, 200, "image/png");
            var formFile = CreateFormFile(testImage, "avatar.png", "image/png");

            // Act
            var result = await _controller.UploadAvatar(formFile, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();

            var response = okResult.Value!;
            var responseType = response.GetType();
            var avatarUrl = responseType.GetProperty("avatarUrl")?.GetValue(response);
            avatarUrl.Should().NotBeNull();
        }

        [Fact]
        public async Task UploadAvatar_WithValidGif_UploadsSuccessfully()
        {
            // Arrange
            var testUser = CreateTestUser();
            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            var testImage = CreateTestImageFile(200, 200, "image/gif");
            var formFile = CreateFormFile(testImage, "avatar.gif", "image/gif");

            // Act
            var result = await _controller.UploadAvatar(formFile, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();

            var response = okResult.Value!;
            var responseType = response.GetType();
            var avatarUrl = responseType.GetProperty("avatarUrl")?.GetValue(response);
            avatarUrl.Should().NotBeNull();
        }

        [Fact]
        public async Task UploadAvatar_WithValidWebP_UploadsSuccessfully()
        {
            // Arrange
            var testUser = CreateTestUser();
            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            var testImage = CreateTestImageFile(200, 200, "image/webp");
            var formFile = CreateFormFile(testImage, "avatar.webp", "image/webp");

            // Act
            var result = await _controller.UploadAvatar(formFile, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();

            var response = okResult.Value!;
            var responseType = response.GetType();
            var avatarUrl = responseType.GetProperty("avatarUrl")?.GetValue(response);
            avatarUrl.Should().NotBeNull();
        }

        [Fact]
        public async Task UploadAvatar_GeneratesUniqueFilename()
        {
            // Arrange
            var testUser = CreateTestUser();
            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            var testImage = CreateTestImageFile(200, 200, "image/jpeg");
            var formFile1 = CreateFormFile(testImage, "avatar.jpg", "image/jpeg");
            var formFile2 = CreateFormFile(testImage, "avatar.jpg", "image/jpeg");

            // Act
            var result1 = await _controller.UploadAvatar(formFile1, CancellationToken.None);
            var result2 = await _controller.UploadAvatar(formFile2, CancellationToken.None);

            // Assert
            var okResult1 = result1 as OkObjectResult;
            var okResult2 = result2 as OkObjectResult;
            okResult1.Should().NotBeNull();
            okResult2.Should().NotBeNull();

            var response1 = okResult1!.Value!;
            var response2 = okResult2!.Value!;
            var responseType = response1.GetType();
            var avatarUrl1 = (string)responseType.GetProperty("avatarUrl")?.GetValue(response1)!;
            var avatarUrl2 = (string)responseType.GetProperty("avatarUrl")?.GetValue(response2)!;

            avatarUrl1.Should().NotBe(avatarUrl2);
        }

        [Fact]
        public async Task UploadAvatar_UpdatesUpdatedAtTimestamp()
        {
            // Arrange
            var testUser = CreateTestUser();
            var originalUpdatedAt = testUser.UpdatedAt;
            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            var testImage = CreateTestImageFile(200, 200, "image/jpeg");
            var formFile = CreateFormFile(testImage, "avatar.jpg", "image/jpeg");

            // Act
            var result = await _controller.UploadAvatar(formFile, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();

            var updatedUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == _testUserId);
            updatedUser.Should().NotBeNull();
            updatedUser!.UpdatedAt.Should().BeAfter(originalUpdatedAt);
        }

        private byte[] CreateTestImageFile(int width, int height, string mimeType)
        {
            // Create a simple test image with the specified dimensions
            using var image = new Image<Rgb24>(width, height);
            using var stream = new MemoryStream();
            IImageEncoder encoder = mimeType switch
            {
                "image/jpeg" => new JpegEncoder(),
                "image/png" => new PngEncoder(),
                "image/gif" => new GifEncoder(),
                "image/webp" => new WebpEncoder(),
                _ => new JpegEncoder()
            };
            image.Save(stream, encoder);
            return stream.ToArray();
        }

        private IFormFile CreateFormFile(byte[] content, string fileName, string contentType)
        {
            var stream = new MemoryStream(content);
            var formFile = new FormFile(stream, 0, content.Length, "avatar", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };
            return formFile;
        }

        /// <summary>
        /// Tests that GetMyOrganizations returns the user's organizations.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Feature", "Organizations")]
        public async Task GetMyOrganizations_ReturnsOrganizationsList()
        {
            // Arrange
            var user = CreateTestUser();
            var org1 = new Organization
            {
                Id = Guid.NewGuid(),
                Slug = "org-1",
                Name = "Organization 1",
                PrimaryRegion = "us-east-1",
                Tier = "free",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            var org2 = new Organization
            {
                Id = Guid.NewGuid(),
                Slug = "org-2",
                Name = "Organization 2",
                PrimaryRegion = "us-east-1",
                Tier = "free",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            var org3 = new Organization
            {
                Id = Guid.NewGuid(),
                Slug = "org-3",
                Name = "Organization 3",
                PrimaryRegion = "us-east-1",
                Tier = "free",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            var team1 = new Team
            {
                Id = Guid.NewGuid(),
                OrganizationId = org1.Id,
                Slug = "team-1",
                Name = "Team 1",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            var team2 = new Team
            {
                Id = Guid.NewGuid(),
                OrganizationId = org2.Id,
                Slug = "team-2",
                Name = "Team 2",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            var team3 = new Team
            {
                Id = Guid.NewGuid(),
                OrganizationId = org3.Id,
                Slug = "team-3",
                Name = "Team 3",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            var membership1 = new TeamMembership
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TeamId = team1.Id,
                OrganizationId = org1.Id,
                Role = "member",
                JoinedAt = DateTime.UtcNow,
            };
            var membership2 = new TeamMembership
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TeamId = team2.Id,
                OrganizationId = org2.Id,
                Role = "member",
                JoinedAt = DateTime.UtcNow,
            };
            var membership3 = new TeamMembership
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TeamId = team3.Id,
                OrganizationId = org3.Id,
                Role = "member",
                JoinedAt = DateTime.UtcNow,
            };

            _dbContext.Organizations.AddRange(org1, org2, org3);
            _dbContext.Teams.AddRange(team1, team2, team3);
            _dbContext.Users.Add(user);
            _dbContext.TeamMemberships.AddRange(membership1, membership2, membership3);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.GetMyOrganizations(1, 10, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            okResult.Value.Should().NotBeNull();

            var response = okResult.Value!;
            var responseType = response.GetType();
            var items = responseType.GetProperty("items")?.GetValue(response) as System.Collections.IList;
            items.Should().NotBeNull();
            items!.Count.Should().Be(3);

            var totalCount = responseType.GetProperty("totalCount")?.GetValue(response);
            totalCount.Should().Be(3);
        }

        /// <summary>
        /// Tests that GetMyTeams returns the user's teams.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Feature", "Teams")]
        public async Task GetMyTeams_ReturnsTeamsList()
        {
            // Arrange
            var user = CreateTestUser();
            var org1 = new Organization
            {
                Id = Guid.NewGuid(),
                Slug = "org-1",
                Name = "Organization 1",
                PrimaryRegion = "us-east-1",
                Tier = "free",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            var org2 = new Organization
            {
                Id = Guid.NewGuid(),
                Slug = "org-2",
                Name = "Organization 2",
                PrimaryRegion = "us-east-1",
                Tier = "free",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            var team1 = new Team
            {
                Id = Guid.NewGuid(),
                OrganizationId = org1.Id,
                Slug = "team-1",
                Name = "Team 1",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            var team2 = new Team
            {
                Id = Guid.NewGuid(),
                OrganizationId = org1.Id,
                Slug = "team-2",
                Name = "Team 2",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            var team3 = new Team
            {
                Id = Guid.NewGuid(),
                OrganizationId = org2.Id,
                Slug = "team-3",
                Name = "Team 3",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            var membership1 = new TeamMembership
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TeamId = team1.Id,
                OrganizationId = org1.Id,
                Role = "member",
                JoinedAt = DateTime.UtcNow,
            };
            var membership2 = new TeamMembership
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TeamId = team2.Id,
                OrganizationId = org1.Id,
                Role = "member",
                JoinedAt = DateTime.UtcNow,
            };
            var membership3 = new TeamMembership
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TeamId = team3.Id,
                OrganizationId = org2.Id,
                Role = "member",
                JoinedAt = DateTime.UtcNow,
            };

            _dbContext.Organizations.AddRange(org1, org2);
            _dbContext.Teams.AddRange(team1, team2, team3);
            _dbContext.Users.Add(user);
            _dbContext.TeamMemberships.AddRange(membership1, membership2, membership3);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.GetMyTeams(1, 10, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            okResult.Value.Should().NotBeNull();

            var response = okResult.Value!;
            var responseType = response.GetType();
            var items = responseType.GetProperty("items")?.GetValue(response) as System.Collections.IList;
            items.Should().NotBeNull();
            items!.Count.Should().Be(3);

            var totalCount = responseType.GetProperty("totalCount")?.GetValue(response);
            totalCount.Should().Be(3);
        }

        /// <summary>
        /// Tests that RequestDataExport returns an accepted response.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Feature", "DataExport")]
        public async Task RequestDataExport_ReturnsAccepted()
        {
            // Arrange
            var user = CreateTestUser();
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.RequestDataExport(CancellationToken.None);

            // Assert
            var acceptedResult = Assert.IsType<AcceptedResult>(result);
            acceptedResult.Value.Should().NotBeNull();

            var response = acceptedResult.Value!;
            var responseType = response.GetType();
            var message = responseType.GetProperty("message")?.GetValue(response) as string;
            message.Should().Be("Data export request received - actual export processing to be implemented");

            var userId = responseType.GetProperty("userId")?.GetValue(response);
            userId.Should().Be(_testUserId);

            var requestedAt = responseType.GetProperty("requestedAt")?.GetValue(response);
            requestedAt.Should().NotBeNull();
        }
    }
}
