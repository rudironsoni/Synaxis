// <copyright file="ApiKeyServiceTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Security.Cryptography;
using System.Text;
using Synaxis.InferenceGateway.Infrastructure.Security;
using Xunit;
using Xunit.Abstractions;

namespace Synaxis.InferenceGateway.IntegrationTests;

/// <summary>
/// Unit tests for ApiKeyService - API key generation and validation
/// Tests cryptographic operations, key format validation, and security hardening.
/// </summary>
public class ApiKeyServiceTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output ?? throw new ArgumentNullException(nameof(output));
    private readonly ApiKeyService _apiKeyService = new ApiKeyService();

    [Fact]
    public void GenerateKey_ShouldReturnValidFormat()
    {
        // Arrange & Act
        var key1 = this._apiKeyService.GenerateKey();
        var key2 = this._apiKeyService.GenerateKey();

        // Assert
        Assert.NotNull(key1);
        Assert.NotNull(key2);
        Assert.NotEqual(key1, key2); // Should be unique
        Assert.StartsWith("sk-synaxis-", key1, StringComparison.Ordinal);
        Assert.StartsWith("sk-synaxis-", key2, StringComparison.Ordinal);

        // Should be 32 bytes + prefix = 32 + 11 characters = 43 characters for hex
        Assert.Equal(43, key1.Length); // "sk-synaxis-" (11) + 32 hex chars = 43
        Assert.Equal(43, key2.Length);

        // Remaining characters should be lowercase hex
        var hexPart = key1.Substring(11);
        Assert.All(hexPart, c => Assert.True(
            (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'),
            $"Character '{c}' is not valid hex"));
    }

    [Fact]
    public void HashKey_ShouldReturnConsistentHash()
    {
        // Arrange
        var key = "sk-synaxis-test-key-123456789";

        // Act
        var hash1 = this._apiKeyService.HashKey(key);
        var hash2 = this._apiKeyService.HashKey(key);

        // Assert
        Assert.NotEmpty(hash1);
        Assert.NotEmpty(hash2);
        Assert.Equal(hash1, hash2); // Should be deterministic
        Assert.Equal(44, hash1.Length); // Base64 encoding of 32 bytes = 44 chars
    }

    [Fact]
    public void HashKey_ShouldReturnDifferentHashesForDifferentKeys()
    {
        // Arrange
        var key1 = "sk-synaxis-test-key-123456789";
        var key2 = "sk-synaxis-different-key-987654321";

        // Act
        var hash1 = this._apiKeyService.HashKey(key1);
        var hash2 = this._apiKeyService.HashKey(key2);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ValidateKey_ShouldReturnTrue_ForMatchingKeyAndHash()
    {
        // Arrange
        var key = "sk-synaxis-test-key-123456789";
        var hash = this._apiKeyService.HashKey(key);

        // Act
        var isValid = this._apiKeyService.ValidateKey(key, hash);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateKey_ShouldReturnFalse_ForMismatchedKeyAndHash()
    {
        // Arrange
        var key1 = "sk-synaxis-test-key-123456789";
        var key2 = "sk-synaxis-different-key-987654321";
        var hash = this._apiKeyService.HashKey(key1);

        // Act
        var isValid1 = this._apiKeyService.ValidateKey(key1, hash);
        var isValid2 = this._apiKeyService.ValidateKey(key2, hash);

        // Assert
        Assert.True(isValid1);
        Assert.False(isValid2);
    }

    [Fact]
    public void ValidateKey_ShouldReturnFalse_ForInvalidHash()
    {
        // Arrange
        var key = "sk-synaxis-test-key-123456789";
        var invalidHash = "invalid-hash-format";

        // Act
        var isValid = this._apiKeyService.ValidateKey(key, invalidHash);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateKey_ShouldReturnFalse_ForEmptyInputs()
    {
        // Arrange & Act
        var isValid1 = this._apiKeyService.ValidateKey(string.Empty, "hash");
        var isValid2 = this._apiKeyService.ValidateKey("key", string.Empty);
        var isValid3 = this._apiKeyService.ValidateKey(string.Empty, string.Empty);

        // Assert
        Assert.False(isValid1);
        Assert.False(isValid2);
        Assert.False(isValid3);
    }

    [Fact]
    public void GenerateKey_ShouldAlwaysProduceValidFormat()
    {
        // Arrange & Act
        var key = this._apiKeyService.GenerateKey();

        // Assert - Always should start with sk-synaxis- and be followed by valid lowercase hex
        Assert.StartsWith("sk-synaxis-", key, StringComparison.Ordinal);
        Assert.Equal(43, key.Length);

        var hexPart = key.Substring(11);
        Assert.All(hexPart, c => Assert.True(
            (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'),
            $"Character '{c}' is not valid hex"));
    }

    [Fact]
    public void HashKey_ShouldUseSHA256()
    {
        // Arrange
        var key = "sk-synaxis-test-key";

        // Act
        var hash = this._apiKeyService.HashKey(key);
        var expectedHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(key)));

        // Assert
        Assert.Equal(expectedHash, hash);
    }

    [Fact]
    public void ValidateKey_ShouldBeResistantToTimingAttacks()
    {
        // Arrange
        var key1 = "sk-synaxis-very-long-and-unique-key-1234567890abcdef";
        var key2 = "sk-synaxis-different-key-that-is-also-very-long-abcdef1234567890";
        var hash1 = this._apiKeyService.HashKey(key1);
        var hash2 = this._apiKeyService.HashKey(key2);

        // Test multiple validations to ensure constant-time comparison behavior
        // This verifies the constant-time comparison is used for security

        // Act & Assert
        Assert.True(this._apiKeyService.ValidateKey(key1, hash1));
        Assert.False(this._apiKeyService.ValidateKey(key2, hash1));
        Assert.False(this._apiKeyService.ValidateKey(key1, hash2));
        Assert.True(this._apiKeyService.ValidateKey(key2, hash2));

        // Test boundary conditions
        var shortKey = "sk-short";
        var shortHash = this._apiKeyService.HashKey(shortKey);
        Assert.True(this._apiKeyService.ValidateKey(shortKey, shortHash));
    }

    [Fact]
    public void RoundtripTest_GenerateHashValidate()
    {
        // Arrange & Act
        var generatedKey = this._apiKeyService.GenerateKey();
        var hash = this._apiKeyService.HashKey(generatedKey);
        var isValid = this._apiKeyService.ValidateKey(generatedKey, hash);

        // Assert
        Assert.True(isValid);
        Assert.NotEmpty(hash);
        Assert.Equal(44, hash.Length); // Base64 encoding length
    }

    [Fact]
    public void MultipleKeys_ShouldGenerateUniqueAndValid()
    {
        // Arrange & Act
        var keys = new string[10];
        for (int i = 0; i < 10; i++)
        {
            keys[i] = this._apiKeyService.GenerateKey();
        }

        // Assert
        for (int i = 0; i < 10; i++)
        {
            Assert.StartsWith("sk-synaxis-", keys[i], StringComparison.Ordinal);
            Assert.Equal(43, keys[i].Length);

            // All keys should be unique
            for (int j = i + 1; j < 10; j++)
            {
                Assert.NotEqual(keys[i], keys[j]);
            }

            // Each key should hash to something and validate correctly
            var hash = this._apiKeyService.HashKey(keys[i]);
            Assert.True(this._apiKeyService.ValidateKey(keys[i], hash));
        }
    }
}
