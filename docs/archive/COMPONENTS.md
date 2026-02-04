# Components

Lightweight component directory based on repository structure and documentation.

## Overview

Components identified from source structure and archived documentation. This directory maps the major architectural components of Synaxis to their locations in the codebase, associated ADRs, and key historical milestones.

## Components

### Inference Gateway (OpenAI-Compatible API)

**Description:** Multi-tenant OpenAI-compatible gateway with ULTRA MISER MODE routing (free > cheapest > healthy), regional data locality, and dynamic model registry.

**Key Paths:**
- `src/InferenceGateway/WebApi/` - ASP.NET Core API endpoints
- `src/InferenceGateway/Application/` - CQRS handlers and domain logic
- `src/InferenceGateway/Infrastructure/` - Provider clients and infrastructure services

**Key ADRs:**
- [ADR-001: Stream-Native CQRS Architecture](../adr/001-stream-native-cqrs.md)
- [ADR-002: Tiered Routing Strategy](../adr/002-tiered-routing-strategy.md)
- [ADR-004: Token Optimization Architecture](../adr/004-token-optimization-architecture.md)

**Key Dates:**
- [2026-01-25](CHANGELOG_BY_DATE.md#2026-01-25): Comprehensive overhaul to use Microsoft Agent Framework and implementation of RoutingAgent
- [2026-01-26](CHANGELOG_BY_DATE.md#2026-01-26): Stream-native CQRS architecture adopted with Mediator pattern

---

### WebApp (React Frontend)

**Description:** Local-first React application for Synaxis that manages chat history and costs client-side using IndexedDB, respecting the stateless "Ultra Miser Mode" backend philosophy.

**Key Paths:**
- `src/Synaxis.WebApp/ClientApp/` - React + TypeScript + Vite frontend
- `src/Synaxis.WebApp/` - ASP.NET Core host with YARP reverse proxy

**Key ADRs:**
- [ADR-003: Authentication Architecture](../adr/003-authentication-architecture.md)

**Key Dates:**
- [2026-01-29](CHANGELOG_BY_DATE.md#2026-01-29): Frontend implementation plan approved (plan1-20260129-frontend-local-first.md)
- [2026-01-29](CHANGELOG_BY_DATE.md#2026-01-29): WebApp containerization plan created (plan2-20260129-webapp-containerization.md)

**Key Documentation:**
- [Frontend Local-First Plan](2026-02-02-pre-refactor/plan/plan1-20260129-frontend-local-first.md)
- [WebApp Containerization](2026-02-02-pre-refactor/plan/plan2-20260129-webapp-containerization.md)

---

### AI Integration (Routing & Chat Clients)

**Description:** Intelligent routing layer with Microsoft Agent Framework integration, smart provider selection, and multi-provider chat client implementations.

**Key Paths:**
- `src/InferenceGateway/Application/Routing/` - Smart routing logic
- `src/InferenceGateway/Application/ChatClients/` - Abstract chat client interfaces
- `src/InferenceGateway/Infrastructure/ChatClients/` - Provider-specific implementations
- `src/InferenceGateway/WebApi/Agents/` - Microsoft Agent Framework integration

**Key ADRs:**
- [ADR-001: Stream-Native CQRS Architecture](../adr/001-stream-native-cqrs.md)
- [ADR-002: Tiered Routing Strategy](../adr/002-tiered-routing-strategy.md)

**Key Dates:**
- [2026-01-24](CHANGELOG_BY_DATE.md#2026-01-24): Add browser-based Tier 4 providers using Ghostwright extensions
- [2026-01-25](CHANGELOG_BY_DATE.md#2026-01-25): Implement OpenAI endpoints with routing agent integration
- [2026-01-29](CHANGELOG_BY_DATE.md#2026-01-29): Dynamic model registry plan approved

**Key Documentation:**
- [OpenAI Gateway Roadmap](2026-02-02-pre-refactor/plan/plan1-20260125-openai-gateway-roadmap.md)
- [Dynamic Model Registry](2026-02-02-pre-refactor/plans/20260129-plan7-dynamic-model-registry.md)

---

### Identity & Authentication

**Description:** Unified, secure, strategy-based Identity System ("Synaxis Identity Vault") supporting OAuth (GitHub, Google) with device flows and encrypted token storage.

**Key Paths:**
- `src/InferenceGateway/Infrastructure/Identity/` - Core identity infrastructure
- `src/InferenceGateway/WebApi/Endpoints/Identity/` - Identity API endpoints

**Key ADRs:**
- [ADR-003: Authentication Architecture](../adr/003-authentication-architecture.md)

**Key Dates:**
- [2026-01-28](CHANGELOG_BY_DATE.md#2026-01-28): Identity refactor plan approved (plan1-20260128-identity-refactor.md)

**Key Documentation:**
- [Identity Refactor Plan](2026-02-02-pre-refactor/plan/plan1-20260128-identity-refactor.md)

---

### Testing Framework

**Description:** Comprehensive multi-layer testing strategy including unit tests, integration tests with TestContainers, smoke tests, benchmarks, and E2E tests.

**Key Paths:**
- `src/Tests/Benchmarks/` - BenchmarkDotNet performance tests

**Key ADRs:**
- [ADR-006: Test Infrastructure Strategy](../adr/006-test-infrastructure.md)

**Key Dates:**
- [2026-01-24](CHANGELOG_BY_DATE.md#2026-01-24): Add integration tests for provider connectivity and setup test project
- [2026-01-29](CHANGELOG_BY_DATE.md#2026-01-29): Smoke test strategy plan created

**Key Documentation:**
- [Smoke Test Strategy](2026-02-02-pre-refactor/plan/20260129-plan2-smoke-tests.md)
- [Test Architecture Review](2026-02-02-pre-refactor/sisyphus/docs/test-architecture-review.md)
- [Testing Summary](2026-02-02-docs-rebuild/TESTING_SUMMARY.md)

---

### Control Plane (Database-Driven Configuration)

**Description:** Dynamic, database-driven model registry and control plane using PostgreSQL with EF Core for provider discovery, model metadata, and tenant limits.

**Key Paths:**
- `src/InferenceGateway/Application/ControlPlane/` - Control plane application logic
- `src/InferenceGateway/Infrastructure/ControlPlane/` - Database context and entities

**Key ADRs:**
- [ADR-001: Stream-Native CQRS Architecture](../adr/001-stream-native-cqrs.md)

**Key Dates:**
- [2026-01-29](CHANGELOG_BY_DATE.md#2026-01-29): Dynamic model registry plan created

**Key Documentation:**
- [Dynamic Model Registry](2026-02-02-pre-refactor/plans/20260129-plan7-dynamic-model-registry.md)

---

### Translation Pipeline

**Description:** Canonical request/response translation system for normalizing different provider formats to OpenAI-compatible schemas with streaming state machine support.

**Key Paths:**
- `src/InferenceGateway/Application/Translation/` - Translation interfaces and pipeline

**Key ADRs:**
- [ADR-001: Stream-Native CQRS Architecture](../adr/001-stream-native-cqrs.md)

**Key Dates:**
- [2026-01-25](CHANGELOG_BY_DATE.md#2026-01-25): Canonical OpenAI schema and translator registry implemented

**Key Documentation:**
- [OpenAI Gateway Roadmap](2026-02-02-pre-refactor/plan/plan1-20260125-openai-gateway-roadmap.md)

---

### Docker & Deployment

**Description:** Containerized deployment infrastructure with Docker Compose orchestration for multi-service setup (WebAPI, WebApp, PostgreSQL, Redis, pgAdmin).

**Key Paths:**
- `docker-compose.yml` - Main orchestration file
- `docker-compose.infrastructure.yml` - Infrastructure services
- `src/Synaxis.WebApp/Dockerfile` - WebApp container
- `src/InferenceGateway/WebApi/Dockerfile` - Gateway container

**Key ADRs:**
- [ADR-005: Observability and Monitoring](../adr/005-observability-monitoring.md)

**Key Dates:**
- [2026-01-25](CHANGELOG_BY_DATE.md#2026-01-25): Add Docker publish and tag release workflows
- [2026-01-25](CHANGELOG_BY_DATE.md#2026-01-25): Upgrade .NET SDK to version 10.0 in Dockerfile
- [2026-01-26](CHANGELOG_BY_DATE.md#2026-01-26): Docker Compose infrastructure plan created

**Key Documentation:**
- [Docker Compose Infrastructure](2026-02-02-pre-refactor/plan/plan2-20260126-docker-compose-infra.md)
- [WebApp Containerization](2026-02-02-pre-refactor/plan/plan2-20260129-webapp-containerization.md)

---

### Vector Database (Qdrant Integration)

**Description:** Qdrant vector database integration for semantic search and embedding storage capabilities.

**Key Paths:**
- Integration points in Infrastructure layer

**Key ADRs:**
- [ADR-007: Qdrant Integration](../adr/007-qdrant-integration.md)

**Key Dates:**
- [2026-02-03](CHANGELOG_BY_DATE.md#2026-02-03): Qdrant integration ADR created

---

## Component Relationships

```
┌─────────────────────────────────────────────────────────────┐
│                         WebApp (React)                       │
│                     (Local-First + YARP)                     │
└────────────────────────┬────────────────────────────────────┘
                         │ HTTP/WebSocket
                         ▼
┌─────────────────────────────────────────────────────────────┐
│              Inference Gateway (WebAPI)                      │
│                  ┌───────────────────┐                       │
│                  │  Mediator (CQRS)  │                       │
│                  └────────┬──────────┘                       │
│                           ▼                                  │
│            ┌─────────────────────────────┐                   │
│            │   RoutingAgent (MS Agent)   │                   │
│            └──────────┬──────────────────┘                   │
│                       ▼                                      │
│         ┌────────────────────────────────┐                   │
│         │  SmartRoutingChatClient        │                   │
│         └──────┬─────────────────────────┘                   │
│                ▼                                             │
│    ┌───────────────────────────┐                             │
│    │  Provider Chat Clients    │                             │
│    │  (OpenAI, Gemini, etc.)   │                             │
│    └───────────────────────────┘                             │
└───────────┬──────────────────────────────────┬───────────────┘
            │                                  │
            ▼                                  ▼
┌───────────────────────┐          ┌──────────────────────┐
│   Control Plane DB    │          │   Identity Vault     │
│    (PostgreSQL)       │          │  (Encrypted Store)   │
└───────────────────────┘          └──────────────────────┘
```

## Notes

- All components follow Clean Architecture principles with Application/Infrastructure separation
- ULTRA MISER MODE™ philosophy drives design decisions (free-tier first, cost-optimized routing)
- Stream-native design throughout (AsyncEnumerable, SSE, proper backpressure)
- Local-first frontend to minimize backend state and costs
- Database-driven configuration for dynamic provider/model management
