# Testing Guide

> **ULTRA MISER MODE™ Testing Philosophy**: Why pay for test infrastructure when you can run 1050 tests locally for the cost of electricity? Every passing test is a victory against subscription-based CI/CD services.

Synaxis maintains a comprehensive test suite with **1050 total tests** covering the entire stack—from backend routing logic to frontend React components. This guide covers everything you need to know about running, writing, and maintaining tests.

---

## Test Infrastructure Overview

### Test Statistics

| Category | Count | Framework | Coverage |
|----------|-------|-----------|----------|
| **Backend Unit Tests** | ~635 | xUnit | Core logic, services, utilities |
| **Backend Integration Tests** | ~150 | xUnit + Testcontainers | Database, routing pipeline |
| **Frontend Unit Tests** | ~415 | Vitest + React Testing Library | Components, stores, utilities |
| **E2E Tests** | ~50 | Playwright | Full user workflows |
| **Total** | **~1050** | — | **85%+ overall** |

### Test Organization

```
tests/
├── InferenceGateway/
│   ├── Application.Tests/          # Application layer unit tests
│   ├── Infrastructure.Tests/       # Infrastructure layer tests
│   │   ├── ControlPlane/           # Database and storage tests
│   │   ├── Identity/               # Authentication tests
│   │   └── Integration/            # OAuth flow tests
│   └── IntegrationTests/           # Full pipeline integration tests
├── InferenceGateway.UnitTests/     # Core domain unit tests
│   └── Retry/                      # Retry policy tests
└── Common/                         # Shared test utilities
    ├── Factories/                  # Test data builders
    └── Infrastructure/             # In-memory database helpers

src/Synaxis.WebApp/ClientApp/
├── src/__tests__/                  # Integration tests
├── src/**/*.test.tsx               # Component tests (co-located)
└── src/**/*.test.ts                # Utility tests (co-located)
```

---

## Running Tests

### Backend Tests (.NET)

#### Run All Tests

```bash
# Run all backend tests (the miser way - one command, zero dollars)
dotnet test src/InferenceGateway/InferenceGateway.sln

# Run with verbosity (for when you need to see what's happening)
dotnet test src/InferenceGateway/InferenceGateway.sln --verbosity normal

# Run in Release mode (faster, like your provider rotation)
dotnet test src/InferenceGateway/InferenceGateway.sln --configuration Release
```

#### Run Specific Test Projects

```bash
# Unit tests only (fast, isolated, pure)
dotnet test tests/InferenceGateway.UnitTests

# Application layer tests
dotnet test tests/InferenceGateway/Application.Tests

# Infrastructure tests (database, file I/O)
dotnet test tests/InferenceGateway/Infrastructure.Tests

# Integration tests (full pipeline)
dotnet test tests/InferenceGateway/IntegrationTests
```

#### Run Tests by Category

```bash
# Run only unit tests (skip integration)
dotnet test --filter "Category!=Integration"

# Run only integration tests
dotnet test --filter "Category=Integration"

# Run performance tests
dotnet test --filter "Category=Performance"

# Run specific test by name
dotnet test --filter "FullyQualifiedName~SmartRouter"
```

#### Test Collections and Parallelization

```bash
# Run tests in parallel (maximize that CPU usage)
dotnet test --parallel

# Run with specific number of parallel workers
dotnet test --maxcpucount:4
```

### Frontend Tests (React/Vitest)

#### Run All Tests

```bash
cd src/Synaxis.WebApp/ClientApp

# Run once (CI mode)
npm test

# Run with coverage (see how much you're getting for free)
npm run test:coverage

# Run in watch mode (for development)
npm run test -- --watch
```

#### Run Specific Tests

```bash
# Run tests matching pattern
npm test -- --grep "ApiKeyList"

# Run specific file
npm test -- src/features/dashboard/keys/ApiKeyList.test.tsx

# Run with UI (for debugging)
npm run test -- --ui
```

### E2E Tests (Playwright)

```bash
cd src/Synaxis.WebApp/ClientApp

# Run all E2E tests
npm run test:e2e

# Run with UI mode (for debugging)
npm run test:e2e:ui

# Run specific test file
npx playwright test tests/e2e/auth.spec.ts
```

### Performance Tests

```bash
# Run performance benchmarks
dotnet test tests/InferenceGateway/IntegrationTests --filter "Category=Performance"

# Run with detailed output
dotnet test tests/InferenceGateway/IntegrationTests --filter "Category=Performance" --verbosity detailed
```

---

## Test Categories

### Unit Tests

Unit tests verify individual components in isolation. They are fast, reliable, and should comprise the majority of your test suite.

**Characteristics:**
- No external dependencies (databases, network)
- Mocked collaborators
- Fast execution (< 100ms per test)
- High volume (hundreds of tests)

**Example Locations:**
- `tests/InferenceGateway.UnitTests/` - Core domain logic
- `tests/InferenceGateway/Application.Tests/` - Application services
- `src/Synaxis.WebApp/ClientApp/src/**/*.test.ts` - Frontend utilities

**Running:**
```bash
dotnet test --filter "Category!=Integration"
npm test -- --grep "unit"
```

### Integration Tests

Integration tests verify that multiple components work together correctly. They use real dependencies where appropriate.

**Characteristics:**
- Real database (in-memory or Testcontainers)
- Real HTTP clients (mocked responses)
- Test full request pipelines
- Slower than unit tests but more comprehensive

**Example Locations:**
- `tests/InferenceGateway/IntegrationTests/` - Full routing pipeline
- `tests/InferenceGateway/Infrastructure.Tests/Integration/` - OAuth flows
- `src/Synaxis.WebApp/ClientApp/src/__tests__/` - Frontend integration

**Running:**
```bash
dotnet test --filter "Category=Integration"
npm test -- src/__tests__
```

### Performance Tests

Performance tests verify that the system meets performance requirements under load.

**Characteristics:**
- Concurrent request handling
- Memory footprint monitoring
- Response time benchmarks
- Scalability validation

**Example Locations:**
- `tests/InferenceGateway/IntegrationTests/PerformanceTests.cs`

**Running:**
```bash
dotnet test --filter "Category=Performance"
```

**Key Metrics:**
- **Throughput**: >100 requests/second
- **Response Time**: <100ms per routing request
- **Concurrent Handling**: 100+ concurrent requests
- **Memory**: <1KB per request

---

## Writing New Tests

### Backend Test Patterns

#### Unit Test Structure

```csharp
public class SmartRouterTests
{
    private readonly Mock<ICostService> _costServiceMock;
    private readonly Mock<IProviderHealthService> _healthServiceMock;
    private readonly SmartRouter _router;

    public SmartRouterTests()
    {
        _costServiceMock = new Mock<ICostService>();
        _healthServiceMock = new Mock<IProviderHealthService>();
        _router = new SmartRouter(_costServiceMock.Object, _healthServiceMock.Object);
    }

    [Fact]
    public void SelectProvider_WithValidCandidates_ReturnsCheapestProvider()
    {
        // Arrange
        var candidates = new[]
        {
            new ProviderCandidate { Id = "groq", Cost = 0.001m },
            new ProviderCandidate { Id = "deepseek", Cost = 0.0005m }
        };

        // Act
        var result = _router.SelectProvider(candidates, "gpt-4");

        // Assert
        Assert.Equal("deepseek", result.Id); // Cheapest wins (miser mode)
    }
}
```

#### Integration Test Structure

```csharp
public class ProviderRoutingIntegrationTests : IDisposable
{
    private readonly ControlPlaneDbContext _dbContext;
    private readonly ServiceProvider _serviceProvider;

    public ProviderRoutingIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddDbContext<ControlPlaneDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        // ... add other services

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<ControlPlaneDbContext>();
    }

    [Fact]
    public async Task RouteRequest_WithMultipleProviders_SelectsOptimalProvider()
    {
        // Arrange - seed database
        await SeedTestDataAsync();

        // Act - execute full pipeline
        var result = await _router.RouteAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
        _serviceProvider?.Dispose();
    }
}
```

#### Test Data Builders

Use builders for complex test data to keep tests readable:

```csharp
public class ModelAliasBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _alias = "gpt-4";
    private Guid _tenantId = Guid.NewGuid();

    public ModelAliasBuilder WithAlias(string alias)
    {
        _alias = alias;
        return this;
    }

    public ModelAliasBuilder WithTenant(Guid tenantId)
    {
        _tenantId = tenantId;
        return this;
    }

    public ModelAlias Build()
    {
        return new ModelAlias
        {
            Id = _id,
            Alias = _alias,
            TenantId = _tenantId
        };
    }
}

// Usage
var alias = new ModelAliasBuilder()
    .WithAlias("claude-3")
    .WithTenant(tenantId)
    .Build();
```

### Frontend Test Patterns

#### Component Test Structure

```typescript
import { render, screen, fireEvent } from '@testing-library/react';
import { ApiKeyList } from './ApiKeyList';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

describe('ApiKeyList', () => {
  const queryClient = new QueryClient();

  const renderComponent = () => {
    return render(
      <QueryClientProvider client={queryClient}>
        <ApiKeyList />
      </QueryClientProvider>
    );
  };

  it('renders API keys list', () => {
    renderComponent();
    expect(screen.getByText('API Keys')).toBeInTheDocument();
  });

  it('opens creation modal on button click', () => {
    renderComponent();
    fireEvent.click(screen.getByText('Create Key'));
    expect(screen.getByText('Create New API Key')).toBeInTheDocument();
  });
});
```

#### Store Test Structure

```typescript
import { createUsageStore } from './usage';

describe('usage store', () => {
  it('tracks token usage', () => {
    const store = createUsageStore();
    store.addUsage({ tokens: 100, cost: 0.001 });
    expect(store.totalTokens).toBe(100);
    expect(store.totalCost).toBe(0.001);
  });
});
```

### Test Naming Conventions

**Backend (xUnit):**
- MethodName_StateUnderTest_ExpectedBehavior
- Examples:
  - `SelectProvider_WithValidCandidates_ReturnsCheapestProvider`
  - `RouteRequest_WhenAllProvidersFail_ReturnsError`
  - `ValidateToken_WithExpiredToken_ReturnsFalse`

**Frontend (Vitest):**
- should_expectedBehavior_when_state
- Examples:
  - `should render API keys list`
  - `should open creation modal on button click`
  - `should track token usage correctly`

### Test Categories and Traits

Mark tests with appropriate categories:

```csharp
[Fact]
[Trait("Category", "Unit")]
public void SelectProvider_ReturnsCheapest()
{
    // Unit test
}

[Fact]
[Trait("Category", "Integration")]
public async Task RouteRequest_FullPipeline()
{
    // Integration test
}

[Fact]
[Trait("Category", "Performance")]
public async Task ConcurrentRequests_HandlesLoad()
{
    // Performance test
}
```

---

## CI/CD Integration

### GitHub Actions Workflow

```yaml
name: Test Suite

on: [push, pull_request]

jobs:
  backend-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Run unit tests
        run: dotnet test --filter "Category!=Integration" --verbosity normal
      
      - name: Run integration tests
        run: dotnet test --filter "Category=Integration" --verbosity normal
      
      - name: Generate coverage report
        run: |
          dotnet test --collect:"XPlat Code Coverage"
          reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coverage

  frontend-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '20'
      
      - name: Install dependencies
        run: |
          cd src/Synaxis.WebApp/ClientApp
          npm ci
      
      - name: Run tests
        run: |
          cd src/Synaxis.WebApp/ClientApp
          npm test
      
      - name: Run E2E tests
        run: |
          cd src/Synaxis.WebApp/ClientApp
          npm run test:e2e
```

### Quality Gates

| Metric | Minimum | Target |
|--------|---------|--------|
| **Line Coverage** | 80% | 90% |
| **Branch Coverage** | 70% | 85% |
| **Test Success Rate** | 100% | 100% |
| **Flaky Tests** | 0 | 0 |
| **Performance Regression** | <10% | <5% |

### Pre-Commit Hooks

```bash
#!/bin/bash
# .git/hooks/pre-commit

# Run fast unit tests before commit
dotnet test --filter "Category!=Integration" --verbosity quiet
if [ $? -ne 0 ]; then
    echo "Tests failed. Commit aborted."
    exit 1
fi

cd src/Synaxis.WebApp/ClientApp
npm test
if [ $? -ne 0 ]; then
    echo "Frontend tests failed. Commit aborted."
    exit 1
fi
```

---

## Test Utilities and Helpers

### Common Test Infrastructure

The `tests/Common/` project provides shared utilities:

```csharp
// Test data factories
public static class TestDataFactory
{
    public static Provider CreateProvider(string id = "groq")
    {
        return new Provider { Id = id, Name = id };
    }
}

// In-memory database setup
public class InMemoryDbContext : ControlPlaneDbContext
{
    public InMemoryDbContext() : base(
        new DbContextOptionsBuilder<ControlPlaneDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options)
    { }
}
```

### Frontend Test Utilities

```typescript
// test-utils.tsx
import { render } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

export function renderWithProviders(ui: React.ReactElement) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } }
  });

  return render(
    <QueryClientProvider client={queryClient}>
      {ui}
    </QueryClientProvider>
  );
}
```

---

## Troubleshooting Tests

### Common Issues

#### Tests Fail in CI but Pass Locally

**Causes:**
- Environment differences (timezone, culture)
- Missing environment variables
- Database state pollution

**Solutions:**
```csharp
// Use invariant culture
Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

// Isolate database per test
options.UseInMemoryDatabase(Guid.NewGuid().ToString())
```

#### Flaky Tests

**Signs:**
- Intermittent failures
- Timing-dependent behavior
- Race conditions

**Solutions:**
- Avoid `Task.Delay` in tests
- Use synchronization primitives
- Mock time-based operations

#### Slow Tests

**Optimization:**
```bash
# Run in parallel
dotnet test --parallel

# Skip slow tests during development
dotnet test --filter "Category!=Slow"

# Profile test execution
dotnet test --diag:logs.txt --verbosity detailed
```

### Debugging Tests

```bash
# Run specific test with debugging
dotnet test --filter "FullyQualifiedName~TestName" -v n

# Attach debugger
dotnet test --filter "FullyQualifiedName~TestName" --debug

# Frontend debugging
npm test -- --reporter=verbose
```

---

## ULTRA MISER MODE™ Testing Tips

1. **Run tests locally first** — CI minutes are for validation, not discovery
2. **Use `--filter`** — Don't run 1000 tests when you only changed one file
3. **Parallelize everything** — Your CPU has cores. Use them.
4. **Mock external services** — Real API calls cost money. Mocks are free.
5. **Write fast tests** — Slow tests get skipped. Fast tests get run.
6. **Test the happy path first** — Edge cases are for people with time (and money)
7. **Coverage is a guide, not a goal** — 100% coverage of useless code is still useless

---

## Related Documentation

- [Architecture Overview](ARCHITECTURE.md) — Understand the system structure
- [API Reference](API.md) — Test against real endpoints
- [Contributing Guide](CONTRIBUTING.md) — PR requirements include tests
- [Performance Guide](ops/performance.md) — Performance testing details

---

*Remember: Every test you write is a promise that your code works. Every test you run is a validation of that promise. And every test that passes is one more reason not to pay for a debugging service.*

**ULTRA MISER MODE™** — Test like your wallet depends on it. Because it does.
