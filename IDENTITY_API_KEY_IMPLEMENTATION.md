# ASP.NET Core Identity Custom Stores and API Key System - Implementation Summary

## Overview

This implementation provides a complete ASP.NET Core Identity system with custom stores, API key authentication, and hierarchical configuration resolution for the Synaxis inference gateway.

## Files Created

### Part 1: Custom Identity Stores

#### Infrastructure Layer
1. **`src/InferenceGateway/Infrastructure/Identity/SynaxisUserStore.cs`**
   - Custom UserStore extending ASP.NET Core Identity
   - Features:
     - `FindByEmailInOrganizationAsync()` - Find users scoped to organization
     - `GetOrganizationsAsync()` - Get all user's organizations
     - Overrides FindById, FindByEmail, FindByName to respect soft delete
     - Filters users where `DeletedAt IS NULL`

2. **`src/InferenceGateway/Infrastructure/Identity/SynaxisRoleStore.cs`**
   - Custom RoleStore for system and organization-specific roles
   - Features:
     - `GetRolesByOrganizationAsync()` - Get roles for organization
     - `GetSystemRolesAsync()` - Get global system roles
     - `GetOrganizationSpecificRolesAsync()` - Get org-specific roles
     - `FindByNameInOrganizationAsync()` - Find role with org context

#### Application Layer
3. **`src/InferenceGateway/Application/Identity/Models/IdentityModels.cs`**
   - DTOs for identity operations:
     - `RegisterRequest` - User registration with optional org
     - `LoginRequest` - Authentication request
     - `AuthenticationResponse` - JWT tokens and user info
     - `UserInfo` - User details with organizations
     - `OrganizationInfo` - Organization summary
     - `RegistrationResult` - Registration outcome

4. **`src/InferenceGateway/Application/Identity/IIdentityService.cs`**
   - Interface defining identity operations

5. **`src/InferenceGateway/Application/Identity/IdentityService.cs`**
   - Complete identity business logic implementation
   - Features:
     - `RegisterUserAsync()` - Register user only
     - `RegisterOrganizationAsync()` - Register user + org + default group
     - `LoginAsync()` - Authenticate and issue JWT
     - `RefreshTokenAsync()` - Token refresh (placeholder)
     - `GetUserInfoAsync()` - Get user details
     - `AssignUserToOrganizationAsync()` - Create membership
     - `AssignUserToGroupAsync()` - Add user to group
   - Transactional org creation with default group
   - JWT generation with organization context

#### API Layer
6. **`src/InferenceGateway/WebApi/Endpoints/IdentityEndpoints.cs`**
   - REST API endpoints:
     - `POST /identity/register` - Register user with organization
     - `POST /identity/login` - Login and get tokens
     - `POST /identity/refresh` - Refresh access token
     - `GET /identity/me` - Get current user info
     - `GET /identity/organizations` - List user's organizations
     - `POST /identity/organizations/{id}/switch` - Switch org context

### Part 2: API Key System

#### Application Layer
7. **`src/InferenceGateway/Application/ApiKeys/Models/ApiKeyModels.cs`**
   - DTOs for API key operations:
     - `GenerateApiKeyRequest` - Key generation parameters
     - `GenerateApiKeyResponse` - Full key (returned once!)
     - `ApiKeyValidationResult` - Validation outcome
     - `ApiKeyInfo` - Key metadata (no secret)
     - `ApiKeyUsageStatistics` - Usage metrics

8. **`src/InferenceGateway/Application/ApiKeys/IApiKeyService.cs`**
   - Interface defining API key operations

9. **`src/InferenceGateway/Application/ApiKeys/ApiKeyService.cs`**
   - Complete API key management implementation
   - Features:
     - `GenerateApiKeyAsync()` - Create key with format `synaxis_build_{base62}_{base62}`
     - `ValidateApiKeyAsync()` - Verify key and return org context
     - `RevokeApiKeyAsync()` - Disable key with reason
     - `ListApiKeysAsync()` - List keys (active or all)
     - `GetApiKeyUsageAsync()` - Get usage stats (placeholder)
     - `UpdateLastUsedAsync()` - Track last usage
   - SHA-256 hashing (keys never stored plaintext)
   - Base62 encoding for compact representation
   - Expiration and revocation support
   - Automatic last-used tracking

#### Infrastructure Layer
10. **`src/InferenceGateway/Infrastructure/Middleware/ApiKeyMiddleware.cs`**
    - Authentication middleware for API keys
    - Features:
      - Checks `Authorization: Bearer synaxis_build_...` header
      - Validates key and sets HttpContext.Items
      - Creates ClaimsPrincipal for authorization
      - Returns 401 for invalid keys
    - Extension method: `UseApiKeyAuthentication()`

#### API Layer
11. **`src/InferenceGateway/WebApi/Endpoints/ApiKeyEndpoints.cs`**
    - REST API endpoints:
      - `POST /apikeys` - Generate new API key
      - `GET /apikeys` - List keys (with optional includeRevoked)
      - `DELETE /apikeys/{id}` - Revoke key
      - `GET /apikeys/{id}/usage` - Get usage statistics

### Part 3: Configuration Resolution

#### Application Layer
12. **`src/InferenceGateway/Application/Configuration/Models/ConfigurationModels.cs`**
    - DTOs for configuration:
      - `ConfigurationSetting<T>` - Generic setting with source
      - `RateLimitConfiguration` - RPM/TPM limits
      - `CostConfiguration` - Model pricing

13. **`src/InferenceGateway/Application/Configuration/IConfigurationResolver.cs`**
    - Interface for hierarchical configuration resolution

14. **`src/InferenceGateway/Application/Configuration/ConfigurationResolver.cs`**
    - Hierarchical configuration resolution service
    - Features:
      - `GetSettingAsync<T>()` - Generic setting lookup (placeholder)
      - `GetRateLimitsAsync()` - Resolve rate limits
        - Order: UserMembership → Group → OrganizationSettings → Global
      - `GetEffectiveCostPer1MTokensAsync()` - Resolve model costs
        - Order: OrganizationModel → OrganizationProvider → Provider → Default
      - `ShouldAutoOptimizeAsync()` - Check auto-optimization flag
        - Order: UserMembership → Group → OrganizationSettings → Global (true)

### Part 4: Unit Tests (>80% coverage target)

#### Application Tests
15. **`tests/InferenceGateway/Application.Tests/Identity/IdentityServiceTests.cs`**
    - Tests for IdentityService
    - Coverage:
      - Organization registration flow
      - Duplicate email handling
      - User-to-organization assignment
      - User-to-group assignment
    - Uses in-memory database and mocked UserManager/SignInManager

16. **`tests/InferenceGateway/Application.Tests/ApiKeys/ApiKeyServiceTests.cs`**
    - Tests for ApiKeyService
    - Coverage:
      - Key generation with correct format
      - Hash storage (not plaintext)
      - Valid key validation
      - Invalid key rejection
      - Revoked key rejection
      - Key revocation
      - List active vs all keys
    - Uses in-memory database

17. **`tests/InferenceGateway/Application.Tests/Configuration/ConfigurationResolverTests.cs`**
    - Tests for ConfigurationResolver
    - Coverage:
      - Rate limit hierarchy (user → group → org → global)
      - Cost resolution hierarchy (model → provider → default)
      - Auto-optimization flag hierarchy
      - Default fallback values
    - Uses in-memory database

#### Infrastructure Tests
18. **`tests/InferenceGateway/Infrastructure.Tests/Identity/SynaxisUserStoreTests.cs`**
    - Tests for SynaxisUserStore
    - Coverage:
      - Find user by email in organization
      - Soft delete filtering
      - Get user's organizations
      - FindById with soft delete
    - Uses in-memory database

## Key Features

### Security
- ✅ API keys never stored as plaintext (SHA-256 hashed)
- ✅ API keys returned only once at generation
- ✅ JWT authentication with organization context
- ✅ Soft delete support (queries filter `DeletedAt IS NULL`)
- ✅ API key expiration and revocation

### Architecture
- ✅ All services use interfaces (testable, mockable)
- ✅ Async/await throughout
- ✅ Comprehensive XML documentation
- ✅ Clean separation: Infrastructure → Application → API
- ✅ In-memory database for unit tests

### Multi-Tenancy
- ✅ Organization-scoped authentication
- ✅ Hierarchical configuration resolution
- ✅ User can belong to multiple organizations
- ✅ Organization-specific and system roles
- ✅ Group-based permissions within orgs

### API Key Format
```
synaxis_build_{keyId_base62}_{keySecret_base62}
Example: synaxis_build_7N42Dg9SKqTMy4a_kL9Xp2VzRtYuE8W
```
- Prefix for easy identification
- Base62 encoding (URL-safe, compact)
- 16 bytes of entropy per component (128-bit security)

### Configuration Hierarchy
Rate limits, costs, and feature flags resolve in order:
1. **User** - UserOrganizationMembership settings
2. **Group** - Primary group settings
3. **Organization** - OrganizationSettings
4. **Global** - System defaults

## Usage Examples

### Register User with Organization
```csharp
var request = new RegisterRequest
{
    Email = "user@example.com",
    Password = "SecurePassword123!",
    FirstName = "John",
    LastName = "Doe",
    OrganizationName = "Acme Corp",
    OrganizationSlug = "acme-corp"
};

var result = await identityService.RegisterOrganizationAsync(request);
```

### Generate API Key
```csharp
var request = new GenerateApiKeyRequest
{
    OrganizationId = orgId,
    Name = "Production API Key",
    Scopes = new[] { "inference:read", "inference:write" },
    RateLimitRpm = 1000
};

var response = await apiKeyService.GenerateApiKeyAsync(request);
// response.ApiKey = "synaxis_build_..." (only shown once!)
```

### Validate API Key (in middleware)
```csharp
var result = await apiKeyService.ValidateApiKeyAsync(apiKey);
if (result.IsValid)
{
    context.Items["OrganizationId"] = result.OrganizationId;
    context.Items["ApiKeyScopes"] = result.Scopes;
}
```

### Resolve Configuration
```csharp
// Get effective rate limits
var limits = await configResolver.GetRateLimitsAsync(userId, orgId);
// Returns user's limit, or group's, or org's, or global default

// Get model cost
var cost = await configResolver.GetEffectiveCostPer1MTokensAsync(
    orgId, providerId, modelId);
// Returns org override, or provider default

// Check if auto-optimization enabled
var shouldOptimize = await configResolver.ShouldAutoOptimizeAsync(
    userId, orgId);
```

## Integration Steps

### 1. Register Services (Startup/Program.cs)
```csharp
// Identity stores
builder.Services.AddScoped<IUserStore<SynaxisUser>, SynaxisUserStore>();
builder.Services.AddScoped<IRoleStore<Role>, SynaxisRoleStore>();

// Application services
builder.Services.AddScoped<IIdentityService, IdentityService>();
builder.Services.AddScoped<IApiKeyService, ApiKeyService>();
builder.Services.AddScoped<IConfigurationResolver, ConfigurationResolver>();

// Add Identity
builder.Services.AddIdentity<SynaxisUser, Role>()
    .AddEntityFrameworkStores<SynaxisDbContext>()
    .AddUserStore<SynaxisUserStore>()
    .AddRoleStore<SynaxisRoleStore>();

// Add JWT authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]))
    };
});
```

### 2. Configure Middleware Pipeline
```csharp
app.UseAuthentication();
app.UseApiKeyAuthentication(); // Custom middleware
app.UseAuthorization();
```

### 3. Configure appsettings.json
```json
{
  "Jwt": {
    "Secret": "your-secret-key-here-min-32-chars-long!",
    "Issuer": "Synaxis",
    "Audience": "Synaxis",
    "ExpirationMinutes": 60
  }
}
```

## Testing

Run unit tests:
```bash
dotnet test tests/InferenceGateway/Application.Tests
dotnet test tests/InferenceGateway/Infrastructure.Tests
```

Expected coverage: >80% for all services

## Notes

1. **Refresh Token Implementation**: The refresh token validation is stubbed and needs completion with token storage (database table or distributed cache).

2. **API Key Usage Tracking**: The `GetApiKeyUsageAsync` method returns placeholder data. Implement by querying audit logs or creating a separate usage tracking table.

3. **Generic Settings**: The `GetSettingAsync<T>` method is a placeholder. Implement by adding a Settings table or JSONB columns to relevant entities.

4. **Organization Switching**: The switch organization endpoint requires JWT regeneration. Consider implementing a token refresh flow that updates the organization claim.

5. **Rate Limiting Enforcement**: The configuration resolver provides rate limit values, but actual enforcement needs integration with a rate limiting middleware (e.g., AspNetCoreRateLimit).

## Future Enhancements

- [ ] Email verification flow
- [ ] Password reset flow
- [ ] MFA support
- [ ] API key scopes enforcement
- [ ] OAuth2/OIDC provider support
- [ ] Audit logging for identity operations
- [ ] Role-based access control (RBAC) middleware
- [ ] Refresh token rotation
- [ ] API key usage analytics dashboard
