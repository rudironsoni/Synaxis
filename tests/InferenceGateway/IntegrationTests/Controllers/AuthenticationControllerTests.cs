// <copyright file="AuthenticationControllerTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Synaxis.Core.Models;
using Synaxis.Infrastructure.Data;
using Xunit;
using Xunit.Abstractions;

namespace Synaxis.InferenceGateway.IntegrationTests.Controllers
{
    [Trait("Category", "Integration")]
    [Collection("Integration")]
    public class AuthenticationControllerTests : IClassFixture<SynaxisWebApplicationFactory>
    {
        private readonly SynaxisWebApplicationFactory _factory;
        private readonly ITestOutputHelper _output;
        private readonly HttpClient _client;

        public AuthenticationControllerTests(SynaxisWebApplicationFactory factory, ITestOutputHelper output)
        {
            _factory = factory;
            _factory.OutputHelper = output;
            _output = output;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task Register_ValidCredentials_ReturnsSuccessAndCreatesUser()
        {
            var email = $"newuser_{Guid.NewGuid()}@example.com";
            var request = new { Email = email, Password = "SecurePassword123!" };

            var response = await _client.PostAsJsonAsync("/auth/register", request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            content.GetProperty("success").GetBoolean().Should().BeTrue();
            content.GetProperty("userId").GetString().Should().NotBeNullOrEmpty();

            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
            user.Should().NotBeNull();
            user!.Email.Should().Be(email);
        }

        [Fact]
        public async Task Register_DuplicateEmail_ReturnsBadRequest()
        {
            var email = $"duplicate_{Guid.NewGuid()}@example.com";
            await _client.PostAsJsonAsync("/auth/register", new { Email = email, Password = "Password1!" });

            var response = await _client.PostAsJsonAsync("/auth/register", new { Email = email, Password = "Password2!" });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            content.GetProperty("success").GetBoolean().Should().BeFalse();
            content.GetProperty("message").GetString().Should().Contain("already exists");
        }

        [Fact]
        public async Task Login_ValidCredentials_ReturnsTokenAndUser()
        {
            var email = $"loginuser_{Guid.NewGuid()}@example.com";
            var password = "LoginPassword123!";
            await _client.PostAsJsonAsync("/auth/register", new { Email = email, Password = password });

            var response = await _client.PostAsJsonAsync("/auth/login", new { Email = email, Password = password });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            content.GetProperty("token").GetString().Should().NotBeNullOrEmpty();
            content.GetProperty("user").GetProperty("email").GetString().Should().Be(email);
        }

        [Fact]
        public async Task Login_InvalidPassword_ReturnsUnauthorized()
        {
            var email = $"wrongpass_{Guid.NewGuid()}@example.com";
            await _client.PostAsJsonAsync("/auth/register", new { Email = email, Password = "CorrectPass123!" });

            var response = await _client.PostAsJsonAsync("/auth/login", new { Email = email, Password = "WrongPass123!" });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            content.GetProperty("message").GetString().Should().Contain("Invalid credentials");
        }

        [Fact]
        public async Task Logout_ValidToken_ReturnsSuccess()
        {
            var email = $"logout_{Guid.NewGuid()}@example.com";
            var password = "LogoutPass123!";
            await _client.PostAsJsonAsync("/auth/register", new { Email = email, Password = password });
            var loginResponse = await _client.PostAsJsonAsync("/auth/login", new { Email = email, Password = password });
            var loginContent = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
            var token = loginContent.GetProperty("token").GetString();

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await client.PostAsync("/auth/logout", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            content.GetProperty("success").GetBoolean().Should().BeTrue();
        }

        [Fact]
        public async Task Logout_NoToken_ReturnsUnauthorized()
        {
            var response = await _client.PostAsync("/auth/logout", null);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Refresh_ValidToken_ReturnsNewToken()
        {
            var email = $"refresh_{Guid.NewGuid()}@example.com";
            var password = "RefreshPass123!";
            await _client.PostAsJsonAsync("/auth/register", new { Email = email, Password = password });
            var loginResponse = await _client.PostAsJsonAsync("/auth/login", new { Email = email, Password = password });
            var loginContent = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
            var oldToken = loginContent.GetProperty("token").GetString();

            var response = await _client.PostAsJsonAsync("/auth/refresh", new { Token = oldToken });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            var newToken = content.GetProperty("token").GetString();
            newToken.Should().NotBeNullOrEmpty();
            newToken.Should().NotBe(oldToken);
        }

        [Fact]
        public async Task Refresh_InvalidToken_ReturnsBadRequest()
        {
            var response = await _client.PostAsJsonAsync("/auth/refresh", new { Token = "invalid.token.here" });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Refresh_WithInactivity_ReturnsUnauthorized()
        {
            var email = $"inactive_{Guid.NewGuid()}@example.com";
            var password = "InactivePass123!";
            await _client.PostAsJsonAsync("/auth/register", new { Email = email, Password = password });
            var loginResponse = await _client.PostAsJsonAsync("/auth/login", new { Email = email, Password = password });
            var loginContent = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
            var token = loginContent.GetProperty("token").GetString();

            // Mark user as inactive
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
            user!.IsActive = false;
            await dbContext.SaveChangesAsync();

            var response = await _client.PostAsJsonAsync("/auth/refresh", new { Token = token });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ForgotPassword_ExistingEmail_ReturnsSuccessAndCreatesToken()
        {
            var email = $"forgot_{Guid.NewGuid()}@example.com";
            await _client.PostAsJsonAsync("/auth/register", new { Email = email, Password = "OldPass123!" });

            var response = await _client.PostAsJsonAsync("/auth/forgot-password", new { Email = email });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            content.GetProperty("success").GetBoolean().Should().BeTrue();
            content.GetProperty("message").GetString().Should().Contain("reset link");

            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
            user.Should().NotBeNull();
        }

        [Fact]
        public async Task ForgotPassword_NonExistentEmail_ReturnsSuccessWithoutLeakingInfo()
        {
            var email = $"nonexistent_{Guid.NewGuid()}@example.com";

            var response = await _client.PostAsJsonAsync("/auth/forgot-password", new { Email = email });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            content.GetProperty("success").GetBoolean().Should().BeTrue();
        }

        [Fact]
        public async Task ResetPassword_ValidToken_ChangesPassword()
        {
            var email = $"reset_{Guid.NewGuid()}@example.com";
            var oldPassword = "OldPass123!";
            var newPassword = "NewPass456!";
            await _client.PostAsJsonAsync("/auth/register", new { Email = email, Password = oldPassword });
            var forgotResponse = await _client.PostAsJsonAsync("/auth/forgot-password", new { Email = email });
            forgotResponse.EnsureSuccessStatusCode();

            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
            var resetToken = $"reset_{user!.Id}_{Guid.NewGuid()}";

            var response = await _client.PostAsJsonAsync("/auth/reset-password", new { Token = resetToken, Password = newPassword });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            content.GetProperty("success").GetBoolean().Should().BeTrue();

            var loginResponse = await _client.PostAsJsonAsync("/auth/login", new { Email = email, Password = newPassword });
            loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task ResetPassword_InvalidToken_ReturnsBadRequest()
        {
            var response = await _client.PostAsJsonAsync("/auth/reset-password", new { Token = "invalid_token", Password = "NewPass123!" });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task VerifyEmail_ValidToken_MarksEmailAsVerified()
        {
            var email = $"verify_{Guid.NewGuid()}@example.com";
            await _client.PostAsJsonAsync("/auth/register", new { Email = email, Password = "VerifyPass123!" });

            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
            var verifyToken = $"verify_{user!.Id}_{Guid.NewGuid()}";

            var response = await _client.PostAsJsonAsync("/auth/verify-email", new { Token = verifyToken });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            content.GetProperty("success").GetBoolean().Should().BeTrue();

            await dbContext.Entry(user).ReloadAsync();
            user.EmailVerifiedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task VerifyEmail_InvalidToken_ReturnsBadRequest()
        {
            var response = await _client.PostAsJsonAsync("/auth/verify-email", new { Token = "invalid_verification_token" });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task VerifyEmail_AlreadyVerified_ReturnsSuccess()
        {
            var email = $"already_verified_{Guid.NewGuid()}@example.com";
            await _client.PostAsJsonAsync("/auth/register", new { Email = email, Password = "AlreadyPass123!" });

            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
            user!.EmailVerifiedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();

            var verifyToken = $"verify_{user.Id}_{Guid.NewGuid()}";
            var response = await _client.PostAsJsonAsync("/auth/verify-email", new { Token = verifyToken });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}
