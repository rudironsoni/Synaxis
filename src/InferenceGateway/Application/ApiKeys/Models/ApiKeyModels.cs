namespace Synaxis.InferenceGateway.Application.ApiKeys.Models;

/// <summary>
/// Request model for generating a new API key.
/// </summary>
public class GenerateApiKeyRequest
{
    /// <summary>
    /// Gets or sets the organization ID.
    /// </summary>
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Gets or sets the API key name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the scopes for the API key.
    /// </summary>
    public string[] Scopes { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the expiration date (optional).
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the rate limit in requests per minute (optional).
    /// </summary>
    public int? RateLimitRpm { get; set; }

    /// <summary>
    /// Gets or sets the rate limit in tokens per minute (optional).
    /// </summary>
    public int? RateLimitTpm { get; set; }

    /// <summary>
    /// Gets or sets the user ID who created this API key (optional).
    /// </summary>
    public Guid? CreatedBy { get; set; }
}

/// <summary>
/// Response model for API key generation.
/// </summary>
public class GenerateApiKeyResponse
{
    /// <summary>
    /// Gets or sets the API key ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the full API key (only returned once at creation).
    /// </summary>
    public required string ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the API key name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the API key prefix (visible for identification).
    /// </summary>
    public required string Prefix { get; set; }

    /// <summary>
    /// Gets or sets the scopes.
    /// </summary>
    public string[] Scopes { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the expiration date.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the creation date.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Response model for API key validation.
/// </summary>
public class ApiKeyValidationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the API key is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the organization ID if valid.
    /// </summary>
    public Guid? OrganizationId { get; set; }

    /// <summary>
    /// Gets or sets the API key ID if valid.
    /// </summary>
    public Guid? ApiKeyId { get; set; }

    /// <summary>
    /// Gets or sets the scopes if valid.
    /// </summary>
    public string[] Scopes { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the rate limit in requests per minute (optional).
    /// </summary>
    public int? RateLimitRpm { get; set; }

    /// <summary>
    /// Gets or sets the rate limit in tokens per minute (optional).
    /// </summary>
    public int? RateLimitTpm { get; set; }

    /// <summary>
    /// Gets or sets the error message if invalid.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Response model for listing API keys.
/// </summary>
public class ApiKeyInfo
{
    /// <summary>
    /// Gets or sets the API key ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the API key name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the API key prefix.
    /// </summary>
    public required string Prefix { get; set; }

    /// <summary>
    /// Gets or sets the scopes.
    /// </summary>
    public string[] Scopes { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets a value indicating whether the key is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the expiration date.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the last used date.
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Gets or sets the creation date.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the revocation date.
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Gets or sets the revocation reason.
    /// </summary>
    public string? RevocationReason { get; set; }
}

/// <summary>
/// Response model for API key usage statistics.
/// </summary>
public class ApiKeyUsageStatistics
{
    /// <summary>
    /// Gets or sets the API key ID.
    /// </summary>
    public Guid ApiKeyId { get; set; }

    /// <summary>
    /// Gets or sets the total number of requests.
    /// </summary>
    public long TotalRequests { get; set; }

    /// <summary>
    /// Gets or sets the total number of successful requests.
    /// </summary>
    public long SuccessfulRequests { get; set; }

    /// <summary>
    /// Gets or sets the total number of failed requests.
    /// </summary>
    public long FailedRequests { get; set; }

    /// <summary>
    /// Gets or sets the date range start.
    /// </summary>
    public DateTime From { get; set; }

    /// <summary>
    /// Gets or sets the date range end.
    /// </summary>
    public DateTime To { get; set; }

    /// <summary>
    /// Gets or sets the last used date.
    /// </summary>
    public DateTime? LastUsedAt { get; set; }
}
