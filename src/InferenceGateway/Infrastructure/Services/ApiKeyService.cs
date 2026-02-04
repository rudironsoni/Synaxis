using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using Synaxis.InferenceGateway.Application.ApiKeys.Models;
using Synaxis.InferenceGateway.Application.ApiKeys;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Operations;

namespace Synaxis.InferenceGateway.Infrastructure.Services;

/// <summary>
/// Implementation of the API key service for managing programmatic access.
/// </summary>
public class ApiKeyService : IApiKeyService
{
    private readonly SynaxisDbContext _context;
    private const string KeyPrefix = "synaxis_build";
    private const int SecretLength = 16; // bytes
    private const string Base62Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiKeyService"/> class.
    /// </summary>
    public ApiKeyService(SynaxisDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<GenerateApiKeyResponse> GenerateApiKeyAsync(
        GenerateApiKeyRequest request,
        CancellationToken cancellationToken = default)
    {
        // Generate random bytes for the key components
        var keyId = GenerateRandomBytes(SecretLength);
        var keySecret = GenerateRandomBytes(SecretLength);

        // Encode to base62
        var keyIdBase62 = EncodeBase62(keyId);
        var keySecretBase62 = EncodeBase62(keySecret);

        // Format: synaxis_build_{keyId}_{keySecret}
        var fullKey = $"{KeyPrefix}_{keyIdBase62}_{keySecretBase62}";
        var keyHash = HashApiKey(fullKey);
        var prefix = $"{KeyPrefix}_{keyIdBase62.Substring(0, Math.Min(8, keyIdBase62.Length))}";

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
            CreatedAt = DateTime.UtcNow
        };

        _context.ApiKeys.Add(apiKey);
        await _context.SaveChangesAsync(cancellationToken);

        return new GenerateApiKeyResponse
        {
            Id = apiKey.Id,
            ApiKey = fullKey, // Only returned once!
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

        // Check format
        if (string.IsNullOrWhiteSpace(apiKey) || !apiKey.StartsWith($"{KeyPrefix}_"))
        {
            result.ErrorMessage = "Invalid API key format.";
            return result;
        }

        // Hash the key
        var keyHash = HashApiKey(apiKey);

        // Find the key in database
        var keyEntity = await _context.ApiKeys
            .Where(k => k.KeyHash == keyHash)
            .FirstOrDefaultAsync(cancellationToken);

        if (keyEntity == null)
        {
            result.ErrorMessage = "API key not found.";
            return result;
        }

        // Check if active
        if (!keyEntity.IsActive)
        {
            result.ErrorMessage = "API key has been revoked.";
            return result;
        }

        // Check if expired
        if (keyEntity.ExpiresAt.HasValue && keyEntity.ExpiresAt.Value < DateTime.UtcNow)
        {
            result.ErrorMessage = "API key has expired.";
            return result;
        }

        // Check if revoked
        if (keyEntity.RevokedAt.HasValue)
        {
            result.ErrorMessage = "API key has been revoked.";
            return result;
        }

        // Valid key
        result.IsValid = true;
        result.OrganizationId = keyEntity.OrganizationId;
        result.ApiKeyId = keyEntity.Id;
        result.Scopes = string.IsNullOrWhiteSpace(keyEntity.Scopes)
            ? Array.Empty<string>()
            : keyEntity.Scopes.Split(',', StringSplitOptions.RemoveEmptyEntries);

        // Update last used timestamp (fire and forget)
        _ = Task.Run(async () =>
        {
            try
            {
                await UpdateLastUsedAsync(keyEntity.Id, CancellationToken.None);
            }
            catch
            {
                // Ignore errors in background update
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
        var apiKey = await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.Id == apiKeyId, cancellationToken);

        if (apiKey == null)
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
            query = query.Where(k => k.IsActive && !k.RevokedAt.HasValue);
        }

        var keys = await query
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync(cancellationToken);

        return keys.Select(k => new ApiKeyInfo
        {
            Id = k.Id,
            Name = k.Name,
            Prefix = k.KeyPrefix,
            Scopes = string.IsNullOrWhiteSpace(k.Scopes)
                ? Array.Empty<string>()
                : k.Scopes.Split(',', StringSplitOptions.RemoveEmptyEntries),
            IsActive = k.IsActive,
            ExpiresAt = k.ExpiresAt,
            LastUsedAt = k.LastUsedAt,
            CreatedAt = k.CreatedAt,
            RevokedAt = k.RevokedAt,
            RevocationReason = k.RevocationReason
        }).ToList();
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

        // TODO: Implement actual usage tracking from audit logs or separate usage table
        // For now, return basic statistics
        return new ApiKeyUsageStatistics
        {
            ApiKeyId = apiKeyId,
            TotalRequests = 0,
            SuccessfulRequests = 0,
            FailedRequests = 0,
            From = from,
            To = to,
            LastUsedAt = apiKey?.LastUsedAt
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

    /// <summary>
    /// Generates cryptographically secure random bytes.
    /// </summary>
    private static byte[] GenerateRandomBytes(int length)
    {
        var bytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return bytes;
    }

    /// <summary>
    /// Encodes bytes to base62 string.
    /// </summary>
    private static string EncodeBase62(byte[] data)
    {
        var builder = new StringBuilder();
        var num = new System.Numerics.BigInteger(data.Concat(new byte[] { 0 }).ToArray());

        while (num > 0)
        {
            var remainder = (int)(num % 62);
            builder.Insert(0, Base62Chars[remainder]);
            num /= 62;
        }

        return builder.Length > 0 ? builder.ToString() : "0";
    }

    /// <summary>
    /// Hashes an API key using SHA-256.
    /// </summary>
    private static string HashApiKey(string apiKey)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(apiKey);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
