using System.Collections.Generic;

namespace Synaxis.InferenceGateway.Application.Configuration;

public class SynaxisConfiguration
{
    public Dictionary<string, ProviderConfig> Providers { get; set; } = new();
    public List<CanonicalModelConfig> CanonicalModels { get; set; } = new();
    public Dictionary<string, AliasConfig> Aliases { get; set; } = new();
    public string? MasterKey { get; set; }
    public string? JwtSecret { get; set; }
    public string? JwtIssuer { get; set; }
    public string? JwtAudience { get; set; }
    public AntigravitySettings? Antigravity { get; set; }
    // Maximum allowed request body size (bytes) for parsing incoming OpenAI-compatible requests.
    // Default set to 30 MB (31457280 bytes).
    public long MaxRequestBodySize { get; set; } = 31457280;
}

public class ProviderConfig
{
    /// <summary>
    /// Indicates whether this provider is enabled for routing requests.
    /// Disabled providers are excluded from all routing calculations.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// API key for authentication with the provider.
    /// Required for most providers except those using custom authentication methods.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Account ID for Cloudflare Workers AI authentication.
    /// Required for Cloudflare provider type.
    /// </summary>
    public string? AccountId { get; set; }

    /// <summary>
    /// Project ID for Antigravity provider authentication.
    /// Required for Antigravity provider type.
    /// </summary>
    public string? ProjectId { get; set; }

    /// <summary>
    /// File path to store authentication tokens for Antigravity provider.
    /// Optional - if not provided, tokens are stored in memory only.
    /// </summary>
    public string? AuthStoragePath { get; set; }

    /// <summary>
    /// Priority tier for this provider (0 = highest priority).
    /// Lower tier numbers are tried first during failover.
    /// Providers within the same tier are load-balanced.
    /// </summary>
    public int Tier { get; set; }

    /// <summary>
    /// List of model IDs supported by this provider.
    /// Used for model routing and availability checks.
    /// </summary>
    public List<string> Models { get; set; } = new();

    /// <summary>
    /// Provider type identifier (e.g., "OpenAI", "Groq", "Cohere", "Cloudflare").
    /// Determines the client implementation and request format.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Custom API endpoint URL to override the default provider endpoint.
    /// Optional - if not provided, uses the provider's default endpoint.
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Fallback endpoint URL to use if the primary endpoint fails.
    /// Optional - provides redundancy for critical providers.
    /// </summary>
    public string? FallbackEndpoint { get; set; }

    /// <summary>
    /// Rate limit for requests per minute (RPM).
    /// Used for intelligent routing to avoid hitting provider limits.
    /// Optional - if not set, no RPM limit is enforced.
    /// </summary>
    public int? RateLimitRPM { get; set; }

    /// <summary>
    /// Rate limit for tokens per minute (TPM).
    /// Used for intelligent routing to avoid hitting provider limits.
    /// Optional - if not set, no TPM limit is enforced.
    /// </summary>
    public int? RateLimitTPM { get; set; }

    /// <summary>
    /// Indicates if this provider offers free tier access.
    /// Free providers are prioritized by default in Ultra Miser Mode.
    /// </summary>
    public bool IsFree { get; set; } = false;

    /// <summary>
    /// Custom HTTP headers to send with each API request.
    /// Required by some providers like GitHub Models for authentication.
    /// </summary>
    public Dictionary<string, string>? CustomHeaders { get; set; }

    /// <summary>
    /// Quality score (1-10) for this provider.
    /// Higher scores indicate better model quality/response accuracy.
    /// Used in intelligent routing calculation.
    /// Default: 5 (average quality).
    /// </summary>
    public int QualityScore { get; set; } = 5;

    /// <summary>
    /// Estimated quota remaining as percentage (0-100).
    /// Used for intelligent routing to prefer providers with higher remaining quota.
    /// Default: 100 (full quota).
    /// Updated dynamically by health monitoring jobs.
    /// </summary>
    public int EstimatedQuotaRemaining { get; set; } = 100;

    /// <summary>
    /// Average latency in milliseconds from recent health checks.
    /// Used in intelligent routing to prefer faster providers.
    /// Updated dynamically by health monitoring jobs.
    /// </summary>
    public int? AverageLatencyMs { get; set; }
}

public class CanonicalModelConfig
{
    public string Id { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string ModelPath { get; set; } = string.Empty;
    public bool Streaming { get; set; }
    public bool Tools { get; set; }
    public bool Vision { get; set; }
    public bool StructuredOutput { get; set; }
    public bool LogProbs { get; set; }
}

public class AliasConfig
{
    public List<string> Candidates { get; set; } = new();
}
