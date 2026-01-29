# Antigravity Provider Implementation Plan (Final)

## Objective
Implement a "Distinguished Engineer" quality integration of the Google Antigravity Gateway into Synaxis.
**Key Goals**:
1.  **Standalone Operation**: No dependency on external plugins; self-contained Auth logic.
2.  **Headless Authentication**: Full support for the "Copy URL -> Paste Code" flow for server/SSH environments.
3.  **Upstream Compatibility**: The core `AntigravityChatClient` must be a pristine `IChatClient` implementation suitable for `microsoft/agent-framework`.
4.  **Resilience**: Robust retry logic (Polly) and quota management matching the reference implementation.

---

## 1. Core Architecture

### A. The Client (`AntigravityChatClient`)
*   **Location**: `src/InferenceGateway/Infrastructure/AntigravityChatClient.cs`
*   **Design**: Pure `IChatClient`. Zero knowledge of "how" auth happens, only that it gets a token.
*   **Dependencies**: `HttpClient`, `ITokenProvider` (Interface we will define).
*   **Features**:
    *   **Protocol**: Implements the `{ project, model, request: { ... } }` envelope.
    *   **Headers**: Enforces `User-Agent: antigravity/1.11.5...`.
    *   **Streaming**: Custom SSE parser for `data: { response: ... }`.
    *   **Thinking**: Maps `ExtensionData["thinking"]` to `generationConfig.thinkingConfig`.

### B. The Auth System (`AntigravityAuthManager`)
*   **Location**: `src/InferenceGateway/Infrastructure/Auth/AntigravityAuthManager.cs`
*   **Responsibility**: Managing the OAuth 2.0 lifecycle for a headless application.
*   **Storage**: Persists credentials to `~/.synaxis/antigravity-auth.json` (encrypted where possible, or restricted permissions).
*   **Flow**:
    1.  **Load**: Try loading valid Access Token.
    2.  **Refresh**: If expired, use Refresh Token to get new Access Token.
    3.  **Login (Headless)**:
        *   Generate Auth URL with `cloud-platform` scope.
        *   Display: *"Please visit: [URL]"*
        *   Prompt: *"Paste the redirect URL or code here:"*
        *   Exchange code for tokens.
        *   Save to storage.

### C. Resilience (Polly)
*   **Policy**:
    *   **429 (Too Many Requests)**: Exponential Backoff + Jitter.
    *   **Specific Header**: If API returns `Retry-After` or specific details, respect it.
    *   **Timeout**: Aggressive timeouts for hung connections.

### D. Docker / Container Support
For headless environments where interactive console is not available (e.g., Docker, Kubernetes), we support passing the Refresh Token via environment variables.

*   **Env Var**: `ANTIGRAVITY_REFRESH_TOKEN`
*   **Logic**:
    1.  If `ANTIGRAVITY_REFRESH_TOKEN` is present, `AntigravityAuthManager` creates a synthetic `TokenResponse` with this refresh token and an expired access token.
    2.  The next call to `GetTokenAsync` detects the expired access token and immediately triggers `RefreshTokenAsync`.
    3.  `RefreshTokenAsync` exchanges the provided refresh token for a valid access token using the Google Auth library.
*   **Workflow**:
    1.  Run locally once to authenticate and generate `~/.synaxis/antigravity-auth.json`.
    2.  Extract the `refresh_token` from this file.
    3.  Set `ANTIGRAVITY_REFRESH_TOKEN` in your Docker container (e.g., via `docker run -e ANTIGRAVITY_REFRESH_TOKEN=...`).

---

## 2. Implementation Steps

### Step 1: Configuration
Update `SynaxisConfiguration.cs`:
```csharp
public class ProviderConfig {
    // ... existing ...
    public string? ProjectId { get; set; }      // Required for Antigravity
    public string? AuthStoragePath { get; set; } // Optional override
}
```

### Step 2: Infrastructure Setup
1.  **Add Packages**:
    *   `Google.Apis.Auth` (for OIDC/OAuth helpers).
    *   `Microsoft.Extensions.Http.Polly` (for resilience).
    *   **Note**: `Microsoft.Extensions.Http.Resilience` is preferred in .NET 8+.
2.  **Create Interfaces**:
    *   `src/InferenceGateway/Application/Auth/ITokenProvider.cs`

### Step 3: Auth Implementation
Create `src/InferenceGateway/Infrastructure/Auth/AntigravityAuthManager.cs`.
*   Implement `GetTokenAsync(CancellationToken)`.
*   Implement `InteractiveLoginAsync()`.

### Step 4: Client Implementation
Create `src/InferenceGateway/Infrastructure/AntigravityChatClient.cs`.
*   **Strict DTOs**: internal classes for `AntigravityRequest`, `AntigravityResponse`, `Candidate`, `Part`.
*   **Mapper**: Transform `IList<ChatMessage>` to Antigravity `Content`.
    *   *Note*: `System` messages become `systemInstruction` field, NOT content parts.

### Step 5: Integration
Update `ApplicationExtensions.cs`:
1.  Register `AntigravityAuthManager` as a Singleton.
2.  Register `AntigravityChatClient` via `AddKeyedSingleton`.
3.  Configure `HttpClient` with Polly policies.

---

## 3. Verification Plan
1.  **Build**: Ensure clean compile.
2.  **Unit Tests**: Mock `HttpClient` to verify JSON serialization matches spec exactly (especially the wrapper object).
3.  **Manual Test**: Run the app in a terminal, trigger the Auth flow, and verify it prints the URL and accepts the code.
