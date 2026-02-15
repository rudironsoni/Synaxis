// <copyright file="EncryptionServiceTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.IntegrationTests;

using System;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Synaxis.Abstractions.Cloud;
using Synaxis.Infrastructure.Encryption;
using Xunit;

/// <summary>
/// Integration tests for Encryption Service.
/// </summary>
public sealed class EncryptionServiceTests
{
    private readonly IEncryptionService _encryptionService;
    private readonly string _tenantId;
    private readonly string _keyId;

    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptionServiceTests"/> class.
    /// </summary>
    public EncryptionServiceTests()
    {
        var mockKeyVault = new Mock<IKeyVault>();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<EncryptionService>();

        var options = Options.Create(new EncryptionOptions
        {
            DefaultKeySizeBits = 256,
            DefaultKeyRotationDays = 90,
            MaxKeyVersions = 5,
            DefaultAlgorithm = "AES-256-GCM"
        });

        mockKeyVault.Setup(x => x.EncryptAsync(It.IsAny<string>(), It.IsAny<byte[]>(), default))
            .Returns<string, byte[], CancellationToken>((name, data, _) =>
            {
                var encrypted = new byte[data.Length];
                Array.Copy(data, encrypted, data.Length);
                for (int i = 0; i < encrypted.Length; i++)
                {
                    encrypted[i] = (byte)(encrypted[i] ^ 0xFF);
                }
                return Task.FromResult(encrypted);
            });

        mockKeyVault.Setup(x => x.DecryptAsync(It.IsAny<string>(), It.IsAny<byte[]>(), default))
            .Returns<string, byte[], CancellationToken>((name, data, _) =>
            {
                var decrypted = new byte[data.Length];
                Array.Copy(data, decrypted, data.Length);
                for (int i = 0; i < decrypted.Length; i++)
                {
                    decrypted[i] = (byte)(decrypted[i] ^ 0xFF);
                }
                return Task.FromResult(decrypted);
            });

        var tenantKeyLogger = loggerFactory.CreateLogger<TenantKeyService>();
        var tenantKeyService = new TenantKeyService(mockKeyVault.Object, tenantKeyLogger, options);
        _encryptionService = new EncryptionService(mockKeyVault.Object, tenantKeyService, logger, options);

        _tenantId = Guid.NewGuid().ToString();
        _keyId = "test-key";
    }

    [Fact]
    public async Task Should_Encrypt_And_Decrypt_Bytes()
    {
        // Arrange
        var plaintext = Encoding.UTF8.GetBytes("Hello, World!");

        // Act
        var encrypted = await _encryptionService.EncryptAsync(plaintext, _tenantId, _keyId);
        var decrypted = await _encryptionService.DecryptAsync(encrypted, _tenantId);

        // Assert
        decrypted.Should().BeEquivalentTo(plaintext);
        Encoding.UTF8.GetString(decrypted).Should().Be("Hello, World!");
    }

    [Fact]
    public async Task Should_Encrypt_And_Decrypt_String()
    {
        // Arrange
        var plaintext = "Sensitive data that needs encryption";

        // Act
        var encrypted = await _encryptionService.EncryptStringAsync(plaintext, _tenantId, _keyId);
        var decrypted = await _encryptionService.DecryptToStringAsync(encrypted, _tenantId);

        // Assert
        decrypted.Should().Be(plaintext);
    }

    [Fact]
    public async Task Should_Encrypt_Large_Data()
    {
        // Arrange
        var largeData = new byte[1024 * 1024]; // 1MB
        new Random().NextBytes(largeData);

        // Act
        var encrypted = await _encryptionService.EncryptAsync(largeData, _tenantId, _keyId);
        var decrypted = await _encryptionService.DecryptAsync(encrypted, _tenantId);

        // Assert
        decrypted.Should().BeEquivalentTo(largeData);
    }

    [Fact]
    public async Task Should_Produce_Different_Ciphertext_For_Same_Plaintext()
    {
        // Arrange
        var plaintext = Encoding.UTF8.GetBytes("Same data");

        // Act
        var encrypted1 = await _encryptionService.EncryptAsync(plaintext, _tenantId, _keyId);
        var encrypted2 = await _encryptionService.EncryptAsync(plaintext, _tenantId, _keyId);

        // Assert
        encrypted1.Ciphertext.Should().NotBeEquivalentTo(encrypted2.Ciphertext);
        encrypted1.EncryptedKey.Should().NotBeEquivalentTo(encrypted2.EncryptedKey);
    }

    [Fact]
    public async Task Should_Handle_Empty_Data()
    {
        // Arrange
        var plaintext = Array.Empty<byte>();

        // Act
        var encrypted = await _encryptionService.EncryptAsync(plaintext, _tenantId, _keyId);
        var decrypted = await _encryptionService.DecryptAsync(encrypted, _tenantId);

        // Assert
        decrypted.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Handle_Special_Characters()
    {
        // Arrange
        var plaintext = "Special chars: !@#$%^&*()_+-=[]{}|;':\",./<>?";

        // Act
        var encrypted = await _encryptionService.EncryptStringAsync(plaintext, _tenantId, _keyId);
        var decrypted = await _encryptionService.DecryptToStringAsync(encrypted, _tenantId);

        // Assert
        decrypted.Should().Be(plaintext);
    }

    [Fact]
    public async Task Should_Handle_Unicode_Characters()
    {
        // Arrange
        var plaintext = "Unicode: 你好世界 🌍 Привет мир";

        // Act
        var encrypted = await _encryptionService.EncryptStringAsync(plaintext, _tenantId, _keyId);
        var decrypted = await _encryptionService.DecryptToStringAsync(encrypted, _tenantId);

        // Assert
        decrypted.Should().Be(plaintext);
    }

    [Fact]
    public async Task Should_Handle_Multiple_Tenants()
    {
        // Arrange
        var plaintext = "Multi-tenant data";
        var tenantId1 = "tenant-1";
        var tenantId2 = "tenant-2";

        // Act
        var encrypted1 = await _encryptionService.EncryptStringAsync(plaintext, tenantId1, _keyId);
        var encrypted2 = await _encryptionService.EncryptStringAsync(plaintext, tenantId2, _keyId);

        var decrypted1 = await _encryptionService.DecryptToStringAsync(encrypted1, tenantId1);
        var decrypted2 = await _encryptionService.DecryptToStringAsync(encrypted2, tenantId2);

        // Assert
        decrypted1.Should().Be(plaintext);
        decrypted2.Should().Be(plaintext);
    }
}
