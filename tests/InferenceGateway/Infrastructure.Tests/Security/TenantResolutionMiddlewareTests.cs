using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Synaxis.InferenceGateway.Application.ApiKeys;
using Synaxis.InferenceGateway.Application.ApiKeys.Models;
using Synaxis.InferenceGateway.Application.Interfaces;
using Synaxis.InferenceGateway.WebApi.Middleware;
using Xunit;

namespace Synaxis.InferenceGateway.Infrastructure.Tests.Security;

/// <summary>
/// Unit tests for TenantResolutionMiddleware.
/// Tests API key extraction, prefix-based lookup, JWT claim extraction, and error handling.
/// </summary>
public class TenantResolutionMiddlewareTests
{
    private readonly Mock<RequestDelegate> _mockNext;
    private readonly Mock<ILogger<TenantResolutionMiddleware>> _mockLogger;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<IApiKeyService> _mockApiKeyService;
    private readonly TenantResolutionMiddleware _middleware;
    private readonly DefaultHttpContext _httpContext;

    public TenantResolutionMiddlewareTests()
    {
        _mockNext = new Mock<RequestDelegate>();
        _mockLogger = new Mock<ILogger<TenantResolutionMiddleware>>();
        _mockTenantContext = new Mock<ITenantContext>();
        _mockApiKeyService = new Mock<IApiKeyService>();
        _middleware = new TenantResolutionMiddleware(_mockNext.Object, _mockLogger.Object);
        _httpContext = new DefaultHttpContext();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullNext_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new TenantResolutionMiddleware(null!, _mockLogger.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("next");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new TenantResolutionMiddleware(_mockNext.Object, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region API Key Authentication Tests

    [Fact]
    public async Task InvokeAsync_WithValidApiKey_ShouldAuthenticateSuccessfully()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var apiKeyId = Guid.NewGuid();
        var apiKey = "synaxis_build_testkey1234567890_abcdefghijklmnop";
        
        _httpContext.Request.Headers["Authorization"] = $"Bearer {apiKey}";

        _mockApiKeyService
            .Setup(s => s.ValidateApiKeyAsync(apiKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiKeyValidationResult
            {
                IsValid = true,
                OrganizationId = orgId,
                ApiKeyId = apiKeyId,
                Scopes = new[] { "read", "write" },
                RateLimitRpm = 100,
                RateLimitTpm = 10000
            });

        // Act
        await _middleware.InvokeAsync(_httpContext, _mockTenantContext.Object, _mockApiKeyService.Object);

        // Assert
        _mockTenantContext.Verify(t => t.SetApiKeyContext(
            orgId,
            apiKeyId,
            It.Is<string[]>(s => s.SequenceEqual(new[] { "read", "write" })),
            100,
            10000), Times.Once);

        _mockNext.Verify(n => n(_httpContext), Times.Once);
        _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task InvokeAsync_WithInvalidApiKey_ShouldReturn401()
    {
        // Arrange
        var apiKey = "synaxis_build_invalidkey";
        _httpContext.Request.Headers["Authorization"] = $"Bearer {apiKey}";

        _mockApiKeyService
            .Setup(s => s.ValidateApiKeyAsync(apiKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiKeyValidationResult
            {
                IsValid = false,
                ErrorMessage = "Invalid API key"
            });

        // Act
        await _middleware.InvokeAsync(_httpContext, _mockTenantContext.Object, _mockApiKeyService.Object);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        _mockNext.Verify(n => n(It.IsAny<HttpContext>()), Times.Never);
        _mockTenantContext.Verify(t => t.SetApiKeyContext(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<string[]>(),
            It.IsAny<int?>(),
            It.IsAny<int?>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithRevokedApiKey_ShouldReturn401()
    {
        // Arrange
        var apiKey = "synaxis_build_revokedkey";
        _httpContext.Request.Headers["Authorization"] = $"Bearer {apiKey}";

        _mockApiKeyService
            .Setup(s => s.ValidateApiKeyAsync(apiKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiKeyValidationResult
            {
                IsValid = false,
                ErrorMessage = "API key has been revoked"
            });

        // Act
        await _middleware.InvokeAsync(_httpContext, _mockTenantContext.Object, _mockApiKeyService.Object);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task InvokeAsync_WithApiKeyPrefix_ShouldExtractCorrectly()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var apiKeyId = Guid.NewGuid();
        var apiKey = "synaxis_build_testprefix1234567890_secretpart123456";
        
        _httpContext.Request.Headers["Authorization"] = $"Bearer {apiKey}";

        _mockApiKeyService
            .Setup(s => s.ValidateApiKeyAsync(apiKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiKeyValidationResult
            {
                IsValid = true,
                OrganizationId = orgId,
                ApiKeyId = apiKeyId,
                Scopes = Array.Empty<string>()
            });

        // Act
        await _middleware.InvokeAsync(_httpContext, _mockTenantContext.Object, _mockApiKeyService.Object);

        // Assert
        _mockApiKeyService.Verify(s => s.ValidateApiKeyAsync(
            apiKey, // Full key should be passed for validation
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region JWT Authentication Tests

    [Fact]
    public async Task InvokeAsync_WithValidJwt_ShouldAuthenticateSuccessfully()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var jwtToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U";
        
        _httpContext.Request.Headers["Authorization"] = $"Bearer {jwtToken}";

        // Set up authenticated user with claims
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("organization_id", orgId.ToString()),
            new Claim("scope", "read write admin")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _httpContext.User = new ClaimsPrincipal(identity);

        // Act
        await _middleware.InvokeAsync(_httpContext, _mockTenantContext.Object, _mockApiKeyService.Object);

        // Assert
        _mockTenantContext.Verify(t => t.SetJwtContext(
            orgId,
            userId,
            It.Is<string[]>(s => s.SequenceEqual(new[] { "read", "write", "admin" }))), Times.Once);

        _mockNext.Verify(n => n(_httpContext), Times.Once);
        _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task InvokeAsync_WithJwtMissingOrganizationId_ShouldReturn401()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var jwtToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U";
        
        _httpContext.Request.Headers["Authorization"] = $"Bearer {jwtToken}";

        // Set up authenticated user without organization_id claim
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _httpContext.User = new ClaimsPrincipal(identity);

        // Act
        await _middleware.InvokeAsync(_httpContext, _mockTenantContext.Object, _mockApiKeyService.Object);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        _mockNext.Verify(n => n(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithJwtMissingUserId_ShouldReturn401()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var jwtToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U";
        
        _httpContext.Request.Headers["Authorization"] = $"Bearer {jwtToken}";

        // Set up authenticated user without user_id claim
        var claims = new[]
        {
            new Claim("organization_id", orgId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _httpContext.User = new ClaimsPrincipal(identity);

        // Act
        await _middleware.InvokeAsync(_httpContext, _mockTenantContext.Object, _mockApiKeyService.Object);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        _mockNext.Verify(n => n(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithJwtInvalidOrganizationId_ShouldReturn401()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var jwtToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U";
        
        _httpContext.Request.Headers["Authorization"] = $"Bearer {jwtToken}";

        // Set up authenticated user with invalid organization_id
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("organization_id", "not-a-guid")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _httpContext.User = new ClaimsPrincipal(identity);

        // Act
        await _middleware.InvokeAsync(_httpContext, _mockTenantContext.Object, _mockApiKeyService.Object);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task InvokeAsync_WithJwtAlternativeClaimNames_ShouldAuthenticate()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var jwtToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U";
        
        _httpContext.Request.Headers["Authorization"] = $"Bearer {jwtToken}";

        // Use alternative claim names: "sub" and "org_id"
        var claims = new[]
        {
            new Claim("sub", userId.ToString()),
            new Claim("org_id", orgId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _httpContext.User = new ClaimsPrincipal(identity);

        // Act
        await _middleware.InvokeAsync(_httpContext, _mockTenantContext.Object, _mockApiKeyService.Object);

        // Assert
        _mockTenantContext.Verify(t => t.SetJwtContext(
            orgId,
            userId,
            It.IsAny<string[]>()), Times.Once);

        _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task InvokeAsync_WithJwtWithoutScopes_ShouldSetEmptyScopes()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var jwtToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U";
        
        _httpContext.Request.Headers["Authorization"] = $"Bearer {jwtToken}";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("organization_id", orgId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _httpContext.User = new ClaimsPrincipal(identity);

        // Act
        await _middleware.InvokeAsync(_httpContext, _mockTenantContext.Object, _mockApiKeyService.Object);

        // Assert
        _mockTenantContext.Verify(t => t.SetJwtContext(
            orgId,
            userId,
            It.Is<string[]>(s => s.Length == 0)), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithUnauthenticatedJwt_ShouldReturn401()
    {
        // Arrange
        var jwtToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U";
        
        _httpContext.Request.Headers["Authorization"] = $"Bearer {jwtToken}";

        // User is not authenticated
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        await _middleware.InvokeAsync(_httpContext, _mockTenantContext.Object, _mockApiKeyService.Object);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    #endregion

    #region Authorization Header Tests

    [Fact]
    public async Task InvokeAsync_WithNoAuthorizationHeader_ShouldCallNext()
    {
        // Arrange
        // No Authorization header set

        // Act
        await _middleware.InvokeAsync(_httpContext, _mockTenantContext.Object, _mockApiKeyService.Object);

        // Assert
        _mockNext.Verify(n => n(_httpContext), Times.Once);
        _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task InvokeAsync_WithEmptyAuthorizationHeader_ShouldCallNext()
    {
        // Arrange
        _httpContext.Request.Headers["Authorization"] = "";

        // Act
        await _middleware.InvokeAsync(_httpContext, _mockTenantContext.Object, _mockApiKeyService.Object);

        // Assert
        _mockNext.Verify(n => n(_httpContext), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithNonBearerScheme_ShouldReturn401()
    {
        // Arrange
        _httpContext.Request.Headers["Authorization"] = "Basic dXNlcjpwYXNz";

        // Act
        await _middleware.InvokeAsync(_httpContext, _mockTenantContext.Object, _mockApiKeyService.Object);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        _mockNext.Verify(n => n(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithBearerSchemeCaseInsensitive_ShouldParse()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var apiKeyId = Guid.NewGuid();
        var apiKey = "synaxis_build_testkey";
        
        _httpContext.Request.Headers["Authorization"] = $"bearer {apiKey}"; // lowercase

        _mockApiKeyService
            .Setup(s => s.ValidateApiKeyAsync(apiKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiKeyValidationResult
            {
                IsValid = true,
                OrganizationId = orgId,
                ApiKeyId = apiKeyId,
                Scopes = Array.Empty<string>()
            });

        // Act
        await _middleware.InvokeAsync(_httpContext, _mockTenantContext.Object, _mockApiKeyService.Object);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task InvokeAsync_WithBearerTokenWithExtraSpaces_ShouldTrim()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var apiKeyId = Guid.NewGuid();
        var apiKey = "synaxis_build_testkey";
        
        _httpContext.Request.Headers["Authorization"] = $"Bearer   {apiKey}  "; // extra spaces

        _mockApiKeyService
            .Setup(s => s.ValidateApiKeyAsync(apiKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiKeyValidationResult
            {
                IsValid = true,
                OrganizationId = orgId,
                ApiKeyId = apiKeyId,
                Scopes = Array.Empty<string>()
            });

        // Act
        await _middleware.InvokeAsync(_httpContext, _mockTenantContext.Object, _mockApiKeyService.Object);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task InvokeAsync_WhenApiKeyServiceThrows_ShouldReturn500()
    {
        // Arrange
        var apiKey = "synaxis_build_testkey";
        _httpContext.Request.Headers["Authorization"] = $"Bearer {apiKey}";

        _mockApiKeyService
            .Setup(s => s.ValidateApiKeyAsync(apiKey, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        await _middleware.InvokeAsync(_httpContext, _mockTenantContext.Object, _mockApiKeyService.Object);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        _mockNext.Verify(n => n(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WhenValidationReturnsNullOrganizationId_ShouldReturn500()
    {
        // Arrange
        var apiKey = "synaxis_build_testkey";
        _httpContext.Request.Headers["Authorization"] = $"Bearer {apiKey}";

        _mockApiKeyService
            .Setup(s => s.ValidateApiKeyAsync(apiKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiKeyValidationResult
            {
                IsValid = true,
                OrganizationId = null, // Missing required field
                ApiKeyId = Guid.NewGuid(),
                Scopes = Array.Empty<string>()
            });

        // Act
        await _middleware.InvokeAsync(_httpContext, _mockTenantContext.Object, _mockApiKeyService.Object);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task InvokeAsync_WhenValidationReturnsNullApiKeyId_ShouldReturn500()
    {
        // Arrange
        var apiKey = "synaxis_build_testkey";
        _httpContext.Request.Headers["Authorization"] = $"Bearer {apiKey}";

        _mockApiKeyService
            .Setup(s => s.ValidateApiKeyAsync(apiKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiKeyValidationResult
            {
                IsValid = true,
                OrganizationId = Guid.NewGuid(),
                ApiKeyId = null, // Missing required field
                Scopes = Array.Empty<string>()
            });

        // Act
        await _middleware.InvokeAsync(_httpContext, _mockTenantContext.Object, _mockApiKeyService.Object);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    #endregion
}
