# Work Item Checklist and Quick Reference

## Beads Issue Creation Commands

Use these commands to create the full work item hierarchy in beads:

### Phase 0: Foundation
```bash
# Create Phase 0 Epic
bd create "Phase 0: Fix All 232 Warnings" \
  --description="Eliminate all compiler and analyzer warnings before refactoring. Big bang migration requires zero-warning baseline." \
  --type=epic --priority=0

# Create Phase 0 tasks (run in parallel)
bd create "Fix SA1614 Parameter Documentation Warnings (88)" \
  --description="Fix 88 SA1614 warnings: Element parameter documentation should have text. Add descriptive text to all <param> XML documentation tags." \
  --type=task --priority=0 --deps=<phase0-epic-id>

bd create "Fix S2325 Make Methods Static (78)" \
  --description="Fix 78 S2325 warnings: Make methods static. Methods that don't use instance state should be static." \
  --type=task --priority=0 --deps=<phase0-epic-id>

bd create "Fix SA1117 Parameter Alignment (66)" \
  --description="Fix 66 SA1117 warnings: Parameters must be on same line or all on separate lines. Reformat method calls." \
  --type=task --priority=0 --deps=<phase0-epic-id>

bd create "Fix MA0051 Method Length (50)" \
  --description="Fix 50 MA0051 warnings: Method is too long. Refactor methods exceeding 60 lines into smaller methods." \
  --type=task --priority=0 --deps=<phase0-epic-id>

bd create "Fix S4144 Duplicate Method Implementations (10)" \
  --description="Fix 10 S4144 warnings: Update methods so implementation is not identical. Extract common code." \
  --type=task --priority=0 --deps=<phase0-epic-id>

bd create "Fix S1172 Unused Parameters (10)" \
  --description="Fix 10 S1172 warnings: Remove unused parameters or use them." \
  --type=task --priority=0 --deps=<phase0-epic-id>

bd create "Fix SA1101 Prefix Local Calls with this (20)" \
  --description="Fix 20 SA1101 warnings: Prefix local calls with this." \
  --type=task --priority=0 --deps=<phase0-epic-id>

bd create "Fix MA0015 Event Level Specification (18)" \
  --description="Fix 18 MA0015 warnings: Specify the EventLevel when using LoggerMessageAttribute." \
  --type=task --priority=0 --deps=<phase0-epic-id>

bd create "Fix Remaining Miscellaneous Warnings (32)" \
  --description="Fix remaining warnings: SA1203, S4487, MA0025, S3267, MA0004, S6608, S1854, MA0011, SA1316" \
  --type=task --priority=0 --deps=<phase0-epic-id>

bd create "Phase 0 Verification Gate" \
  --description="Verify zero warnings: dotnet build Synaxis.sln -warnaserror && dotnet format --verify-no-changes" \
  --type=task --priority=0 --deps=<phase0-epic-id>
```

### Phase 1: Shared Infrastructure
```bash
# Create Phase 1 Epic
bd create "Phase 1: Shared Infrastructure" \
  --description="Create shared kernel, event sourcing abstractions, multi-cloud layer, BYOK encryption, and messaging infrastructure." \
  --type=epic --priority=0 --deps=<phase0-completion>

# Core Infrastructure
bd create "P1-001: Create Shared Kernel Project" \
  --description="Create Synaxis.SharedKernel project with cross-cutting types, base entities, value objects, and common utilities." \
  --type=task --priority=0

bd create "P1-002: Event Sourcing Abstractions" \
  --description="Define IEventStore, IEvent, IAggregateRoot, ISnapshotStore, IEventBus interfaces and base implementations." \
  --type=task --priority=0

bd create "P1-003: Multi-Cloud Abstraction Layer" \
  --description="Create ICloudProvider, IKeyVault, IStorage, ISecretsManager abstractions for cloud-agnostic infrastructure." \
  --type=task --priority=0

# Cloud Providers (parallel)
bd create "P1-004: Azure Provider Implementation" \
  --description="Implement Azure Key Vault, Blob Storage, Event Grid integrations." \
  --type=task --priority=1

bd create "P1-005: AWS Provider Implementation" \
  --description="Implement AWS KMS, S3, EventBridge integrations." \
  --type=task --priority=1

bd create "P1-006: GCP Provider Implementation" \
  --description="Implement GCP KMS, Cloud Storage, Pub/Sub integrations." \
  --type=task --priority=1

bd create "P1-007: OnPrem Provider Implementation" \
  --description="Implement HashiCorp Vault, MinIO, RabbitMQ for on-premises deployments." \
  --type=task --priority=1

# Dependent Infrastructure
bd create "P1-008: BYOK Encryption Service" \
  --description="Implement tenant-specific key management with key rotation support. Multi-provider key storage." \
  --type=task --priority=0

bd create "P1-009: Multi-Tenant Context Propagation" \
  --description="Implement TenantId resolution and propagation through middleware and async contexts." \
  --type=task --priority=0

bd create "P1-010: Event Store Implementations" \
  --description="Implement PostgreSQL (Marten), Azure Cosmos DB, DynamoDB event store adapters." \
  --type=task --priority=0

bd create "P1-011: Message Bus Abstractions" \
  --description="Create IEventBus, ICommandBus interfaces and implementations for cross-context communication." \
  --type=task --priority=0

bd create "P1-012: Integration Event Catalog" \
  --description="Document and define all cross-context integration events with versioning strategy." \
  --type=task --priority=0

bd create "P1-013: Observability Infrastructure" \
  --description="Set up OpenTelemetry, structured logging, distributed tracing across all contexts." \
  --type=task --priority=1

bd create "P1-014: Testing Infrastructure" \
  --description="Create shared test fixtures, event sourcing test helpers, integration test utilities." \
  --type=task --priority=1
```

### Phase 2: Identity Bounded Context
```bash
bd create "Phase 2: Identity Bounded Context" \
  --description="Authentication, authorization, user management, organizations, teams, RBAC with event sourcing." \
  --type=epic --priority=0 --deps=<phase1-completion>

# Structure
bd create "P2-001: Identity Project Structure" \
  --description="Create Synaxis.Identity.Domain, .Application, .Infrastructure, .Api projects with proper references." \
  --type=task --priority=0

# Domain
bd create "P2-002: Identity Domain Model" \
  --description="Implement User, Organization, Team aggregates with event sourcing. Value objects, entities, domain events." \
  --type=task --priority=0

bd create "P2-003: Identity Events" \
  --description="Define UserCreated, UserUpdated, OrganizationCreated, TeamCreated, UserAuthenticated domain events." \
  --type=task --priority=0

# Services
bd create "P2-004: Authentication Service Migration" \
  --description="Migrate JWT, MFA, password policies, session management from Synaxis.Infrastructure." \
  --type=task --priority=0

bd create "P2-005: Authorization Service Migration" \
  --description="Migrate RBAC, permissions, authorization policies from Synaxis.Core." \
  --type=task --priority=0

bd create "P2-006: Invitation System Migration" \
  --description="Migrate invitation lifecycle with event sourcing from existing implementation." \
  --type=task --priority=1

# Infrastructure
bd create "P2-007: Identity Event Store" \
  --description="Configure PostgreSQL event store with Marten for Identity context." \
  --type=task --priority=0

bd create "P2-008: Identity Read Models" \
  --description="Create projections for user queries, organization queries, team membership queries." \
  --type=task --priority=0

# API
bd create "P2-009: Identity API Controllers" \
  --description="Migrate REST endpoints from Synaxis.Api: auth, users, organizations, teams." \
  --type=task --priority=0

bd create "P2-010: Identity Integration Events" \
  --description="Publish user/org events to other contexts via integration event bus." \
  --type=task --priority=0

# Testing
bd create "P2-011: Identity Unit Tests" \
  --description="Domain logic tests, event sourcing tests, aggregate behavior tests." \
  --type=task --priority=0

bd create "P2-012: Identity Integration Tests" \
  --description="API integration tests, event publishing tests, multi-tenant tests." \
  --type=task --priority=0

# Migration
bd create "P2-013: Identity Data Migration Scripts" \
  --description="Create scripts to migrate identity entities from SynaxisDbContext to event streams." \
  --type=task --priority=0
```

### Phase 3: Inference Bounded Context
```bash
bd create "Phase 3: Inference Bounded Context" \
  --description="LLM inference, model routing, quota management, usage tracking with event sourcing." \
  --type=epic --priority=0 --deps=<phase1-completion>

bd create "P3-001: Inference Project Structure" \
  --description="Create Synaxis.Inference.Domain, .Application, .Infrastructure, .Api projects." \
  --type=task --priority=0

bd create "P3-002: Inference Domain Model" \
  --description="Implement Request, Model, Provider aggregates with event sourcing." \
  --type=task --priority=0

bd create "P3-003: Inference Events" \
  --description="Define InferenceRequested, InferenceCompleted, QuotaExceeded, ModelRouted events." \
  --type=task --priority=0

bd create "P3-004: Model Routing Migration" \
  --description="Migrate routing logic from InferenceGateway with event sourcing." \
  --type=task --priority=0

bd create "P3-005: Quota Management Migration" \
  --description="Migrate VirtualKey quotas, rate limiting from existing services." \
  --type=task --priority=0

bd create "P3-006: Usage Tracking Migration" \
  --description="Migrate SpendLog, CreditTransaction events from Synaxis.Infrastructure." \
  --type=task --priority=0

bd create "P3-007: Provider Integration Migration" \
  --description="Migrate OpenAI, Anthropic, Azure provider adapters." \
  --type=task --priority=0

bd create "P3-008: Inference Event Store" \
  --description="Configure separate PostgreSQL schema for inference events." \
  --type=task --priority=0

bd create "P3-009: Inference Read Models" \
  --description="Create usage projections, quota projections, request history queries." \
  --type=task --priority=0

bd create "P3-010: Inference API Controllers" \
  --description="Migrate chat completions, embeddings endpoints from InferenceGateway." \
  --type=task --priority=0

bd create "P3-011: Inference Integration Events" \
  --description="Publish usage events to Billing context." \
  --type=task --priority=0

bd create "P3-012: Inference Streaming" \
  --description="Implement server-sent events, streaming responses for LLM output." \
  --type=task --priority=1

bd create "P3-013: Inference Unit Tests" \
  --description="Domain logic tests, routing algorithm tests, provider adapter tests." \
  --type=task --priority=0

bd create "P3-014: Inference Integration Tests" \
  --description="End-to-end inference flow tests, quota enforcement tests." \
  --type=task --priority=0

bd create "P3-015: Inference Data Migration Scripts" \
  --description="Create scripts to migrate request logs to event streams." \
  --type=task --priority=0
```

### Phase 4: Agents Bounded Context
```bash
bd create "Phase 4: Agents Bounded Context" \
  --description="Agent definition, agent execution, tool integration, Microsoft Agent Framework with event sourcing." \
  --type=epic --priority=0 --deps=<phase1-completion>

bd create "P4-001: Agents Project Structure" \
  --description="Create Synaxis.Agents.Domain, .Application, .Infrastructure, .Api projects." \
  --type=task --priority=0

bd create "P4-002: Agents Domain Model" \
  --description="Implement Agent, Tool, Session aggregates with event sourcing." \
  --type=task --priority=0

bd create "P4-003: Agent Events" \
  --description="Define AgentCreated, AgentExecuted, ToolCalled, SessionStarted events." \
  --type=task --priority=0

bd create "P4-004: Microsoft Agent Framework Integration" \
  --description="Integrate Azure AI Agent Service and Semantic Kernel for agent execution." \
  --type=task --priority=0

bd create "P4-005: Tool System Migration" \
  --description="Migrate MCP tools, custom tools, tool registration system." \
  --type=task --priority=0

bd create "P4-006: Agent Orchestration Migration" \
  --description="Migrate multi-agent workflows from AgentOrchestrator." \
  --type=task --priority=0

bd create "P4-007: Conversation System Migration" \
  --description="Migrate Conversation, ConversationTurn to event sourcing." \
  --type=task --priority=0

bd create "P4-008: Agents Event Store" \
  --description="Configure separate event store for agent lifecycle." \
  --type=task --priority=0

bd create "P4-009: Agents Read Models" \
  --description="Create agent state projections, session history queries." \
  --type=task --priority=0

bd create "P4-010: Agents API Controllers" \
  --description="Implement agent management and execution endpoints." \
  --type=task --priority=0

bd create "P4-011: MCP Adapter Migration" \
  --description="Migrate Model Context Protocol integration from Synaxis.Adapters.Mcp." \
  --type=task --priority=1

bd create "P4-012: Agents Integration Events" \
  --description="Publish agent events to Orchestration context." \
  --type=task --priority=0

bd create "P4-013: Agents Unit Tests" \
  --description="Agent logic tests, tool execution tests." \
  --type=task --priority=0

bd create "P4-014: Agents Integration Tests" \
  --description="Agent workflow integration tests." \
  --type=task --priority=0

bd create "P4-015: Agents Data Migration Scripts" \
  --description="Migrate conversations to event streams." \
  --type=task --priority=0
```

### Phase 5: Orchestration Bounded Context
```bash
bd create "Phase 5: Orchestration Bounded Context" \
  --description="Multi-agent workflows, long-running processes, saga coordination with event sourcing." \
  --type=epic --priority=0 --deps=<phase1-completion>,<agents-domain>

bd create "P5-001: Orchestration Project Structure" \
  --description="Create Synaxis.Orchestration.Domain, .Application, .Infrastructure, .Api projects." \
  --type=task --priority=0

bd create "P5-002: Orchestration Domain Model" \
  --description="Implement Workflow, Saga, Activity aggregates with event sourcing." \
  --type=task --priority=0

bd create "P5-003: Orchestration Events" \
  --description="Define WorkflowStarted, ActivityCompleted, SagaCompensated events." \
  --type=task --priority=0

bd create "P5-004: Saga Pattern Implementation" \
  --description="Implement distributed transaction coordination with compensation." \
  --type=task --priority=0

bd create "P5-005: Workflow Engine Integration" \
  --description="Integrate Temporal.io or custom workflow engine for durable execution." \
  --type=task --priority=0

bd create "P5-006: Parallel Work Stream Support" \
  --description="Implement multi-agent parallel execution coordination." \
  --type=task --priority=0

bd create "P5-007: Compensation Logic" \
  --description="Design and implement rollback strategies for failed operations." \
  --type=task --priority=0

bd create "P5-008: Orchestration Event Store" \
  --description="Configure durable event store for long-running processes." \
  --type=task --priority=0

bd create "P5-009: Orchestration Read Models" \
  --description="Create workflow state projections, execution history." \
  --type=task --priority=0

bd create "P5-010: Orchestration API Controllers" \
  --description="Implement workflow management and execution control endpoints." \
  --type=task --priority=0

bd create "P5-011: Integration with Other Contexts" \
  --description="Subscribe to events from all contexts for orchestration triggers." \
  --type=task --priority=0

bd create "P5-012: Orchestration Unit Tests" \
  --description="Saga logic tests, compensation tests." \
  --type=task --priority=0

bd create "P5-013: Orchestration Integration Tests" \
  --description="End-to-end workflow tests." \
  --type=task --priority=0

bd create "P5-014: Chaos Engineering Tests" \
  --description="Failure injection, resilience testing." \
  --type=task --priority=1
```

### Phase 6-9: Remaining Phases
```bash
# Phase 6: Billing
bd create "Phase 6: Billing Bounded Context" \
  --description="Invoicing, payments, credit management, billing reports with event sourcing." \
  --type=epic --priority=1 --deps=<phase1-completion>,<identity-events>,<inference-events>

# Phase 7: Audit
bd create "Phase 7: Audit Bounded Context" \
  --description="Audit logging, compliance reports, data retention with event sourcing." \
  --type=epic --priority=1 --deps=<phase1-completion>,<all-context-events>

# Phase 8: API Gateway
bd create "Phase 8: API Gateway" \
  --description="Unified API gateway, BFF aggregation, routing to bounded contexts." \
  --type=epic --priority=0 --deps=<identity-api>,<inference-api>,<agents-api>,<orch-api>,<billing-api>

# Phase 9: Big Bang Migration
bd create "Phase 9: Big Bang Migration" \
  --description="Execute big bang migration from monolith to bounded contexts with event sourcing." \
  --type=epic --priority=0 --deps=<all-contexts-complete>,<data-migrations-ready>,<gateway-ready>
```

---

## Quick Reference: Phase Exit Criteria

### Phase 0
```bash
# Must all pass
dotnet build Synaxis.sln -warnaserror
dotnet format --verify-no-changes
dotnet test Synaxis.sln --no-build
```

### Phase 1
```bash
# Must all pass
dotnet build src/Synaxis.SharedKernel/Synaxis.SharedKernel.csproj -warnaserror
dotnet test tests/Synaxis.SharedKernel.Tests --no-build
# Azure provider functional
# BYOK encryption tested
# Event store (PostgreSQL/Marten) functional
```

### Phase 2-7 (Each Context)
```bash
# Must all pass
dotnet build src/<Context>/Synaxis.<Context>.sln -warnaserror
dotnet test tests/Synaxis.<Context>.Tests
# API endpoints functional
# Integration events publishing/subscribing
# Data migration scripts tested
# Code coverage >80%
```

### Phase 8
```bash
# Must all pass
dotnet build src/ApiGateway/Synaxis.ApiGateway.csproj -warnaserror
dotnet test tests/Synaxis.ApiGateway.Tests
# All routes functional
# JWT validation working
# Health check aggregation accurate
```

### Phase 9
```bash
# Must all pass
# Production migration completed
# Data integrity verified (row counts, checksums)
# All health checks passing
# Smoke tests passing
# Performance benchmarks met
```

---

## Team Assignment Matrix

| Phase | Team | Size | Duration | Start Week | End Week |
|-------|------|------|----------|------------|----------|
| P0 | Foundation | 3-4 | 2.5 weeks | 1 | 3 |
| P1 | Infrastructure | 3-4 | 9 weeks | 3 | 12 |
| P2 | Identity | 2-3 | 5 weeks | 12 | 17 |
| P3 | Inference | 2-3 | 6 weeks | 12 | 18 |
| P4 | Agents | 2-3 | 6 weeks | 12 | 18 |
| P5 | Orchestration | 2-3 | 7 weeks | 14 | 21 |
| P6 | Billing | 1-2 | 5 weeks | 20 | 25 |
| P7 | Audit | 1-2 | 4 weeks | 22 | 26 |
| P8 | Gateway | 1-2 | 3 weeks | 30 | 33 |
| P9 | Migration | 2-3 | 6 weeks | 32 | 37 |

---

## Dependency Quick Reference

### Hard Blockers (Cannot proceed without)
- P0-010 → P1-001, P1-002, P1-003
- P1-001, P1-002, P1-010, P1-012 → P2-001, P3-001, P4-001, P5-001
- P2-009 → P8-003
- P3-010 → P8-003
- P4-010 → P8-003
- P5-010 → P8-003
- P6-009 → P8-003
- P2-013, P3-015, P4-015, P6-013, P7-012, P8-007 → P9-002

### Soft Dependencies (Can work in parallel with coordination)
- P1-002 ↔ P1-003 (Event sourcing informs cloud abstraction)
- P2-010 → P6-010 (Billing subscribes to Identity)
- P3-011 → P6-010 (Billing processes Inference usage)

---

## Risk Register Summary

| ID | Risk | Probability | Impact | Mitigation |
|----|------|-------------|--------|------------|
| R1 | BYOK implementation complexity | Medium | High | Early PoC, security review |
| R2 | Event store performance issues | Medium | High | Load testing early |
| R3 | MS Agent Framework integration | Medium | High | Spike, vendor support |
| R4 | Big bang migration failure | Low | Critical | Multiple rehearsals, rollback plan |
| R5 | Team capacity constraints | Medium | Medium | Buffer time, parallel streams |
| R6 | Integration event contract drift | Medium | Medium | Consumer-driven contracts |

---

## Daily Standup Questions

1. What did you complete yesterday that brings us closer to zero warnings / phase completion?
2. What are you working on today?
3. Are you blocked by any dependencies?
4. Are you blocking any other work?
5. Any new risks or concerns?

---

## Weekly Review Checklist

- [ ] Progress against timeline
- [ ] Blockers identified and escalated
- [ ] Quality gates status
- [ ] Risk register updated
- [ ] Dependency changes communicated
- [ ] Resource allocation review
- [ ] Next week priorities set
