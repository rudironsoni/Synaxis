# Antigravity Multi-Account & API Plan

## Objective
Enable multiple Google accounts for the Antigravity provider with Load Balancing, and expose API endpoints for managing these accounts.

## 1. Refactor `AntigravityAuthManager`

### Storage Format Change
The `antigravity-auth.json` file will now store a **List** of account objects, including the email.
```json
[
  { 
    "Email": "user1@example.com",
    "Token": { "access_token": "...", "refresh_token": "...", ... } 
  },
  { 
    "Email": "user2@example.com",
    "Token": { "access_token": "...", "refresh_token": "...", ... } 
  }
]
```

### Load Balancing
*   Maintain a `List<AntigravityAccount> _accounts`.
*   Use an `int _requestCount` and `Interlocked.Increment` to implement Round-Robin selection in `GetTokenAsync`.
*   If a token is expired, refresh it in-place using the associated refresh token.

### Scopes & User Info
*   Update scopes to include `https://www.googleapis.com/auth/userinfo.email`.
*   After authentication, call the Google UserInfo API to retrieve the email address before saving the account.

### New Methods (Interface `IAntigravityAuthManager`)
*   `IEnumerable<AccountInfo> ListAccounts()`: Returns emails and status.
*   `string StartAuthFlow(string redirectUrl)`: Returns the Auth URL.
*   `Task CompleteAuthFlowAsync(string code, string redirectUrl)`: Exchanges code, fetches email, adds account, saves.

## 4. Persistence & Docker Strategy

To ensure credentials persist across container recreations and work seamlessly in IDE/CLI:

### Path Resolution Logic
1.  **Configuration Override**: Check `ProviderConfig.AuthStoragePath`.
    *   In Docker, set this via Env Var: `Synaxis__Providers__Antigravity__AuthStoragePath=/app/data/antigravity-auth.json`.
2.  **Default Fallback**: `Path.Combine(Environment.GetFolderPath(SpecialFolder.UserProfile), ".synaxis", "antigravity-auth.json")`.
    *   Works in IDE: `C:\Users\Me\.synaxis\` or `/home/me/.synaxis/`.
    *   Works in Docker (Default): `/root/.synaxis/`.

### Docker Volume Setup
We will rely on standard Docker volumes mapped to the configured path.
```yaml
services:
  synaxis:
    image: synaxis:latest
    environment:
      - Synaxis__Providers__Antigravity__AuthStoragePath=/data/auth.json
    volumes:
      - synaxis_auth_data:/data
volumes:
  synaxis_auth_data:
```

## 5. Implementation Steps

1.  **Define Interface**: Create `IAntigravityAuthManager` extending `ITokenProvider`.
2.  **Update Manager**: Refactor `AntigravityAuthManager`:
    *   Implement `List<Account>` storage.
    *   Add Email fetching (Google UserInfo API).
    *   Implement Round-Robin load balancing.
3.  **Create Endpoints**: Implement the Minimal API endpoints.
4.  **Register**: Update `Program.cs` to map the new endpoints.
