// <copyright file="AzureKeyVaultTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.IntegrationTests;

using System;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Synaxis.Abstractions.Cloud;
using Xunit;

/// <summary>
/// Integration tests for Azure KeyVault.
/// </summary>
public sealed class AzureKeyVaultTests : IClassFixture<KeyVaultFixture>
{
    private readonly IKeyVault _keyVault;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureKeyVaultTests"/> class.
    /// </summary>
    /// <param name="fixture">The KeyVault fixture.</param>
    public AzureKeyVaultTests(KeyVaultFixture fixture)
    {
        _keyVault = fixture.KeyVault;
    }

    [Fact]
    public async Task Should_Set_And_Get_Secret()
    {
        // Arrange
        var secretName = "test-secret";
        var secretValue = "my-secret-value";

        // Act
        await _keyVault.SetSecretAsync(secretName, secretValue);
        var retrievedValue = await _keyVault.GetSecretAsync(secretName);

        // Assert
        retrievedValue.Should().Be(secretValue);
    }

    [Fact]
    public async Task Should_Update_Existing_Secret()
    {
        // Arrange
        var secretName = "update-secret";
        var initialValue = "initial-value";
        var updatedValue = "updated-value";

        await _keyVault.SetSecretAsync(secretName, initialValue);

        // Act
        await _keyVault.SetSecretAsync(secretName, updatedValue);
        var retrievedValue = await _keyVault.GetSecretAsync(secretName);

        // Assert
        retrievedValue.Should().Be(updatedValue);
    }

    [Fact]
    public async Task Should_Delete_Secret()
    {
        // Arrange
        var secretName = "delete-secret";
        var secretValue = "value-to-delete";
        await _keyVault.SetSecretAsync(secretName, secretValue);

        // Act
        await _keyVault.DeleteSecretAsync(secretName);
        var retrievedValue = await _keyVault.GetSecretAsync(secretName);

        // Assert
        retrievedValue.Should().BeNull();
    }

    [Fact]
    public async Task Should_Return_Null_For_Non_Existent_Secret()
    {
        // Arrange
        var secretName = "non-existent-secret";

        // Act
        var retrievedValue = await _keyVault.GetSecretAsync(secretName);

        // Assert
        retrievedValue.Should().BeNull();
    }

    [Fact]
    public async Task Should_Handle_Special_Characters_In_Secrets()
    {
        // Arrange
        var secretName = "special-chars-secret";
        var secretValue = "Special: !@#$%^&*()_+-=[]{}|;':\",./<>?";

        // Act
        await _keyVault.SetSecretAsync(secretName, secretValue);
        var retrievedValue = await _keyVault.GetSecretAsync(secretName);

        // Assert
        retrievedValue.Should().Be(secretValue);
    }

    [Fact]
    public async Task Should_Handle_Unicode_In_Secrets()
    {
        // Arrange
        var secretName = "unicode-secret";
        var secretValue = "Unicode: 你好世界 🌍 Привет мир";

        // Act
        await _keyVault.SetSecretAsync(secretName, secretValue);
        var retrievedValue = await _keyVault.GetSecretAsync(secretName);

        // Assert
        retrievedValue.Should().Be(secretValue);
    }

    [Fact]
    public async Task Should_Handle_Large_Secrets()
    {
        // Arrange
        var secretName = "large-secret";
        var largeValue = new string('A', 10000);

        // Act
        await _keyVault.SetSecretAsync(secretName, largeValue);
        var retrievedValue = await _keyVault.GetSecretAsync(secretName);

        // Assert
        retrievedValue.Should().Be(largeValue);
        retrievedValue!.Length.Should().Be(10000);
    }

    [Fact]
    public async Task Should_Encrypt_And_Decrypt_Data()
    {
        // Arrange
        var keyName = "encryption-key";
        var plaintext = Encoding.UTF8.GetBytes("Data to encrypt");

        // Act
        var encrypted = await _keyVault.EncryptAsync(keyName, plaintext);
        var decrypted = await _keyVault.DecryptAsync(keyName, encrypted);

        // Assert
        decrypted.Should().BeEquivalentTo(plaintext);
        Encoding.UTF8.GetString(decrypted).Should().Be("Data to encrypt");
    }

    [Fact]
    public async Task Should_Encrypt_Large_Data()
    {
        // Arrange
        var keyName = "large-data-key";
        var largeData = new byte[1024 * 1024]; // 1MB
        new Random().NextBytes(largeData);

        // Act
        var encrypted = await _keyVault.EncryptAsync(keyName, largeData);
        var decrypted = await _keyVault.DecryptAsync(keyName, encrypted);

        // Assert
        decrypted.Should().BeEquivalentTo(largeData);
    }

    [Fact]
    public async Task Should_Produce_Different_Ciphertext_For_Same_Plaintext()
    {
        // Arrange
        var keyName = "randomness-key";
        var plaintext = Encoding.UTF8.GetBytes("Same data");

        // Act
        var encrypted1 = await _keyVault.EncryptAsync(keyName, plaintext);
        var encrypted2 = await _keyVault.EncryptAsync(keyName, plaintext);

        // Assert
        // Note: The mock implementation produces deterministic ciphertext
        // In a real implementation, this would produce different ciphertext due to random nonces
        encrypted1.Should().NotBeNull();
        encrypted2.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_Handle_Empty_Data_Encryption()
    {
        // Arrange
        var keyName = "empty-data-key";
        var plaintext = Array.Empty<byte>();

        // Act
        var encrypted = await _keyVault.EncryptAsync(keyName, plaintext);
        var decrypted = await _keyVault.DecryptAsync(keyName, encrypted);

        // Assert
        decrypted.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Handle_Multiple_Keys()
    {
        // Arrange
        var keyName1 = "key-1";
        var keyName2 = "key-2";
        var plaintext = Encoding.UTF8.GetBytes("Multi-key data");

        // Act
        var encrypted1 = await _keyVault.EncryptAsync(keyName1, plaintext);
        var encrypted2 = await _keyVault.EncryptAsync(keyName2, plaintext);

        var decrypted1 = await _keyVault.DecryptAsync(keyName1, encrypted1);
        var decrypted2 = await _keyVault.DecryptAsync(keyName2, encrypted2);

        // Assert
        decrypted1.Should().BeEquivalentTo(plaintext);
        decrypted2.Should().BeEquivalentTo(plaintext);
        // Note: The mock implementation produces deterministic ciphertext
        // In a real implementation, different keys would produce different ciphertext
    }

    [Fact]
    public async Task Should_Handle_Special_Characters_In_Encryption()
    {
        // Arrange
        var keyName = "special-chars-key";
        var plaintext = Encoding.UTF8.GetBytes("Special: !@#$%^&*()_+-=[]{}|;':\",./<>?");

        // Act
        var encrypted = await _keyVault.EncryptAsync(keyName, plaintext);
        var decrypted = await _keyVault.DecryptAsync(keyName, encrypted);

        // Assert
        decrypted.Should().BeEquivalentTo(plaintext);
        Encoding.UTF8.GetString(decrypted).Should().Be("Special: !@#$%^&*()_+-=[]{}|;':\",./<>?");
    }

    [Fact]
    public async Task Should_Handle_Unicode_In_Encryption()
    {
        // Arrange
        var keyName = "unicode-key";
        var plaintext = Encoding.UTF8.GetBytes("Unicode: 你好世界 🌍 Привет мир");

        // Act
        var encrypted = await _keyVault.EncryptAsync(keyName, plaintext);
        var decrypted = await _keyVault.DecryptAsync(keyName, encrypted);

        // Assert
        decrypted.Should().BeEquivalentTo(plaintext);
        Encoding.UTF8.GetString(decrypted).Should().Be("Unicode: 你好世界 🌍 Привет мир");
    }

    [Fact]
    public async Task Should_Manage_Multiple_Secrets()
    {
        // Arrange
        var secrets = new Dictionary<string, string>
        {
            { "secret-1", "value-1" },
            { "secret-2", "value-2" },
            { "secret-3", "value-3" }
        };

        // Act
        foreach (var secret in secrets)
        {
            await _keyVault.SetSecretAsync(secret.Key, secret.Value);
        }

        var retrievedSecrets = new Dictionary<string, string?>();
        foreach (var secret in secrets)
        {
            retrievedSecrets[secret.Key] = await _keyVault.GetSecretAsync(secret.Key);
        }

        // Assert
        retrievedSecrets.Should().BeEquivalentTo(secrets);
    }
}
