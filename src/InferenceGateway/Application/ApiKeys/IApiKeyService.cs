using Synaxis.InferenceGateway.Application.ApiKeys.Models;

namespace Synaxis.InferenceGateway.Application.ApiKeys;

/// <summary>
/// Service for managing API keys for programmatic access.
/// </summary>
public interface IApiKeyService
{
    /// <summary>
    /// Generates a new API key for an organization.
    /// Format: synaxis_build_{base62(16bytes)}_{base62(16bytes)}
    /// The full key is only returned once at generation.
    /// </summary>
    /// <param name="request">The generation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated API key response with the full key.</returns>
    Task<GenerateApiKeyResponse> GenerateApiKeyAsync(
        GenerateApiKeyRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates an API key and returns the organization ID if valid.
    /// </summary>
    /// <param name="apiKey">The API key to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The validation result with organization ID if valid.</returns>
    Task<ApiKeyValidationResult> ValidateApiKeyAsync(
        string apiKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes an API key.
    /// </summary>
    /// <param name="apiKeyId">The API key ID to revoke.</param>
    /// <param name="reason">The reason for revocation.</param>
    /// <param name="revokedBy">The user ID who revoked the key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful, otherwise false.</returns>
    Task<bool> RevokeApiKeyAsync(
        Guid apiKeyId,
        string reason,
        Guid? revokedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all API keys for an organization.
    /// </summary>
    /// <param name="organizationId">The organization ID.</param>
    /// <param name="includeRevoked">Whether to include revoked keys.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of API key information.</returns>
    Task<IList<ApiKeyInfo>> ListApiKeysAsync(
        Guid organizationId,
        bool includeRevoked = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets usage statistics for an API key within a date range.
    /// </summary>
    /// <param name="apiKeyId">The API key ID.</param>
    /// <param name="from">The start date.</param>
    /// <param name="to">The end date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The usage statistics.</returns>
    Task<ApiKeyUsageStatistics> GetApiKeyUsageAsync(
        Guid apiKeyId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the last used timestamp for an API key.
    /// </summary>
    /// <param name="apiKeyId">The API key ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateLastUsedAsync(
        Guid apiKeyId,
        CancellationToken cancellationToken = default);
}
