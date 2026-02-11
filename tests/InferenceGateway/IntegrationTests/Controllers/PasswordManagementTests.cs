// <copyright file="PasswordManagementTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.IntegrationTests.Controllers
{
    using System;
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Synaxis.Core.Models;
    using Synaxis.Infrastructure.Data;
    using Xunit;
    using Xunit.Abstractions;

    [Trait("Category", "Integration")]
    [Collection("Integration")]
    public class PasswordManagementTests : IClassFixture<SynaxisWebApplicationFactory>
    {
        private readonly SynaxisWebApplicationFactory _factory;
        private readonly ITestOutputHelper _output;
        private readonly HttpClient _client;

        public PasswordManagementTests(SynaxisWebApplicationFactory factory, ITestOutputHelper output)
        {
            _factory = factory;
            _factory.OutputHelper = output;
            _output = output;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task ChangePassword_WithoutAuth_ReturnsUnauthorized()
        {
            var request = new
            {
                CurrentPassword = "OldPassword123!",
                NewPassword = "NewPassword123!"
            };

            var response = await _client.PostAsJsonAsync("/api/v1/users/me/password", request);
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ChangePassword_WithValidCredentials_ChangesPassword()
        {
            var (client, user) = await CreateAuthenticatedUserAsync("TestPassword123!");

            var request = new
            {
                CurrentPassword = "TestPassword123!",
                NewPassword = "NewSecurePassword456!"
            };

            var response = await client.PostAsJsonAsync("/api/v1/users/me/password", request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            content.GetProperty("success").GetBoolean().Should().BeTrue();

            // Verify password was changed
            var updatedUser = await GetUserByIdAsync(user.Id);
            updatedUser.Should().NotBeNull();
            updatedUser!.PasswordChangedAt.Should().NotBeNull();
            updatedUser.PasswordChangedAt.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task ChangePassword_WithInvalidCurrentPassword_ReturnsBadRequest()
        {
            var (client, user) = await CreateAuthenticatedUserAsync("TestPassword123!");

            var request = new
            {
                CurrentPassword = "WrongPassword123!",
                NewPassword = "NewSecurePassword456!"
            };

            var response = await client.PostAsJsonAsync("/api/v1/users/me/password", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            content.GetProperty("success").GetBoolean().Should().BeFalse();
            content.GetProperty("errorMessage").GetString().Should().Contain("incorrect");
        }

        [Fact]
        public async Task ChangePassword_WithWeakPassword_ReturnsBadRequest()
        {
            var (client, user) = await CreateAuthenticatedUserAsync("TestPassword123!");

            var request = new
            {
                CurrentPassword = "TestPassword123!",
                NewPassword = "weak"
            };

            var response = await client.PostAsJsonAsync("/api/v1/users/me/password", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            content.GetProperty("success").GetBoolean().Should().BeFalse();
            content.GetProperty("errorMessage").GetString().Should().Contain("at least");
        }

        [Fact]
        public async Task ChangePassword_WithReusedPassword_ReturnsBadRequest()
        {
            var (client, user) = await CreateAuthenticatedUserAsync("TestPassword123!");

            // First change
            var request1 = new
            {
                CurrentPassword = "TestPassword123!",
                NewPassword = "NewSecurePassword456!"
            };
            await client.PostAsJsonAsync("/api/v1/users/me/password", request1);

            // Try to change back to old password
            var request2 = new
            {
                CurrentPassword = "NewSecurePassword456!",
                NewPassword = "TestPassword123!"
            };
            var response = await client.PostAsJsonAsync("/api/v1/users/me/password", request2);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            content.GetProperty("success").GetBoolean().Should().BeFalse();
            content.GetProperty("errorMessage").GetString().Should().Contain("reuse");
        }

        [Fact]
        public async Task ValidatePassword_WithoutAuth_ReturnsUnauthorized()
        {
            var request = new { Password = "TestPassword123!" };
            var response = await _client.PostAsJsonAsync("/api/v1/users/me/password/validate", request);
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ValidatePassword_WithStrongPassword_ReturnsValid()
        {
            var (client, user) = await CreateAuthenticatedUserAsync("TestPassword123!");

            var request = new { Password = "VeryStrongPassword!@#123" };
            var response = await client.PostAsJsonAsync("/api/v1/users/me/password/validate", request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            content.GetProperty("isValid").GetBoolean().Should().BeTrue();
            content.GetProperty("strengthScore").GetInt32().Should().BeGreaterThan(70);
        }

        [Fact]
        public async Task ValidatePassword_WithWeakPassword_ReturnsInvalid()
        {
            var (client, user) = await CreateAuthenticatedUserAsync("TestPassword123!");

            var request = new { Password = "weak" };
            var response = await client.PostAsJsonAsync("/api/v1/users/me/password/validate", request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            content.GetProperty("isValid").GetBoolean().Should().BeFalse();
            content.GetProperty("errors").GetArrayLength().Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task ValidatePassword_WithCommonPassword_ReturnsInvalid()
        {
            var (client, user) = await CreateAuthenticatedUserAsync("TestPassword123!");

            var request = new { Password = "password123" };
            var response = await client.PostAsJsonAsync("/api/v1/users/me/password/validate", request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            content.GetProperty("isValid").GetBoolean().Should().BeFalse();
            var errors = content.GetProperty("errors").EnumerateArray();
            errors.Should().Contain(e => e.GetString()!.Contains("too common"));
        }

        [Fact]
        public async Task ValidatePassword_WithUserInfo_ReturnsInvalid()
        {
            var (client, user) = await CreateAuthenticatedUserAsync("TestPassword123!");

            var request = new { Password = $"{user.FirstName}123!" };
            var response = await client.PostAsJsonAsync("/api/v1/users/me/password/validate", request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            content.GetProperty("isValid").GetBoolean().Should().BeFalse();
            var errors = content.GetProperty("errors").EnumerateArray();
            errors.Should().Contain(e => e.GetString()!.Contains("first name"));
        }

        [Fact]
        public async Task GetPasswordPolicy_WithoutAuth_ReturnsUnauthorized()
        {
            var org = await CreateTestOrganizationAsync();
            var response = await _client.GetAsync($"/api/v1/organizations/{org.Id}/password-policy");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetPasswordPolicy_WithAuth_ReturnsPolicy()
        {
            var (client, user) = await CreateAuthenticatedUserAsync("TestPassword123!");

            var response = await client.GetAsync($"/api/v1/organizations/{user.OrganizationId}/password-policy");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            content.GetProperty("minLength").GetInt32().Should().BeGreaterThan(0);
            content.GetProperty("requireUppercase").GetBoolean().Should().BeTrue();
            content.GetProperty("requireLowercase").GetBoolean().Should().BeTrue();
            content.GetProperty("requireNumbers").GetBoolean().Should().BeTrue();
            content.GetProperty("requireSpecialCharacters").GetBoolean().Should().BeTrue();
        }

        [Fact]
        public async Task UpdatePasswordPolicy_WithValidRequest_UpdatesPolicy()
        {
            var (client, user) = await CreateAuthenticatedUserAsync("TestPassword123!");

            var request = new
            {
                minLength = 16,
                requireUppercase = true,
                requireLowercase = true,
                requireNumbers = true,
                requireSpecialCharacters = true,
                passwordHistoryCount = 10,
                passwordExpirationDays = 60,
                passwordExpirationWarningDays = 7,
                maxFailedChangeAttempts = 3,
                lockoutDurationMinutes = 30,
                blockCommonPasswords = true,
                blockUserInfoInPassword = true
            };

            var response = await client.PutAsJsonAsync($"/api/v1/organizations/{user.OrganizationId}/password-policy", request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            content.GetProperty("minLength").GetInt32().Should().Be(16);
            content.GetProperty("passwordHistoryCount").GetInt32().Should().Be(10);
            content.GetProperty("passwordExpirationDays").GetInt32().Should().Be(60);
        }

        [Fact]
        public async Task UpdatePasswordPolicy_WithInvalidMinLength_ReturnsBadRequest()
        {
            var (client, user) = await CreateAuthenticatedUserAsync("TestPassword123!");

            var request = new
            {
                minLength = 5, // Too short
                requireUppercase = true,
                requireLowercase = true,
                requireNumbers = true,
                requireSpecialCharacters = true
            };

            var response = await client.PutAsJsonAsync($"/api/v1/organizations/{user.OrganizationId}/password-policy", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task ChangePassword_SetsExpirationDate()
        {
            var (client, user) = await CreateAuthenticatedUserAsync("TestPassword123!");

            var request = new
            {
                CurrentPassword = "TestPassword123!",
                NewPassword = "NewSecurePassword456!"
            };

            var response = await client.PostAsJsonAsync("/api/v1/users/me/password", request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            content.GetProperty("passwordExpiresAt").TryGetDateTime(out var expiresAt).Should().BeTrue();

            var updatedUser = await GetUserByIdAsync(user.Id);
            updatedUser.Should().NotBeNull();
            updatedUser!.PasswordExpiresAt.Should().NotBeNull();
            updatedUser.PasswordExpiresAt.Value.Should().BeAfter(DateTime.UtcNow.AddDays(89));
        }

        [Fact]
        public async Task ChangePassword_ResetsFailedAttempts()
        {
            var (client, user) = await CreateAuthenticatedUserAsync("TestPassword123!");

            // Fail a password change attempt
            var failRequest = new
            {
                CurrentPassword = "WrongPassword123!",
                NewPassword = "NewSecurePassword456!"
            };
            await client.PostAsJsonAsync("/api/v1/users/me/password", failRequest);

            // Verify failed attempts increased
            var userAfterFail = await GetUserByIdAsync(user.Id);
            userAfterFail!.FailedPasswordChangeAttempts.Should().BeGreaterThan(0);

            // Successfully change password
            var successRequest = new
            {
                CurrentPassword = "TestPassword123!",
                NewPassword = "NewSecurePassword456!"
            };
            var successResponse = await client.PostAsJsonAsync("/api/v1/users/me/password", successRequest);
            successResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify failed attempts reset
            var userAfterSuccess = await GetUserByIdAsync(user.Id);
            userAfterSuccess!.FailedPasswordChangeAttempts.Should().Be(0);
        }

        private async Task<(HttpClient Client, User User)> CreateAuthenticatedUserAsync(string password)
        {
            var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();
            var userService = scope.ServiceProvider.GetRequiredService<Synaxis.Core.Contracts.IUserService>();

            var org = new Organization
            {
                Id = Guid.NewGuid(),
                Slug = $"test-org-{Guid.NewGuid():N}"[..20],
                Name = "Test Organization",
                Description = "Test Description",
                PrimaryRegion = "us-east-1",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var user = new User
            {
                Id = Guid.NewGuid(),
                OrganizationId = org.Id,
                Email = $"testuser-{Guid.NewGuid():N}@example.com",
                PasswordHash = userService.HashPassword(password),
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1",
                FirstName = "Test",
                LastName = "User",
                AvatarUrl = "https://example.com/avatar.png",
                Timezone = "UTC",
                Locale = "en-US",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            dbContext.Organizations.Add(org);
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            var authenticatedClient = _factory.CreateClient();
            var token = GenerateTestToken(user.Id);
            authenticatedClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            return (authenticatedClient, user);
        }

        private Task<User?> GetUserByIdAsync(Guid userId)
        {
            var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();
            return dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        }

        private async Task<Organization> CreateTestOrganizationAsync()
        {
            var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();

            var org = new Organization
            {
                Id = Guid.NewGuid(),
                Slug = $"test-org-{Guid.NewGuid():N}"[..20],
                Name = "Test Organization",
                Description = "Test Organization",
                PrimaryRegion = "us-east-1",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            dbContext.Organizations.Add(org);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);
            return org;
        }

        private string GenerateTestToken(Guid userId)
        {
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes("TestJwtSecretKeyThatIsAtLeast32BytesLongForHmacSha256Algorithm"));

            var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
            {
                Subject = new System.Security.Claims.ClaimsIdentity(new[]
                {
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString()),
                    new System.Security.Claims.Claim("sub", userId.ToString())
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                    key,
                    Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature)
            };

            var token = handler.CreateToken(tokenDescriptor);
            return handler.WriteToken(token);
        }
    }
}
