# Plan: Stream-Native CQRS with Mediator and Microsoft Agent Framework

**Date:** 2026-01-26
**Status:** Approved
**Goal:** Transition Synaxis to a high-availability, stream-native CQRS architecture using `Mediator` and the Microsoft Agent Framework.

## 1. Executive Summary

This plan outlines the re-architecture of the Inference Gateway to support robust high availability and strictly typed streaming pipelines. We will transition from a service-chained architecture to a Command Query Responsibility Segregation (CQRS) model using `martinothamar/Mediator`. The `RoutingAgent` will remain the core domain orchestrator, while `SmartRoutingChatClient` will handle infrastructure resilience (provider rotation, circuit breaking).

## 2. Architectural Stack

| Layer | Component | Responsibility |
| :--- | :--- | :--- |
| **API** | `MapPost("/v1/chat/completions")` | Entry point. Deserializes `OpenAIRequest`. Dispatches to Mediator based on `stream` flag. |
| **CQRS** | `Mediator` (martinothamar) | Request Pipeline. Separates "What" (Command) from "How" (Handler). Optimized with Source Generators. |
| **Domain** | `RoutingAgent` (MS Agent Framework) | **First-Class Citizen.** Orchestrates the flow: Input -> Translation -> Execution -> Translation -> Output. |
| **Routing** | `SmartRoutingChatClient` | **Resilience Core.** Handles provider pool, rotation, quota management, and failover. |
| **Infra** | `IChatClientFactory` | Resolves the specific `IChatClient` implementation for a given provider key. |

## 3. Detailed Component Design

### 3.1. Contracts (CQRS Commands)

We define strict contracts for the Mediator pipeline.

```csharp
// Non-Streaming Command
public record ChatCommand(OpenAIRequest Request, IEnumerable<ChatMessage> Messages) 
    : IRequest<AgentResponse>;

// Streaming Command (Native AsyncEnumerable support)
public record ChatStreamCommand(OpenAIRequest Request, IEnumerable<ChatMessage> Messages) 
    : IStreamRequest<AgentResponseUpdate>;
```

### 3.2. The Handler (The Bridge)

The `ChatCompletionHandler` acts as the adapter between the CQRS pipeline and the Agent Framework.

*   **Role:** Invokes the `RoutingAgent`.
*   **Implementation:**
    *   `Handle(ChatCommand)`: Calls `agent.RunAsync`.
    *   `Handle(ChatStreamCommand)`: Calls `agent.RunStreamingAsync`.

### 3.3. RoutingAgent (Domain Core)

*   **Lifecycle:** Scoped.
*   **Dependencies:** `IChatClient` (which is `SmartRoutingChatClient`), `ITranslationPipeline`.
*   **Responsibility:**
    1.  Translate canonical request using `ITranslationPipeline`.
    2.  Invoke `IChatClient.GetResponseAsync`.
    3.  Translate response back to canonical format.
    4.  Maintain agent context (future-proofing).

### 3.4. SmartRoutingChatClient (Resilience Core)

*   **Lifecycle:** Scoped.
*   **Dependencies:** `ISmartRouter`, `IChatClientFactory`, `IQuotaTracker`, `IHealthStore`.
*   **Logic:**
    1.  Query `ISmartRouter` for candidates (e.g., `["azure-east", "openai-org-1"]`).
    2.  **Rotation Loop:**
        *   Check Quota/Health.
        *   Resolve Client via Factory.
        *   Execute Call.
        *   On Failure: Log, Record Failure, Continue to next candidate.
    3.  Throw if all candidates fail.

### 3.5. Infrastructure Wiring

*   **Mediator:** Registered in `Program.cs` via `services.AddMediator()`.
*   **Endpoints:** Minimal APIs map requests to Commands.

## 4. Migration Steps

1.  **Dependencies:** Add `Mediator.Abstractions` and `Mediator.SourceGenerator`.
2.  **Contracts:** Create Command records.
3.  **Refactor SmartRoutingChatClient:** Ensure robust rotation logic and removal of Service Locator anti-patterns (use Factory).
4.  **Refactor RoutingAgent:** Clean up and ensure strict Constructor Injection.
5.  **Implement Handlers:** Create `ChatCompletionHandler`.
6.  **Refactor Endpoints:** Update `OpenAIEndpointsExtensions` to use Mediator.
7.  **Tests:** Implement comprehensive Unit and Integration tests (80% coverage target).

## 5. Testing Strategy

*   **Unit Tests:**
    *   `SmartRoutingChatClientTests`: Verify rotation, quota checks, and exception handling using Mock Factories.
    *   `RoutingAgentTests`: Verify translation flow and agent orchestration.
    *   `ChatCompletionHandlerTests`: Verify command-to-agent delegation.
*   **Integration Tests:**
    *   End-to-End API tests via `TestServer`.
    *   Verify SSE streaming format.
    *   Verify OpenAI JSON format.
    *   Verify Headers (Routing Context).
