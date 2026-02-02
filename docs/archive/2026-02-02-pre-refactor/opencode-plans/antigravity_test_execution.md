# Antigravity Comprehensive Test Execution Plan

## Objective
Verify the implementation of Antigravity provider (Client + Auth + API) via rigorous automated testing.

## 1. Unit Tests (Infrastructure)

### A. `AntigravityChatClientTests`
*   **Location**: `tests/InferenceGateway/Infrastructure.Tests/AntigravityChatClientTests.cs`
*   **Coverage**:
    *   `GetResponseAsync`: Verifies strict Protocol Compliance (Wrapper Object, Headers, System Instructions).
    *   `GetStreamingResponseAsync`: Verifies custom SSE parser handles `data: { response: ... }` correctly.
    *   **Tools**: `Moq` for `HttpClient` and `ITokenProvider`.

### B. `AntigravityAuthManagerTests`
*   **Location**: `tests/InferenceGateway/Infrastructure.Tests/AntigravityAuthManagerTests.cs`
*   **Coverage**:
    *   `ListAccounts`: Verifies loading from disk.
    *   `GetTokenAsync (Env Var)`: Verifies Docker/Headless injection of `ANTIGRAVITY_REFRESH_TOKEN`.
    *   `GetTokenAsync (Round Robin)`: Verifies load balancing logic across multiple accounts.
    *   **Tools**: Temporary files for persistence testing.

## 2. Integration Tests (WebApi)

### A. `AntigravityEndpointTests`
*   **Location**: `tests/InferenceGateway/IntegrationTests/AntigravityEndpointTests.cs`
*   **Coverage**:
    *   `GET /antigravity/accounts`: Verifies endpoint reachability and serialization.
    *   `POST /antigravity/auth/start`: Verifies URL generation.
    *   `POST /antigravity/auth/complete`: Verifies error handling for invalid codes.
    *   **Tools**: `SynaxisWebApplicationFactory` for in-memory server testing.

## Execution Steps
1.  **Create** `AntigravityChatClientTests.cs`.
2.  **Create** `AntigravityAuthManagerTests.cs`.
3.  **Create** `AntigravityEndpointTests.cs`.
4.  **Run** `dotnet test` to validate all new and existing tests.
