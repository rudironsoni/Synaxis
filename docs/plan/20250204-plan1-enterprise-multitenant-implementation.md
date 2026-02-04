# Synaxis Enterprise Multi-Tenant Implementation Plan

**Date**: 2026-02-04
**Status**: Ready for Implementation

## Executive Summary

This plan implements an enterprise-grade multi-tenant inference gateway with ULTRA MISER MODE cost optimization, comprehensive security hardening, and 80%+ test coverage.

## Architecture Decisions

| Aspect | Decision |
|--------|----------|
| **Multi-tenancy** | Shared database with Organization isolation |
| **Identity** | ASP.NET Core Identity with custom stores |
| **Database Schemas** | 4 schemas: platform, identity, operations, audit |
| **Table Naming** | Plural (Users, Organizations, Groups) |
| **Cost Units** | Per 1M tokens |
| **API Keys** | `synaxis_build_{base62-id}` format |
| **Audit Retention** | 90 days default, configurable per organization |
| **Soft Deletes** | Cascade (Org deleted → all related soft deleted) |
| **Real-time Updates** | WebSocket |

## Phase 1: Security Hardening

### Critical Issues

#### Issue 1: JWT Secret Default Fallback
```csharp
public static void ValidateSecurityConfiguration(IConfiguration config, ILogger logger)
{
    var issues = new List<string>();
    
    var jwtSecret = config["Synaxis:JwtSecret"];
    if (string.IsNullOrEmpty(jwtSecret) || jwtSecret.Length < 32)
        issues.Add("JWT secret must be at least 32 characters");
    
    if (issues.Any())
    {
        foreach (var issue in issues)
            logger.LogCritical("Security validation failed: {Issue}", issue);
        logger.LogCritical("Synaxis cannot start with insecure configuration");
        Environment.Exit(1);
    }
}
```

#### Issue 2: Rate Limiting Enforcement
- Implement `RedisQuotaTracker.CheckQuotaAsync` with Lua scripts
- Hierarchical: User → Group → Organization

#### Issue 3: Input Validation
- FluentValidation for OpenAI requests
- Return 400 for malformed JSON

#### Issue 4: CORS Configuration
- PublicApi (open)
- AdminUi (credentials)
- TenantSpecific (DB-driven)

#### Issue 5: Security Headers
- HSTS, CSP, X-Frame-Options, X-Content-Type-Options

## Phase 2: Database Schema

### Schema: platform (Tenant-Agnostic)

```sql
CREATE SCHEMA platform;

CREATE TABLE platform.Providers (
    Id UUID PRIMARY KEY,
    Key VARCHAR(100) UNIQUE NOT NULL,
    DisplayName VARCHAR(255) NOT NULL,
    ProviderType VARCHAR(50) NOT NULL,
    BaseEndpoint VARCHAR(500) NOT NULL,
    DefaultApiKeyEnvironmentVariable VARCHAR(100),
    SupportsStreaming BOOLEAN NOT NULL DEFAULT TRUE,
    SupportsTools BOOLEAN NOT NULL DEFAULT FALSE,
    SupportsVision BOOLEAN NOT NULL DEFAULT FALSE,
    DefaultInputCostPer1MTokens DECIMAL(12,6),
    DefaultOutputCostPer1MTokens DECIMAL(12,6),
    IsFreeTier BOOLEAN NOT NULL DEFAULT FALSE,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    IsPublic BOOLEAN NOT NULL DEFAULT TRUE,
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UpdatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE platform.Models (
    Id UUID PRIMARY KEY,
    ProviderId UUID NOT NULL REFERENCES platform.Providers(Id),
    CanonicalId VARCHAR(255) UNIQUE NOT NULL,
    DisplayName VARCHAR(255) NOT NULL,
    Description TEXT,
    ContextWindowTokens INT,
    MaxOutputTokens INT,
    SupportsStreaming BOOLEAN NOT NULL DEFAULT TRUE,
    SupportsTools BOOLEAN NOT NULL DEFAULT FALSE,
    SupportsVision BOOLEAN NOT NULL DEFAULT FALSE,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    IsPublic BOOLEAN NOT NULL DEFAULT TRUE,
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UpdatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### Schema: identity (Multi-Tenant)

```sql
CREATE SCHEMA identity;

CREATE TABLE identity.Organizations (
    Id UUID PRIMARY KEY,
    LegalName VARCHAR(255) NOT NULL,
    DisplayName VARCHAR(255) NOT NULL,
    Slug VARCHAR(100) UNIQUE NOT NULL,
    RegistrationNumber VARCHAR(100),
    TaxId VARCHAR(100),
    LegalAddress TEXT,
    PrimaryContactEmail VARCHAR(255) NOT NULL,
    BillingEmail VARCHAR(255),
    SupportEmail VARCHAR(255),
    PhoneNumber VARCHAR(50),
    Industry VARCHAR(100),
    CompanySize VARCHAR(50),
    WebsiteUrl VARCHAR(500),
    Status VARCHAR(50) NOT NULL DEFAULT 'pending',
    PlanTier VARCHAR(50) NOT NULL DEFAULT 'free',
    TrialEndsAt TIMESTAMPTZ,
    RequireMfa BOOLEAN NOT NULL DEFAULT FALSE,
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CreatedBy VARCHAR(255) NOT NULL,
    UpdatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UpdatedBy VARCHAR(255) NOT NULL,
    DeletedAt TIMESTAMPTZ,
    DeletedBy VARCHAR(255)
);

CREATE TABLE identity.OrganizationSettings (
    OrganizationId UUID PRIMARY KEY REFERENCES identity.Organizations(Id),
    JwtTokenLifetimeMinutes INT NOT NULL DEFAULT 10080,
    MaxRequestBodySizeBytes INT NOT NULL DEFAULT 31457280,
    DefaultRateLimitRpm INT NOT NULL DEFAULT 60,
    DefaultRateLimitTpm INT NOT NULL DEFAULT 100000,
    AllowAutoOptimization BOOLEAN NOT NULL DEFAULT TRUE,
    AllowCustomProviders BOOLEAN NOT NULL DEFAULT FALSE,
    AllowAuditLogExport BOOLEAN NOT NULL DEFAULT FALSE,
    MaxUsers INT NOT NULL DEFAULT 10,
    MaxGroups INT NOT NULL DEFAULT 5,
    MonthlyTokenQuota BIGINT,
    AuditLogRetentionDays INT NOT NULL DEFAULT 90,
    UpdatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UpdatedBy VARCHAR(255) NOT NULL
);

CREATE TABLE identity.Groups (
    Id UUID PRIMARY KEY,
    OrganizationId UUID NOT NULL REFERENCES identity.Organizations(Id),
    Name VARCHAR(255) NOT NULL,
    Description TEXT,
    Slug VARCHAR(100) NOT NULL,
    ParentGroupId UUID REFERENCES identity.Groups(Id),
    RateLimitRpm INT,
    RateLimitTpm INT,
    AllowAutoOptimization BOOLEAN,
    Status VARCHAR(50) NOT NULL DEFAULT 'active',
    IsDefaultGroup BOOLEAN NOT NULL DEFAULT FALSE,
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CreatedBy VARCHAR(255) NOT NULL,
    UpdatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UpdatedBy VARCHAR(255) NOT NULL,
    DeletedAt TIMESTAMPTZ
);

CREATE TABLE identity.Users (
    Id UUID PRIMARY KEY,
    UserName VARCHAR(256),
    NormalizedUserName VARCHAR(256),
    Email VARCHAR(256) NOT NULL,
    NormalizedEmail VARCHAR(256) NOT NULL,
    EmailConfirmed BOOLEAN NOT NULL DEFAULT FALSE,
    PasswordHash VARCHAR(500),
    SecurityStamp VARCHAR(100) NOT NULL DEFAULT gen_random_uuid(),
    ConcurrencyStamp VARCHAR(100) NOT NULL DEFAULT gen_random_uuid(),
    PhoneNumber VARCHAR(50),
    PhoneNumberConfirmed BOOLEAN NOT NULL DEFAULT FALSE,
    TwoFactorEnabled BOOLEAN NOT NULL DEFAULT FALSE,
    LockoutEnd TIMESTAMPTZ,
    LockoutEnabled BOOLEAN NOT NULL DEFAULT TRUE,
    AccessFailedCount INT NOT NULL DEFAULT 0,
    FirstName VARCHAR(100) NOT NULL,
    LastName VARCHAR(100) NOT NULL,
    DisplayName VARCHAR(255),
    AvatarUrl VARCHAR(500),
    MfaSecretEncrypted VARCHAR(500),
    Status VARCHAR(50) NOT NULL DEFAULT 'active',
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UpdatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    LastLoginAt TIMESTAMPTZ,
    DeletedAt TIMESTAMPTZ
);

CREATE TABLE identity.Roles (
    Id UUID PRIMARY KEY,
    Name VARCHAR(256),
    NormalizedName VARCHAR(256),
    ConcurrencyStamp VARCHAR(100),
    IsSystemRole BOOLEAN NOT NULL DEFAULT FALSE,
    OrganizationId UUID REFERENCES identity.Organizations(Id),
    Description TEXT
);

CREATE TABLE identity.UserRoles (
    UserId UUID NOT NULL REFERENCES identity.Users(Id),
    RoleId UUID NOT NULL REFERENCES identity.Roles(Id),
    OrganizationId UUID REFERENCES identity.Organizations(Id),
    PRIMARY KEY (UserId, RoleId)
);

CREATE TABLE identity.UserOrganizationMemberships (
    Id UUID PRIMARY KEY,
    UserId UUID NOT NULL REFERENCES identity.Users(Id),
    OrganizationId UUID NOT NULL REFERENCES identity.Organizations(Id),
    OrganizationRole VARCHAR(50) NOT NULL DEFAULT 'member',
    PrimaryGroupId UUID REFERENCES identity.Groups(Id),
    RateLimitRpm INT,
    RateLimitTpm INT,
    AllowAutoOptimization BOOLEAN,
    Status VARCHAR(50) NOT NULL DEFAULT 'active',
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CreatedBy VARCHAR(255) NOT NULL,
    UpdatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UpdatedBy VARCHAR(255) NOT NULL
);

CREATE TABLE identity.UserGroupMemberships (
    Id UUID PRIMARY KEY,
    UserId UUID NOT NULL REFERENCES identity.Users(Id),
    GroupId UUID NOT NULL REFERENCES identity.Groups(Id),
    GroupRole VARCHAR(50) NOT NULL DEFAULT 'member',
    IsPrimary BOOLEAN NOT NULL DEFAULT FALSE,
    JoinedAt TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CreatedBy VARCHAR(255) NOT NULL
);
```

### Schema: operations

```sql
CREATE SCHEMA operations;

CREATE TABLE operations.OrganizationProviders (
    Id UUID PRIMARY KEY,
    OrganizationId UUID NOT NULL REFERENCES identity.Organizations(Id),
    ProviderId UUID NOT NULL REFERENCES platform.Providers(Id),
    ApiKeyEncrypted VARCHAR(1000),
    CustomEndpoint VARCHAR(500),
    InputCostPer1MTokens DECIMAL(12,6),
    OutputCostPer1MTokens DECIMAL(12,6),
    SupportsStreaming BOOLEAN,
    SupportsTools BOOLEAN,
    SupportsVision BOOLEAN,
    IsEnabled BOOLEAN NOT NULL DEFAULT TRUE,
    IsDefault BOOLEAN NOT NULL DEFAULT FALSE,
    RateLimitRpm INT,
    RateLimitTpm INT,
    HealthCheckEnabled BOOLEAN NOT NULL DEFAULT TRUE,
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UpdatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE operations.OrganizationModels (
    Id UUID PRIMARY KEY,
    OrganizationId UUID NOT NULL REFERENCES identity.Organizations(Id),
    ModelId UUID NOT NULL REFERENCES platform.Models(Id),
    IsEnabled BOOLEAN NOT NULL DEFAULT TRUE,
    DisplayName VARCHAR(255),
    InputCostPer1MTokens DECIMAL(12,6),
    OutputCostPer1MTokens DECIMAL(12,6),
    CustomAlias VARCHAR(255),
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UpdatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE operations.RoutingStrategies (
    Id UUID PRIMARY KEY,
    OrganizationId UUID NOT NULL REFERENCES identity.Organizations(Id),
    Name VARCHAR(255) NOT NULL,
    Description TEXT,
    StrategyType VARCHAR(50) NOT NULL,
    PrioritizeFreeProviders BOOLEAN NOT NULL DEFAULT TRUE,
    MaxCostPer1MTokens DECIMAL(12,6),
    FallbackToPaid BOOLEAN NOT NULL DEFAULT TRUE,
    MaxLatencyMs INT,
    RequireStreaming BOOLEAN NOT NULL DEFAULT FALSE,
    MinHealthScore DECIMAL(3,2) NOT NULL DEFAULT 0.80,
    IsDefault BOOLEAN NOT NULL DEFAULT FALSE,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UpdatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE operations.ProviderHealthStatus (
    Id UUID PRIMARY KEY,
    OrganizationId UUID NOT NULL REFERENCES identity.Organizations(Id),
    OrganizationProviderId UUID NOT NULL REFERENCES operations.OrganizationProviders(Id),
    IsHealthy BOOLEAN NOT NULL DEFAULT TRUE,
    HealthScore DECIMAL(3,2) NOT NULL DEFAULT 1.00,
    LastCheckedAt TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    LastSuccessAt TIMESTAMPTZ,
    LastFailureAt TIMESTAMPTZ,
    ConsecutiveFailures INT NOT NULL DEFAULT 0,
    LastErrorMessage TEXT,
    LastErrorCode VARCHAR(100),
    AverageLatencyMs INT,
    SuccessRate DECIMAL(3,2),
    IsInCooldown BOOLEAN NOT NULL DEFAULT FALSE,
    CooldownUntil TIMESTAMPTZ,
    UpdatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE operations.ApiKeys (
    Id UUID PRIMARY KEY,
    OrganizationId UUID NOT NULL REFERENCES identity.Organizations(Id),
    Name VARCHAR(255) NOT NULL,
    KeyHash VARCHAR(500) NOT NULL,
    KeyPrefix VARCHAR(20) NOT NULL,
    ExpiresAt TIMESTAMPTZ,
    Scopes TEXT[] NOT NULL DEFAULT ARRAY['inference:read', 'inference:write'],
    RateLimitRpm INT,
    RateLimitTpm INT,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    LastUsedAt TIMESTAMPTZ,
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CreatedBy VARCHAR(255) NOT NULL,
    RevokedAt TIMESTAMPTZ,
    RevokedBy VARCHAR(255),
    RevocationReason TEXT
);
```

### Schema: audit

```sql
CREATE SCHEMA audit;

CREATE TABLE audit.Logs (
    Id UUID PRIMARY KEY,
    OrganizationId UUID REFERENCES identity.Organizations(Id),
    UserId UUID REFERENCES identity.Users(Id),
    Action VARCHAR(100) NOT NULL,
    EntityType VARCHAR(100) NOT NULL,
    EntityId UUID NOT NULL,
    PreviousValues JSONB,
    NewValues JSONB,
    IpAddress INET,
    UserAgent VARCHAR(500),
    CorrelationId UUID NOT NULL,
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PartitionDate DATE NOT NULL GENERATED ALWAYS AS (DATE(CreatedAt)) STORED
) PARTITION BY RANGE (PartitionDate);
```

## Phase 3: ASP.NET Core Identity

### Custom User

```csharp
public class SynaxisUser : IdentityUser<Guid>
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Status { get; set; } = "active";
    public DateTime? DeletedAt { get; set; }
    public virtual ICollection<UserOrganizationMembership> OrganizationMemberships { get; set; }
}
```

### Registration Flow
1. Create User
2. Create Organization
3. Create OrganizationSettings
4. Create default Group
5. Create UserOrganizationMembership (Owner)
6. Create UserGroupMembership (Admin, primary)
7. Initialize default OrganizationProviders

## Phase 4: API Key System

```csharp
public async Task<(string key, string keyHash)> GenerateApiKeyAsync(
    Guid organizationId, string name, string[] scopes)
{
    var randomBytes = new byte[32];
    using (var rng = RandomNumberGenerator.Create())
        rng.GetBytes(randomBytes);
    
    var id = Base62.Encode(randomBytes.Take(16).ToArray());
    var secret = Base62.Encode(randomBytes.Skip(16).ToArray());
    
    var fullKey = $"synaxis_build_{id}_{secret}";
    var keyHash = HashKey(fullKey);
    
    var apiKey = new ApiKey
    {
        OrganizationId = organizationId,
        KeyHash = keyHash,
        KeyPrefix = fullKey.Substring(0, 20),
        Scopes = scopes
    };
    
    await _dbContext.ApiKeys.AddAsync(apiKey);
    await _dbContext.SaveChangesAsync();
    
    return (fullKey, keyHash);
}
```

## Phase 5: Specialized Agents

| Agent | Schedule | Purpose |
|-------|----------|---------|
| RoutingAgent | Per-request | Request routing |
| HealthMonitoringAgent | Every 2 min | Provider health checks |
| CostOptimizationAgent | Every 15 min | ULTRA MISER MODE |
| ModelDiscoveryAgent | Daily 2 AM | Discover new models |
| SecurityAuditAgent | Every 6 hrs | Security audits |

### ULTRA MISER MODE Algorithm

```csharp
// Priority 1: Paid → Free (ULTRA MISER MODE $0)
// Priority 2: Paid → Cheaper Paid (>20% cheaper)
// Never: Free → Paid

private async Task<bool> ShouldAutoOptimizeAsync()
{
    var userSetting = await GetSettingAsync<bool?>("autoOptimize");
    if (userSetting.HasValue) return userSetting.Value;
    
    var groupSetting = await GetGroupSettingAsync<bool?>("autoOptimize");
    if (groupSetting.HasValue) return groupSetting.Value;
    
    var orgSetting = await GetOrgSettingAsync<bool?>("autoOptimize");
    return orgSetting ?? true;
}
```

## Phase 6: WebSocket Real-Time Updates

### WebSocket Hub

```csharp
public class SynaxisHub : Hub
{
    public async Task JoinOrganization(string organizationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, organizationId);
    }
    
    public async Task SendProviderHealthUpdate(Guid organizationId, ProviderHealthUpdate update)
    {
        await Clients.Group(organizationId.ToString())
            .SendAsync("ProviderHealthUpdated", update);
    }
}
```

## Phase 7: Testing Requirements

### Coverage Targets
- Backend: >80%
- Frontend: >80%

### Test Categories
1. Unit tests for all services
2. Integration tests for database operations
3. API endpoint tests
4. Agent behavior tests
5. WebSocket tests
6. Security tests

## Timeline

| Week | Focus |
|------|-------|
| 1 | Security Hardening + Database Schema |
| 2 | Identity System |
| 3 | API Keys + Agents |
| 4 | WebSocket + Audit System |
| 5 | Testing & Bug Fixes |
| 6 | Documentation & Final Verification |

## Success Criteria

- [ ] 0 critical security issues
- [ ] Backend test coverage >80%
- [ ] Frontend test coverage >80%
- [ ] All agents functioning
- [ ] WebSocket real-time updates working
- [ ] ULTRA MISER MODE optimizing costs
- [ ] Audit logs with 90-day retention
- [ ] `dotnet run` starts successfully

---

**Status**: Ready for Implementation
**Next Action**: Begin Phase 1 implementation
