# Documentation Archive Index

**Archive Date:** 2026-02-02  
**Reason:** Complete documentation rebuild with new structure  
**Status:** Preserved for historical reference

---

## What Is This?

This directory contains the **legacy documentation** from the pre-rebuild era. These files have been archived as part of a comprehensive documentation restructuring effort to create a more organized, complete, and ULTRA MISER MODE™-compliant documentation suite.

> **Note:** These archived documents are preserved for reference but are no longer maintained. For current documentation, see the [new structure](#new-documentation-structure) below.

---

## Archived Files Inventory

### Core Documentation

| File | Lines | Description |
|------|-------|-------------|
| [`API.md`](./API.md) | 1,147 | OpenAI-compatible API reference. Complete endpoint documentation for chat completions, streaming, models, admin operations, and authentication. Includes request/response schemas and SDK examples. |
| [`ARCHITECTURE.md`](./ARCHITECTURE.md) | 58 | Clean Architecture overview. High-level system design describing the WebApi, Application, and Infrastructure layers. CQRS pattern and routing agent concepts. |
| [`CONFIGURATION.md`](./CONFIGURATION.md) | 140 | Provider configuration guide. Setup instructions for all supported AI providers (Groq, Cloudflare, Cohere, etc.), JSON configuration examples, and environment variables. |
| [`TESTING_SUMMARY.md`](./TESTING_SUMMARY.md) | 147 | Test infrastructure summary. Overview of test coverage, test categories (unit, integration, performance), and quality metrics. |

### Architecture Decision Records

| File | Lines | Description |
|------|-------|-------------|
| [`adr/001-stream-native-cqrs.md`](./adr/001-stream-native-cqrs.md) | 58 | ADR-001: Stream-Native CQRS Architecture. Decision record explaining the choice of CQRS pattern with streaming-first design for real-time AI inference. |

---

## Why These Were Archived

The documentation underwent a **complete rebuild** (not just reorganization) to achieve:

1. **Better Structure**: Tiered organization (Core → Reference → ADR → Operational)
2. **Missing Content**: New documents for deployment, security, contributing, troubleshooting
3. **Consistency**: Unified formatting, cross-linking, and ULTRA MISER MODE™ personality throughout
4. **Completeness**: 17 documents instead of 5, covering all aspects of the project

---

## New Documentation Structure

For current, maintained documentation, refer to:

### Tier 1 - Core Documentation
- [`docs/README.md`](../README.md) - Project overview and quick start
- [`docs/ARCHITECTURE.md`](../ARCHITECTURE.md) - Clean Architecture deep dive
- [`docs/API.md`](../API.md) - Reorganized API reference
- [`docs/CONFIGURATION.md`](../CONFIGURATION.md) - Updated configuration guide
- [`docs/DEPLOYMENT.md`](../DEPLOYMENT.md) - Docker and deployment guide *(NEW)*
- [`docs/SECURITY.md`](../SECURITY.md) - Authentication and security *(NEW)*
- [`docs/TESTING.md`](../TESTING.md) - Testing strategy and guides *(NEW)*
- [`docs/CONTRIBUTING.md`](../CONTRIBUTING.md) - Development and PR guidelines *(NEW)*

### Tier 2 - Reference Documentation
- [`docs/reference/providers.md`](../reference/providers.md) - Provider capabilities and details *(NEW)*
- [`docs/reference/models.md`](../reference/models.md) - Model feature matrix *(NEW)*
- [`docs/reference/errors.md`](../reference/errors.md) - Error codes and troubleshooting *(NEW)*

### Tier 3 - Architecture Decisions
- [`docs/adr/001-stream-native-cqrs.md`](../adr/001-stream-native-cqrs.md) - Restored from archive
- [`docs/adr/002-tiered-routing-strategy.md`](../adr/002-tiered-routing-strategy.md) - Routing algorithm ADR *(NEW)*
- [`docs/adr/003-authentication-architecture.md`](../adr/003-authentication-architecture.md) - OAuth/JWT ADR *(NEW)*

### Tier 4 - Operational
- [`docs/ops/troubleshooting.md`](../ops/troubleshooting.md) - Common issues and solutions *(NEW)*
- [`docs/ops/monitoring.md`](../ops/monitoring.md) - Health checks and metrics *(NEW)*
- [`docs/ops/performance.md`](../ops/performance.md) - Benchmarks and optimization *(NEW)*

---

## ULTRA MISER MODE™ Preservation Notice

These archived documents contain the original ULTRA MISER MODE™ energy. The new documentation maintains this sacred tradition—every document includes at least one ULTRA MISER MODE™ reference, because even documentation should refuse to pay for premium features.

---

## Archive Metadata

- **Total Files Archived:** 5
- **Total Lines Preserved:** 1,550
- **Archive Created:** 2026-02-02
- **Preservation Commit:** `docs: archive legacy documentation to 2026-02-02-docs-rebuild`

---

*This archive is read-only. Do not modify archived files. For updates, use the new documentation structure.*
