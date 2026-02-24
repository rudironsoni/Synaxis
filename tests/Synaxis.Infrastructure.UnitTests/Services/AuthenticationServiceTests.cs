// <copyright file="AuthenticationServiceTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Tests.Services;

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Synaxis.Core.Contracts;
using Synaxis.Core.Models;
using Synaxis.Infrastructure.Data;
using Synaxis.Infrastructure.Services;
using Xunit;

/// <summary>
/// Tests for the AuthenticationService.
/// </summary>
public sealed class AuthenticationServiceTests : IDisposable
{
    private readonly SynaxisDbContext _context;
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<ILogger<AuthenticationService>> _loggerMock;
    private readonly JwtOptions _jwtOptions;
    private readonly AuthenticationService _authenticationService;

    public AuthenticationServiceTests()
    {
        var options = new DbContextOptionsBuilder<SynaxisDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SynaxisDbContext(options);
        _userServiceMock = new Mock<IUserService>();
        _loggerMock = new Mock<ILogger<AuthenticationService>>();
        _jwtOptions = new JwtOptions
        {
            SecretKey = "SuperSecretKeyForDevelopmentPurposesOnly123456",
            Issuer = "Synaxis",
            Audience = "SynaxisAPI",
            AccessTokenExpirationMinutes = 60,
            RefreshTokenExpirationDays = 7
        };

        var jwtOptionsMock = new Mock<IOptions<JwtOptions>>();
        jwtOptionsMock.Setup(x => x.Value).Returns(_jwtOptions);

        _authenticationService = new AuthenticationService(
            _context,
            _userServiceMock.Object,
            jwtOptionsMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task AuthenticateAsync_WithValidCredentials_ReturnsSuccess()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hashedpassword",
            MfaEnabled = false,
            IsActive = true
        };

        _userServiceMock
            .Setup(x => x.AuthenticateAsync("test@example.com", "password"))
            .ReturnsAsync(user);

        // Act
        var result = await _authenticationService.AuthenticateAsync("test@example.com", "password");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.User.Should().NotBeNull();
        result.User.Email.Should().Be("test@example.com");
        result.RequiresMfa.Should().BeFalse();
    }

    [Fact]
    public async Task AuthenticateAsync_WithMfaEnabled_ReturnsRequiresMfa()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hashedpassword",
            MfaEnabled = true,
            IsActive = true
        };

        _userServiceMock
            .Setup(x => x.AuthenticateAsync("test@example.com", "password"))
            .ReturnsAsync(user);

        // Act
        var result = await _authenticationService.AuthenticateAsync("test@example.com", "password");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.RequiresMfa.Should().BeTrue();
        result.User.Should().NotBeNull();
        result.ErrorMessage.Should().Be("MFA code required");
    }

    [Fact]
    public async Task AuthenticateAsync_WithInvalidCredentials_ReturnsFailure()
    {
        // Arrange
        _userServiceMock
            .Setup(x => x.AuthenticateAsync("test@example.com", "wrongpassword"))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid email or password"));

        // Act
        var result = await _authenticationService.AuthenticateAsync("test@example.com", "wrongpassword");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid email or password");
    }

    [Fact]
    public async Task AuthenticateAsync_WithNullEmail_ReturnsFailure()
    {
        // Act
        var result = await _authenticationService.AuthenticateAsync(null!, "password");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Email is required");
    }

    [Fact]
    public async Task AuthenticateAsync_WithNullPassword_ReturnsFailure()
    {
        // Act
        var result = await _authenticationService.AuthenticateAsync("test@example.com", null!);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Password is required");
    }

    [Fact]
    public async Task ValidateToken_WithValidToken_ReturnsTrue()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hashedpassword",
            MfaEnabled = false,
            IsActive = true
        };

        _userServiceMock
            .Setup(x => x.AuthenticateAsync("test@example.com", "password"))
            .ReturnsAsync(user);

        var authResult = await _authenticationService.AuthenticateAsync("test@example.com", "password");
        var token = authResult.AccessToken!;

        // Act
        var isValid = await _authenticationService.ValidateTokenAsync(token);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateToken_WithInvalidToken_ReturnsFalse()
    {
        // Act
        var isValid = await _authenticationService.ValidateTokenAsync("invalid.token.here");

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task GetUserIdFromToken_WithValidToken_ReturnsUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hashedpassword",
            MfaEnabled = false,
            IsActive = true
        };

        _userServiceMock
            .Setup(x => x.AuthenticateAsync("test@example.com", "password"))
            .ReturnsAsync(user);

        var authResult = await _authenticationService.AuthenticateAsync("test@example.com", "password");
        var token = authResult.AccessToken!;

        // Debug: Read the token and check all claims
        var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jsonToken = tokenHandler.ReadJwtToken(token);

        // Print all claims to debug
        var claimsList = jsonToken.Claims.ToList();
        claimsList.Should().NotBeEmpty("Token should have claims");

        // Find the claim with the user ID and check its type
        var userIdClaim = claimsList.FirstOrDefault(c => c.Value == userId.ToString());
        userIdClaim.Should().NotBeNull($"Token should have a claim with value {userId}");

        // Check if the claim type is "nameid" (JWT short form) or the full URI
        userIdClaim.Type.Should().BeOneOf("nameid", System.Security.Claims.ClaimTypes.NameIdentifier, "Claim type should be 'nameid' or the full URI");

        // Act
        var extractedUserId = _authenticationService.GetUserIdFromToken(token);

        // Assert
        extractedUserId.Should().NotBeNull();
        extractedUserId.Should().Be(userId);
    }

    [Fact]
    public void GetUserIdFromToken_WithInvalidToken_ReturnsNull()
    {
        // Act
        var userId = _authenticationService.GetUserIdFromToken("invalid.token.here");

        // Assert
        userId.Should().BeNull();
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
