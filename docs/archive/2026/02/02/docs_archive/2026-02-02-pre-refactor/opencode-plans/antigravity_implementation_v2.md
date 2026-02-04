# Antigravity Provider Implementation Plan (Revised)

## Objective
Implement a production-grade, upstream-ready `AntigravityChatClient` and a robust Authentication/Quota subsystem. The goal is to match the `opencode-antigravity-auth` plugin's feature set (Headless Auth, Token Management, Quota Handling) while remaining a clean, independent C# library suitable for `microsoft/agent-framework`.

## 1. Authentication Subsystem (`Synaxis.Infrastructure.Auth`)

We will build a standalone Auth Module that mimics the plugin's behavior but using standard .NET patterns.

### Components
*   **`AntigravityAuthManager`**: The central coordinator.
*   **`TokenStorage`**: Securely persists tokens.
    *   Path: `~/.config/opencode/antigravity-accounts.json` (for compatibility) or `~/.synaxis/antigravity.json`.
    *   Encryption: Use `System.Security.Cryptography.ProtectedData` (Windows) or file ACLs (Linux/Mac).
*   **`GoogleDeviceAuthenticator`**: Handles the Headless Flow.
    *   Generates Auth URL (`https://accounts.google.com/o/oauth2/auth...`).
    *   Prints to Console (or logs).
    *   Waits for user input (Redirect URL or Auth Code) via `Console.ReadLine` (if interactive) or a temporary local HTTP listener (if possible/configured).
    *   Exchanges code for Tokens.
*   **`TokenRefresher`**: Automatically refreshes expired access tokens using the saved Refresh Token.

### Workflow
1.  **Startup**: Check if valid token exists.
2.  **If Missing**: Log "Antigravity Auth Required".
    *   If Interactive (CLI): Trigger `GoogleDeviceAuthenticator`.
    *   If Service: Block/Fail until config is provided.

## 2. Antigravity Client (`Synaxis.Infrastructure.Antigravity`)

A custom `IChatClient` implementation is chosen over hacking the `Google.GenAI` SDK.
*Reason*: Antigravity's wire protocol (Request Wrapper, Response Envelope, Custom Headers, different SSE format) is sufficiently different that wrapping the SDK via `DelegatingHandler` introduces excessive complexity (double-serialization) and brittleness. A clean `HttpClient` implementation ensures strict adherence to the spec.

### Features
*   **Protocol Compliance**: Correctly implements the `{ "project": ..., "request": ... }` wrapper and `antigravity` user-agent headers.
*   **Streaming**: Custom SSE parser for `data: { "response": ... }` format.
*   **Resilience**:
    *   **Polly** policies for Retries (Exponential Backoff).
    *   **429 Handling**: Respects `retryDelay` from error details.
*   **Thinking**: Maps `ExtensionData["thinking"]` to `generationConfig.thinkingConfig`.

## 3. Configuration & Integration

*   **`SynaxisConfiguration`**: Add `Antigravity` section.
*   **`ProviderRegistry`**: Register the new client.

## 4. Upstream Compatibility
The `AntigravityChatClient` and `AntigravityAuthManager` will be decoupled. The Client just needs an `IAccessTokenProvider`. This makes the *Client* logic portable to `agent-framework`, while the *Auth* logic (which is app-specific) remains in the application layer.

## Execution Steps
1.  **Dependencies**: Add `Polly` (for retries) if not present.
2.  **Configuration**: Update `SynaxisConfiguration.cs`.
3.  **Auth Implementation**: Create `AntigravityAuthManager.cs`.
4.  **Client Implementation**: Create `AntigravityChatClient.cs`.
5.  **Integration**: Update `ApplicationExtensions.cs`.
