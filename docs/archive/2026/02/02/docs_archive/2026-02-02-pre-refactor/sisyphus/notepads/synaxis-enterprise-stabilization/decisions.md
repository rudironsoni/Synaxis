# Synaxis Enterprise Stabilization - Decisions

## [2026-01-30] Session Start

### Architectural Decisions (from Plan)
- **Testing Strategy**: Two-layer approach
  - Unit/Integration Tests: Mock all external providers (deterministic)
  - Smoke Tests: Keep real providers but reduce frequency + add circuit breaker

- **"ALL permutations" Definition**: Limited to representative subset
  - 13 providers × 1 representative model each = 13 model tests
  - WebAPI: 3 endpoints × {streaming, non-streaming} × {happy, error} = 12 scenarios
  - WebApp: Same as WebAPI + UI interactions
  - Total: ~80 test scenarios (not exponential)

- **Representative Model Selection**: First model listed in appsettings.json for each provider

- **Test Prioritization**: Critical path first (Routing → Chat → API Endpoints)

### Scope Boundaries
- **Must Have**: Feature parity, streaming, admin UI, 80% coverage, zero flaky tests
- **Must NOT Have**: Skipping tests, #pragma/NOWARN, external provider dependencies in unit/integration tests, new features beyond WebAPI spec

### Technology Stack
- Backend: .NET 10, xUnit, Coverlet, Moq/NSubstitute, Testcontainers
- Frontend: React 19, Vite, TypeScript, Zustand, TanStack Query, Vitest, Playwright
- Infrastructure: PostgreSQL, Redis, Docker Compose

