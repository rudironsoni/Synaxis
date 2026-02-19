// <copyright file="KeyVaultIntegrationTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.IntegrationTests;

using System;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Synaxis.Abstractions.Cloud;
using Synaxis.Providers.OnPrem;
using Xunit;

/// <summary>
/// Integration tests for KeyVault using Redis with TestContainers.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "Infrastructure")]
public sealed class KeyVaultIntegrationTests : IClassFixture<RedisTestFixture>, IAsyncLifetime
{
    private readonly RedisTestFixture _fixture;
    private RedisKeyVault? _keyVault;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyVaultIntegrationTests"/> class.
    /// </summary>
    /// <param name="fixture">The Redis fixture.</param>
    public KeyVaultIntegrationTests(RedisTestFixture fixture)
    {
        _fixture = fixture;
    }

    /// <inheritdoc />
    public Task InitializeAsync()
    {
        var logger = _fixture.LoggerFactory.CreateLogger<RedisKeyVault>();
        _keyVault = new RedisKeyVault(_fixture.ConnectionString, logger);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        await _fixture.ClearDataAsync();
    }

    [Fact]
    public async Task SetSecretAsync_StoresSecret()
    {
        // Arrange
        var secretName = "test-secret";
        var secretValue = "my-secret-value";

        // Act
        await _keyVault!.SetSecretAsync(secretName, secretValue);
        var retrievedValue = await _keyVault.GetSecretAsync(secretName);

        // Assert
        retrievedValue.Should().Be(secretValue);
    }

    [Fact]
    public async Task GetSecretAsync_RetrievesStoredSecret()
    {
        // Arrange
        var secretName = "retrieve-secret";
        var secretValue = "stored-value";
        await _keyVault!.SetSecretAsync(secretName, secretValue);

        // Act
        var retrievedValue = await _keyVault.GetSecretAsync(secretName);

        // Assert
        retrievedValue.Should().Be(secretValue);
    }

    [Fact]
    public async Task GetSecretAsync_ReturnsNullForMissingSecret()
    {
        // Arrange
        var secretName = "non-existent-secret";

        // Act
        var retrievedValue = await _keyVault!.GetSecretAsync(secretName);

        // Assert
        retrievedValue.Should().BeNull();
    }

    [Fact]
    public async Task DeleteSecretAsync_RemovesSecret()
    {
        // Arrange
        var secretName = "delete-secret";
        var secretValue = "value-to-delete";
        await _keyVault!.SetSecretAsync(secretName, secretValue);

        // Act
        await _keyVault.DeleteSecretAsync(secretName);
        var retrievedValue = await _keyVault.GetSecretAsync(secretName);

        // Assert
        retrievedValue.Should().BeNull();
    }

    [Fact]
    public async Task SetSecretAsync_UpdatesExistingSecret()
    {
        // Arrange
        var secretName = "update-secret";
        var initialValue = "initial-value";
        var updatedValue = "updated-value";

        await _keyVault!.SetSecretAsync(secretName, initialValue);

        // Act
        await _keyVault.SetSecretAsync(secretName, updatedValue);
        var retrievedValue = await _keyVault.GetSecretAsync(secretName);

        // Assert
        retrievedValue.Should().Be(updatedValue);
    }

    [Fact]
    public async Task SetSecretAsync_HandlesSpecialCharacters()
    {
        // Arrange
        var secretName = "special-chars-secret";
        var secretValue = "Special: !@#$%^&*()_+-=[]{}|;':\",./<>?";

        // Act
        await _keyVault!.SetSecretAsync(secretName, secretValue);
        var retrievedValue = await _keyVault.GetSecretAsync(secretName);

        // Assert
        retrievedValue.Should().Be(secretValue);
    }

    [Fact]
    public async Task SetSecretAsync_HandlesUnicodeCharacters()
    {
        // Arrange
        var secretName = "unicode-secret";
        var secretValue = "Unicode: 你好世界 🌍 Привет мир";

        // Act
        await _keyVault!.SetSecretAsync(secretName, secretValue);
        var retrievedValue = await _keyVault.GetSecretAsync(secretName);

        // Assert
        retrievedValue.Should().Be(secretValue);
    }

    [Fact]
    public async Task SetSecretAsync_HandlesLargeSecrets()
    {
        // Arrange
        var secretName = "large-secret";
        var largeValue = new string('A', 10000);

        // Act
        await _keyVault!.SetSecretAsync(secretName, largeValue);
        var retrievedValue = await _keyVault.GetSecretAsync(secretName);

        // Assert
        retrievedValue.Should().Be(largeValue);
        retrievedValue!.Length.Should().Be(10000);
    }

    [Fact]
    public async Task EncryptAsync_ReturnsData()
    {
        // Arrange
        var keyName = "encryption-key";
        var plaintext = Encoding.UTF8.GetBytes("Data to encrypt");

        // Act
        var encrypted = await _keyVault!.EncryptAsync(keyName, plaintext);

        // Assert
        encrypted.Should().NotBeNull();
    }

    [Fact]
    public async Task DecryptAsync_ReturnsData()
    {
        // Arrange
        var keyName = "decryption-key";
        var ciphertext = Encoding.UTF8.GetBytes("Encrypted data");

        // Act
        var decrypted = await _keyVault!.DecryptAsync(keyName, ciphertext);

        // Assert
        decrypted.Should().NotBeNull();
    }

    [Fact]
    public async Task EncryptAsync_HandlesEmptyData()
    {
        // Arrange
        var keyName = "empty-key";
        var plaintext = Array.Empty<byte>();

        // Act
        var encrypted = await _keyVault!.EncryptAsync(keyName, plaintext);

        // Assert
        encrypted.Should().BeEmpty();
    }

    [Fact]
    public async Task EncryptAsync_HandlesLargeData()
    {
        // Arrange
        var keyName = "large-data-key";
        var largeData = new byte[1024 * 1024]; // 1MB
        new Random().NextBytes(largeData);

        // Act
        var encrypted = await _keyVault!.EncryptAsync(keyName, largeData);

        // Assert
        encrypted.Should().NotBeNull();
    }

    [Fact]
    public async Task SetSecretAsync_ManagesMultipleSecrets()
    {
        // Arrange
        var secrets = new (string Name, string Value)[]
        {
            ("secret-1", "value-1"),
            ("secret-2", "value-2"),
            ("secret-3", "value-3")
        };

        // Act
        foreach (var (name, value) in secrets)
        {
            await _keyVault!.SetSecretAsync(name, value);
        }

        // Assert
        foreach (var (name, expectedValue) in secrets)
        {
            var retrievedValue = await _keyVault!.GetSecretAsync(name);
            retrievedValue.Should().Be(expectedValue);
        }
    }

    [Fact]
    public async Task DeleteSecretAsync_IsIdempotent()
    {
        // Arrange
        var secretName = "idempotent-delete";

        // Act
        await _keyVault!.DeleteSecretAsync(secretName);
        await _keyVault.DeleteSecretAsync(secretName);

        // Assert
        var retrievedValue = await _keyVault.GetSecretAsync(secretName);
        retrievedValue.Should().BeNull();
    }

    [Fact]
    public async Task GetSecretAsync_DifferentSecretsAreIndependent()
    {
        // Arrange
        var secretName1 = "secret-a";
        var secretName2 = "secret-b";
        var value1 = "value-a";
        var value2 = "value-b";

        // Act
        await _keyVault!.SetSecretAsync(secretName1, value1);
        await _keyVault.SetSecretAsync(secretName2, value2);

        var retrieved1 = await _keyVault.GetSecretAsync(secretName1);
        var retrieved2 = await _keyVault.GetSecretAsync(secretName2);

        // Assert
        retrieved1.Should().Be(value1);
        retrieved2.Should().Be(value2);
    }

    [Fact]
    public async Task SetSecretAsync_OverwritesWithEmptyValue()
    {
        // Arrange
        var secretName = "empty-overwrite";
        await _keyVault!.SetSecretAsync(secretName, "initial");

        // Act
        await _keyVault.SetSecretAsync(secretName, string.Empty);
        var retrievedValue = await _keyVault.GetSecretAsync(secretName);

        // Assert
        retrievedValue.Should().Be(string.Empty);
    }

    [Fact]
    public async Task MultiTenant_Isolation()
    {
        // Arrange
        var tenantId1 = "tenant-1";
        var tenantId2 = "tenant-2";
        var secretName = "shared-secret-name";

        var tenantSecretName1 = RedisKeyVault.GetTenantSecretName(tenantId1, secretName);
        var tenantSecretName2 = RedisKeyVault.GetTenantSecretName(tenantId2, secretName);

        // Act
        await _keyVault!.SetSecretAsync(tenantSecretName1, "tenant-1-value");
        await _keyVault.SetSecretAsync(tenantSecretName2, "tenant-2-value");

        var value1 = await _keyVault.GetSecretAsync(tenantSecretName1);
        var value2 = await _keyVault.GetSecretAsync(tenantSecretName2);

        // Assert
        value1.Should().Be("tenant-1-value");
        value2.Should().Be("tenant-2-value");
        value1.Should().NotBe(value2);
    }
}
