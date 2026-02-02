# Plan: Code Quality, Safety, and Refactoring

**Date:** 2026-01-28
**Status:** Approved
**Goal:** Address technical debt, safety risks, and architectural fragility identified in the Code Audit.

## 1. Safety & Robustness (DoS Prevention)

### 1.1. Unbounded Request Buffering
*   **Target:** `src/InferenceGateway/WebApi/Helpers/OpenAIRequestParser.cs`
*   **Issue:** Reads full body without limit.
*   **Action:**
    *   Check `context.Request.ContentLength`.
    *   Throw `BadHttpRequestException` if content exceeds 10MB (reasonable for text chat/embeddings).
    *   Ensure `EnableBuffering` is safe.

## 2. Cleanup & Security (Hardcoded Secrets)

### 2.1. Remove Hardcoded Fallback Keys
*   **Target:** `src/InferenceGateway/Infrastructure/Security/AesGcmTokenVault.cs`
*   **Target:** `src/InferenceGateway/Infrastructure/Security/JwtService.cs`
*   **Action:** Remove the "SynaxisDefault..." fallback strings entirely. Logic should strictly throw `InvalidOperationException` if the config value is missing or empty.

## 3. Architecture & Refactoring (Tech Debt)

### 3.1. Refactor God Class (`AntigravityAuthManager`)
*   **Target:** `src/InferenceGateway/Infrastructure/Auth/AntigravityAuthManager.cs`
*   **Action:** Extract persistence logic into a dedicated `FileTokenStore`.
    *   **New Class:** `src/InferenceGateway/Infrastructure/Auth/FileTokenStore.cs` (Implements `ITokenStore`).
    *   **Responsibility:** Load/Save `List<AntigravityAccount>` to JSON file.
    *   **Refactor:** Inject `ITokenStore` into `AntigravityAuthManager`.

### 3.2. Fix Legacy Completions Fragility
*   **Target:** `src/InferenceGateway/WebApi/Endpoints/OpenAI/LegacyCompletionsEndpoint.cs`
*   **Action:** Improve the "Prompt" field parsing.
    *   Instead of raw `JsonElement` manipulation in the endpoint, ensure robustness.
    *   Extract the parsing logic to a helper or DTO property `GetPromptText()` to centralize the array-vs-string logic and handle errors gracefully.

## 4. Testing Strategy

*   **Unit Tests:**
    *   `OpenAIRequestParserTests`: Verify large payloads throw.
    *   `FileTokenStoreTests`: Verify read/write isolation.
    *   `AntigravityAuthManagerTests`: Update to mock `ITokenStore`.
*   **Verification:**
    *   Build successfully.
    *   Run `dotnet test` (ensure >80% coverage).
    *   Run `test_all_models.sh` to ensure no regression in routing.

## 5. Execution Order
1.  Safety (Parser).
2.  Cleanup (Keys).
3.  Refactor (Auth Manager).
4.  Refactor (Legacy Endpoint).
5.  Tests & Verify.
