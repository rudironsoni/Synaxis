# Antigravity Provider Implementation Plan

## Objective
Implement a robust, upstream-compatible `AntigravityChatClient` for Synaxis/AgentFramework that connects to Google's Unified Gateway API. It will use `HttpClient` for transport and standard `Microsoft.Extensions.AI` abstractions.

## Architecture

### 1. `AntigravityChatClient` (The Core)
A clean, reusable client library that can be PR'd to `microsoft/agent-framework`.
*   **Location**: `src/InferenceGateway/Infrastructure/AntigravityChatClient.cs`
*   **Interfaces**: `IChatClient`.
*   **Dependencies**: `HttpClient`, `Func<CancellationToken, Task<string>>` (for auth token).
*   **Responsibilities**:
    *   **Protocol**: Request/Response mapping to Antigravity's `{ "project": ..., "request": ... }` format.
    *   **Streaming**: SSE parser for `data: JSON` lines.
    *   **Headers**: Sets strict headers (`User-Agent`, `X-Goog-Api-Client`).
    *   **Features**:
        *   Maps `ChatRole` -> `user`/`model`.
        *   Maps `ChatOptions.Tools` -> Antigravity Tools (future).
        *   Maps `ChatOptions.AdditionalProperties["Thinking"]` -> `thinkingConfig`.

### 2. Authentication Strategy (The Bridge)
To support the user's specific `opencode-antigravity-auth` workflow while keeping the Client clean, we will implement the auth loading logic in the **Application Layer**, not the Client.

*   **`AntigravityAuthService`**:
    *   Located in `src/InferenceGateway/Infrastructure/Auth/`.
    *   **Priority 1**: Check `SynaxisConfiguration.CredentialFile`.
    *   **Priority 2**: Check `~/.config/opencode/antigravity-accounts.json` (The plugin's storage).
    *   **Priority 3**: Fallback to `GoogleCredential.GetApplicationDefaultAsync()`.
    *   **Token Refresh**: If using the plugin's refresh token, use `Google.Apis.Auth.OAuth2.UserCredential` with the known Google Cloud SDK Client ID to refresh the access token transparently.

### 3. Configuration (`SynaxisConfiguration.cs`)
*   Add `ProjectId` (required).
*   Add `CredentialFile` (optional override).

### 4. Integration (`ApplicationExtensions.cs`)
*   Register `AntigravityChatClient` as a keyed service.
*   Inject the `AntigravityAuthService` as the token provider.

## Implementation Details

### Request Wrapper
```csharp
var requestBody = new
{
    project = _projectId,
    model = _modelId,
    request = new
    {
        contents = MapMessages(messages),
        systemInstruction = ExtractSystemMessage(messages),
        generationConfig = MapConfig(options)
    }
};
```

### Auth Logic (Pseudo-code)
```csharp
if (File.Exists(pluginConfigPath)) {
    var config = JsonNode.Parse(File.ReadAllText(pluginConfigPath));
    var refreshToken = config["accounts"][0]["refreshToken"].ToString();
    // Use standard Google Client ID for "gcloud"
    var clientId = "764086051850-6qs53kkk1141151..."; 
    return new UserCredential(..., refreshToken).Token.AccessToken;
}
```

## "Upstream" Readiness
*   The `AntigravityChatClient` will rely *only* on standard .NET types and `IChatClient`.
*   It will accept a generic token provider delegate, making it agnostic to *how* the token was obtained (file, env var, or magic plugin).
*   This makes it perfectly suitable for inclusion in `agent-framework`.

## Next Steps
1.  Update `SynaxisConfiguration`.
2.  Implement `AntigravityChatClient` (Clean).
3.  Implement `AntigravityTokenProvider` (The bridge).
4.  Register in `ApplicationExtensions`.
