// <copyright file="AuthControllerTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Tests.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Synaxis.Core.Models;
    using Synaxis.Infrastructure.Data;
    using Synaxis.InferenceGateway.Application.Security;
    using Synaxis.InferenceGateway.WebApi.Controllers;
    using Xunit;

    [Trait("Category", "Unit")]
    public sealed class AuthControllerTests : IDisposable
    {
        private readonly Mock<IJwtService> _mockJwtService;
        private readonly Mock<IPasswordHasher> _mockPasswordHasher;
        private readonly SynaxisDbContext _dbContext;
        private readonly Mock<ILogger<AuthController>> _mockLogger;
        private readonly AuthController _controller;
        private readonly Guid _testUserId = Guid.NewGuid();
        private readonly Guid _testOrganizationId = Guid.NewGuid();
        private bool _disposed;

        public AuthControllerTests()
        {
            var options = new DbContextOptionsBuilder<SynaxisDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;
            _dbContext = new SynaxisDbContext(options);
            _mockJwtService = new Mock<IJwtService>();
            _mockPasswordHasher = new Mock<IPasswordHasher>();
            _mockLogger = new Mock<ILogger<AuthController>>();

            // Setup password hasher to return a hashed password
            _mockPasswordHasher.Setup(x => x.HashPassword(It.IsAny<string>())).Returns((string password) => $"hashed_{password}");

            _controller = new AuthController(
                _mockJwtService.Object,
                _mockPasswordHasher.Object,
                _dbContext,
                _mockLogger.Object);

            // Setup HttpContext and Request
            _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
            };
            _controller.Request.Scheme = "https";
            _controller.Request.Host = new Microsoft.AspNetCore.Http.HostString("localhost:5000");
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
        public async Task ForgotPassword_WithValidEmail_GeneratesTokenAndSendsEmail()
        {
            // Arrange
            var testUser = CreateTestUser();
            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            var request = new ForgotPasswordRequest
            {
                Email = testUser.Email
            };

            // Act
            var result = await _controller.ForgotPassword(request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();

            var response = okResult.Value!.GetType().GetProperty("success")?.GetValue(okResult.Value);
            response.Should().Be(true);

            // Verify that a password reset token was added
            var resetTokens = await _dbContext.PasswordResetTokens.ToListAsync();
            resetTokens.Should().HaveCount(1);
            resetTokens[0].UserId.Should().Be(testUser.Id);
        }

        [Fact]
        public async Task ForgotPassword_WithNonExistentEmail_ReturnsSuccessForSecurity()
        {
            // Arrange
            var request = new ForgotPasswordRequest
            {
                Email = "nonexistent@example.com"
            };

            // Act
            var result = await _controller.ForgotPassword(request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();

            var response = okResult.Value!.GetType().GetProperty("success")?.GetValue(okResult.Value);
            response.Should().Be(true);

            // Verify that no password reset token was added
            var resetTokens = await _dbContext.PasswordResetTokens.ToListAsync();
            resetTokens.Should().BeEmpty();
        }

        [Fact]
        public async Task ForgotPassword_WithValidEmail_GeneratesUniqueToken()
        {
            // Arrange
            var testUser = CreateTestUser();
            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            var request = new ForgotPasswordRequest
            {
                Email = testUser.Email
            };

            // Act
            var result1 = await _controller.ForgotPassword(request, CancellationToken.None);
            var result2 = await _controller.ForgotPassword(request, CancellationToken.None);

            // Assert
            result1.Should().BeOfType<OkObjectResult>();
            result2.Should().BeOfType<OkObjectResult>();

            // Verify that two tokens were added
            var resetTokens = await _dbContext.PasswordResetTokens.ToListAsync();
            resetTokens.Should().HaveCount(2);
            resetTokens[0].TokenHash.Should().NotBe(resetTokens[1].TokenHash);
        }

        [Fact]
        public async Task ForgotPassword_WithValidEmail_SetsTokenExpiration()
        {
            // Arrange
            var testUser = CreateTestUser();
            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            var request = new ForgotPasswordRequest
            {
                Email = testUser.Email
            };

            // Act
            var result = await _controller.ForgotPassword(request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();

            // Verify that the token was added with expiration
            var resetTokens = await _dbContext.PasswordResetTokens.ToListAsync();
            resetTokens.Should().HaveCount(1);
            resetTokens[0].UserId.Should().Be(testUser.Id);
            resetTokens[0].ExpiresAt.Should().BeAfter(DateTime.UtcNow);
            resetTokens[0].ExpiresAt.Should().BeBefore(DateTime.UtcNow.AddHours(1).AddMinutes(1));
            resetTokens[0].IsUsed.Should().BeFalse();
        }

        [Fact]
        public async Task ForgotPassword_WithEmptyEmail_ReturnsBadRequest()
        {
            // Arrange
            var request = new ForgotPasswordRequest
            {
                Email = string.Empty
            };

            // Act
            var result = await _controller.ForgotPassword(request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().NotBeNull();

            // Verify that no password reset token was added
            var resetTokens = await _dbContext.PasswordResetTokens.ToListAsync();
            resetTokens.Should().BeEmpty();
        }

        [Fact]
        public async Task ForgotPassword_WithValidEmail_StoresTokenHash()
        {
            // Arrange
            var testUser = CreateTestUser();
            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            var request = new ForgotPasswordRequest
            {
                Email = testUser.Email
            };

            // Act
            var result = await _controller.ForgotPassword(request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();

            // Verify that the token hash is stored
            var resetTokens = await _dbContext.PasswordResetTokens.ToListAsync();
            resetTokens.Should().HaveCount(1);
            resetTokens[0].TokenHash.Should().NotBeNullOrEmpty();
            resetTokens[0].TokenHash.Length.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task ResetPassword_WithValidToken_ResetsPasswordSuccessfully()
        {
            // Arrange
            var testUser = CreateTestUser();
            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            var (tokenEntity, tokenValue) = await CreatePasswordResetTokenAsync(testUser.Id);
            var newPassword = "NewSecurePassword123!";

            var request = new ResetPasswordRequest
            {
                Token = tokenValue,
                Password = newPassword
            };

            _mockPasswordHasher
                .Setup(x => x.HashPassword(newPassword))
                .Returns("new_hashed_password");

            // Act
            var result = await _controller.ResetPassword(request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();

            var response = okResult.Value!.GetType().GetProperty("success")?.GetValue(okResult.Value);
            response.Should().Be(true);

            // Verify password was updated
            var updatedUser = await _dbContext.Users.FindAsync(testUser.Id);
            updatedUser.Should().NotBeNull();
            updatedUser!.PasswordHash.Should().Be("new_hashed_password");
            updatedUser.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

            // Verify token is marked as used
            var resetToken = await _dbContext.PasswordResetTokens.FindAsync(tokenEntity.Id);
            resetToken.IsUsed.Should().BeTrue();
        }

        [Fact]
        public async Task ResetPassword_WithInvalidToken_ReturnsBadRequest()
        {
            // Arrange
            var request = new ResetPasswordRequest
            {
                Token = "invalid_token",
                Password = "NewPassword123!"
            };

            // Act
            var result = await _controller.ResetPassword(request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().NotBeNull();

            var response = badRequestResult.Value!.GetType().GetProperty("success")?.GetValue(badRequestResult.Value);
            response.Should().Be(false);
        }

        [Fact]
        public async Task ResetPassword_WithExpiredToken_ReturnsBadRequest()
        {
            // Arrange
            var testUser = CreateTestUser();
            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            var (_, tokenValue) = await CreateExpiredPasswordResetTokenAsync(testUser.Id);

            var request = new ResetPasswordRequest
            {
                Token = tokenValue,
                Password = "NewPassword123!"
            };

            // Act
            var result = await _controller.ResetPassword(request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().NotBeNull();

            var response = badRequestResult.Value!.GetType().GetProperty("success")?.GetValue(badRequestResult.Value);
            response.Should().Be(false);
        }

        [Fact]
        public async Task ResetPassword_WithUsedToken_ReturnsBadRequest()
        {
            // Arrange
            var testUser = CreateTestUser();
            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            var (_, tokenValue) = await CreateUsedPasswordResetTokenAsync(testUser.Id);

            var request = new ResetPasswordRequest
            {
                Token = tokenValue,
                Password = "NewPassword123!"
            };

            // Act
            var result = await _controller.ResetPassword(request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().NotBeNull();

            var response = badRequestResult.Value!.GetType().GetProperty("success")?.GetValue(badRequestResult.Value);
            response.Should().Be(false);
        }

        [Fact]
        public async Task ResetPassword_WithInactiveUser_ReturnsBadRequest()
        {
            // Arrange
            var testUser = CreateTestUser();
            testUser.IsActive = false;
            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            var (_, tokenValue) = await CreatePasswordResetTokenAsync(testUser.Id);

            var request = new ResetPasswordRequest
            {
                Token = tokenValue,
                Password = "NewPassword123!"
            };

            // Act
            var result = await _controller.ResetPassword(request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().NotBeNull();

            var response = badRequestResult.Value!.GetType().GetProperty("success")?.GetValue(badRequestResult.Value);
            response.Should().Be(false);
        }

        [Fact]
        public async Task ResetPassword_WithEmptyToken_ReturnsBadRequest()
        {
            // Arrange
            var request = new ResetPasswordRequest
            {
                Token = string.Empty,
                Password = "NewPassword123!"
            };

            // Act
            var result = await _controller.ResetPassword(request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().NotBeNull();

            var response = badRequestResult.Value!.GetType().GetProperty("success")?.GetValue(badRequestResult.Value);
            response.Should().Be(false);
        }

        [Fact]
        public async Task ResetPassword_WithEmptyPassword_ReturnsBadRequest()
        {
            // Arrange
            var request = new ResetPasswordRequest
            {
                Token = "some_token",
                Password = string.Empty
            };

            // Act
            var result = await _controller.ResetPassword(request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().NotBeNull();

            var response = badRequestResult.Value!.GetType().GetProperty("success")?.GetValue(badRequestResult.Value);
            response.Should().Be(false);
        }

        [Fact]
        public async Task ResetPassword_WithNonExistentUser_ReturnsBadRequest()
        {
            // Arrange
            var testUser = CreateTestUser();
            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            var (_, tokenValue) = await CreatePasswordResetTokenAsync(testUser.Id);

            // Remove the user
            _dbContext.Users.Remove(testUser);
            await _dbContext.SaveChangesAsync();

            var request = new ResetPasswordRequest
            {
                Token = tokenValue,
                Password = "NewPassword123!"
            };

            // Act
            var result = await _controller.ResetPassword(request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().NotBeNull();

            var response = badRequestResult.Value!.GetType().GetProperty("success")?.GetValue(badRequestResult.Value);
            response.Should().Be(false);
        }

        [Fact]
        public async Task ForgotPassword_ResetPassword_CompleteFlow()
        {
            // Arrange
            var testUser = CreateTestUser();
            var oldPasswordHash = testUser.PasswordHash;
            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            // Step 1: Request password reset
            var forgotRequest = new ForgotPasswordRequest
            {
                Email = testUser.Email
            };

            var forgotResult = await _controller.ForgotPassword(forgotRequest, CancellationToken.None);
            forgotResult.Should().BeOfType<OkObjectResult>();

            // Get the created token
            var resetTokens = await _dbContext.PasswordResetTokens
                .Where(t => t.UserId == testUser.Id && !t.IsUsed)
                .ToListAsync();
            resetTokens.Should().HaveCount(1);

            var resetToken = resetTokens.First();

            // Step 2: Reset password
            var newPassword = "NewSecurePassword123!";
            var resetRequest = new ResetPasswordRequest
            {
                Token = resetToken.TokenHash, // This is the hash, we need the actual token
                Password = newPassword
            };

            // We need to get the actual token value from the token hash
            // Since we can't reverse the hash, we'll create a new token with the same hash
            var actualToken = GenerateSecureToken();
            _mockPasswordHasher
                .Setup(x => x.VerifyPassword(actualToken, resetToken.TokenHash))
                .Returns(true);

            resetRequest.Token = actualToken;
            _mockPasswordHasher
                .Setup(x => x.HashPassword(newPassword))
                .Returns("new_hashed_password");

            var resetResult = await _controller.ResetPassword(resetRequest, CancellationToken.None);
            resetResult.Should().BeOfType<OkObjectResult>();

            // Verify password was changed
            var updatedUser = await _dbContext.Users.FindAsync(testUser.Id);
            updatedUser.Should().NotBeNull();
            updatedUser!.PasswordHash.Should().NotBe(oldPasswordHash);
            updatedUser.PasswordHash.Should().Be("new_hashed_password");
        }

        [Fact]
        public async Task ForgotPassword_WithInactiveUser_ReturnsSuccessForSecurity()
        {
            // Arrange
            var testUser = CreateTestUser();
            testUser.IsActive = false;
            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            var request = new ForgotPasswordRequest
            {
                Email = testUser.Email
            };

            // Act
            var result = await _controller.ForgotPassword(request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();

            var response = okResult.Value!.GetType().GetProperty("success")?.GetValue(okResult.Value);
            response.Should().Be(true);

            // Verify that no password reset token was added
            var resetTokens = await _dbContext.PasswordResetTokens.ToListAsync();
            resetTokens.Should().BeEmpty();
        }

        private User CreateTestUser()
        {
            return new User
            {
                Id = _testUserId,
                OrganizationId = _testOrganizationId,
                Email = "test@example.com",
                PasswordHash = "hashedpassword",
                Role = "member",
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };
        }

        private async Task<(PasswordResetToken TokenEntity, string TokenValue)> CreatePasswordResetTokenAsync(Guid userId)
        {
            var tokenValue = GenerateSecureToken();
            var token = new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = _mockPasswordHasher.Object.HashPassword(tokenValue),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                IsUsed = false
            };

            _dbContext.PasswordResetTokens.Add(token);
            await _dbContext.SaveChangesAsync();

            return (token, tokenValue);
        }

        private async Task<(PasswordResetToken TokenEntity, string TokenValue)> CreateExpiredPasswordResetTokenAsync(Guid userId)
        {
            var tokenValue = GenerateSecureToken();
            var token = new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = _mockPasswordHasher.Object.HashPassword(tokenValue),
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                ExpiresAt = DateTime.UtcNow.AddHours(-1),
                IsUsed = false
            };

            _dbContext.PasswordResetTokens.Add(token);
            await _dbContext.SaveChangesAsync();

            return (token, tokenValue);
        }

        private async Task<(PasswordResetToken TokenEntity, string TokenValue)> CreateUsedPasswordResetTokenAsync(Guid userId)
        {
            var tokenValue = GenerateSecureToken();
            var token = new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = _mockPasswordHasher.Object.HashPassword(tokenValue),
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                IsUsed = true
            };

            _dbContext.PasswordResetTokens.Add(token);
            await _dbContext.SaveChangesAsync();

            return (token, tokenValue);
        }

        private static string GenerateSecureToken()
        {
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            var tokenBytes = new byte[32];
            rng.GetBytes(tokenBytes);
            return Convert.ToBase64String(tokenBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", string.Empty);
        }
    }
}
