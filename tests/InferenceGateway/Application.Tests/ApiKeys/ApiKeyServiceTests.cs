using Microsoft.EntityFrameworkCore;
using Synaxis.InferenceGateway.Application.ApiKeys;
using Synaxis.InferenceGateway.Application.ApiKeys.Models;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Identity;
using Xunit;

namespace Synaxis.InferenceGateway.Application.Tests.ApiKeys;

/// <summary>
/// Unit tests for ApiKeyService.
/// </summary>
public class ApiKeyServiceTests : IDisposable
{
    private readonly SynaxisDbContext _context;
    private readonly ApiKeyService _service;
    private readonly Guid _organizationId;

    public ApiKeyServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<SynaxisDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SynaxisDbContext(options);
        _service = new ApiKeyService(_context);

        // Setup test organization
        _organizationId = Guid.NewGuid();
        var organization = new Organization
        {
            Id = _organizationId,
            LegalName = "Test Org",
            DisplayName = "Test Org",
            Slug = "test-org",
            Status = "Active",
            PlanTier = "Free"
        };

        _context.Organizations.Add(organization);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GenerateApiKeyAsync_CreatesKeyWithCorrectFormat()
    {
        // Arrange
        var request = new GenerateApiKeyRequest
        {
            OrganizationId = _organizationId,
            Name = "Test API Key",
            Scopes = new[] { "read", "write" }
        };

        // Act
        var result = await _service.GenerateApiKeyAsync(request);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.NotNull(result.ApiKey);
        Assert.StartsWith("synaxis_build_", result.ApiKey);
        Assert.Contains("_", result.ApiKey.Substring("synaxis_build_".Length));
        Assert.Equal("Test API Key", result.Name);
        Assert.Equal(2, result.Scopes.Length);
        Assert.Contains("read", result.Scopes);
        Assert.Contains("write", result.Scopes);
    }

    [Fact]
    public async Task GenerateApiKeyAsync_StoresHashNotPlaintext()
    {
        // Arrange
        var request = new GenerateApiKeyRequest
        {
            OrganizationId = _organizationId,
            Name = "Test API Key",
            Scopes = Array.Empty<string>()
        };

        // Act
        var result = await _service.GenerateApiKeyAsync(request);

        // Assert
        var storedKey = await _context.ApiKeys.FindAsync(result.Id);
        Assert.NotNull(storedKey);
        Assert.NotEqual(result.ApiKey, storedKey.KeyHash);
        Assert.DoesNotContain(result.ApiKey, storedKey.KeyHash);
    }

    [Fact]
    public async Task ValidateApiKeyAsync_WithValidKey_ReturnsSuccess()
    {
        // Arrange
        var request = new GenerateApiKeyRequest
        {
            OrganizationId = _organizationId,
            Name = "Test API Key",
            Scopes = new[] { "read" }
        };

        var generated = await _service.GenerateApiKeyAsync(request);

        // Act
        var result = await _service.ValidateApiKeyAsync(generated.ApiKey);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(_organizationId, result.OrganizationId);
        Assert.Equal(generated.Id, result.ApiKeyId);
        Assert.Single(result.Scopes);
        Assert.Contains("read", result.Scopes);
    }

    [Fact]
    public async Task ValidateApiKeyAsync_WithInvalidKey_ReturnsFailure()
    {
        // Arrange
        var invalidKey = "synaxis_build_invalid_key";

        // Act
        var result = await _service.ValidateApiKeyAsync(invalidKey);

        // Assert
        Assert.False(result.IsValid);
        Assert.Null(result.OrganizationId);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateApiKeyAsync_WithRevokedKey_ReturnsFailure()
    {
        // Arrange
        var request = new GenerateApiKeyRequest
        {
            OrganizationId = _organizationId,
            Name = "Test API Key",
            Scopes = Array.Empty<string>()
        };

        var generated = await _service.GenerateApiKeyAsync(request);
        await _service.RevokeApiKeyAsync(generated.Id, "Test revocation");

        // Act
        var result = await _service.ValidateApiKeyAsync(generated.ApiKey);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("revoked", result.ErrorMessage?.ToLower() ?? "");
    }

    [Fact]
    public async Task RevokeApiKeyAsync_MarksKeyAsRevoked()
    {
        // Arrange
        var request = new GenerateApiKeyRequest
        {
            OrganizationId = _organizationId,
            Name = "Test API Key",
            Scopes = Array.Empty<string>()
        };

        var generated = await _service.GenerateApiKeyAsync(request);

        // Act
        var result = await _service.RevokeApiKeyAsync(generated.Id, "Test reason");

        // Assert
        Assert.True(result);

        var storedKey = await _context.ApiKeys.FindAsync(generated.Id);
        Assert.NotNull(storedKey);
        Assert.False(storedKey.IsActive);
        Assert.NotNull(storedKey.RevokedAt);
        Assert.Equal("Test reason", storedKey.RevocationReason);
    }

    [Fact]
    public async Task ListApiKeysAsync_ReturnsOnlyActiveKeysByDefault()
    {
        // Arrange
        var request1 = new GenerateApiKeyRequest
        {
            OrganizationId = _organizationId,
            Name = "Active Key",
            Scopes = Array.Empty<string>()
        };

        var request2 = new GenerateApiKeyRequest
        {
            OrganizationId = _organizationId,
            Name = "Revoked Key",
            Scopes = Array.Empty<string>()
        };

        var key1 = await _service.GenerateApiKeyAsync(request1);
        var key2 = await _service.GenerateApiKeyAsync(request2);
        await _service.RevokeApiKeyAsync(key2.Id, "Test");

        // Act
        var result = await _service.ListApiKeysAsync(_organizationId, includeRevoked: false);

        // Assert
        Assert.Single(result);
        Assert.Equal("Active Key", result.First().Name);
    }

    [Fact]
    public async Task ListApiKeysAsync_WithIncludeRevoked_ReturnsAllKeys()
    {
        // Arrange
        var request1 = new GenerateApiKeyRequest
        {
            OrganizationId = _organizationId,
            Name = "Active Key",
            Scopes = Array.Empty<string>()
        };

        var request2 = new GenerateApiKeyRequest
        {
            OrganizationId = _organizationId,
            Name = "Revoked Key",
            Scopes = Array.Empty<string>()
        };

        var key1 = await _service.GenerateApiKeyAsync(request1);
        var key2 = await _service.GenerateApiKeyAsync(request2);
        await _service.RevokeApiKeyAsync(key2.Id, "Test");

        // Act
        var result = await _service.ListApiKeysAsync(_organizationId, includeRevoked: true);

        // Assert
        Assert.Equal(2, result.Count);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
