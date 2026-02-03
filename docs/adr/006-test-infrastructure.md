# ADR-006: Test Infrastructure Strategy

## Status
**Accepted** | 2026-02-03

## Context

The Inference Gateway is a critical infrastructure component that routes LLM requests with high availability requirements. We need a testing strategy that provides confidence in:
- Correctness of routing logic and provider integrations
- Resilience under failure conditions
- Performance under load
- Security of authentication and authorization
- Integration with external dependencies (Redis, Qdrant, PostgreSQL)

## Decision

Implement a **comprehensive multi-layer testing strategy**:

### Test Pyramid Layers

```
       /\
      /  \
     / E2E \        Integration Tests (E2E, Provider Validation)
    /________\
   /          \
  / Integration \   Infrastructure Integration (TestContainers)
 /______________\
/                \
/     Unit        \  Unit Tests, Property Tests
/__________________\
```

### 1. Unit Tests
**Scope**: Individual classes, methods, pure functions
**Location**: `tests/InferenceGateway/UnitTests/`
**Tools**: xUnit, Moq, FluentAssertions
**Coverage Target**: >80%

**Key Areas:**
- Routing logic (SmartRouter)
- Provider adapters
- Configuration resolution
- Error handling

### 2. Property-Based Tests
**Scope**: Invariant validation across random inputs
**Tools**: FsCheck
**Purpose**: Find edge cases traditional tests miss

**Properties Tested:**
- Idempotency of cache operations
- Monotonicity of circuit breaker states
- Round-robin distribution fairness

### 3. Integration Tests (Infrastructure)
**Scope**: Database, cache, and external service integration
**Location**: `tests/InferenceGateway/IntegrationTests/`
**Tools**: TestContainers, WebApplicationFactory

**TestContainers Setup:**
- PostgreSQL for persistence tests
- Redis for caching tests
- Qdrant for semantic cache tests

### 4. End-to-End Tests
**Scope**: Full request lifecycle through HTTP endpoints
**Location**: `tests/InferenceGateway/IntegrationTests/`
**Categories:**
- **Smoke Tests**: Basic functionality, health checks
- **Provider Validation**: Live API calls to each provider
- **Security Tests**: JWT validation, API key management
- **Performance Tests**: Load testing and latency benchmarks

### 5. Mutation Testing
**Tool**: Stryker Mutator
**Purpose**: Validate test quality by introducing bugs
**Threshold**: 70% mutation score minimum

### Continuous Testing Strategy

**Pre-commit**: Fast unit tests (<30s)
**Pre-merge**: Full test suite + coverage check
**Nightly**: Mutation testing, extended integration tests
**Weekly**: Full E2E against live providers

## Test Data Management

**Test Data Factory Pattern:**
- Centralized test data generation
- Consistent seed data for reproducibility
- Disposable test databases per test run

**Mock Providers:**
- HttpClient interception for external APIs
- Configurable response latency and errors
- Recording/replay for deterministic tests

## Code Coverage

**Baseline**: `COVERAGE_BASELINE.md` tracks current coverage
**Tool**: Coverlet + ReportGenerator
**Gates:**
- No PR decreases overall coverage
- New code must have >80% coverage
- Critical paths require >90% coverage

## Consequences

### Positive
- **High Confidence**: Multiple layers catch different bug classes
- **Fast Feedback**: Unit tests provide immediate validation
- **Regression Prevention**: Comprehensive suite prevents regressions
- **Living Documentation**: Tests document expected behavior

### Negative
- **Maintenance Burden**: Test code requires ongoing maintenance
- **CI Time**: Full suite takes 10-15 minutes
- **Flakiness**: Integration tests can be flaky (mitigated with retries)
- **Test Data Complexity**: Realistic test data requires effort

## Implementation Notes

- TestContainers provide isolated databases per test
- xUnit's `IAsyncLifetime` for async test setup/teardown
- Parallel test execution enabled where safe
- Smoke tests categorized with `[Trait("Category", "Smoke")]`

## Related ADRs
- ADR-001: Stream-Native CQRS (testing streaming behavior)
- ADR-003: Authentication Architecture (security testing)
