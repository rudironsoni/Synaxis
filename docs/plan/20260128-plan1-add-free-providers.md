# Plan: Add Free Tier Providers (GitHub Copilot, DuckDuckGo, AI Horde)

**Date:** 2026-01-28
**Status:** Completed
**Objective:** Integrate three "miser-approved" AI providers to expand Synaxis's free tier capabilities.

## 1. Provider Implementations

### A. GitHub Copilot (Official SDK)
**Goal:** Access GPT-4o/Claude-3.5 quality models using the official SDK.
*   **Implementation:** `src/InferenceGateway/Infrastructure/External/GitHub/CopilotSdkClient.cs`
*   **Dependency:** `GitHub.Copilot.SDK` (NuGet).
*   **Architecture:** Wraps the `CopilotClient` which communicates with the local `copilot` CLI agent.
*   **Auth:** Relies on environment/CLI authentication.
*   **Features:** Streaming support, Message mapping.

### B. DuckDuckGo AI (Stealth)
**Goal:** Free access to `gpt-4o-mini`, `claude-3-haiku`, `llama-3.1-70b`.
*   **Implementation:** `src/InferenceGateway/Infrastructure/External/DuckDuckGo/DuckDuckGoChatClient.cs`
*   **Architecture:** Reverse-engineered HTTP client.
*   **Key Logic:**
    *   `GET /status` for initial `x-vqd-4` token.
    *   Token rotation on every response.
    *   Header management (User-Agent, Origin).

### C. AI Horde (Community/Swarm)
**Goal:** Distributed, uncensored community compute.
*   **Implementation:** `src/InferenceGateway/Infrastructure/External/AiHorde/AiHordeChatClient.cs`
*   **Architecture:** Async Polling Client.
*   **Key Logic:**
    *   Submit Job -> Receive Job ID.
    *   Poll Status loop (simulating synchronous `GetResponseAsync`).
    *   Fallback for streaming (yield when chunks available or wait for completion).
    *   Default Key: `0000000000`.

## 2. Infrastructure & Integration

### A. Dependency Injection
*   **File:** `src/InferenceGateway/Infrastructure/Extensions/CustomProviderExtensions.cs`
*   **Methods:**
    *   `AddGitHubCopilotSdk()`
    *   `AddDuckDuckGo()`
    *   `AddAiHorde()`

### B. Configuration
*   **File:** `src/InferenceGateway/WebApi/appsettings.json`
*   **Updates:** Add sections for `GitHubCopilot`, `DuckDuckGo`, `AiHorde`.

## 3. Quality Assurance (Test Coverage > 80%)

*   **Unit Tests:**
    *   Mock `HttpClient` / `HttpMessageHandler` for DuckDuckGo and Horde.
    *   Mock `CopilotClient` or wrap it to test logic without spawning processes if possible, or use integration tests.
*   **Verification:**
    *   Build success.
    *   Runtime verification of service resolution.

## 4. Execution Steps
1.  Add Dependencies (`GitHub.Copilot.SDK`).
2.  Create Folder Structures.
3.  Implement `CopilotSdkClient` + Tests.
4.  Implement `DuckDuckGoChatClient` + Tests.
5.  Implement `AiHordeChatClient` + Tests.
6.  Register Services & Update Config.
7.  Final Build & Verification.
