# ADR 009: Identity Vault Refactoring Strategy

**Status:** Accepted  
**Date:** 2026-01-28

> **ULTRA MISER MODE™ Engineering**: OAuth 2.0 flows are free. PKCE is free. Device Authorization Flow is free. The only thing that costs money is paying someone else to manage your tokens—so we built our own vault.

---

## Context

The initial identity management implementation suffered from scattered, provider-specific authentication handlers (`AntigravityAuthManager` for Google, ad-hoc GitHub token handling). Each provider required custom code paths, making it difficult to:

1. **Add New Providers:** Each integration was a snowflake
2. **Test Reliably:** Logic was entangled with HTTP clients and file I/O
3. **Secure Credentials:** Tokens were stored in plain JSON files
4. **Handle Token Refresh:** No unified background refresh mechanism

As Synaxis expanded to support GitHub Copilot, Google Gemini, and future providers (Azure, AWS), the codebase needed a **unified, secure, strategy-based identity system** that could scale without duplicating authentication logic.

---

## Decision

We have refactored authentication into a **Synaxis Identity Vault**—a unified, plugin-based system with encrypted storage and automatic token refresh.

### Core Architecture

#### 1. Unified Identity Model

```csharp
// src/InferenceGateway/Infrastructure/Identity/Core/IdentityAccount.cs
public class IdentityAccount
{
    public string Id { get; set; }                  // Unique identifier
    public string Provider { get; set; }            // "github" | "google"
    public string Email { get; set; }               // User email (if available)
    public string AccessToken { get; set; }         // OAuth access token
    public string? RefreshToken { get; set; }       // OAuth refresh token (optional)
    public DateTime? ExpiresAt { get; set; }        // Token expiration
    public Dictionary<string, string> Properties { get; set; } // Provider metadata
}
```

**Benefits:**
- Single model for all providers (no provider-specific classes)
- Extensible via `Properties` dictionary (e.g., GitHub org memberships, Google project IDs)
- Serializable for persistence

#### 2. Strategy Pattern for Authentication

```csharp
// src/InferenceGateway/Infrastructure/Identity/Core/IAuthStrategy.cs
public interface IAuthStrategy
{
    string ProviderName { get; }
    
    /// <summary>
    /// Initiates the OAuth flow (returns URL or device code)
    /// </summary>
    Task<AuthResult> InitiateFlowAsync(CancellationToken ct);
    
    /// <summary>
    /// Completes the OAuth flow (exchanges code for tokens)
    /// </summary>
    Task<AuthResult> CompleteFlowAsync(string code, string state, CancellationToken ct);
    
    /// <summary>
    /// Refreshes an expired access token
    /// </summary>
    Task<TokenResponse> RefreshTokenAsync(IdentityAccount account, CancellationToken ct);
}
```

**Implementations:**

| Provider | Strategy Class | Flow Type |
|----------|----------------|-----------|
| GitHub | `GitHubAuthStrategy` | Device Authorization Flow (RFC 8628) |
| Google | `GoogleAuthStrategy` | PKCE Web Flow (RFC 7636) |
| Azure (future) | `AzureAuthStrategy` | OAuth 2.0 Authorization Code |

#### 3. Encrypted Storage Layer

```csharp
// src/InferenceGateway/Infrastructure/Identity/Core/ISecureTokenStore.cs
public interface ISecureTokenStore
{
    Task<IReadOnlyList<IdentityAccount>> GetAllAccountsAsync();
    Task SaveAccountAsync(IdentityAccount account);
    Task DeleteAccountAsync(string accountId);
}

// Implementation: EncryptedFileTokenStore
public class EncryptedFileTokenStore : ISecureTokenStore
{
    private readonly IDataProtectionProvider _protectionProvider;
    private readonly string _filePath;

    public async Task SaveAccountAsync(IdentityAccount account)
    {
        var accounts = await GetAllAccountsAsync();
        accounts.Add(account);
        
        var json = JsonSerializer.Serialize(accounts);
        var protector = _protectionProvider.CreateProtector("IdentityVault");
        var encrypted = protector.Protect(json);
        
        await File.WriteAllTextAsync(_filePath, encrypted);
    }
}
```

**Security Features:**
- Uses ASP.NET Core `IDataProtectionProvider` for encryption
- Key derivation tied to machine/user (not portable, but secure)
- Protects against plaintext token leaks in file system

#### 4. Centralized Identity Manager

```csharp
// src/InferenceGateway/Infrastructure/Identity/Core/IdentityManager.cs
public class IdentityManager
{
    private readonly ISecureTokenStore _tokenStore;
    private readonly IEnumerable<IAuthStrategy> _strategies;
    private readonly ILogger<IdentityManager> _logger;

    public async Task<AuthResult> LoginAsync(string providerName)
    {
        var strategy = _strategies.FirstOrDefault(s => s.ProviderName == providerName);
        if (strategy == null)
            throw new InvalidOperationException($"Provider '{providerName}' not found");

        return await strategy.InitiateFlowAsync(CancellationToken.None);
    }

    public async Task<IdentityAccount> GetAccountAsync(string providerName)
    {
        var accounts = await _tokenStore.GetAllAccountsAsync();
        return accounts.FirstOrDefault(a => a.Provider == providerName);
    }

    // Background service for token refresh
    public async Task RefreshExpiredTokensAsync(CancellationToken ct)
    {
        var accounts = await _tokenStore.GetAllAccountsAsync();
        var expiredAccounts = accounts
            .Where(a => a.ExpiresAt.HasValue && a.ExpiresAt.Value <= DateTime.UtcNow.AddMinutes(5))
            .ToList();

        foreach (var account in expiredAccounts)
        {
            var strategy = _strategies.FirstOrDefault(s => s.ProviderName == account.Provider);
            if (strategy == null)
                continue;

            try
            {
                var newToken = await strategy.RefreshTokenAsync(account, ct);
                account.AccessToken = newToken.AccessToken;
                account.ExpiresAt = DateTime.UtcNow.AddSeconds(newToken.ExpiresIn);
                await _tokenStore.SaveAccountAsync(account);
                
                _logger.LogInformation("Refreshed token for {Provider}", account.Provider);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh token for {Provider}", account.Provider);
            }
        }
    }
}
```

**Features:**
- Automatic background token refresh via `BackgroundService`
- Provider-agnostic account lookup
- Centralized error handling and logging

---

## Provider Implementations

### A. GitHub Strategy (Device Authorization Flow)

```csharp
// src/InferenceGateway/Infrastructure/Identity/Strategies/GitHubAuthStrategy.cs
public class GitHubAuthStrategy : IAuthStrategy
{
    public string ProviderName => "github";
    
    private const string ClientId = "178c6fc778ccc68e1d6a"; // Official GitHub CLI client
    private const string Scopes = "repo read:org copilot";

    public async Task<AuthResult> InitiateFlowAsync(CancellationToken ct)
    {
        // Step 1: Request device code
        var response = await _httpClient.PostAsync(
            "https://github.com/login/device/code",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = ClientId,
                ["scope"] = Scopes
            }), ct);

        var data = await response.Content.ReadFromJsonAsync<DeviceCodeResponse>(ct);
        
        return new AuthResult
        {
            DeviceCode = data.DeviceCode,
            UserCode = data.UserCode,
            VerificationUri = data.VerificationUri,
            ExpiresIn = data.ExpiresIn,
            Interval = data.Interval
        };
    }

    public async Task<AuthResult> CompleteFlowAsync(string deviceCode, string state, CancellationToken ct)
    {
        // Step 2: Poll for access token
        var tokenResponse = await PollForTokenAsync(deviceCode, ct);
        
        // Step 3: Fetch user info
        var userInfo = await GetUserInfoAsync(tokenResponse.AccessToken, ct);
        
        // Step 4: Save to Identity Vault
        var account = new IdentityAccount
        {
            Id = Guid.NewGuid().ToString(),
            Provider = ProviderName,
            Email = userInfo.Email,
            AccessToken = tokenResponse.AccessToken,
            ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn)
        };
        
        await _tokenStore.SaveAccountAsync(account);
        
        // Step 5: Dual-write to ~/.config/gh/hosts.yml (for Copilot SDK)
        await _ghConfigWriter.WriteTokenAsync(tokenResponse.AccessToken);
        
        return new AuthResult { Success = true, Account = account };
    }
}
```

**Dual-Write Rationale:**
- GitHub Copilot SDK reads from `~/.config/gh/hosts.yml`
- Identity Vault maintains encrypted JSON for unified management
- Both sources stay synchronized

### B. Google Strategy (PKCE Web Flow)

```csharp
// src/InferenceGateway/Infrastructure/Identity/Strategies/GoogleAuthStrategy.cs
public class GoogleAuthStrategy : IAuthStrategy
{
    public string ProviderName => "google";

    public async Task<AuthResult> InitiateFlowAsync(CancellationToken ct)
    {
        // Step 1: Generate PKCE challenge
        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = GenerateCodeChallenge(codeVerifier);
        
        // Store verifier in temp cache (Redis or in-memory)
        await _cache.SetAsync($"pkce:{state}", codeVerifier, TimeSpan.FromMinutes(10));

        // Step 2: Generate authorization URL
        var authUrl = $"https://accounts.google.com/o/oauth2/v2/auth?" +
            $"client_id={_clientId}&" +
            $"redirect_uri={_redirectUri}&" +
            $"response_type=code&" +
            $"scope=openid email profile https://www.googleapis.com/auth/cloud-platform&" +
            $"code_challenge={codeChallenge}&" +
            $"code_challenge_method=S256&" +
            $"state={state}";

        return new AuthResult { AuthorizationUrl = authUrl };
    }

    public async Task<AuthResult> CompleteFlowAsync(string code, string state, CancellationToken ct)
    {
        // Step 1: Retrieve PKCE verifier
        var codeVerifier = await _cache.GetAsync($"pkce:{state}");
        
        // Step 2: Exchange code for tokens
        var tokenResponse = await ExchangeCodeForTokenAsync(code, codeVerifier, ct);
        
        // Step 3: Post-login hook: Resolve Google Cloud ProjectId
        var projectId = await ResolveProjectIdAsync(tokenResponse.AccessToken, ct);
        
        var account = new IdentityAccount
        {
            Id = Guid.NewGuid().ToString(),
            Provider = ProviderName,
            Email = tokenResponse.Email,
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = tokenResponse.RefreshToken,
            ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn),
            Properties = new Dictionary<string, string>
            {
                ["ProjectId"] = projectId
            }
        };
        
        await _tokenStore.SaveAccountAsync(account);
        
        return new AuthResult { Success = true, Account = account };
    }
}
```

**Legacy Compatibility:**
- `TransientIdentitySource` reads `ANTIGRAVITY_REFRESH_TOKEN` env var
- Automatically migrates old tokens to new vault on first startup

---

## Implementation Phases

### Phase 1: Core Infrastructure ✅
- Define `IdentityAccount` model
- Implement `ISecureTokenStore` with encryption
- Create `IdentityManager` service

### Phase 2: GitHub Implementation ✅
- Implement `GitHubAuthStrategy`
- Implement `DeviceFlowService` (polling mechanism)
- Create `GhConfigWriter` for YAML dual-write

### Phase 3: Google Migration ✅
- Refactor `AntigravityAuthManager` into `GoogleAuthStrategy`
- Implement PKCE flow with code verifier caching
- Add post-login project ID resolution

### Phase 4: API & Integration ✅
- Create `IdentityEndpoints` (Minimal API for login/logout)
- Register services in `InfrastructureExtensions`
- Add background service for token refresh

---

## Testing Strategy

### Unit Tests (>80% Coverage Target)

```csharp
// Synaxis.InferenceGateway.Tests/Identity/EncryptedFileTokenStoreTests.cs
public class EncryptedFileTokenStoreTests
{
    [Fact]
    public async Task SaveAccount_EncryptsTokens()
    {
        // Arrange
        var store = CreateStore();
        var account = new IdentityAccount
        {
            Id = "test-1",
            Provider = "github",
            AccessToken = "gho_secret123"
        };

        // Act
        await store.SaveAccountAsync(account);

        // Assert
        var fileContent = await File.ReadAllTextAsync(_filePath);
        Assert.DoesNotContain("gho_secret123", fileContent); // Token is encrypted
    }
}

// GitHubAuthStrategyTests.cs
public class GitHubAuthStrategyTests
{
    [Fact]
    public async Task InitiateFlow_ReturnsDeviceCode()
    {
        // Arrange
        var httpClient = CreateMockHttpClient();
        var strategy = new GitHubAuthStrategy(httpClient);

        // Act
        var result = await strategy.InitiateFlowAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result.DeviceCode);
        Assert.NotNull(result.UserCode);
        Assert.NotNull(result.VerificationUri);
    }
}
```

### Integration Tests

```csharp
// Manual CLI verification flow
$ dotnet run -- identity login github
✓ Device code generated
→ Visit https://github.com/login/device
→ Enter code: ABCD-1234
✓ Polling for authorization...
✓ Authorized! Token saved to vault
✓ GitHub Copilot configuration updated

$ dotnet run -- identity list
github    user@example.com    Expires: 2026-02-28 12:00 UTC
google    user@example.com    Expires: 2026-03-01 14:30 UTC
```

---

## Consequences

### Positive

- **Unified Codebase:** All providers use the same interfaces and patterns
- **Secure by Default:** Tokens are encrypted at rest using ASP.NET Core Data Protection
- **Extensible:** New providers (Azure, AWS) can be added without touching core logic
- **Automatic Refresh:** Background service keeps tokens valid without user intervention
- **Testable:** Strategy pattern enables easy mocking and unit testing

### Negative

- **Complexity:** More abstraction layers compared to simple HTTP calls
- **Migration Effort:** Existing tokens need migration to new vault format
- **Local Encryption:** Data protection keys are machine-specific (not portable across machines)

### Mitigations

- **Legacy Adapter:** `TransientIdentitySource` provides backward compatibility
- **Export/Import:** CLI commands to export vault to JSON (for machine migration)
- **Clear Documentation:** ADR and inline code comments explain architecture

---

## Related Decisions

- [ADR-003: Authentication Architecture](./003-authentication-architecture.md) — JWT and OAuth integration with CQRS
- [ADR-011: Ultra-Miser Mode](./011-ultra-miser-mode.md) — Free provider strategy requiring secure credential management

---

## Evidence

- **Archived Plan:** `docs/archive/2026/01/28/docs_archive/2026-02-02-pre-refactor/plan/plan1-20260128-identity-refactor.md`
- **Related Commits:** Identity vault implementation with GitHub and Google strategies
- **Implementation:** `src/InferenceGateway/Infrastructure/Identity/`

---

> *"The best security is the kind you build yourself—at least then you know where the backdoors are."* — ULTRA MISER MODE™ Principle #14
