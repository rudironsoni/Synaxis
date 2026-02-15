// <copyright file="AuthControllerPasswordResetTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.Tests.Controllers;

using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Synaxis.Api.Controllers;
using Synaxis.Api.DTOs.Authentication;
using Synaxis.Core.Contracts;
using Synaxis.Core.Models;
using Synaxis.Infrastructure.Data;
using Synaxis.Infrastructure.Services;
using Xunit;

/// <summary>
/// Tests for the AuthController password reset endpoints.
/// </summary>
public sealed class AuthControllerPasswordResetTests : IDisposable
{
    private readonly SynaxisDbContext _context;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<ILogger<AuthController>> _mockLogger;
    private readonly JwtOptions _jwtOptions;
    private readonly IAuthenticationService _authenticationService;
    private readonly AuthController _controller;

    public AuthControllerPasswordResetTests()
    {
        var options = new DbContextOptionsBuilder<SynaxisDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SynaxisDbContext(options);
        _mockUserService = new Mock<IUserService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockLogger = new Mock<ILogger<AuthController>>();

        _jwtOptions = new JwtOptions
        {
            SecretKey = "ThisIsAVeryLongSecretKeyForTestingPurposesOnly123456789",
            Issuer = "Synaxis",
            Audience = "SynaxisAPI",
            AccessTokenExpirationMinutes = 60,
            RefreshTokenExpirationDays = 7
        };

        var jwtOptionsMock = new Mock<IOptions<JwtOptions>>();
        jwtOptionsMock.Setup(x => x.Value).Returns(_jwtOptions);

        _authenticationService = new AuthenticationService(
            _context,
            _mockUserService.Object,
            jwtOptionsMock.Object,
            Mock.Of<ILogger<AuthenticationService>>());

        _controller = new AuthController(
            _authenticationService,
            _mockUserService.Object,
            _mockEmailService.Object,
            _mockLogger.Object,
            _context);

        // Set up the Request object
        _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
            {
                Request =
                {
                    Scheme = "https",
                    Host = new Microsoft.AspNetCore.Http.HostString("localhost:5000")
                }
            }
        };
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    [Fact]
    public async Task ForgotPassword_ValidEmail_SendsResetEmail()
    {
        // Arrange
        var user = CreateTestUser();
        var request = new ForgotPasswordRequest
        {
            Email = user.Email
        };

        // Act
        var result = await _controller.ForgotPassword(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Value.Should().BeEquivalentTo(new { message = "If the email exists, a password reset link has been sent" });

        // Verify email service was called
        _mockEmailService.Verify(
            x => x.SendPasswordResetEmailAsync(
                user.Email,
                It.Is<string>(url => url.Contains("token="))),
            Times.Once);

        // Verify token was created
        var resetToken = await _context.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.UserId == user.Id && !t.IsUsed);
        resetToken.Should().NotBeNull();
        resetToken.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddHours(1), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ForgotPassword_NonExistentEmail_ReturnsSuccessMessage()
    {
        // Arrange
        var request = new ForgotPasswordRequest
        {
            Email = "nonexistent@example.com"
        };

        // Act
        var result = await _controller.ForgotPassword(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Value.Should().BeEquivalentTo(new { message = "If the email exists, a password reset link has been sent" });

        // Verify email service was NOT called
        _mockEmailService.Verify(
            x => x.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ForgotPassword_InactiveUser_ReturnsSuccessMessage()
    {
        // Arrange
        var user = CreateTestUser(isActive: false);
        var request = new ForgotPasswordRequest
        {
            Email = user.Email
        };

        // Act
        var result = await _controller.ForgotPassword(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Value.Should().BeEquivalentTo(new { message = "If the email exists, a password reset link has been sent" });

        // Verify email service was NOT called
        _mockEmailService.Verify(
            x => x.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ForgotPassword_InvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new ForgotPasswordRequest
        {
            Email = string.Empty
        };

        // Act
        var result = await _controller.ForgotPassword(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Value.Should().BeEquivalentTo(new { message = "Email is required" });
    }

    [Fact]
    public async Task ForgotPassword_ExistingTokens_InvalidateOldTokens()
    {
        // Arrange
        var user = CreateTestUser();
        var (oldToken, _) = await CreatePasswordResetTokenAsync(user.Id);

        var request = new ForgotPasswordRequest
        {
            Email = user.Email
        };

        // Act
        await _controller.ForgotPassword(request);

        // Assert
        // Old token should be invalidated
        var invalidatedToken = await _context.PasswordResetTokens.FindAsync(oldToken.Id);
        invalidatedToken.Should().NotBeNull();
        invalidatedToken.IsUsed.Should().BeTrue();

        // New token should be created
        var newTokens = await _context.PasswordResetTokens
            .Where(t => t.UserId == user.Id && !t.IsUsed)
            .ToListAsync();
        newTokens.Should().HaveCount(1);
    }

    [Fact]
    public async Task ResetPassword_ValidToken_ResetsPassword()
    {
        // Arrange
        var user = CreateTestUser();
        var (tokenEntity, tokenValue) = await CreatePasswordResetTokenAsync(user.Id);
        var newPassword = "NewSecurePassword123!";

        var request = new ResetPasswordRequest
        {
            Token = tokenValue,
            NewPassword = newPassword
        };

        _mockUserService
            .Setup(x => x.HashPassword(newPassword))
            .Returns("new_hashed_password");

        // Act
        var result = await _controller.ResetPassword(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Value.Should().BeEquivalentTo(new { success = true, message = "Password reset successful" });

        // Verify password was updated
        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser.PasswordHash.Should().Be("new_hashed_password");
        updatedUser.PasswordChangedAt.Should().NotBeNull();
        updatedUser.PasswordChangedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        updatedUser.MustChangePassword.Should().BeFalse();

        // Verify token is marked as used
        var resetToken = await _context.PasswordResetTokens.FindAsync(tokenEntity.Id);
        resetToken.IsUsed.Should().BeTrue();
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_ReturnsBadRequest()
    {
        // Arrange
        var request = new ResetPasswordRequest
        {
            Token = "invalid_token",
            NewPassword = "NewPassword123!"
        };

        // Act
        var result = await _controller.ResetPassword(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Value.Should().BeEquivalentTo(new { success = false, message = "Invalid or expired token" });
    }

    [Fact]
    public async Task ResetPassword_ExpiredToken_ReturnsBadRequest()
    {
        // Arrange
        var user = CreateTestUser();
        var (_, tokenValue) = await CreateExpiredPasswordResetTokenAsync(user.Id);

        var request = new ResetPasswordRequest
        {
            Token = tokenValue,
            NewPassword = "NewPassword123!"
        };

        // Act
        var result = await _controller.ResetPassword(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Value.Should().BeEquivalentTo(new { success = false, message = "Token has expired" });
    }

    [Fact]
    public async Task ResetPassword_UsedToken_ReturnsBadRequest()
    {
        // Arrange
        var user = CreateTestUser();
        var (_, tokenValue) = await CreateUsedPasswordResetTokenAsync(user.Id);

        var request = new ResetPasswordRequest
        {
            Token = tokenValue,
            NewPassword = "NewPassword123!"
        };

        // Act
        var result = await _controller.ResetPassword(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Value.Should().BeEquivalentTo(new { success = false, message = "Token has already been used" });
    }

    [Fact]
    public async Task ResetPassword_InactiveUser_ReturnsBadRequest()
    {
        // Arrange
        var user = CreateTestUser(isActive: false);
        var (_, tokenValue) = await CreatePasswordResetTokenAsync(user.Id);

        var request = new ResetPasswordRequest
        {
            Token = tokenValue,
            NewPassword = "NewPassword123!"
        };

        // Act
        var result = await _controller.ResetPassword(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Value.Should().BeEquivalentTo(new { success = false, message = "User account is inactive" });
    }

    [Fact]
    public async Task ResetPassword_EmptyToken_ReturnsBadRequest()
    {
        // Arrange
        var request = new ResetPasswordRequest
        {
            Token = string.Empty,
            NewPassword = "NewPassword123!"
        };

        // Act
        var result = await _controller.ResetPassword(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Value.Should().BeEquivalentTo(new { success = false, message = "Token and password are required" });
    }

    [Fact]
    public async Task ResetPassword_EmptyPassword_ReturnsBadRequest()
    {
        // Arrange
        var request = new ResetPasswordRequest
        {
            Token = "some_token",
            NewPassword = string.Empty
        };

        // Act
        var result = await _controller.ResetPassword(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Value.Should().BeEquivalentTo(new { success = false, message = "Token and password are required" });
    }

    [Fact]
    public async Task ResetPassword_ResetsFailedPasswordChangeAttempts()
    {
        // Arrange
        var user = CreateTestUser();
        user.FailedPasswordChangeAttempts = 3;
        user.PasswordChangeLockedUntil = DateTime.UtcNow.AddMinutes(5);
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        var (_, tokenValue) = await CreatePasswordResetTokenAsync(user.Id);
        var newPassword = "NewSecurePassword123!";

        var request = new ResetPasswordRequest
        {
            Token = tokenValue,
            NewPassword = newPassword
        };

        _mockUserService
            .Setup(x => x.HashPassword(newPassword))
            .Returns("new_hashed_password");

        // Act
        await _controller.ResetPassword(request);

        // Assert
        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser.FailedPasswordChangeAttempts.Should().Be(0);
        updatedUser.PasswordChangeLockedUntil.Should().BeNull();
    }

    [Fact]
    public async Task ForgotPassword_ResetPassword_CompleteFlow()
    {
        // Arrange
        var user = CreateTestUser();
        var oldPasswordHash = user.PasswordHash;

        // Step 1: Request password reset
        var forgotRequest = new ForgotPasswordRequest
        {
            Email = user.Email
        };

        var forgotResult = await _controller.ForgotPassword(forgotRequest);
        forgotResult.Should().BeOfType<OkObjectResult>();

        // Get the created token
        var resetToken = await _context.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.UserId == user.Id && !t.IsUsed);
        resetToken.Should().NotBeNull();

        // Step 2: Reset password
        var newPassword = "NewSecurePassword123!";
        var resetRequest = new ResetPasswordRequest
        {
            Token = resetToken.TokenHash, // This is the hash, we need the actual token
            NewPassword = newPassword
        };

        // We need to get the actual token value from the email service call
        string actualToken = null;
        _mockEmailService
            .Setup(x => x.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((to, url) =>
            {
                // Extract token from URL
                var urlParts = url.Split("token=");
                actualToken = urlParts[urlParts.Length - 1];
            })
            .Returns(Task.CompletedTask);

        // Re-run forgot password to capture the token
        await _controller.ForgotPassword(forgotRequest);

        // Now reset with the actual token
        resetRequest.Token = actualToken;
        _mockUserService
            .Setup(x => x.HashPassword(newPassword))
            .Returns("new_hashed_password");

        var resetResult = await _controller.ResetPassword(resetRequest);
        resetResult.Should().BeOfType<OkObjectResult>();

        // Verify password was changed
        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser.PasswordHash.Should().NotBe(oldPasswordHash);
        updatedUser.PasswordHash.Should().Be("new_hashed_password");
    }

    private User CreateTestUser(bool isActive = true)
    {
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Organization",
            Slug = "test-org",
            PrimaryRegion = "us-east-1",
            Tier = "free",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Organizations.Add(organization);
        _context.SaveChanges();

        var user = new User
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "old_hashed_password",
            Role = "member",
            IsActive = isActive,
            EmailVerifiedAt = DateTime.UtcNow,
            DataResidencyRegion = "us-east-1",
            CreatedInRegion = "us-east-1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        _context.SaveChanges();

        return user;
    }

    private async Task<(PasswordResetToken TokenEntity, string TokenValue)> CreatePasswordResetTokenAsync(Guid userId)
    {
        var tokenValue = Guid.NewGuid().ToString();
        var token = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = HashToken(tokenValue),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = false
        };

        _context.PasswordResetTokens.Add(token);
        await _context.SaveChangesAsync();

        return (token, tokenValue);
    }

    private async Task<(PasswordResetToken TokenEntity, string TokenValue)> CreateExpiredPasswordResetTokenAsync(Guid userId)
    {
        var tokenValue = Guid.NewGuid().ToString();
        var token = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = HashToken(tokenValue),
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            ExpiresAt = DateTime.UtcNow.AddHours(-1),
            IsUsed = false
        };

        _context.PasswordResetTokens.Add(token);
        await _context.SaveChangesAsync();

        return (token, tokenValue);
    }

    private async Task<(PasswordResetToken TokenEntity, string TokenValue)> CreateUsedPasswordResetTokenAsync(Guid userId)
    {
        var tokenValue = Guid.NewGuid().ToString();
        var token = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = HashToken(tokenValue),
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = true
        };

        _context.PasswordResetTokens.Add(token);
        await _context.SaveChangesAsync();

        return (token, tokenValue);
    }

    private static string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
