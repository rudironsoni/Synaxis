# Architecture Decision Records (ADRs)

This directory contains Architecture Decision Records (ADRs) for the Synaxis project. ADRs document significant architectural decisions, their context, alternatives considered, and consequences.

## What is an ADR?

An Architecture Decision Record (ADR) captures a single architectural decision along with its context and consequences. ADRs are immutable—once accepted, they are not edited. Instead, new ADRs supersede old ones if decisions change.

## ADR Index

### Core Architecture (January 2026)

- [**ADR-001: Stream-Native CQRS Architecture**](./001-stream-native-cqrs.md) _(2026-01-26)_  
  Adopted CQRS with Mediator and Microsoft Agent Framework for high-availability streaming pipelines.

- [**ADR-002: Tiered Routing Strategy**](./002-tiered-routing-strategy.md) _(2026-02-02)_  
  Smart routing algorithm with multi-provider failover, cost optimization, and health-based candidate selection.

- [**ADR-003: Authentication Architecture**](./003-authentication-architecture.md) _(2026-01-30)_  
  Dual-mode authentication (JWT for API access, OAuth 2.0 for user login) with RBAC and rate limiting.

- [**ADR-004: Token Optimization Architecture**](./004-token-optimization-architecture.md) _(2026-02-03)_  
  Context window management, token counting, and optimization strategies for cost reduction.

- [**ADR-005: Observability & Monitoring**](./005-observability-monitoring.md) _(2026-02-03)_  
  OpenTelemetry integration, distributed tracing, metrics, and logging infrastructure.

- [**ADR-006: Test Infrastructure**](./006-test-infrastructure.md) _(2026-02-03)_  
  xUnit testing strategy, mocking patterns, and integration test architecture (80%+ coverage target).

- [**ADR-007: Qdrant Vector Database Integration**](./007-qdrant-integration.md) _(2026-02-03)_  
  Vector search and semantic caching using Qdrant for RAG and context optimization.

### Specialized Components (January 2026)

- [**ADR-008: Frontend Local-First Architecture**](./008-frontend-local-first.md) _(2026-01-29)_  
  Client-side state management with IndexedDB, eliminating backend storage costs while enabling offline capability.

- [**ADR-009: Identity Vault Refactoring**](./009-identity-refactoring.md) _(2026-01-28)_  
  Unified, encrypted identity management system with strategy-based OAuth flows (GitHub, Google).

- [**ADR-010: Ultra-Miser Mode**](./010-ultra-miser-mode.md) _(2026-01-29)_  
  Cost optimization strategy prioritizing free-tier AI providers with intelligent failover and savings tracking.

- [**ADR-011: Dynamic Model Registry**](./011-dynamic-model-registry.md) _(2026-01-29)_  
  Database-driven model discovery and synchronization, eliminating static configuration maintenance.

### SDK Architecture (February 2026)

- [**ADR-012: SDK-First Package Architecture**](./012-sdk-first-package-architecture.md) _(2026-02-08)_  
  Multi-tier package structure with strict dependency rules enabling SDK, Gateway, and SaaS consumption patterns.

- [**ADR-013: Transport Abstraction with Mediator**](./013-transport-abstraction-mediator.md) _(2026-02-08)_  
  CQRS backbone using MartinOthamar.Mediator with ICommandExecutor/IStreamExecutor abstractions for HTTP, gRPC, WebSocket.

- [**ADR-014: Explicit Registration Pattern**](./014-explicit-registration-pattern.md) _(2026-02-08)_  
  No auto-discovery, explicit DI registration only with AddSynaxisTransport*() and MapSynaxis*() patterns.

- [**ADR-015: Contracts Versioning Strategy**](./015-contracts-versioning-strategy.md) _(2026-02-08)_  
  Namespace-based versioning (V1.Messages, V2.Messages) with stability tiers and N-1 compatibility guarantees.

## ADR Format

All ADRs follow this structure:

```markdown
# ADR-XXX: [Title]

**Status:** [Proposed | Accepted | Deprecated | Superseded]
**Date:** YYYY-MM-DD

> **ULTRA MISER MODE™ Engineering**: [Witty principle or philosophy]

---

## Context
[Background, problem statement, and constraints]

## Decision
[What was decided, including code examples and diagrams]

## Consequences
[Positive and negative outcomes, with mitigations]

## Related Decisions
[Links to other ADRs]

## Evidence
[Archived docs, commits, implementation paths]

---

> *"[Closing ULTRA MISER MODE™ quote]"* — ULTRA MISER MODE™ Principle #X
```

## Creating New ADRs

When proposing a new ADR:

1. **Copy the template** from an existing ADR
2. **Assign the next number** (check the index above)
3. **Set status to "Proposed"** (change to "Accepted" after review)
4. **Include diagrams** (Mermaid preferred)
5. **Link related ADRs** (bi-directional links)
6. **Update this README** (add to index)

## Updating the Index

After creating a new ADR, update this file:

```bash
# Add entry to index
- [**ADR-XXX: Title**](./XXX-filename.md) _(YYYY-MM-DD)_  
  Brief one-sentence description.
```

Then commit with a descriptive message:

```bash
git add docs/adr/XXX-filename.md docs/adr/README.md
git commit -m "docs: add ADR-XXX for [topic]"
```

## Status Definitions

- **Proposed:** Under review, not yet implemented
- **Accepted:** Approved and implemented (or in progress)
- **Deprecated:** No longer recommended (superseded by newer ADR)
- **Superseded:** Replaced by a newer ADR (link to replacement)

## Related Documentation

- **Plans Archive:** `docs/archive/` — Historical planning documents
- **API Docs:** `docs/api/` — OpenAPI specifications
- **Contributing:** `CONTRIBUTING.md` — Development guidelines

---

> *"Architecture is the stuff you can't change later. ADRs are the receipts for the decisions you made when you could."* — ULTRA MISER MODE™ Principle #42
