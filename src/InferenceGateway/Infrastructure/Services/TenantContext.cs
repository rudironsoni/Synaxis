using Synaxis.InferenceGateway.Application.Interfaces;

namespace Synaxis.InferenceGateway.Infrastructure.Services;

/// <summary>
/// Implementation of tenant context that stores request-scoped tenant information.
/// This is registered as scoped service and populated by TenantResolutionMiddleware.
/// </summary>
public sealed class TenantContext : ITenantContext
{
    /// <inheritdoc/>
    public Guid? OrganizationId { get; private set; }

    /// <inheritdoc/>
    public Guid? UserId { get; private set; }

    /// <inheritdoc/>
    public Guid? ApiKeyId { get; private set; }

    /// <inheritdoc/>
    public bool IsApiKeyAuthenticated => ApiKeyId.HasValue;

    /// <inheritdoc/>
    public bool IsJwtAuthenticated => UserId.HasValue;

    /// <inheritdoc/>
    public string[] Scopes { get; private set; } = Array.Empty<string>();

    /// <inheritdoc/>
    public int? RateLimitRpm { get; private set; }

    /// <inheritdoc/>
    public int? RateLimitTpm { get; private set; }

    /// <inheritdoc/>
    public void SetApiKeyContext(Guid organizationId, Guid apiKeyId, string[] scopes, int? rateLimitRpm, int? rateLimitTpm)
    {
        OrganizationId = organizationId;
        ApiKeyId = apiKeyId;
        Scopes = scopes ?? Array.Empty<string>();
        RateLimitRpm = rateLimitRpm;
        RateLimitTpm = rateLimitTpm;
        UserId = null; // Clear JWT-specific data
    }

    /// <inheritdoc/>
    public void SetJwtContext(Guid organizationId, Guid userId, string[] scopes)
    {
        OrganizationId = organizationId;
        UserId = userId;
        Scopes = scopes ?? Array.Empty<string>();
        ApiKeyId = null; // Clear API key-specific data
        RateLimitRpm = null;
        RateLimitTpm = null;
    }
}
