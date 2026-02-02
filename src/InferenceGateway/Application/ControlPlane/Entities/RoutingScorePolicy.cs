namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities;

/// <summary>
/// Base record for routing score configuration policies across the hierarchy.
/// Supports 3-level precedence: Global → Tenant → User.
/// Undefined fields inherit from parent level.
/// </summary>
public abstract record RoutingScorePolicyBase(
    string Id,
    string OwnerType,  // "Global", "Tenant", or "User"
    string? OwnerId     // null for Global, tenantId for Tenant, userId for User
);

/// <summary>
/// Global routing score configuration - system-wide default.
/// All tenants inherit from this unless overridden.
/// </summary>
public record GlobalRoutingScorePolicy(
    string Id,
    RoutingScoreConfiguration Configuration
) : RoutingScorePolicyBase(Id, "Global", null);

/// <summary>
/// Tenant-level routing score configuration overrides.
/// Merges with global config; only specified fields override.
/// </summary>
public record TenantRoutingScorePolicy(
    string Id,
    string TenantId,
    RoutingScorePolicyOverrides Overrides
) : RoutingScorePolicyBase(Id, "Tenant", TenantId);

/// <summary>
/// User-level routing score configuration overrides.
/// Merges with tenant config (which merges with global).
/// Only specified fields override.
/// </summary>
public record UserRoutingScorePolicy(
    string Id,
    string UserId,
    RoutingScorePolicyOverrides Overrides
) : RoutingScorePolicyBase(Id, "User", UserId);

/// <summary>
/// Overrides for routing score configuration.
/// Nullable properties allow partial overrides (inherit from parent).
/// </summary>
public record RoutingScorePolicyOverrides(
    RoutingScoreWeights? Weights = null,
    int? FreeTierBonusPoints = null,
    double? MinScoreThreshold = null,
    bool? PreferFreeByDefault = null
);

/// <summary>
/// Weighted scoring factors for intelligent routing.
/// All weights must sum to approximately 1.0 for consistent scoring.
/// </summary>
public record RoutingScoreWeights(
    double? QualityScoreWeight = 0.3,        // 30% weight for quality
    double? QuotaRemainingWeight = 0.3,      // 30% weight for remaining quota
    double? RateLimitSafetyWeight = 0.2,     // 20% weight for rate limit safety
    double? LatencyScoreWeight = 0.2         // 20% weight for latency
);

/// <summary>
/// Configuration for routing score calculation.
/// </summary>
public record RoutingScoreConfiguration(
    RoutingScoreWeights Weights,
    int FreeTierBonusPoints = 50,           // Bonus points for free providers
    double MinScoreThreshold = 0.0,
    bool PreferFreeByDefault = true
);
