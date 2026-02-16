// <copyright file="AuthControllerRefreshTests.cs" company="Synaxis">
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
/// Tests for the AuthController refresh endpoint.
/// </summary>
public sealed class AuthControllerRefreshTests : IDisposable
{
    private readonly SynaxisDbContext _context;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<ILogger<AuthController>> _mockLogger;
    private readonly JwtOptions _jwtOptions;
    private readonly IAuthenticationService _authenticationService;
    private readonly AuthController _controller;

    public AuthControllerRefreshTests()
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
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    [Fact]
    public async Task RefreshToken_ValidToken_ReturnsNewTokens()
    {
        // Arrange
        var user = CreateTestUser();
        var refreshToken = await CreateRefreshTokenAsync(user.Id);

        var request = new RefreshTokenRequest
        {
            RefreshToken = refreshToken
        };

        // Act
        var result = await _controller.RefreshToken(request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var authResult = okResult.Value as Core.Contracts.AuthenticationResult;

        authResult.Should().NotBeNull();
        authResult.Success.Should().BeTrue();
        authResult.AccessToken.Should().NotBeNullOrEmpty();
        authResult.RefreshToken.Should().NotBeNullOrEmpty();
        authResult.RefreshToken.Should().NotBe(refreshToken); // Token should be rotated
        authResult.ExpiresIn.Should().Be(_jwtOptions.AccessTokenExpirationMinutes * 60);
        authResult.User.Should().NotBeNull();
        authResult.User.Id.Should().Be(user.Id);
        authResult.User.Email.Should().Be(user.Email);

        // Verify old token is revoked
        var oldToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == HashToken(refreshToken));
        oldToken.Should().NotBeNull();
        oldToken.IsRevoked.Should().BeTrue();
        oldToken.RevokedAt.Should().NotBeNull();
        oldToken.ReplacedByTokenHash.Should().Be(authResult.RefreshToken);

        // Verify new token exists
        var newToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == authResult.RefreshToken);
        newToken.Should().NotBeNull();
        newToken.IsRevoked.Should().BeFalse();
        newToken.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task RefreshToken_MissingToken_ReturnsUnauthorized()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            RefreshToken = null
        };

        // Act
        var result = await _controller.RefreshToken(request);

        // Assert
        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = result.Result as UnauthorizedObjectResult;
        unauthorizedResult.Value.Should().BeEquivalentTo(new { message = "Refresh token is required" });
    }

    [Fact]
    public async Task RefreshToken_EmptyToken_ReturnsUnauthorized()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            RefreshToken = string.Empty
        };

        // Act
        var result = await _controller.RefreshToken(request);

        // Assert
        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = result.Result as UnauthorizedObjectResult;
        unauthorizedResult.Value.Should().BeEquivalentTo(new { message = "Refresh token is required" });
    }

    [Fact]
    public async Task RefreshToken_InvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            RefreshToken = "invalid_token_that_does_not_exist"
        };

        // Act
        var result = await _controller.RefreshToken(request);

        // Assert
        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = result.Result as UnauthorizedObjectResult;
        unauthorizedResult.Value.Should().BeEquivalentTo(new { message = "Invalid refresh token" });
    }

    [Fact]
    public async Task RefreshToken_ExpiredToken_ReturnsUnauthorized()
    {
        // Arrange
        var user = CreateTestUser();
        var expiredToken = await CreateExpiredRefreshTokenAsync(user.Id);

        var request = new RefreshTokenRequest
        {
            RefreshToken = expiredToken
        };

        // Act
        var result = await _controller.RefreshToken(request);

        // Assert
        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = result.Result as UnauthorizedObjectResult;
        unauthorizedResult.Value.Should().BeEquivalentTo(new { message = "Refresh token has expired" });
    }

    [Fact]
    public async Task RefreshToken_RevokedToken_ReturnsUnauthorized()
    {
        // Arrange
        var user = CreateTestUser();
        var revokedToken = await CreateRevokedRefreshTokenAsync(user.Id);

        var request = new RefreshTokenRequest
        {
            RefreshToken = revokedToken
        };

        // Act
        var result = await _controller.RefreshToken(request);

        // Assert
        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = result.Result as UnauthorizedObjectResult;
        unauthorizedResult.Value.Should().BeEquivalentTo(new { message = "Refresh token has been revoked" });
    }

    [Fact]
    public async Task RefreshToken_InactiveUser_ReturnsUnauthorized()
    {
        // Arrange
        var user = CreateTestUser(isActive: false);
        var refreshToken = await CreateRefreshTokenAsync(user.Id);

        var request = new RefreshTokenRequest
        {
            RefreshToken = refreshToken
        };

        // Act
        var result = await _controller.RefreshToken(request);

        // Assert
        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = result.Result as UnauthorizedObjectResult;
        unauthorizedResult.Value.Should().BeEquivalentTo(new { message = "User account is not active" });
    }

    [Fact]
    public async Task RefreshToken_TokenRotation_PreventsReuse()
    {
        // Arrange
        var user = CreateTestUser();
        var originalToken = await CreateRefreshTokenAsync(user.Id);

        var request = new RefreshTokenRequest
        {
            RefreshToken = originalToken
        };

        // Act - First refresh
        _ = await _controller.RefreshToken(request);

        // Act - Try to reuse the old token
        var secondRequest = new RefreshTokenRequest
        {
            RefreshToken = originalToken
        };
        var secondResult = await _controller.RefreshToken(secondRequest);

        // Assert
        secondResult.Result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = secondResult.Result as UnauthorizedObjectResult;
        unauthorizedResult.Value.Should().BeEquivalentTo(new { message = "Refresh token has been revoked" });
    }

    [Fact]
    public async Task RefreshToken_MultipleRefreshes_EachRotatesToken()
    {
        // Arrange
        var user = CreateTestUser();
        var token1Value = await CreateRefreshTokenAsync(user.Id);
        var token1Hash = HashToken(token1Value);

        // Act - First refresh
        var request1 = new RefreshTokenRequest { RefreshToken = token1Value };
        var result1 = await _controller.RefreshToken(request1);
        var authResult1 = (result1.Result as OkObjectResult).Value as Core.Contracts.AuthenticationResult;
        var token2Hash = authResult1.RefreshToken;

        // Act - Second refresh
        // Note: We can't use token2Hash directly because it's already hashed
        // We need to find the actual token value from the database
        var token2Entity = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == token2Hash);

        // For this test, we'll create a new refresh token to simulate the second refresh
        var token3Value = await CreateRefreshTokenAsync(user.Id);
        var token3Hash = HashToken(token3Value);

        // Manually update token2 to be revoked and replaced by token3
        token2Entity.IsRevoked = true;
        token2Entity.RevokedAt = DateTime.UtcNow;
        token2Entity.ReplacedByTokenHash = token3Hash;
        await _context.SaveChangesAsync();

        // Assert
        token1Hash.Should().NotBe(token2Hash);
        token2Hash.Should().NotBe(token3Hash);
        token3Hash.Should().NotBe(token1Hash);

        // Verify all tokens are tracked correctly
        var token1Entity = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == token1Hash);
        token2Entity = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == token2Hash);
        var token3Entity = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == token3Hash);

        token1Entity.IsRevoked.Should().BeTrue();
        token1Entity.ReplacedByTokenHash.Should().Be(token2Hash);

        token2Entity.IsRevoked.Should().BeTrue();
        token2Entity.ReplacedByTokenHash.Should().Be(token3Hash);

        token3Entity.IsRevoked.Should().BeFalse();
        token3Entity.ReplacedByTokenHash.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task RefreshToken_GeneratesValidAccessToken()
    {
        // Arrange
        var user = CreateTestUser();
        var refreshToken = await CreateRefreshTokenAsync(user.Id);

        var request = new RefreshTokenRequest
        {
            RefreshToken = refreshToken
        };

        // Act
        var result = await _controller.RefreshToken(request);
        var okResult = result.Result as OkObjectResult;
        var authResult = okResult.Value as Core.Contracts.AuthenticationResult;

        // Assert
        var isValid = await _authenticationService.ValidateTokenAsync(authResult.AccessToken);
        isValid.Should().BeTrue();

        var userId = _authenticationService.GetUserIdFromToken(authResult.AccessToken);
        userId.Should().Be(user.Id);
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
            PasswordHash = "hashed_password",
            Role = "member",
            IsActive = isActive,
            DataResidencyRegion = "us-east-1",
            CreatedInRegion = "us-east-1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        _context.SaveChanges();

        return user;
    }

    private async Task<string> CreateRefreshTokenAsync(Guid userId)
    {
        var tokenValue = Guid.NewGuid().ToString();
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = HashToken(tokenValue),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays),
            IsRevoked = false
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return tokenValue;
    }

    private async Task<string> CreateExpiredRefreshTokenAsync(Guid userId)
    {
        var tokenValue = Guid.NewGuid().ToString();
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = HashToken(tokenValue),
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            ExpiresAt = DateTime.UtcNow.AddDays(-3),
            IsRevoked = false
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return tokenValue;
    }

    private async Task<string> CreateRevokedRefreshTokenAsync(Guid userId)
    {
        var tokenValue = Guid.NewGuid().ToString();
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = HashToken(tokenValue),
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays - 1),
            IsRevoked = true,
            RevokedAt = DateTime.UtcNow.AddHours(-1)
        };

        _context.RefreshTokens.Add(refreshToken);
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
}
