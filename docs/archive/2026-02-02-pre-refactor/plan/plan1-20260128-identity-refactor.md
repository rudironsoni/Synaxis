# Plan: Synaxis Identity Vault Refactor

**Date:** 2026-01-28
**Goal:** Replace ad-hoc authentication managers with a unified, secure, strategy-based Identity System ("Synaxis Identity Vault").
**Target Coverage:** >80% Unit Test Coverage.

## 1. Architecture Overview

The new system moves from specific "Managers" to a unified `IdentityManager` that delegates to `IAuthStrategy` implementations. Storage is upgraded from plain JSON to `ISecureTokenStore` using ASP.NET Core Data Protection.

### Core Components (`src/InferenceGateway/Infrastructure/Identity/Core`)

1.  **`IdentityAccount`**
    *   Unified model for all providers.
    *   Properties: `Id`, `Provider` (github/google), `Email`, `AccessToken`, `RefreshToken`, `ExpiresAt`, `Properties` (Metadata).

2.  **`ISecureTokenStore`**
    *   Interface for persistence.
    *   Implementation: `EncryptedFileTokenStore`.
    *   Security: Uses `IDataProtectionProvider` to encrypt the underlying JSON file on disk.

3.  **`IAuthStrategy`**
    *   Interface for provider logic.
    *   Methods:
        *   `Task<AuthResult> InitiateFlowAsync(CancellationToken ct)`
        *   `Task<AuthResult> CompleteFlowAsync(string code, string state, CancellationToken ct)`
        *   `Task<TokenResponse> RefreshTokenAsync(IdentityAccount account, CancellationToken ct)`

4.  **`IdentityManager`**
    *   Central service (Singleton).
    *   Manages the list of accounts.
    *   Handles automatic background refreshing via `Microsoft.Extensions.Hosting.BackgroundService`.

## 2. Strategies (`src/InferenceGateway/Infrastructure/Identity/Strategies`)

### A. GitHub Strategy (`GitHubAuthStrategy`)
*   **Flow:** Device Authorization Flow (RFC 8628).
*   **Client ID:** `178c6fc778ccc68e1d6a` (Official GitHub CLI).
*   **Scopes:** `repo`, `read:org`, `copilot`.
*   **Dual-Write:**
    *   Saves to `ISecureTokenStore`.
    *   Saves to `~/.config/gh/hosts.yml` (YAML) for Copilot SDK compatibility.
*   **Components:**
    *   `DeviceFlowService`: Background poller for the "Device Code" flow.

### B. Google Strategy (`GoogleAuthStrategy`)
*   **Flow:** PKCE Web Flow (Ported from `AntigravityAuthManager`).
*   **Logic:**
    *   Initiates URL generation.
    *   Exchanges Code for Token.
    *   **Post-Login Hook:** Iterates `cloudcode-pa` endpoints to resolve `ProjectId`.
*   **Legacy Support:**
    *   `TransientIdentitySource`: Injects accounts from `ANTIGRAVITY_REFRESH_TOKEN` environment variable.

## 3. Implementation Phases

### Phase 1: Core Infrastructure
*   Define Interfaces.
*   Implement `EncryptedFileTokenStore` with Data Protection.
*   Implement `IdentityManager`.

### Phase 2: GitHub Implementation
*   Implement `GitHubAuthStrategy`.
*   Implement `DeviceFlowService` (Polling mechanism).
*   Implement `GhConfigWriter` (YAML handler).

### Phase 3: Google Migration
*   Refactor `AntigravityAuthManager` logic into `GoogleAuthStrategy`.
*   Ensure backward compatibility via an Adapter if necessary (or update consumers).

### Phase 4: API & Integration
*   Create `IdentityEndpoints` (Minimal API).
*   Register services in `InfrastructureExtensions`.
*   Update `Program.cs`.

## 4. Testing Strategy
*   **Project:** `Synaxis.InferenceGateway.Tests` (xUnit).
*   **Mocks:** `Mock<IHttpClientFactory>`, `Mock<IDataProtectionProvider>`, `Mock<ISecureTokenStore>`.
*   **Scenarios:**
    *   Token storage encryption/decryption roundtrip.
    *   GitHub Device Flow state transitions (Initiate -> Poll -> Success).
    *   Google PKCE URL generation and Token Exchange.
    *   IdentityManager auto-refresh logic.

## 5. Verification
*   Build Solution.
*   Run All Tests (`dotnet test`).
*   Manual CLI Verification (`dotnet run`).
