# Antigravity Comprehensive Test Plan

## Objective
Verify the implementation of Antigravity provider (Client + Auth) and ensure no regressions in the rest of the application.

## 1. Unit Tests (Infrastructure)

### A. `AntigravityChatClientTests` (Existing)
*   Verifies Request Mapping (Wrappers, Headers, System Instructions).
*   Verifies Response Mapping (Candidates, Content).
*   Verifies Streaming (SSE Parsing).

### B. `AntigravityAuthManagerTests` (New)
*   **Location**: `tests/InferenceGateway/Infrastructure.Tests/AntigravityAuthManagerTests.cs`
*   **Scope**:
    *   **Load Balancing**: Verify `GetTokenAsync` cycles through accounts.
    *   **Persistence**: Verify `SaveAccounts` writes correctly and `LoadAccounts` reads correctly.
    *   **Email Injection**: Verify `ANTIGRAVITY_REFRESH_TOKEN` env var injects a transient account.
    *   **ListAccounts**: Verify it returns the safe `AccountInfo` projections.

## 2. Integration Tests (WebApi)

### A. `AntigravityEndpointTests` (New)
*   **Location**: `tests/InferenceGateway/IntegrationTests/AntigravityEndpointTests.cs`
*   **Scope**:
    *   `GET /antigravity/accounts`: Verify it returns 200 OK and an empty list (initially).
    *   `POST /antigravity/auth/start`: Verify it returns a valid Google Auth URL.
    *   `POST /antigravity/auth/complete`: Verify it handles invalid codes gracefully (we can't easily test valid codes without a real mock server for Google).

## 3. Regression Testing
*   Run `dotnet test` on the entire solution to ensure `GatewayIntegrationTests` and others still pass.

## Execution Steps
1.  **Write** `AntigravityAuthManagerTests.cs`.
2.  **Write** `AntigravityEndpointTests.cs`.
3.  **Execute** `dotnet test` and analyze results.
