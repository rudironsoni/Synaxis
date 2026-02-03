# Plan: Integrate GitHub Copilot via Microsoft Agent Framework

**Date:** 2026-01-28
**Status:** Approved
**Objective:** Integrate `GithubCopilotAgent` by porting the source code from `microsoft/agent-framework` and wrapping it in an `IChatClient` adapter. Update Docker environment to support the underlying SDK requirements.

## 1. Environment (Dockerfile)
*   **File:** `src/InferenceGateway/WebApi/Dockerfile`
*   **Action:** Install `gh` CLI, `nodejs`, `npm`, and `@githubnext/github-copilot-cli` in the final image.

## 2. Infrastructure (Dependencies & Porting)
*   **Packages:**
    *   `GitHub.Copilot.SDK`
    *   `Microsoft.Agents.AI.Abstractions`
*   **Ported Source (External/MicrosoftAgents/GithubCopilot):**
    *   `GithubCopilotAgent.cs`
    *   `GithubCopilotAgentSession.cs`
    *   `GithubCopilotJsonUtilities.cs`
    *   `CopilotClientExtensions.cs`
    *   *Namespace adjustments*: `Synaxis.InferenceGateway.Infrastructure.External.MicrosoftAgents.GithubCopilot`

## 3. Adapter Implementation
*   **File:** `src/InferenceGateway/Infrastructure/External/GitHub/GithubCopilotAgentClient.cs`
*   **Role:** `IChatClient` implementation that delegates to `GithubCopilotAgent`.
*   **Logic:**
    *   Initialize `CopilotClient` and `GithubCopilotAgent`.
    *   Map `IEnumerable<ChatMessage>` to Agent input.
    *   Stream responses via `RunCoreStreamingAsync`.
    *   Handle `GetStreamingResponseAsync` mapping `AgentResponseUpdate` to `ChatResponseUpdate`.

## 4. Integration
*   **Extension:** `src/InferenceGateway/Infrastructure/Extensions/CustomProviderExtensions.cs`
    *   Register `CopilotClient` (Singleton).
    *   Register `GithubCopilotAgent` (Transient).
    *   Register `GithubCopilotAgentClient` (Keyed Scoped: "GitHubCopilot").
*   **Config:** `src/InferenceGateway/WebApi/appsettings.json` (Verify "GitHubCopilot" section).

## 5. Verification
*   **Unit Tests:**
    *   `tests/InferenceGateway/Infrastructure.Tests/External/GitHub/GithubCopilotAgentClientTests.cs`
    *   Mocking: Mock `CopilotClient` (if possible) or abstract the Agent interaction.
*   **Build & Run:** Verify clean build and startup.
