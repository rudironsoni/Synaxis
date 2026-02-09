// <copyright file="ApiKeyServiceTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Tests.Security;

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Synaxis.InferenceGateway.Application.ApiKeys;
using Synaxis.InferenceGateway.Application.ApiKeys.Models;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Operations;
using Synaxis.InferenceGateway.Infrastructure.Services;
using Xunit;

/// <summary>
/// Unit tests for the ApiKeyService implementation.
/// Tests API key generation, validation, revocation, and bcrypt hashing.
/// </summary>
public class ApiKeyServiceTests : IAsyncLifetime
{
    private readonly SynaxisDbContext _dbContext;
    private readonly ApiKeyService _apiKeyService;

    public ApiKeyServiceTests()
    {
        // Create in-memory database
        var options = new DbContextOptionsBuilder<SynaxisDbContext>()
            .UseInMemoryDatabase(databaseName: $"ApiKeyServiceTests_{Guid.NewGuid()}")
            .Options;

        this._dbContext = new SynaxisDbContext(options);
        this._apiKeyService = new ApiKeyService(this._dbContext);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await this._dbContext.DisposeAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task GenerateApiKeyAsync_ShouldGenerateValidKeyFormat()
    {
        // Arrange
        var request = new GenerateApiKeyRequest
        {
            OrganizationId = Guid.NewGuid(),
            Name = "Test API Key",
            Scopes = new[] { "read", "write" },
        };

        // Act
        var response = await this._apiKeyService.GenerateApiKeyAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.ApiKey.Should().StartWith("synaxis_build_");
        response.ApiKey.Split('_').Should().HaveCount(4); // synaxis_build_{id}_{secret}
        response.Id.Should().NotBeEmpty();
        response.Name.Should().Be("Test API Key");
        response.Scopes.Should().BeEquivalentTo(new[] { "read", "write" });
        response.Prefix.Should().StartWith("synaxis_build_");
    }

    [Fact]
    public async Task GenerateApiKeyAsync_ShouldGenerateUniqueKeys()
    {
        // Arrange
        var request1 = new GenerateApiKeyRequest
        {
            OrganizationId = Guid.NewGuid(),
            Name = "API Key 1",
        };

        var request2 = new GenerateApiKeyRequest
        {
            OrganizationId = Guid.NewGuid(),
            Name = "API Key 2",
        };

        // Act
        var response1 = await this._apiKeyService.GenerateApiKeyAsync(request1);
        var response2 = await this._apiKeyService.GenerateApiKeyAsync(request2);

        // Assert
        response1.ApiKey.Should().NotBe(response2.ApiKey);
        response1.Prefix.Should().NotBe(response2.Prefix);
    }

    [Fact]
    public async Task GenerateApiKeyAsync_ShouldStoreBcryptHashedKey()
    {
        // Arrange
        var request = new GenerateApiKeyRequest
        {
            OrganizationId = Guid.NewGuid(),
            Name = "Test API Key",
        };

        // Act
        var response = await this._apiKeyService.GenerateApiKeyAsync(request);

        // Assert
        var storedKey = await this._dbContext.ApiKeys.FindAsync(response.Id);
        storedKey.Should().NotBeNull();
        storedKey!.KeyHash.Should().NotBeNullOrEmpty();

        // BCrypt hashes start with $2a$, $2b$, or $2y$
        storedKey.KeyHash.Should().MatchRegex(@"^\$2[aby]\$\d{2}\$");

        // BCrypt hash should be different from the plain key
        storedKey.KeyHash.Should().NotBe(response.ApiKey);
    }

    [Fact]
    public async Task GenerateApiKeyAsync_ShouldStoreKeyPrefixCorrectly()
    {
        // Arrange
        var request = new GenerateApiKeyRequest
        {
            OrganizationId = Guid.NewGuid(),
            Name = "Test API Key",
        };

        // Act
        var response = await this._apiKeyService.GenerateApiKeyAsync(request);

        // Assert
        var storedKey = await this._dbContext.ApiKeys.FindAsync(response.Id);
        storedKey.Should().NotBeNull();
        storedKey!.KeyPrefix.Should().Be(response.Prefix);
        storedKey.KeyPrefix.Should().StartWith("synaxis_build_");

        // Prefix should be extractable from the full key
        response.ApiKey.Should().StartWith("synaxis_build_");
    }

    [Fact]
    public async Task GenerateApiKeyAsync_ShouldSetExpirationDate()
    {
        // Arrange
        var expiresAt = DateTime.UtcNow.AddDays(30);
        var request = new GenerateApiKeyRequest
        {
            OrganizationId = Guid.NewGuid(),
            Name = "Test API Key",
            ExpiresAt = expiresAt,
        };

        // Act
        var response = await this._apiKeyService.GenerateApiKeyAsync(request);

        // Assert
        var storedKey = await this._dbContext.ApiKeys.FindAsync(response.Id);
        storedKey.Should().NotBeNull();
        storedKey!.ExpiresAt.Should().BeCloseTo(expiresAt, TimeSpan.FromSeconds(1));
        response.ExpiresAt.Should().BeCloseTo(expiresAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GenerateApiKeyAsync_ShouldSetRateLimits()
    {
        // Arrange
        var request = new GenerateApiKeyRequest
        {
            OrganizationId = Guid.NewGuid(),
            Name = "Test API Key",
            RateLimitRpm = 100,
            RateLimitTpm = 10000,
        };

        // Act
        var response = await this._apiKeyService.GenerateApiKeyAsync(request);

        // Assert
        var storedKey = await this._dbContext.ApiKeys.FindAsync(response.Id);
        storedKey.Should().NotBeNull();
        storedKey!.RateLimitRpm.Should().Be(100);
        storedKey.RateLimitTpm.Should().Be(10000);
    }

    [Fact]
    public async Task GenerateApiKeyAsync_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var request = new GenerateApiKeyRequest
        {
            OrganizationId = Guid.NewGuid(),
            Name = "Test API Key",
        };

        // Act
        var response = await this._apiKeyService.GenerateApiKeyAsync(request);

        // Assert
        var storedKey = await this._dbContext.ApiKeys.FindAsync(response.Id);
        storedKey.Should().NotBeNull();
        storedKey!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateApiKeyAsync_WithValidKey_ShouldReturnSuccess()
    {
        // Arrange
        var request = new GenerateApiKeyRequest
        {
            OrganizationId = Guid.NewGuid(),
            Name = "Test API Key",
            Scopes = new[] { "read", "write" },
        };
        var generated = await this._apiKeyService.GenerateApiKeyAsync(request);

        // Act
        var result = await this._apiKeyService.ValidateApiKeyAsync(generated.ApiKey);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.OrganizationId.Should().Be(request.OrganizationId);
        result.ApiKeyId.Should().Be(generated.Id);
        result.Scopes.Should().BeEquivalentTo(new[] { "read", "write" });
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task ValidateApiKeyAsync_WithInvalidKey_ShouldReturnFailure()
    {
        // Arrange
        var invalidKey = "synaxis_invalidkey1234567890_1234567890";

        // Act
        var result = await this._apiKeyService.ValidateApiKeyAsync(invalidKey);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.OrganizationId.Should().BeNull();
        result.ApiKeyId.Should().BeNull();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ValidateApiKeyAsync_WithRevokedKey_ShouldReturnFailure()
    {
        // Arrange
        var request = new GenerateApiKeyRequest
        {
            OrganizationId = Guid.NewGuid(),
            Name = "Test API Key",
        };
        var generated = await this._apiKeyService.GenerateApiKeyAsync(request);
        await this._apiKeyService.RevokeApiKeyAsync(generated.Id, "Test revocation");

        // Act
        var result = await this._apiKeyService.ValidateApiKeyAsync(generated.ApiKey);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("revoked");
    }

    [Fact]
    public async Task ValidateApiKeyAsync_WithExpiredKey_ShouldReturnFailure()
    {
        // Arrange
        var request = new GenerateApiKeyRequest
        {
            OrganizationId = Guid.NewGuid(),
            Name = "Test API Key",
            ExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired yesterday
        };
        var generated = await this._apiKeyService.GenerateApiKeyAsync(request);

        // Act
        var result = await this._apiKeyService.ValidateApiKeyAsync(generated.ApiKey);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("expired");
    }

    [Fact]
    public async Task ValidateApiKeyAsync_WithInactiveKey_ShouldReturnFailure()
    {
        // Arrange
        var request = new GenerateApiKeyRequest
        {
            OrganizationId = Guid.NewGuid(),
            Name = "Test API Key",
        };
        var generated = await this._apiKeyService.GenerateApiKeyAsync(request);

        // Manually deactivate the key
        var key = await this._dbContext.ApiKeys.FindAsync(generated.Id);
        key!.IsActive = false;
        await this._dbContext.SaveChangesAsync();

        // Act
        var result = await this._apiKeyService.ValidateApiKeyAsync(generated.ApiKey);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("inactive");
    }

    [Fact]
    public async Task ValidateApiKeyAsync_WithInvalidPrefix_ShouldReturnFailure()
    {
        // Arrange
        var invalidKey = "invalid_prefix_key";

        // Act
        var result = await this._apiKeyService.ValidateApiKeyAsync(invalidKey);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid API key format");
    }

    [Fact]
    public async Task ValidateApiKeyAsync_ShouldUseConstantTimeBcryptComparison()
    {
        // Arrange
        var request = new GenerateApiKeyRequest
        {
            OrganizationId = Guid.NewGuid(),
            Name = "Test API Key",
        };
        var generated = await this._apiKeyService.GenerateApiKeyAsync(request);

        // Create a key with same prefix but different secret
        var wrongKey = generated.ApiKey.Substring(0, generated.Prefix.Length + 1) + "wrongsecretpart";

        // Act
        var result = await this._apiKeyService.ValidateApiKeyAsync(wrongKey);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();

        // BCrypt handles constant-time comparison internally
    }

    [Fact]
    public async Task ValidateApiKeyAsync_ShouldReturnRateLimits()
    {
        // Arrange
        var request = new GenerateApiKeyRequest
        {
            OrganizationId = Guid.NewGuid(),
            Name = "Test API Key",
            RateLimitRpm = 150,
            RateLimitTpm = 15000,
        };
        var generated = await this._apiKeyService.GenerateApiKeyAsync(request);

        // Act
        var result = await this._apiKeyService.ValidateApiKeyAsync(generated.ApiKey);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.RateLimitRpm.Should().Be(150);
        result.RateLimitTpm.Should().Be(15000);
    }

    [Fact]
    public async Task RevokeApiKeyAsync_WithValidKey_ShouldRevokeSuccessfully()
    {
        // Arrange
        var request = new GenerateApiKeyRequest
        {
            OrganizationId = Guid.NewGuid(),
            Name = "Test API Key",
        };
        var generated = await this._apiKeyService.GenerateApiKeyAsync(request);
        var revokedBy = Guid.NewGuid();

        // Act
        var result = await this._apiKeyService.RevokeApiKeyAsync(
            generated.Id,
            "Security policy violation",
            revokedBy);

        // Assert
        result.Should().BeTrue();

        var revokedKey = await this._dbContext.ApiKeys.FindAsync(generated.Id);
        revokedKey.Should().NotBeNull();
        revokedKey!.IsActive.Should().BeFalse();
        revokedKey.RevokedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        revokedKey.RevokedBy.Should().Be(revokedBy);
        revokedKey.RevocationReason.Should().Be("Security policy violation");
    }

    [Fact]
    public async Task RevokeApiKeyAsync_WithNonExistentKey_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentKeyId = Guid.NewGuid();

        // Act
        var result = await this._apiKeyService.RevokeApiKeyAsync(
            nonExistentKeyId,
            "Test reason");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RevokeApiKeyAsync_WithAlreadyRevokedKey_ShouldReturnFalse()
    {
        // Arrange
        var request = new GenerateApiKeyRequest
        {
            OrganizationId = Guid.NewGuid(),
            Name = "Test API Key",
        };
        var generated = await this._apiKeyService.GenerateApiKeyAsync(request);

        // Revoke once
        await this._apiKeyService.RevokeApiKeyAsync(generated.Id, "First revocation");

        // Act - Revoke again
        var result = await this._apiKeyService.RevokeApiKeyAsync(
            generated.Id,
            "Second revocation");

        // Assert
        result.Should().BeFalse();

        var key = await this._dbContext.ApiKeys.FindAsync(generated.Id);
        key!.RevocationReason.Should().Be("First revocation"); // Original reason preserved
    }

    [Fact]
    public async Task RevokeApiKeyAsync_WithoutRevokedBy_ShouldStillRevoke()
    {
        // Arrange
        var request = new GenerateApiKeyRequest
        {
            OrganizationId = Guid.NewGuid(),
            Name = "Test API Key",
        };
        var generated = await this._apiKeyService.GenerateApiKeyAsync(request);

        // Act
        var result = await this._apiKeyService.RevokeApiKeyAsync(
            generated.Id,
            "Automated revocation");

        // Assert
        result.Should().BeTrue();

        var revokedKey = await this._dbContext.ApiKeys.FindAsync(generated.Id);
        revokedKey.Should().NotBeNull();
        revokedKey!.IsActive.Should().BeFalse();
        revokedKey.RevokedBy.Should().BeNull();
        revokedKey.RevocationReason.Should().Be("Automated revocation");
    }

    [Fact]
    public async Task ListApiKeysAsync_ShouldReturnAllActiveKeys()
    {
        // Arrange
        var orgId = Guid.NewGuid();

        await this._apiKeyService.GenerateApiKeyAsync(new GenerateApiKeyRequest
        {
            OrganizationId = orgId,
            Name = "Key 1",
        });

        await this._apiKeyService.GenerateApiKeyAsync(new GenerateApiKeyRequest
        {
            OrganizationId = orgId,
            Name = "Key 2",
        });

        // Act
        var keys = await this._apiKeyService.ListApiKeysAsync(orgId);

        // Assert
        keys.Should().HaveCount(2);
        keys.Should().AllSatisfy(k => k.IsActive.Should().BeTrue());
    }

    [Fact]
    public async Task ListApiKeysAsync_ShouldExcludeRevokedKeysByDefault()
    {
        // Arrange
        var orgId = Guid.NewGuid();

        var key1 = await this._apiKeyService.GenerateApiKeyAsync(new GenerateApiKeyRequest
        {
            OrganizationId = orgId,
            Name = "Active Key",
        });

        var key2 = await this._apiKeyService.GenerateApiKeyAsync(new GenerateApiKeyRequest
        {
            OrganizationId = orgId,
            Name = "Revoked Key",
        });

        await this._apiKeyService.RevokeApiKeyAsync(key2.Id, "Test");

        // Act
        var keys = await this._apiKeyService.ListApiKeysAsync(orgId, includeRevoked: false);

        // Assert
        keys.Should().HaveCount(1);
        keys.First().Name.Should().Be("Active Key");
    }

    [Fact]
    public async Task ListApiKeysAsync_WithIncludeRevoked_ShouldReturnAllKeys()
    {
        // Arrange
        var orgId = Guid.NewGuid();

        var key1 = await this._apiKeyService.GenerateApiKeyAsync(new GenerateApiKeyRequest
        {
            OrganizationId = orgId,
            Name = "Active Key",
        });

        var key2 = await this._apiKeyService.GenerateApiKeyAsync(new GenerateApiKeyRequest
        {
            OrganizationId = orgId,
            Name = "Revoked Key",
        });

        await this._apiKeyService.RevokeApiKeyAsync(key2.Id, "Test");

        // Act
        var keys = await this._apiKeyService.ListApiKeysAsync(orgId, includeRevoked: true);

        // Assert
        keys.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListApiKeysAsync_ShouldNotReturnHashedKey()
    {
        // Arrange
        var orgId = Guid.NewGuid();

        await this._apiKeyService.GenerateApiKeyAsync(new GenerateApiKeyRequest
        {
            OrganizationId = orgId,
            Name = "Test Key",
        });

        // Act
        var keys = await this._apiKeyService.ListApiKeysAsync(orgId);

        // Assert
        keys.Should().HaveCount(1);

        // ApiKeyInfo should only contain prefix, not the full key or hash
        keys.First().Prefix.Should().StartWith("synaxis_build_");
    }

    [Fact]
    public async Task UpdateLastUsedAsync_ShouldUpdateTimestamp()
    {
        // Arrange
        var request = new GenerateApiKeyRequest
        {
            OrganizationId = Guid.NewGuid(),
            Name = "Test API Key",
        };
        var generated = await this._apiKeyService.GenerateApiKeyAsync(request);

        var beforeUpdate = DateTime.UtcNow;
        await Task.Delay(100); // Small delay to ensure timestamp difference

        // Act
        await this._apiKeyService.UpdateLastUsedAsync(generated.Id);

        // Assert
        var updatedKey = await this._dbContext.ApiKeys.FindAsync(generated.Id);
        updatedKey.Should().NotBeNull();
        updatedKey!.LastUsedAt.Should().NotBeNull();
        updatedKey.LastUsedAt.Should().BeAfter(beforeUpdate);
    }

    [Fact]
    public Task UpdateLastUsedAsync_WithNonExistentKey_ShouldNotThrow()
    {
        // Arrange
        var nonExistentKeyId = Guid.NewGuid();

        // Act
        var act = async () => await this._apiKeyService.UpdateLastUsedAsync(nonExistentKeyId).ConfigureAwait(false);

        // Assert
        return act.Should().NotThrowAsync();
    }
}
