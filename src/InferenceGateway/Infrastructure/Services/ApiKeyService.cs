// <copyright file="ApiKeyService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Services
{
    using System.Numerics;
    using System.Security.Cryptography;
    using System.Text;
    using BCrypt.Net;
    using Microsoft.EntityFrameworkCore;
    using Synaxis.InferenceGateway.Application.ApiKeys;
    using Synaxis.InferenceGateway.Application.ApiKeys.Models;
    using Synaxis.InferenceGateway.Infrastructure.ControlPlane;
    using Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Operations;

    /// <summary>
    /// Implementation of the API key service for managing programmatic access with bcrypt hashing.
    /// </summary>
    /// <remarks>
    /// API Key Format: synaxis_{43-char-base62-id}_{43-char-base62-secret}
    /// - ID Part: 32 bytes (256 bits) of entropy encoded as base62 (~43 characters)
    /// - Secret Part: 32 bytes (256 bits) of entropy encoded as base62 (~43 characters)
    /// - Prefix Format: synaxis_{first-8-chars-of-id}
    /// - Hash: bcrypt with work factor 12
    /// </remarks>
    public class ApiKeyService : IApiKeyService
    {
        private readonly SynaxisDbContext _context;
        private const string KeyPrefix = "synaxis";
        private const int EntropyBytes = 32; // 256 bits
        private const int WorkFactor = 12; // bcrypt work factor
        private const int PrefixIdLength = 8;
        private const string Base62Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiKeyService"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
        public ApiKeyService(SynaxisDbContext context)
        {
            this._context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc />
        public async Task<GenerateApiKeyResponse> GenerateApiKeyAsync(
            GenerateApiKeyRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            // Generate 256-bit entropy for both ID and secret parts
            var keyId = GenerateSecureRandomBytes(EntropyBytes);
            var keySecret = GenerateSecureRandomBytes(EntropyBytes);

            // Encode to base62 (will produce ~43 characters each)
            var keyIdBase62 = EncodeBase62(keyId);
            var keySecretBase62 = EncodeBase62(keySecret);

            // Format: synaxis_{43-char-base62-id}_{43-char-base62-secret}
            var fullKey = $"{KeyPrefix}_{keyIdBase62}_{keySecretBase62}";

            // Hash the full key using bcrypt with work factor 12
            var keyHash = HashApiKeyWithBcrypt(fullKey);

            // Extract prefix: synaxis_{first-8-chars-of-id}
            var prefix = ExtractKeyPrefix(keyIdBase62);

            // Create API key entity
            var apiKey = new ApiKey
            {
                Id = Guid.NewGuid(),
                OrganizationId = request.OrganizationId,
                Name = request.Name,
                KeyHash = keyHash,
                KeyPrefix = prefix,
                Scopes = request.Scopes.Length > 0 ? string.Join(",", request.Scopes) : null,
                ExpiresAt = request.ExpiresAt,
                RateLimitRpm = request.RateLimitRpm,
                RateLimitTpm = request.RateLimitTpm,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = request.CreatedBy
            };

            _context.ApiKeys.Add(apiKey);
            await _context.SaveChangesAsync(cancellationToken);

            return new GenerateApiKeyResponse
            {
                Id = apiKey.Id,
                ApiKey = fullKey, // Only returned once at generation!
                Name = apiKey.Name,
                Prefix = prefix,
                Scopes = request.Scopes,
                ExpiresAt = apiKey.ExpiresAt,
                CreatedAt = apiKey.CreatedAt
            };
        }

        /// <inheritdoc />
        public async Task<ApiKeyValidationResult> ValidateApiKeyAsync(
            string apiKey,
            CancellationToken cancellationToken = default)
        {
            var result = new ApiKeyValidationResult();

            // Check format: synaxis_{id}_{secret}
            if (!IsValidKeyFormat(apiKey))
            {
                result.ErrorMessage = "Invalid API key format.";
                return result;
            }

            // Extract prefix to narrow database search
            var parts = apiKey.Split('_', 3);
            if (parts.Length != 3)
            {
                result.ErrorMessage = "Invalid API key structure.";
                return result;
            }

            var keyIdPart = parts[1];
            var prefix = ExtractKeyPrefix(keyIdPart);

            // Find potential matching keys by prefix (indexed lookup)
            var candidates = await _context.ApiKeys
                .Where(k => k.KeyPrefix == prefix)
                .ToListAsync(cancellationToken);

            ApiKey? matchedKey = null;

            // Verify against each candidate using bcrypt
            foreach (var candidate in candidates)
            {
                try
                {
                    if (VerifyApiKeyWithBcrypt(apiKey, candidate.KeyHash))
                    {
                        matchedKey = candidate;
                        break;
                    }
                }
                catch (Exception)
                {
                    // Continue checking other candidates if bcrypt verification fails
                    continue;
                }
            }

            if (matchedKey == null)
            {
                result.ErrorMessage = "API key not found.";
                return result;
            }

            // Check if active
            if (!matchedKey.IsActive)
            {
                result.ErrorMessage = "API key has been deactivated.";
                return result;
            }

            // Check if revoked
            if (matchedKey.Revokedthis.At.HasValue)
            {
                result.ErrorMessage = $"API key was revoked: {matchedKey.RevocationReason ?? "No reason provided"}";
                return result;
            }

            // Check if expired
            if (matchedKey.ExpiresAt.HasValue && matchedKey.ExpiresAt.Value < DateTime.UtcNow)
            {
                result.ErrorMessage = $"API key expired on {matchedKey.ExpiresAt.Value:yyyy-MM-dd HH:mm:ss} UTC.";
                return result;
            }

            // Valid key - populate result
            result.IsValid = true;
            result.OrganizationId = matchedKey.OrganizationId;
            result.ApiKeyId = matchedKey.Id;
            result.Scopes = string.IsNullOrWhiteSpace(matchedKey.Scopes)
                ? Array.Empty<string>()
                : matchedKey.Scopes.Split(',', StringSplitOptions.RemoveEmptyEntries);
            result.RateLimitRpm = matchedKey.RateLimitRpm;
            result.RateLimitTpm = matchedKey.RateLimitTpm;

            // Update last used timestamp (fire and forget to avoid blocking validation)
            _ = Task.Run(async () =>
            {
                try
                {
                    await UpdateLastUsedAsync(matchedKey.Id, CancellationToken.None);
                }
                catch
                {
                    // Silently ignore errors in background update
                }
            }, CancellationToken.None);

            return result;
        }

        /// <inheritdoc />
        public async Task<bool> RevokeApiKeyAsync(
            Guid apiKeyId,
            string reason,
            Guid? revokedBy = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException("Revocation reason is required.", nameof(reason));
            }

            var apiKey = await _context.ApiKeys
                .FirstOrDefaultAsync(k => k.Id == apiKeyId, cancellationToken);

            if (apiKey == null)
            {
                return false;
            }

            // Check if already revoked
            if (apiKey.Revokedthis.At.HasValue)
            {
                return false;
            }

            apiKey.IsActive = false;
            apiKey.RevokedAt = DateTime.UtcNow;
            apiKey.RevokedBy = revokedBy;
            apiKey.RevocationReason = reason;

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        /// <inheritdoc />
        public async Task<IList<ApiKeyInfo>> ListApiKeysAsync(
            Guid organizationId,
            bool includeRevoked = false,
            CancellationToken cancellationToken = default)
        {
            var query = _context.ApiKeys
                .Where(k => k.OrganizationId == organizationId);

            if (!includeRevoked)
            {
                query = query.Where(k => k.IsActive && !k.Revokedthis.At.HasValue);
            }

            var keys = await query
                .OrderByDescending(k => k.CreatedAt)
                .ToListAsync(cancellationToken);

            return keys.Select(MapToApiKeyInfo).ToList();
        }

        /// <inheritdoc />
        public async Task<ApiKeyUsageStatistics> GetApiKeyUsageAsync(
            Guid apiKeyId,
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default)
        {
            var apiKey = await _context.ApiKeys
                .FirstOrDefaultAsync(k => k.Id == apiKeyId, cancellationToken);

            if (apiKey == null)
            {
                throw new InvalidOperationException($"API key with ID {apiKeyId} not found.");
            }

            // NOTE: Implement actual usage tracking from audit logs or separate usage table
            // For now, return basic statistics
            return new ApiKeyUsageStatistics
            {
                ApiKeyId = apiKeyId,
                TotalRequests = 0,
                SuccessfulRequests = 0,
                FailedRequests = 0,
                From = from,
                To = to,
                LastUsedAt = apiKey.LastUsedAt
            };
        }

        /// <inheritdoc />
        public async Task UpdateLastUsedAsync(
            Guid apiKeyId,
            CancellationToken cancellationToken = default)
        {
            var apiKey = await _context.ApiKeys
                .FirstOrDefaultAsync(k => k.Id == apiKeyId, cancellationToken);

            if (apiKey != null)
            {
                apiKey.LastUsedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Generates cryptographically secure random bytes.
        /// </summary>
        /// <param name="length">The number of bytes to generate.</param>
        /// <returns>A byte array with the specified length of random data.</returns>
        private static byte[] GenerateSecureRandomBytes(int length)
        {
            var bytes = new byte[length];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return bytes;
        }

        /// <summary>
        /// Encodes bytes to base62 string.
        /// </summary>
        /// <param name="data">The byte array to encode.</param>
        /// <returns>A base62 encoded string.</returns>
        /// <remarks>
        /// Base62 uses characters 0-9, A-Z, a-z for compact, URL-safe encoding.
        /// 32 bytes (256 bits) encodes to approximately 43 base62 characters.
        /// </remarks>
        private static string EncodeBase62(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return "0";
            }

            // Convert bytes to BigInteger (add trailing zero byte to ensure positive)
            var num = new BigInteger(data.Concat(new byte[] { 0 }).ToArray());

            if (num.IsZero)
            {
                return "0";
            }

            var builder = new StringBuilder();
            var base62 = new BigInteger(62);

            while (num > 0)
            {
                var remainder = (int)(num % base62);
                builder.Insert(0, Base62Chars[remainder]);
                num /= base62;
            }

            return builder.ToString();
        }

        /// <summary>
        /// Hashes an API key using bcrypt with work factor 12.
        /// </summary>
        /// <param name="apiKey">The API key to hash.</param>
        /// <returns>The bcrypt hash string.</returns>
        /// <remarks>
        /// Work factor 12 provides a good balance between security and performance.
        /// Each increment doubles the computation time.
        /// </remarks>
        private static string HashApiKeyWithBcrypt(string apiKey)
        {
            return BCrypt.HashPassword(apiKey, WorkFactor);
        }

        /// <summary>
        /// Verifies an API key against a bcrypt hash.
        /// </summary>
        /// <param name="apiKey">The API key to verify.</param>
        /// <param name="hash">The bcrypt hash to verify against.</param>
        /// <returns>True if the key matches the hash, false otherwise.</returns>
        private static bool VerifyApiKeyWithBcrypt(string apiKey, string hash)
        {
            try
            {
                return BCrypt.Verify(apiKey, hash);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Extracts the key prefix from the key ID part.
        /// </summary>
        /// <param name="keyIdBase62">The base62-encoded key ID.</param>
        /// <returns>The prefix in format "synaxis_{first-8-chars-of-id}".</returns>
        private static string ExtractKeyPrefix(string keyIdBase62)
        {
            var idPrefix = keyIdBase62.Length >= PrefixIdLength
                ? keyIdBase62.Substring(0, PrefixIdLength)
                : keyIdBase62;

            return $"{KeyPrefix}_{idPrefix}";
        }

        /// <summary>
        /// Validates the API key format.
        /// </summary>
        /// <param name="apiKey">The API key to validate.</param>
        /// <returns>True if the format is valid, false otherwise.</returns>
        private static bool IsValidKeyFormat(string? apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return false;
            }

            return apiKey.StartsWith($"{KeyPrefix}_", StringComparison.Ordinal)
                   && apiKey.Split('_').Length == 3;
        }

        /// <summary>
        /// Maps an ApiKey entity to ApiKeyInfo DTO.
        /// </summary>
        /// <param name="apiKey">The API key entity.</param>
        /// <returns>The API key information DTO.</returns>
        private static ApiKeyInfo MapToApiKeyInfo(ApiKey apiKey)
        {
            return new ApiKeyInfo
            {
                Id = apiKey.Id,
                Name = apiKey.Name,
                Prefix = apiKey.KeyPrefix,
                Scopes = string.IsNullOrWhiteSpace(apiKey.Scopes)
                    ? Array.Empty<string>()
                    : apiKey.Scopes.Split(',', StringSplitOptions.RemoveEmptyEntries),
                IsActive = apiKey.IsActive,
                ExpiresAt = apiKey.ExpiresAt,
                LastUsedAt = apiKey.LastUsedAt,
                CreatedAt = apiKey.CreatedAt,
                RevokedAt = apiKey.RevokedAt,
                RevocationReason = apiKey.RevocationReason
            };
        }

        #endregion
    }
}
