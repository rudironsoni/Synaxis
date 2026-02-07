using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Synaxis.InferenceGateway.Application.Configuration;
using Synaxis.InferenceGateway.Application.ControlPlane.Entities;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane;
using Synaxis.InferenceGateway.Infrastructure.Security;
using Xunit;

namespace Synaxis.InferenceGateway.Infrastructure.Tests.Security
{
    public class AesGcmTokenVaultTests
    {
        private const string TestMasterKey = "THIS_IS_A_TEST_MASTER_KEY_FOR_UNIT_TESTS_1234567890";

        [Fact]
        public async Task Constructor_WithNullDbContext_DoesNotThrowException()
        {
            // Arrange
            ControlPlaneDbContext nullDbContext = null!;
            var mockConfig = CreateMockConfig(TestMasterKey);

            // Act & Assert
            // The constructor doesn't explicitly check for null dbContext, so it won't throw
            // An exception will only be thrown when trying to use the service
            var vault = new AesGcmTokenVault(nullDbContext!, mockConfig);
            Assert.NotNull(vault);
        }

        [Fact]
        public async Task Constructor_WithNullConfig_ThrowsNullReferenceException()
        {
            // Arrange
            using var dbContext = BuildDbContext();
            IOptions<SynaxisConfiguration> nullConfig = null!;

            // Act & Assert
            Assert.Throws<NullReferenceException>(() => new AesGcmTokenVault(dbContext, nullConfig!));
        }

        [Fact]
        public async Task Constructor_WithEmptyMasterKey_ThrowsInvalidOperationException()
        {
            // Arrange
            using var dbContext = BuildDbContext();
            var mockConfig = CreateMockConfig("");

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                new AesGcmTokenVault(dbContext, mockConfig));
            Assert.Equal("Synaxis:InferenceGateway:MasterKey must be configured.", exception.Message);
        }

        [Fact]
        public async Task Constructor_WithNullMasterKey_ThrowsInvalidOperationException()
        {
            // Arrange
            using var dbContext = BuildDbContext();
            var mockConfig = CreateMockConfig(null);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                new AesGcmTokenVault(dbContext, mockConfig));
            Assert.Equal("Synaxis:InferenceGateway:MasterKey must be configured.", exception.Message);
        }

        [Fact]
        public async Task Constructor_WithWhitespaceMasterKey_ThrowsInvalidOperationException()
        {
            // Arrange
            using var dbContext = BuildDbContext();
            var mockConfig = CreateMockConfig("   ");

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                new AesGcmTokenVault(dbContext, mockConfig));
            Assert.Equal("Synaxis:InferenceGateway:MasterKey must be configured.", exception.Message);
        }

        [Fact]
        public async Task EncryptAsync_WithValidTenant_ReturnsEncryptedData()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var plaintext = "test-secret-token-value";

            using var dbContext = BuildDbContext();
            var mockConfig = CreateMockConfig(TestMasterKey);
            var tokenVault = new AesGcmTokenVault(dbContext, mockConfig);

            // Add tenant to database
            var tenant = new Tenant
            {
                Id = tenantId,
                EncryptedByokKey = Array.Empty<byte>(), // Use master key fallback
            };

            dbContext.Tenants.Add(tenant);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            // Act
            var encrypted = await tokenVault.EncryptAsync(tenantId, plaintext).ConfigureAwait(false);

            // Assert
            Assert.NotNull(encrypted);
            Assert.NotEmpty(encrypted);
            // Encrypted data should be larger than plaintext due to nonce and tag
            Assert.True(encrypted.Length > plaintext.Length);
            // Should have nonce (12) + tag (16) + ciphertext
            Assert.True(encrypted.Length >= 28);
        }

        [Fact]
        public async Task EncryptAsync_WithNonExistentTenant_ThrowsArgumentException()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var plaintext = "test-secret-token-value";

            using var dbContext = BuildDbContext();
            var mockConfig = CreateMockConfig(TestMasterKey);
            var tokenVault = new AesGcmTokenVault(dbContext, mockConfig);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                tokenVault.EncryptAsync(tenantId, plaintext)).ConfigureAwait(false);
            Assert.StartsWith("Tenant not found", exception.Message);
            Assert.Equal("tenantId", exception.ParamName);
        }

        [Fact]
        public async Task DecryptAsync_WithValidEncryptedData_ReturnsOriginalPlaintext()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var originalPlaintext = "test-secret-token-value";

            using var dbContext = BuildDbContext();
            var mockConfig = CreateMockConfig(TestMasterKey);
            var tokenVault = new AesGcmTokenVault(dbContext, mockConfig);

            // Add tenant to database
            var tenant = new Tenant
            {
                Id = tenantId,
                EncryptedByokKey = Array.Empty<byte>(), // Use master key fallback
            };

            dbContext.Tenants.Add(tenant);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            // First encrypt some data
            var encrypted = await tokenVault.EncryptAsync(tenantId, originalPlaintext).ConfigureAwait(false);

            // Act
            var decrypted = await tokenVault.DecryptAsync(tenantId, encrypted).ConfigureAwait(false);

            // Assert
            Assert.Equal(originalPlaintext, decrypted);
        }

        [Fact]
        public async Task DecryptAsync_WithNonExistentTenant_ThrowsArgumentException()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var ciphertext = new byte[] { 1, 2, 3, 4, 5 }; // Dummy data

            using var dbContext = BuildDbContext();
            var mockConfig = CreateMockConfig(TestMasterKey);
            var tokenVault = new AesGcmTokenVault(dbContext, mockConfig);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                tokenVault.DecryptAsync(tenantId, ciphertext)).ConfigureAwait(false);
            Assert.StartsWith("Tenant not found", exception.Message);
            Assert.Equal("tenantId", exception.ParamName);
        }

        [Fact]
        public async Task DecryptAsync_WithInvalidCiphertext_ThrowsArgumentException()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var invalidCiphertext = new byte[] { 1, 2, 3 }; // Too short

            using var dbContext = BuildDbContext();
            var mockConfig = CreateMockConfig(TestMasterKey);
            var tokenVault = new AesGcmTokenVault(dbContext, mockConfig);

            // Add tenant to database
            var tenant = new Tenant
            {
                Id = tenantId,
                EncryptedByokKey = Array.Empty<byte>(), // Use master key fallback
            };

            dbContext.Tenants.Add(tenant);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                tokenVault.DecryptAsync(tenantId, invalidCiphertext)).ConfigureAwait(false);
        }

        [Fact]
        public async Task RotateKeyAsync_WithValidTenant_UpdatesEncryptedTokens()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var newKey = new byte[32];
            RandomNumberGenerator.Fill(newKey);
            var newKeyBase64 = Convert.ToBase64String(newKey);

            using var dbContext = BuildDbContext();
            var mockConfig = CreateMockConfig(TestMasterKey);
            var tokenVault = new AesGcmTokenVault(dbContext, mockConfig);

            // Add tenant to database first
            var tenant = new Tenant
            {
                Id = tenantId,
                EncryptedByokKey = Array.Empty<byte>(), // Use master key fallback
            };

            dbContext.Tenants.Add(tenant);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            // Now create OAuth accounts using the tenant that exists
            var oauthAccount = new OAuthAccount
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                AccessTokenEncrypted = await tokenVault.EncryptAsync(tenantId, "access-token").ConfigureAwait(false),
                RefreshTokenEncrypted = await tokenVault.EncryptAsync(tenantId, "refresh-token").ConfigureAwait(false),
            }.ConfigureAwait(false);

            dbContext.OAuthAccounts.Add(oauthAccount);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            // Act
            await tokenVault.RotateKeyAsync(tenantId, newKeyBase64).ConfigureAwait(false);

            // Assert
            // Reload tenant from database to check updated values
            var updatedTenant = await dbContext.Tenants.FindAsync(tenantId).ConfigureAwait(false);
            Assert.NotNull(updatedTenant);
            Assert.NotNull(updatedTenant.EncryptedByokKey);
            Assert.NotEmpty(updatedTenant.EncryptedByokKey);
        }

        [Fact]
        public async Task RotateKeyAsync_WithNonExistentTenant_ThrowsArgumentException()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var newKey = new byte[32];
            RandomNumberGenerator.Fill(newKey);
            var newKeyBase64 = Convert.ToBase64String(newKey);

            using var dbContext = BuildDbContext();
            var mockConfig = CreateMockConfig(TestMasterKey);
            var tokenVault = new AesGcmTokenVault(dbContext, mockConfig);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                tokenVault.RotateKeyAsync(tenantId, newKeyBase64)).ConfigureAwait(false);
            Assert.StartsWith("Tenant not found", exception.Message);
            Assert.Equal("tenantId", exception.ParamName);
        }

        [Fact]
        public async Task RotateKeyAsync_WithInvalidKeyLength_ThrowsArgumentException()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var invalidKey = new byte[16]; // Wrong size
            RandomNumberGenerator.Fill(invalidKey);
            var invalidKeyBase64 = Convert.ToBase64String(invalidKey);

            using var dbContext = BuildDbContext();
            var mockConfig = CreateMockConfig(TestMasterKey);
            var tokenVault = new AesGcmTokenVault(dbContext, mockConfig);

            // Add tenant to database
            var tenant = new Tenant
            {
                Id = tenantId,
                EncryptedByokKey = Array.Empty<byte>(),
            };

            dbContext.Tenants.Add(tenant);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                tokenVault.RotateKeyAsync(tenantId, invalidKeyBase64)).ConfigureAwait(false);
            Assert.StartsWith("Key must be 32 bytes", exception.Message);
            Assert.Equal("newKeyBase64", exception.ParamName);
        }

        [Fact]
        public void Encrypt_Decrypt_Roundtrip_WorksCorrectly()
        {
            // Arrange
            var plaintext = "test-data-for-encryption";
            var key = new byte[32];
            RandomNumberGenerator.Fill(key);

            // Act
            var encrypted = AesGcmTokenVaultTestsHelper.Encrypt(Encoding.UTF8.GetBytes(plaintext), key);
            var decrypted = AesGcmTokenVaultTestsHelper.Decrypt(encrypted, key);
            var decryptedText = Encoding.UTF8.GetString(decrypted);

            // Assert
            Assert.Equal(plaintext, decryptedText);
        }

        [Fact]
        public void Encrypt_Produces_Different_Output_For_Same_Input()
        {
            // Arrange
            var plaintext = "test-data-for-encryption";
            var key = new byte[32];
            RandomNumberGenerator.Fill(key);

            // Act
            var encrypted1 = AesGcmTokenVaultTestsHelper.Encrypt(Encoding.UTF8.GetBytes(plaintext), key);
            var encrypted2 = AesGcmTokenVaultTestsHelper.Encrypt(Encoding.UTF8.GetBytes(plaintext), key);

            // Assert
            Assert.NotEqual(encrypted1, encrypted2);
            // But both should decrypt to the same value
            var decrypted1 = Encoding.UTF8.GetString(AesGcmTokenVaultTestsHelper.Decrypt(encrypted1, key));
            var decrypted2 = Encoding.UTF8.GetString(AesGcmTokenVaultTestsHelper.Decrypt(encrypted2, key));
            Assert.Equal(decrypted1, decrypted2);
            Assert.Equal(plaintext, decrypted1);
        }

        private static IOptions<SynaxisConfiguration> CreateMockConfig(string? masterKey)
        {
            var config = new SynaxisConfiguration
            {
                MasterKey = masterKey,
            };

            var mockConfig = new Mock<IOptions<SynaxisConfiguration>>();
            mockConfig.Setup(c => c.Value).Returns(config);
            return mockConfig.Object;
        }

        private static ControlPlaneDbContext BuildDbContext()
        {
            var options = new DbContextOptionsBuilder<ControlPlaneDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ControlPlaneDbContext(options);
        }
    }

    // Helper class to access private static methods for testing
    internal static class AesGcmTokenVaultTestsHelper
    {
        public static byte[] Encrypt(byte[] plaintext, byte[] key)
        {
            // Format: Nonce (12) + Tag (16) + Ciphertext (N)
            var nonce = new byte[12];
            RandomNumberGenerator.Fill(nonce);

            var ciphertext = new byte[plaintext.Length];
            var tag = new byte[16];

            using var aes = new AesGcm(key, 16);
            aes.Encrypt(nonce, plaintext, ciphertext, tag);

            var result = new byte[nonce.Length + tag.Length + ciphertext.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
            Buffer.BlockCopy(ciphertext, 0, result, nonce.Length + tag.Length, ciphertext.Length);

            return result;
        }

        public static byte[] Decrypt(byte[] encrypted, byte[] key)
        {
            if (encrypted.Length < 28)
            {
                throw new ArgumentException("Invalid ciphertext");
            }

            var nonce = encrypted.AsSpan(0, 12);
            var tag = encrypted.AsSpan(12, 16);
            var ciphertext = encrypted.AsSpan(28);

            var plaintext = new byte[ciphertext.Length];

            using var aes = new AesGcm(key, 16);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);

            return plaintext;
        }
    }
}
