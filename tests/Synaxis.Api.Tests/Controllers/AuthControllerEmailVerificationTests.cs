// <copyright file="AuthControllerEmailVerificationTests.cs" company="Synaxis">
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
/// Tests for the AuthController email verification endpoints.
/// </summary>
public sealed class AuthControllerEmailVerificationTests : IDisposable
{
    private readonly SynaxisDbContext _context;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<ILogger<AuthController>> _mockLogger;
    private readonly JwtOptions _jwtOptions;
    private readonly IAuthenticationService _authenticationService;
    private readonly AuthController _controller;

    public AuthControllerEmailVerificationTests()
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
    public async Task VerifyEmail_ValidToken_VerifiesEmail()
    {
        // Arrange
        var user = CreateTestUser();
        var token = await CreateEmailVerificationTokenAsync(user.Id);

        var request = new VerifyEmailRequest
        {
            Token = token
        };

        // Act
        var result = await _controller.VerifyEmail(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();

        // Verify user email is marked as verified
        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser.EmailVerifiedAt.Should().NotBeNull();
        updatedUser.EmailVerifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify token is marked as used
        var verificationToken = await _context.EmailVerificationTokens
            .FirstOrDefaultAsync(t => t.TokenHash == HashToken(token));
        verificationToken.Should().NotBeNull();
        verificationToken.IsUsed.Should().BeTrue();
    }

    [Fact]
    public async Task GetVerificationStatus_VerifiedUser_ReturnsVerified()
    {
        // Arrange
        var verifiedAt = DateTime.UtcNow.AddHours(-1);
        var user = CreateTestUser(emailVerified: true, verifiedAt: verifiedAt);
        SetupUserClaim(user.Id);

        // Set up the mock to return the user
        _mockUserService
            .Setup(x => x.GetUserAsync(user.Id))
            .ReturnsAsync(user);

        // Act
        var result = await _controller.GetVerificationStatus();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var status = okResult.Value as EmailVerificationStatusDto;
        status.Should().NotBeNull();
        status.IsVerified.Should().BeTrue();
        status.Email.Should().Be(user.Email);
        status.VerifiedAt.Should().BeCloseTo(verifiedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetVerificationStatus_UnauthorizedUser_ReturnsUnauthorized()
    {
        // Arrange - No user claim set up

        // Act
        var result = await _controller.GetVerificationStatus();

        // Assert
        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Register_SendsVerificationEmail()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "newuser@example.com",
            Password = "SecurePassword123!",
            FirstName = "New",
            LastName = "User",
            DataResidencyRegion = "us-east-1",
            CreatedInRegion = "us-east-1"
        };

        _mockUserService
            .Setup(x => x.CreateUserAsync(It.IsAny<CreateUserRequest>()))
            .ReturnsAsync((CreateUserRequest r) => new User
            {
                Id = Guid.NewGuid(),
                OrganizationId = Guid.NewGuid(),
                Email = r.Email,
                FirstName = r.FirstName,
                LastName = r.LastName,
                PasswordHash = "hashed_password",
                Role = "member",
                IsActive = true,
                EmailVerifiedAt = null,
                DataResidencyRegion = r.DataResidencyRegion,
                CreatedInRegion = r.CreatedInRegion,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        // Act
        var result = await _controller.Register(request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var authResult = okResult.Value as DTOs.Authentication.AuthenticationResult;
        authResult.Should().NotBeNull();
        authResult.Success.Should().BeTrue();
        authResult.Message.Should().Be("Registration successful. Please check your email to verify your account.");

        // Verify email service was called
        _mockEmailService.Verify(
            x => x.SendVerificationEmailAsync(
                request.Email,
                It.Is<string>(url => url.Contains("token="))),
            Times.Once);
    }

    [Fact]
    public async Task VerifyEmail_ExpiredToken_ReturnsBadRequest()
    {
        // Arrange
        var user = CreateTestUser();
        var token = await CreateExpiredEmailVerificationTokenAsync(user.Id);

        var request = new VerifyEmailRequest
        {
            Token = token
        };

        // Act
        var result = await _controller.VerifyEmail(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();

        // Verify user email is NOT marked as verified
        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser.EmailVerifiedAt.Should().BeNull();
    }

    [Fact]
    public async Task VerifyEmail_UsedToken_ReturnsBadRequest()
    {
        // Arrange
        var user = CreateTestUser();
        var token = await CreateUsedEmailVerificationTokenAsync(user.Id);

        var request = new VerifyEmailRequest
        {
            Token = token
        };

        // Act
        var result = await _controller.VerifyEmail(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();

        // Verify user email is NOT marked as verified
        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser.EmailVerifiedAt.Should().BeNull();
    }

    [Fact]
    public async Task VerifyEmail_InvalidToken_ReturnsBadRequest()
    {
        // Arrange
        var request = new VerifyEmailRequest
        {
            Token = "invalid_token"
        };

        // Act
        var result = await _controller.VerifyEmail(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
    }

    [Fact]
    public async Task VerifyEmail_AlreadyVerified_ReturnsBadRequest()
    {
        // Arrange
        var verifiedAt = DateTime.UtcNow.AddHours(-1);
        var user = CreateTestUser(emailVerified: true, verifiedAt: verifiedAt);
        var token = await CreateEmailVerificationTokenAsync(user.Id);

        var request = new VerifyEmailRequest
        {
            Token = token
        };

        // Act
        var result = await _controller.VerifyEmail(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
    }

    [Fact]
    public async Task ResendVerificationEmail_ValidEmail_SendsNewEmail()
    {
        // Arrange
        var user = CreateTestUser(emailVerified: false);
        var request = new ResendVerificationRequest
        {
            Email = user.Email
        };

        // Act
        var result = await _controller.ResendVerificationEmail(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();

        // Verify email service was called
        _mockEmailService.Verify(
            x => x.SendVerificationEmailAsync(
                user.Email,
                It.Is<string>(url => url.Contains("token="))),
            Times.Once);
    }

    [Fact]
    public async Task ResendVerificationEmail_AlreadyVerified_ReturnsSuccess()
    {
        // Arrange
        var verifiedAt = DateTime.UtcNow.AddHours(-1);
        var user = CreateTestUser(emailVerified: true, verifiedAt: verifiedAt);
        var request = new ResendVerificationRequest
        {
            Email = user.Email
        };

        // Act
        var result = await _controller.ResendVerificationEmail(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();

        // Verify email service was NOT called
        _mockEmailService.Verify(
            x => x.SendVerificationEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ResendVerificationEmail_NonExistentEmail_ReturnsSuccess()
    {
        // Arrange
        var request = new ResendVerificationRequest
        {
            Email = "nonexistent@example.com"
        };

        // Act
        var result = await _controller.ResendVerificationEmail(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();

        // Verify email service was NOT called
        _mockEmailService.Verify(
            x => x.SendVerificationEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);
    }

    private User CreateTestUser(bool emailVerified = false, DateTime? verifiedAt = null)
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
            PasswordHash = "hashed_password",
            Role = "member",
            IsActive = true,
            EmailVerifiedAt = emailVerified ? verifiedAt ?? DateTime.UtcNow : null,
            DataResidencyRegion = "us-east-1",
            CreatedInRegion = "us-east-1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        _context.SaveChanges();

        return user;
    }

    private async Task<string> CreateEmailVerificationTokenAsync(Guid userId)
    {
        var tokenValue = Guid.NewGuid().ToString();
        var token = new EmailVerificationToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = HashToken(tokenValue),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            IsUsed = false
        };

        _context.EmailVerificationTokens.Add(token);
        await _context.SaveChangesAsync();

        return tokenValue;
    }

    private async Task<string> CreateExpiredEmailVerificationTokenAsync(Guid userId)
    {
        var tokenValue = Guid.NewGuid().ToString();
        var token = new EmailVerificationToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = HashToken(tokenValue),
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            IsUsed = false
        };

        _context.EmailVerificationTokens.Add(token);
        await _context.SaveChangesAsync();

        return tokenValue;
    }

    private async Task<string> CreateUsedEmailVerificationTokenAsync(Guid userId)
    {
        var tokenValue = Guid.NewGuid().ToString();
        var token = new EmailVerificationToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = HashToken(tokenValue),
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            ExpiresAt = DateTime.UtcNow.AddHours(23),
            IsUsed = true
        };

        _context.EmailVerificationTokens.Add(token);
        await _context.SaveChangesAsync();

        return tokenValue;
    }

    private static string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private void SetupUserClaim(Guid userId)
    {
        var claims = new[]
        {
            new System.Security.Claims.Claim(
                System.Security.Claims.ClaimTypes.NameIdentifier,
                userId.ToString())
        };
        var identity = new System.Security.Claims.ClaimsIdentity(claims);
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);
        _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
            {
                User = principal
            }
        };
    }
}
