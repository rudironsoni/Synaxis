using System;
using System.Security.Cryptography;
using System.Text;
using Synaxis.InferenceGateway.Infrastructure.Security;
using Xunit;

namespace Synaxis.InferenceGateway.Infrastructure.Tests.Security
{
    public class ApiKeyServiceTests
    {
        private readonly ApiKeyService _apiKeyService;

        public ApiKeyServiceTests()
        {
            _apiKeyService = new ApiKeyService();
        }

        [Fact]
        public void GenerateKey_ReturnsValidKeyFormat()
        {
            // Act
            var key = _apiKeyService.GenerateKey();

            // Assert
            Assert.StartsWith("sk-synaxis-", key);
            Assert.True(key.Length > "sk-synaxis-".Length);
            // Should be 32 hex chars + prefix
            Assert.Equal("sk-synaxis-".Length + 32, key.Length);
        }

        [Fact]
        public void GenerateKey_GeneratesUniqueKeys()
        {
            // Act
            var key1 = _apiKeyService.GenerateKey();
            var key2 = _apiKeyService.GenerateKey();

            // Assert
            Assert.NotEqual(key1, key2);
        }

        [Fact]
        public void HashKey_ReturnsConsistentHashForSameKey()
        {
            // Arrange
            var key = "sk-synaxis-testkey1234567890abcdef";

            // Act
            var hash1 = _apiKeyService.HashKey(key);
            var hash2 = _apiKeyService.HashKey(key);

            // Assert
            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public void HashKey_ReturnsDifferentHashesForDifferentKeys()
        {
            // Arrange
            var key1 = "sk-synaxis-testkey1234567890abcdef";
            var key2 = "sk-synaxis-differentkey1234567890";

            // Act
            var hash1 = _apiKeyService.HashKey(key1);
            var hash2 = _apiKeyService.HashKey(key2);

            // Assert
            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public void HashKey_ReturnsBase64EncodedString()
        {
            // Arrange
            var key = "sk-synaxis-testkey1234567890abcdef";

            // Act
            var hash = _apiKeyService.HashKey(key);

            // Assert
            // Should be valid base64
            try
            {
                Convert.FromBase64String(hash);
                Assert.True(true); // Valid base64
            }
            catch (FormatException)
            {
                Assert.Fail("Hash should be valid base64 string");
            }
        }

        [Fact]
        public void ValidateKey_ReturnsTrueForMatchingKeyAndHash()
        {
            // Arrange
            var key = "sk-synaxis-testkey1234567890abcdef";
            var hash = _apiKeyService.HashKey(key);

            // Act
            var isValid = _apiKeyService.ValidateKey(key, hash);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void ValidateKey_ReturnsFalseForNonMatchingKeyAndHash()
        {
            // Arrange
            var key1 = "sk-synaxis-testkey1234567890abcdef";
            var key2 = "sk-synaxis-differentkey1234567890";
            var hash = _apiKeyService.HashKey(key1);

            // Act
            var isValid = _apiKeyService.ValidateKey(key2, hash);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void ValidateKey_ReturnsFalseForEmptyKey()
        {
            // Arrange
            var key = "sk-synaxis-testkey1234567890abcdef";
            var hash = _apiKeyService.HashKey(key);

            // Act
            var isValid = _apiKeyService.ValidateKey("", hash);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void ValidateKey_ReturnsFalseForEmptyHash()
        {
            // Arrange
            var key = "sk-synaxis-testkey1234567890abcdef";

            // Act
            var isValid = _apiKeyService.ValidateKey(key, "");

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void ValidateKey_UsesConstantTimeComparison()
        {
            // This test verifies the method uses constant-time comparison
            // by checking it uses CryptographicOperations.FixedTimeEquals
            
            // We can't directly test the implementation detail, but we can
            // verify it behaves correctly for timing attacks (not practical in unit tests)
            // Instead, we'll verify it works correctly with valid and invalid inputs
            
            // Arrange
            var key = "sk-synaxis-testkey1234567890abcdef";
            var hash = _apiKeyService.HashKey(key);
            
            // Test valid key
            var isValid1 = _apiKeyService.ValidateKey(key, hash);
            Assert.True(isValid1);
            
            // Test invalid key (different length)
            var isValid2 = _apiKeyService.ValidateKey("sk-synaxis-short", hash);
            Assert.False(isValid2);
            
            // Test invalid key (same length, different content)
            var isValid3 = _apiKeyService.ValidateKey("sk-synaxis-different1234567890", hash);
            Assert.False(isValid3);
        }

        [Fact]
        public void GenerateKey_UsesCryptographicallySecureRandom()
        {
            // While we can't directly test randomness quality in a unit test,
            // we can verify the key has the expected format and properties
            
            // Act
            var key1 = _apiKeyService.GenerateKey();
            var key2 = _apiKeyService.GenerateKey();
            
            // Assert
            Assert.NotEqual(key1, key2);
            
            // Verify format
            Assert.StartsWith("sk-synaxis-", key1);
            Assert.StartsWith("sk-synaxis-", key2);
            
            // Extract hex parts and verify they're valid hex
            var hexPart1 = key1.Substring("sk-synaxis-".Length);
            var hexPart2 = key2.Substring("sk-synaxis-".Length);
            
            Assert.True(hexPart1.Length == 32);
            Assert.True(hexPart2.Length == 32);
            
            // Verify they're valid hex strings
            Assert.True(IsHexString(hexPart1));
            Assert.True(IsHexString(hexPart2));
        }

        private static bool IsHexString(string hex)
        {
            foreach (char c in hex)
            {
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                {
                    return false;
                }
            }
            return true;
        }
    }
}