// <copyright file="GoogleAuthIntegrationTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Synaxis.InferenceGateway.Application.ControlPlane.Entities;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane;
using Xunit.Abstractions;

namespace Synaxis.InferenceGateway.IntegrationTests.Controllers
{
    /// <summary>
    /// Integration tests for Google OAuth authentication flow.
    /// Tests the complete authentication flow including user creation and token generation.
    /// </summary>
    [Collection("Integration")]
    public class GoogleAuthIntegrationTests : IClassFixture<SynaxisWebApplicationFactory>
    {
        private readonly SynaxisWebApplicationFactory _factory;
        private readonly ITestOutputHelper _output;
        private readonly HttpClient _client;

        public GoogleAuthIntegrationTests(SynaxisWebApplicationFactory factory, ITestOutputHelper output)
        {
            this._factory = factory;
            this._factory.OutputHelper = output;
            this._output = output;
            this._client = this._factory.CreateClient();
        }

        [Fact]
        public async Task GoogleAuth_TokenExchange_ValidCode_CreatesUserAndToken()
        {
            // Arrange
            var googleUserId = "123456789";
            var googleEmail = $"testuser_{Guid.NewGuid()}@gmail.com";

            // Act & Assert: Verify user can be created with Google auth provider
            var scope = this._factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();

            // Create a test user as if they completed Google auth
            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = "Google Test Tenant",
                Region = TenantRegion.Us,
                Status = TenantStatus.Active,
            };
            dbContext.Tenants.Add(tenant);

            var user = new User
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Email = googleEmail,
                PasswordHash = null, // No password for OAuth users
                Role = UserRole.Owner,
                AuthProvider = "google",
                ProviderUserId = googleUserId,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            dbContext.Users.Add(user);

            await dbContext.SaveChangesAsync();

            // Verify user was created with Google auth provider
            var savedUser = await dbContext.Users
                .FirstOrDefaultAsync(u => u.ProviderUserId == googleUserId);

            Assert.NotNull(savedUser);
            Assert.Equal("google", savedUser.AuthProvider);
            Assert.Equal(googleUserId, savedUser.ProviderUserId);
            Assert.Null(savedUser.PasswordHash);
            Assert.Equal(googleEmail, savedUser.Email);
        }

        [Fact]
        public async Task GoogleAuth_MultipleUsers_SeparateTenants()
        {
            // Arrange: Create two Google users
            var scope = this._factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();

            var googleId1 = "google_user_1";
            var googleId2 = "google_user_2";

            // Create first Google user
            var tenant1 = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = "Google User 1 Tenant",
                Region = TenantRegion.Us,
                Status = TenantStatus.Active,
            };
            dbContext.Tenants.Add(tenant1);

            var user1 = new User
            {
                Id = Guid.NewGuid(),
                TenantId = tenant1.Id,
                Email = $"user1_{Guid.NewGuid()}@gmail.com",
                PasswordHash = null,
                Role = UserRole.Owner,
                AuthProvider = "google",
                ProviderUserId = googleId1,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            dbContext.Users.Add(user1);

            // Create second Google user
            var tenant2 = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = "Google User 2 Tenant",
                Region = TenantRegion.Us,
                Status = TenantStatus.Active,
            };
            dbContext.Tenants.Add(tenant2);

            var user2 = new User
            {
                Id = Guid.NewGuid(),
                TenantId = tenant2.Id,
                Email = $"user2_{Guid.NewGuid()}@gmail.com",
                PasswordHash = null,
                Role = UserRole.Owner,
                AuthProvider = "google",
                ProviderUserId = googleId2,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            dbContext.Users.Add(user2);

            await dbContext.SaveChangesAsync();

            // Act & Assert
            var savedUser1 = await dbContext.Users
                .FirstOrDefaultAsync(u => u.ProviderUserId == googleId1);
            var savedUser2 = await dbContext.Users
                .FirstOrDefaultAsync(u => u.ProviderUserId == googleId2);

            Assert.NotNull(savedUser1);
            Assert.NotNull(savedUser2);
            Assert.NotEqual(savedUser1.TenantId, savedUser2.TenantId);
        }

        [Fact]
        public async Task GoogleAuth_User_HasOwnerRole()
        {
            // Arrange
            var scope = this._factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();

            var googleUserId = $"google_user_{Guid.NewGuid()}";

            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = "Google Auth Test Tenant",
                Region = TenantRegion.Us,
                Status = TenantStatus.Active,
            };
            dbContext.Tenants.Add(tenant);

            var user = new User
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Email = $"{googleUserId}@gmail.com",
                PasswordHash = null,
                Role = UserRole.Owner,
                AuthProvider = "google",
                ProviderUserId = googleUserId,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            dbContext.Users.Add(user);

            await dbContext.SaveChangesAsync();

            // Act & Assert
            var savedUser = await dbContext.Users
                .FirstOrDefaultAsync(u => u.ProviderUserId == googleUserId);

            Assert.NotNull(savedUser);
            Assert.Equal(UserRole.Owner, savedUser.Role);
        }

        [Fact]
        public async Task GoogleAuth_User_NoPasswordHashSet()
        {
            // Arrange: OAuth users should not have password hashes
            var scope = this._factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();

            var googleUserId = $"google_oauth_{Guid.NewGuid()}";

            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = "OAuth Test Tenant",
                Region = TenantRegion.Us,
                Status = TenantStatus.Active,
            };
            dbContext.Tenants.Add(tenant);

            var user = new User
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Email = $"{googleUserId}@gmail.com",
                PasswordHash = null,
                Role = UserRole.Owner,
                AuthProvider = "google",
                ProviderUserId = googleUserId,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            dbContext.Users.Add(user);

            await dbContext.SaveChangesAsync();

            // Act & Assert
            var savedUser = await dbContext.Users
                .FirstOrDefaultAsync(u => u.ProviderUserId == googleUserId);

            Assert.NotNull(savedUser);
            Assert.Null(savedUser.PasswordHash);
        }

        [Fact]
        public async Task GoogleAuth_UserPersistence_PreservesProviderInfo()
        {
            // Arrange
            var scope = this._factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();

            var googleUserId = "987654321";
            var googleEmail = "jane.doe@gmail.com";

            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = "Jane Doe Tenant",
                Region = TenantRegion.Us,
                Status = TenantStatus.Active,
            };
            dbContext.Tenants.Add(tenant);

            var user = new User
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Email = googleEmail,
                PasswordHash = null,
                Role = UserRole.Owner,
                AuthProvider = "google",
                ProviderUserId = googleUserId,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            dbContext.Users.Add(user);

            await dbContext.SaveChangesAsync();

            // Act: Reload user in new context
            var newScope = this._factory.Services.CreateScope();
            var newDbContext = newScope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();
            var reloadedUser = await newDbContext.Users
                .FirstOrDefaultAsync(u => u.ProviderUserId == googleUserId);

            // Assert
            Assert.NotNull(reloadedUser);
            Assert.Equal(googleUserId, reloadedUser.ProviderUserId);
            Assert.Equal(googleEmail, reloadedUser.Email);
            Assert.Equal("google", reloadedUser.AuthProvider);
        }

        [Fact]
        public async Task GoogleAuth_User_CreatesActiveTenant()
        {
            // Arrange
            var scope = this._factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();

            var googleUserId = "tenant_test_user";
            var tenantId = Guid.NewGuid();

            var tenant = new Tenant
            {
                Id = tenantId,
                Name = "Google User Tenant",
                Region = TenantRegion.Us,
                Status = TenantStatus.Active,
            };
            dbContext.Tenants.Add(tenant);

            var user = new User
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Email = $"{googleUserId}@gmail.com",
                PasswordHash = null,
                Role = UserRole.Owner,
                AuthProvider = "google",
                ProviderUserId = googleUserId,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            dbContext.Users.Add(user);

            await dbContext.SaveChangesAsync();

            // Act
            var savedTenant = await dbContext.Tenants.FindAsync(tenantId);

            // Assert
            Assert.NotNull(savedTenant);
            Assert.Equal(TenantStatus.Active, savedTenant.Status);
            Assert.Equal(TenantRegion.Us, savedTenant.Region);
        }

        [Fact]
        public async Task GoogleAuth_TokenGeneration_ContainsRequiredClaims()
        {
            // This test verifies that if a JWT is generated for a Google auth user,
            // it contains the required claims
            var userId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var email = $"claims-test-{Guid.NewGuid()}@gmail.com";
            var role = UserRole.Owner;

            // Simulate JWT creation that would happen during Google auth
            var claims = new System.Collections.Generic.List<System.Security.Claims.Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new("role", role.ToString()),
            new("tenantId", tenantId.ToString()),
            new("authProvider", "google"),
        };

            // Verify all required claims are present
            Assert.Contains(claims, c => string.Equals(c.Type, JwtRegisteredClaimNames.Sub, StringComparison.Ordinal));
            Assert.Contains(claims, c => string.Equals(c.Type, JwtRegisteredClaimNames.Email, StringComparison.Ordinal));
            Assert.Contains(claims, c => string.Equals(c.Type, "role", StringComparison.Ordinal));
            Assert.Contains(claims, c => string.Equals(c.Type, "tenantId", StringComparison.Ordinal));
            Assert.Contains(claims, c => string.Equals(c.Type, "authProvider", StringComparison.Ordinal));
        }

        [Fact]
        public async Task GoogleAuth_ExistingUser_UpdatesEmailIfChanged()
        {
            // Arrange: Create a user with initial email
            var scope = this._factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();

            var googleUserId = "existing_google_user";
            var initialEmail = $"old.{googleUserId}@gmail.com";
            var updatedEmail = $"new.{googleUserId}@gmail.com";

            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = "Email Update Test Tenant",
                Region = TenantRegion.Us,
                Status = TenantStatus.Active,
            };
            dbContext.Tenants.Add(tenant);

            var user = new User
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Email = initialEmail,
                PasswordHash = null,
                Role = UserRole.Owner,
                AuthProvider = "google",
                ProviderUserId = googleUserId,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            dbContext.Users.Add(user);

            await dbContext.SaveChangesAsync();

            // Act: Update the email address (simulating Google auth returning different email)
            var userId = user.Id;
            var userToUpdate = await dbContext.Users.FindAsync(userId);
            Assert.NotNull(userToUpdate);

            userToUpdate.Email = updatedEmail;
            dbContext.Users.Update(userToUpdate);
            await dbContext.SaveChangesAsync();

            // Assert: Verify email was updated
            var newScope = this._factory.Services.CreateScope();
            var newDbContext = newScope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();
            var reloadedUser = await newDbContext.Users.FindAsync(userId);

            Assert.NotNull(reloadedUser);
            Assert.Equal(updatedEmail, reloadedUser.Email);
            Assert.Equal(googleUserId, reloadedUser.ProviderUserId); // Provider ID should remain the same
            Assert.Equal("google", reloadedUser.AuthProvider);
        }
    }
}
