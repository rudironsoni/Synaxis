// <copyright file="ApiKeyTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.UnitTests.Aggregates;

using Synaxis.Abstractions.Time;
using Synaxis.Common.Tests.Time;
using Synaxis.Identity.Domain.Aggregates;
using Synaxis.Identity.Domain.ValueObjects;
using Xunit;
using FluentAssertions;

[Trait("Category", "Unit")]
public class ApiKeyTests
{
    [Fact]
    public void Create_ValidData_CreatesKey()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var keyId = KeyId.Create("test-key-id");
        var encryptedKey = "encrypted-key-value";
        var providerType = "TestProvider";
        var tenantId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();
        var expiresAt = DateTime.UtcNow.AddDays(30);
        var timeProvider = new TestTimeProvider();

        // Act
        var apiKey = ApiKey.Create(id, keyId, encryptedKey, providerType, tenantId, userId, expiresAt, timeProvider);

        // Assert
        apiKey.Id.Should().Be(id);
        apiKey.KeyId.Should().Be(keyId);
        apiKey.EncryptedKey.Should().Be(encryptedKey);
        apiKey.ProviderType.Should().Be(providerType);
        apiKey.TenantId.Should().Be(tenantId);
        apiKey.UserId.Should().Be(userId);
        apiKey.IsActive.Should().BeTrue();
        apiKey.CreatedAt.Should().Be(timeProvider.UtcNow);
        apiKey.ExpiresAt.Should().BeCloseTo(expiresAt, TimeSpan.FromSeconds(1));
        apiKey.LastUsedAt.Should().BeNull();
    }

    [Fact]
    public void Create_WithoutExpiration_CreatesKey()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var keyId = KeyId.Create("test-key-id");
        var encryptedKey = "encrypted-key-value";
        var providerType = "TestProvider";
        var tenantId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();
        var timeProvider = new TestTimeProvider();

        // Act
        var apiKey = ApiKey.Create(id, keyId, encryptedKey, providerType, tenantId, userId, null, timeProvider);

        // Assert
        apiKey.ExpiresAt.Should().BeNull();
    }

    [Fact]
    public void Rotate_GeneratesNewKeyId()
    {
        // Arrange
        var apiKey = CreateTestApiKey();
        var newKeyId = KeyId.Create("new-key-id");
        var newEncryptedKey = "new-encrypted-key";

        // Act
        apiKey.Rotate(newKeyId, newEncryptedKey);

        // Assert
        apiKey.KeyId.Should().Be(newKeyId);
        apiKey.EncryptedKey.Should().Be(newEncryptedKey);
    }

    [Fact]
    public void Rotate_InactiveKey_ThrowsException()
    {
        // Arrange
        var apiKey = CreateTestApiKey();
        apiKey.Revoke();
        var newKeyId = KeyId.Create("new-key-id");
        var newEncryptedKey = "new-encrypted-key";

        // Act
        var act = () => apiKey.Rotate(newKeyId, newEncryptedKey);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot rotate an inactive API key.");
    }

    [Fact]
    public void Revoke_SetsIsActiveToFalse()
    {
        // Arrange
        var apiKey = CreateTestApiKey();

        // Act
        apiKey.Revoke();

        // Assert
        apiKey.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Revoke_AlreadyRevoked_ThrowsException()
    {
        // Arrange
        var apiKey = CreateTestApiKey();
        apiKey.Revoke();

        // Act
        var act = () => apiKey.Revoke();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("API key is already revoked.");
    }

    [Fact]
    public void MarkAsUsed_UpdatesLastUsedAt()
    {
        // Arrange
        var timeProvider = new TestTimeProvider();
        var apiKey = CreateTestApiKey(timeProvider: timeProvider);

        // Act
        apiKey.MarkAsUsed();

        // Assert
        apiKey.LastUsedAt.Should().Be(timeProvider.UtcNow);
    }

    [Fact]
    public void MarkAsUsed_InactiveKey_ThrowsException()
    {
        // Arrange
        var apiKey = CreateTestApiKey();
        apiKey.Revoke();

        // Act
        var act = () => apiKey.MarkAsUsed();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot mark an inactive API key as used.");
    }

    [Fact]
    public void MarkAsUsed_ExpiredKey_ThrowsException()
    {
        // Arrange
        var timeProvider = new TestTimeProvider();
        var pastTime = timeProvider.UtcNow.AddHours(-1);
        var apiKey = CreateTestApiKey(pastTime, timeProvider);

        // Act
        var act = () => apiKey.MarkAsUsed();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot mark an expired API key as used.");
    }

    [Fact]
    public void IsExpired_WithExpiration_ReturnsTrue()
    {
        // Arrange
        var timeProvider = new TestTimeProvider();
        var pastTime = timeProvider.UtcNow.AddHours(-1);
        var apiKey = CreateTestApiKey(pastTime, timeProvider);

        // Act
        var result = apiKey.IsExpired();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WithoutExpiration_ReturnsFalse()
    {
        // Arrange
        var timeProvider = new TestTimeProvider();
        var apiKey = CreateTestApiKey(null, timeProvider);

        // Act
        var result = apiKey.IsExpired();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_NotExpired_ReturnsFalse()
    {
        // Arrange
        var timeProvider = new TestTimeProvider();
        var futureTime = timeProvider.UtcNow.AddDays(30);
        var apiKey = CreateTestApiKey(futureTime, timeProvider);

        // Act
        var result = apiKey.IsExpired();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValid_ActiveAndNotExpired_ReturnsTrue()
    {
        // Arrange
        var timeProvider = new TestTimeProvider();
        var futureTime = timeProvider.UtcNow.AddDays(30);
        var apiKey = CreateTestApiKey(futureTime, timeProvider);

        // Act
        var result = apiKey.IsValid();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValid_Inactive_ReturnsFalse()
    {
        // Arrange
        var timeProvider = new TestTimeProvider();
        var futureTime = timeProvider.UtcNow.AddDays(30);
        var apiKey = CreateTestApiKey(futureTime, timeProvider);
        apiKey.Revoke();

        // Act
        var result = apiKey.IsValid();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValid_Expired_ReturnsFalse()
    {
        // Arrange
        var timeProvider = new TestTimeProvider();
        var pastTime = timeProvider.UtcNow.AddHours(-1);
        var apiKey = CreateTestApiKey(pastTime, timeProvider);

        // Act
        var result = apiKey.IsValid();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Delete_SetsIsActiveToFalse()
    {
        // Arrange
        var apiKey = CreateTestApiKey();

        // Act
        apiKey.Delete();

        // Assert
        apiKey.IsActive.Should().BeFalse();
    }

    private static ApiKey CreateTestApiKey(DateTime? expiresAt = null, ITimeProvider? timeProvider = null)
    {
        timeProvider ??= new TestTimeProvider();
        return ApiKey.Create(
            Guid.NewGuid().ToString(),
            KeyId.Create("test-key-id"),
            "encrypted-key-value",
            "TestProvider",
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            expiresAt ?? timeProvider.UtcNow.AddDays(30),
            timeProvider);
    }
}
