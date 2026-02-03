# Synaxis 2.0: Master Execution Plan

**Date**: 2026-01-24
**Goal**: Rebuild Synaxis as a clean-slate, Agentic Gateway using Microsoft Agent Framework.

## 0. The Clean Slate (Preparation)
*   **Strategy**: Side-by-side construction ("Synaxis.Next.sln") to preserve legacy until verification.
*   **Target**: .NET 9.0

## 1. Solution Architecture

### A. `Synaxis.Connectors` (The Integration Library)
*   **Type**: Class Library
*   **Responsibility**: Pure implementation of `IChatClient` for specific providers.
*   **Dependencies**:
    *   `Microsoft.Extensions.AI` (Abstraction)
    *   `Google.GenAI` (Official Gemini)
    *   `TryAGI.Groq` (Unofficial Groq)
*   **Key Components**:
    *   `GeminiChatClient`: Native wrapper or direct adaptation.
    *   `GroqChatClient`: Adapter mapping TryAGI.Groq to `IChatClient`.

### B. `Synaxis.Brain` (The Application Logic)
*   **Type**: Class Library
*   **Responsibility**: Routing, Telemetry, Orchestration.
*   **Dependencies**: `Synaxis.Connectors`, `Microsoft.Agents.Core`.
*   **Key Components**:
    *   `AgentRouter`: Selects provider based on model name.
    *   `UsageTrackingHandler`: Middleware for cost/token tracking.
    *   `OrchestratorAgent`: Microsoft Agent Framework implementation.

### C. `Synaxis.Gateway` (The API)
*   **Type**: ASP.NET Core Web API
*   **Responsibility**: Hosting, SSE Streaming, OpenAI Compatibility.
*   **Endpoints**:
    *   `POST /v1/chat/completions` (Standard OpenAI spec).
    *   `GET /v1/models`.

### D. Testing
*   Unit Test projects for all layers (xUnit + Moq).
*   Target: >80% Code Coverage.

## 2. Implementation Phases

### Phase 1: Scaffolding
*   Create `Synaxis.Next.sln`.
*   Create all projects and test projects.
*   Install NuGet packages.

### Phase 2: The Connectors
*   Implement `GeminiChatClient`.
*   Implement `GroqChatClient` (with careful type mapping).
*   Unit Test Connectors.

### Phase 3: The Brain
*   Implement `UsageTrackingHandler`.
*   Implement `AgentRouter`.
*   Unit Test Brain logic.

### Phase 4: The Gateway
*   Implement Minimal API endpoints.
*   Implement SSE Streaming logic.
*   Integration Tests.

### Phase 5: Verification
*   Build & Test Run.
*   Manual Verification via CLI/Curl.
