# Plan: Security & Quality Remediation

**Date:** 2026-01-27
**Status:** Approved
**Goal:** Remediate critical security risks, complete stubbed implementations, and address technical debt identified in the Code Audit.

## 1. Critical Remediation (Security & Correctness)

### 1.1. Secure Hardcoded Secrets
*   **Target:** `src/InferenceGateway/Infrastructure/Auth/AntigravityAuthManager.cs`
*   **Action:** Remove `ClientId` and `ClientSecret` constants. Inject `IOptions<AntigravitySettings>` (new config class).
*   **Config:** Update `appsettings.json` to include `Antigravity:ClientId` and `Antigravity:ClientSecret`.

### 1.2. Enforce Cryptographic Key Configuration
*   **Target:** `src/InferenceGateway/Infrastructure/Security/AesGcmTokenVault.cs`, `src/InferenceGateway/Infrastructure/Security/JwtService.cs`
*   **Action:** Remove "SynaxisDefault..." fallback strings. Throw `InvalidOperationException` if keys are missing in configuration.
*   **Safe-guard:** Ensure `appsettings.Development.json` has valid (dev-only) keys so local dev doesn't break.

### 1.3. Fix Swallowed Exceptions
*   **Target:** `src/InferenceGateway/WebApi/Helpers/OpenAIRequestParser.cs`
*   **Action:** Remove empty `catch`. Log deserialization errors. Allow critical `JsonException` to bubble or return 400 Bad Request explicitly with details.

## 2. Completeness (Feature Gaps)

### 2.1. Cohere V2 Streaming
*   **Target:** `src/InferenceGateway/Infrastructure/CohereChatClient.cs`
*   **Action:** Implement `GetStreamingResponseAsync` using Cohere's V2 SSE protocol.
*   **Details:** Parse `stream-start`, `text-generation`, `stream-end` events.

### 2.2. Cloudflare Strategy
*   **Target:** `src/InferenceGateway/Infrastructure/ChatClients/Strategies/CloudflareStrategy.cs`
*   **Action:** Verify if Cloudflare needs specific handling (e.g. system prompt structure) and implement it, or confirm generic handling is sufficient and remove "Placeholder" comment.

## 3. Refactoring (Tech Debt)

### 3.1. Centralize HTTP Configuration
*   **Target:** `src/InferenceGateway/Infrastructure/AntigravityChatClient.cs`
*   **Action:** Move BaseURL and User-Agent configuration to `InfrastructureExtensions.cs` using Named HttpClient ("Antigravity"). Remove magic strings from the client class.

## 4. Verification Plan

1.  **Build**: Ensure clean build.
2.  **Unit Tests**: Run existing tests. Add new tests for:
    *   Configuration validation (ensure app throws if keys missing).
    *   `OpenAIRequestParser` error logging.
3.  **Manual Verification**:
    *   Run `test_all_models.sh` to ensure no regression.
    *   Specifically test Cohere streaming.

## 5. Execution Order
1.  Config & Secrets (Critical).
2.  Error Handling (Critical).
3.  Cohere Streaming (High).
4.  Refactoring.
