# ADR 001: Stream-Native CQRS Architecture with Microsoft Agent Framework

**Status:** Accepted  
**Date:** 2026-01-26

> **ULTRA MISER MODE™ Engineering**: We didn't just choose CQRS because it sounds enterprise-grade. We chose it because streaming tokens shouldn't cost extra, and neither should clean architecture.

---

## Context

The initial architecture of Synaxis relied on a service-chained model where the `RoutingAgent` (Singleton) manually managed scopes and dependencies, leading to hidden coupling and testing difficulties. The system required a more robust way to handle high-availability streaming token delivery, strictly typed pipelines, and better separation of concerns between domain logic (Agents), routing policy (Smart Router), and infrastructure (Resilience).

Additionally, as a project built on **ULTRA MISER MODE™** principles, we needed an architecture that could gracefully handle the chaos of free-tier AI providers—where rate limits, downtime, and quota exhaustion are not exceptions but daily realities.

---

## Decision

We have adopted a **Stream-Native CQRS** architecture using `Mediator` (martinothamar) combined with the **Microsoft Agent Framework**.

### Key Architectural Components

#### 1. CQRS Pipeline (Mediator)

We use `martinothamar/Mediator` (Source Generated) to handle the request pipeline.

- **Commands:** `ChatCommand` (Unary) and `ChatStreamCommand` (Streaming) strictly define the intent.
- **Handlers:** `ChatCompletionHandler` acts as the bridge, accepting commands and delegating execution to the Domain Core (`RoutingAgent`).
- **Benefit:** Decouples the HTTP Transport (API) from the Domain Execution. Allows for future behavioral middleware (validation, rate limiting).

#### 2. Domain Core (RoutingAgent)

The `RoutingAgent` is elevated to a **Scoped**, first-class citizen of the Microsoft Agent Framework.

- **Responsibility:** Pure orchestration of the cognitive flow (Translation -> Execution -> Translation).
- **Lifecycle:** Request-Scoped.
- **DI:** Strict Constructor Injection (no `ServiceLocator` or `CreateScope`).

#### 3. Resilience Core (SmartRoutingChatClient)

Infrastructure resilience is encapsulated within the `IChatClient` implementation, not the Agent.

- **Responsibility:** Implements the **Provider Rotation Loop**.
  - Consults `ISmartRouter` for candidate pools.
  - Checks `IQuotaTracker` and `IHealthStore`.
  - Rotates through providers on failure transparently to the Agent.
- **Benefit:** The Agent "thinks" it's talking to a single reliable provider, while the infrastructure handles the chaos of distributed AI providers.

#### 4. Infrastructure Isolation (IChatClientFactory)

Service Location is confined to a single infrastructure adapter (`ChatClientFactory`).

- **Responsibility:** Resolves the concrete `IChatClient` for a specific provider key (e.g., "openai-east-us").
- **Benefit:** Keeps the Application Core clean of DI container logic.

---

## Diagram

```mermaid
graph LR
    API[API Endpoint] --> Med[Mediator]
    Med --> Handler[ChatCompletionHandler]
    Handler --> Agent[RoutingAgent]
    Agent --> Smart[SmartRoutingChatClient]
    
    subgraph Resilience Loop
    Smart --> Router[ISmartRouter]
    Smart --> Factory[IChatClientFactory]
    Factory --> Provider[External AI Provider]
    end
```

---

## Consequences

### Positive

- **Testability:** Significantly improved. Each component can be tested in isolation with mocked dependencies.
- **Streaming-First:** Real-time token delivery is typed and first-class, not bolted on as an afterthought.
- **Explicit Dependencies:** No hidden service location or scope management. What you see is what you get.
- **ULTRA MISER MODE™ Compatibility:** The architecture gracefully handles provider failures, making free-tier rotation seamless and invisible to consumers.

### Negative

- **Slight Complexity Increase:** Due to indirection (Mediator), but mitigated by compile-time source generation.
- **Learning Curve:** Developers must understand the CQRS pattern and Mediator library conventions.

---

## Related Decisions

- [ADR-002: Tiered Routing Strategy](./002-tiered-routing-strategy.md) — How the SmartRoutingChatClient selects providers
- [ADR-003: Authentication Architecture](./003-authentication-architecture.md) — Security layer integration with the CQRS pipeline

---

> *"The best architecture is one that makes being cheap look like being smart."* — ULTRA MISER MODE™ Principle #7
