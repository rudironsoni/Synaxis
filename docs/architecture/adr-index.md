# Architecture Decision Records (ADR) Index

> **Quick Reference**: All architectural decisions made for Synaxis, with one-line summaries and links to full documentation.

## 📋 Overview

Architecture Decision Records (ADRs) document significant architectural choices, their context, consequences, and alternatives considered. This index provides quick navigation to all decisions.

**ADR Format**: Each ADR includes:
- **Context**: Problem statement and constraints
- **Decision**: What was chosen and why
- **Consequences**: Positive and negative outcomes
- **Alternatives**: Options considered and rejected

## 🏗️ Core Architecture

### ADR-001: Stream-Native CQRS Architecture
**Summary**: Adopts CQRS with Mediator pattern for streaming-first request handling  
**Status**: ✅ Accepted  
**Date**: 2026-01-26  
**Link**: [Full ADR](../adr/001-stream-native-cqrs.md)

**Key Points**:
- Uses `martinothamar/Mediator` for source-generated CQRS
- Commands, Queries, and Streams as first-class citizens
- Pipeline behaviors for cross-cutting concerns
- Supports `IAsyncEnumerable<T>` for native streaming

---

### ADR-002: Tiered Routing Strategy
**Summary**: 4-tier provider fallback prioritizing free tiers before paid providers  
**Status**: ✅ Accepted  
**Date**: 2026-01-28  
**Link**: [Full ADR](../adr/002-tiered-routing-strategy.md)

**Key Points**:
- Tier 1: User-preferred provider
- Tier 2: Free tier providers (ULTRA MISER MODE™)
- Tier 3: Paid providers (fallback)
- Tier 4: Emergency mode (any healthy provider)
- Health-aware selection with quota tracking

---

### ADR-003: Authentication Architecture
**Summary**: JWT-based authentication with multi-tenancy and API key support  
**Status**: ✅ Accepted  
**Date**: 2026-01-29  
**Link**: [Full ADR](../adr/003-authentication-architecture.md)

**Key Points**:
- JWT tokens for user authentication
- API keys for service-to-service
- Multi-tenant isolation
- Role-based authorization
- Secure credential storage

---

## 🎯 Optimization & Performance

### ADR-004: Token Optimization Architecture
**Summary**: Semantic caching and prompt compression to reduce token costs  
**Status**: ✅ Accepted  
**Date**: 2026-02-01  
**Link**: [Full ADR](../adr/004-token-optimization-architecture.md)

**Key Points**:
- Semantic caching with embedding similarity
- Prompt compression for repeated requests
- Token counting before API calls
- Cost tracking per request

---

### ADR-005: Observability and Monitoring Stack
**Summary**: OpenTelemetry for metrics, traces, and logs with Prometheus/Grafana  
**Status**: ✅ Accepted  
**Date**: 2026-02-02  
**Link**: [Full ADR](../adr/005-observability-monitoring.md)

**Key Points**:
- OpenTelemetry for instrumentation
- Prometheus for metrics storage
- Grafana for visualization
- Distributed tracing with Jaeger
- Structured logging with Serilog

---

### ADR-010: Ultra-Miser Mode Cost Optimization
**Summary**: Aggressive cost optimization strategy prioritizing free-tier usage  
**Status**: ✅ Accepted  
**Date**: 2026-02-05  
**Link**: [Full ADR](../adr/010-ultra-miser-mode.md)

**Key Points**:
- Free-tier prioritization
- Quota tracking and health monitoring
- Graceful degradation on rate limits
- Cost-per-request tracking
- Automatic provider rotation

---

## 🧪 Testing & Quality

### ADR-006: Test Infrastructure Strategy
**Summary**: Test pyramid with unit, integration, and E2E tests using xUnit  
**Status**: ✅ Accepted  
**Date**: 2026-02-03  
**Link**: [Full ADR](../adr/006-test-infrastructure.md)

**Key Points**:
- xUnit for test framework
- Testcontainers for integration tests
- Playwright for E2E tests
- Test pyramid: 70% unit, 20% integration, 10% E2E
- Property-based testing with FsCheck

---

## 🗄️ Data & Storage

### ADR-007: Qdrant Vector Database Integration
**Summary**: Qdrant for vector embeddings storage and semantic search  
**Status**: ✅ Accepted  
**Date**: 2026-02-04  
**Link**: [Full ADR](../adr/007-qdrant-integration.md)

**Key Points**:
- Qdrant for vector storage
- HNSW indexing for similarity search
- Embedding caching
- Multi-tenancy support
- Rust-based performance

---

### ADR-011: Dynamic Model Registry & Intelligence Core
**Summary**: Dynamic model discovery and capability detection system  
**Status**: ✅ Accepted  
**Date**: 2026-02-06  
**Link**: [Full ADR](../adr/011-dynamic-model-registry.md)

**Key Points**:
- Auto-discovery of provider models
- Capability detection (chat, vision, tools)
- Model metadata caching
- Cost and performance metrics
- Model recommendation engine

---

## 🎨 Frontend & UI

### ADR-008: Frontend Local-First Architecture
**Summary**: Local-first web app with offline support and sync  
**Status**: ✅ Accepted  
**Date**: 2026-02-04  
**Link**: [Full ADR](../adr/008-frontend-local-first.md)

**Key Points**:
- IndexedDB for local storage
- Service workers for offline mode
- Optimistic UI updates
- Background sync when online
- React + TypeScript

---

### ADR-009: Identity Vault Refactoring Strategy
**Summary**: Secure credential management with encryption and key rotation  
**Status**: ✅ Accepted  
**Date**: 2026-02-05  
**Link**: [Full ADR](../adr/009-identity-refactoring.md)

**Key Points**:
- Encrypted credential storage
- Key rotation strategy
- Secrets management integration
- Per-user encryption keys
- Audit logging

---

## 📦 Package Architecture

### ADR-012: SDK-First Package Architecture
**Summary**: 4-tier package structure optimized for SDK consumers  
**Status**: ✅ Accepted  
**Date**: 2026-02-08  
**Link**: [Full ADR](../adr/012-sdk-first-package-architecture.md)

**Key Points**:
- Tier 1: Abstractions (zero dependencies)
- Tier 2: Contracts (versioned DTOs)
- Tier 3: Mediator (CQRS implementation)
- Tier 4: Client (high-level SDK)
- Clear dependency boundaries

---

### ADR-013: Transport Abstraction with Mediator
**Summary**: Decouples transport mechanisms from business logic via executors  
**Status**: ✅ Accepted  
**Date**: 2026-02-08  
**Link**: [Full ADR](../adr/013-transport-abstraction-mediator.md)

**Key Points**:
- `ICommandExecutor`, `IQueryExecutor`, `IStreamExecutor` interfaces
- Transport-agnostic handlers
- HTTP, gRPC, WebSocket, In-Process support
- Streaming via `IAsyncEnumerable<T>`
- Easy testing with mocks

---

### ADR-014: Explicit Registration Pattern
**Summary**: Requires explicit registration of all components (no auto-discovery)  
**Status**: ✅ Accepted  
**Date**: 2026-02-08  
**Link**: [Full ADR](../adr/014-explicit-registration-pattern.md)

**Key Points**:
- No assembly scanning
- Explicit provider registration
- Predictable startup behavior
- Better testability
- Clear dependency tracking

---

### ADR-015: Contracts Versioning Strategy
**Summary**: Namespace-based versioning for backward-compatible contract evolution  
**Status**: ✅ Accepted  
**Date**: 2026-02-08  
**Link**: [Full ADR](../adr/015-contracts-versioning-strategy.md)

**Key Points**:
- Versioned namespaces (V1, V2, V3)
- Independent contract versioning
- Backward compatibility support
- Multiple versions coexist
- Clear migration paths

---

## 📊 ADR Summary Table

| ADR | Title | Status | Category | Date |
|-----|-------|--------|----------|------|
| [001](../adr/001-stream-native-cqrs.md) | Stream-Native CQRS | ✅ Accepted | Core | 2026-01-26 |
| [002](../adr/002-tiered-routing-strategy.md) | Tiered Routing Strategy | ✅ Accepted | Core | 2026-01-28 |
| [003](../adr/003-authentication-architecture.md) | Authentication Architecture | ✅ Accepted | Security | 2026-01-29 |
| [004](../adr/004-token-optimization-architecture.md) | Token Optimization | ✅ Accepted | Performance | 2026-02-01 |
| [005](../adr/005-observability-monitoring.md) | Observability Stack | ✅ Accepted | Operations | 2026-02-02 |
| [006](../adr/006-test-infrastructure.md) | Test Infrastructure | ✅ Accepted | Quality | 2026-02-03 |
| [007](../adr/007-qdrant-integration.md) | Qdrant Integration | ✅ Accepted | Data | 2026-02-04 |
| [008](../adr/008-frontend-local-first.md) | Frontend Local-First | ✅ Accepted | Frontend | 2026-02-04 |
| [009](../adr/009-identity-refactoring.md) | Identity Vault | ✅ Accepted | Security | 2026-02-05 |
| [010](../adr/010-ultra-miser-mode.md) | Ultra-Miser Mode | ✅ Accepted | Performance | 2026-02-05 |
| [011](../adr/011-dynamic-model-registry.md) | Dynamic Model Registry | ✅ Accepted | Core | 2026-02-06 |
| [012](../adr/012-sdk-first-package-architecture.md) | Package Architecture | ✅ Accepted | Architecture | 2026-02-08 |
| [013](../adr/013-transport-abstraction-mediator.md) | Transport Abstraction | ✅ Accepted | Architecture | 2026-02-08 |
| [014](../adr/014-explicit-registration-pattern.md) | Explicit Registration | ✅ Accepted | Architecture | 2026-02-08 |
| [015](../adr/015-contracts-versioning-strategy.md) | Contracts Versioning | ✅ Accepted | Architecture | 2026-02-08 |

## 🔍 ADRs by Category

### Core Architecture (4)
- ADR-001: Stream-Native CQRS
- ADR-002: Tiered Routing Strategy
- ADR-011: Dynamic Model Registry
- ADR-012: Package Architecture

### Transport & Communication (2)
- ADR-013: Transport Abstraction
- ADR-014: Explicit Registration

### Security & Authentication (2)
- ADR-003: Authentication Architecture
- ADR-009: Identity Vault

### Performance & Optimization (2)
- ADR-004: Token Optimization
- ADR-010: Ultra-Miser Mode

### Data & Storage (1)
- ADR-007: Qdrant Integration

### Frontend (1)
- ADR-008: Frontend Local-First

### Operations (1)
- ADR-005: Observability Stack

### Quality & Testing (1)
- ADR-006: Test Infrastructure

### Contracts & Versioning (1)
- ADR-015: Contracts Versioning

## 📝 ADR Lifecycle

### Status Values
- ✅ **Accepted**: Decision is final and implemented
- 🚧 **Proposed**: Under review, not yet implemented
- ⚠️ **Deprecated**: No longer recommended, being phased out
- ❌ **Superseded**: Replaced by a newer ADR

### Creating New ADRs

When making significant architectural decisions:

1. **Create ADR File**: Use template `docs/adr/XXX-short-title.md`
2. **Document Context**: Explain the problem and constraints
3. **List Alternatives**: Show what was considered
4. **Make Decision**: Clearly state what was chosen
5. **Note Consequences**: Both positive and negative
6. **Link Related**: Cross-reference related ADRs
7. **Update Index**: Add entry to this index

### ADR Template

```markdown
# ADR-XXX: Title

## Status
Proposed | Accepted | Deprecated | Superseded

## Context
What problem are we solving? What constraints exist?

## Decision
What did we decide to do?

## Consequences

### Positive
- Benefit 1
- Benefit 2

### Negative
- Tradeoff 1
- Tradeoff 2

## Alternatives Considered
What else did we evaluate and why was it rejected?

## Related
- Links to related ADRs
```

## 🔗 Related Documentation

- [Architecture Overview](./README.md) - High-level architecture guide
- [Package Architecture](./packages.md) - Package structure details
- [Mediator Pattern](./mediator.md) - CQRS implementation
- [Transport Layer](./transports.md) - Transport abstraction
- [Provider System](./providers.md) - AI provider integration

## 📖 Reading Path

**New to Synaxis?** Read ADRs in this order:

1. **ADR-012**: Package Architecture - Understand the layers
2. **ADR-001**: Stream-Native CQRS - Core request handling
3. **ADR-013**: Transport Abstraction - How clients connect
4. **ADR-002**: Tiered Routing - Provider selection logic
5. **ADR-014**: Explicit Registration - Component registration
6. **ADR-015**: Contracts Versioning - API evolution strategy

**Implementing Features?** Focus on:
- ADR-001: CQRS patterns
- ADR-013: Transport interfaces
- ADR-014: Registration patterns

**Optimizing Costs?** Read:
- ADR-002: Tiered routing
- ADR-004: Token optimization
- ADR-010: Ultra-Miser Mode

**Building UI?** Check:
- ADR-008: Frontend architecture
- ADR-003: Authentication

---

**Last Updated**: 2026-02-08  
**Total ADRs**: 15  
**Maintainers**: Synaxis Core Team
