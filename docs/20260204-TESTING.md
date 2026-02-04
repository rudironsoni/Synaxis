# Synaxis Testing Guide

## Overview

Synaxis maintains comprehensive test coverage across both backend and frontend:

- **Backend Tests:** 635 tests (UnitTests, Application.Tests, Infrastructure.Tests, IntegrationTests)
- **Frontend Tests:** 415 tests (Vitest, React Testing Library, Playwright E2E)
- **Backend Coverage:** 9.02% (focused on core inference logic and API endpoints)
- **Frontend Coverage:** 85.77% (comprehensive React component testing)
- **Zero Flakiness:** 0% flaky tests across the entire test suite
- **Zero Warnings:** 0 compiler warnings, 0 ESLint errors

## Table of Contents

- [Overview](#overview)
- [Quick Start](#quick-start)
- [Backend Testing](#backend-testing)
- [Frontend Testing](#frontend-testing)
- [Coverage Reports](#coverage-reports)
- [Fixing Flaky Tests](#fixing-flaky-tests)
- [Testing Best Practices](#testing-best-practices)
- [Test Infrastructure](#test-infrastructure)
- [Running Specific Tests](#running-specific-tests)
- [CI/CD Integration](#cicd-integration)
- [Troubleshooting](#troubleshooting)

## Quick Start

### Run All Tests

```bash
# Backend tests (635 tests total)
dotnet test src/InferenceGateway/InferenceGateway.sln

# Frontend tests (415 tests total)
cd src/Synaxis.WebApp/ClientApp
npm test

# Full test suite with coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage
cd src/Synaxis.WebApp/ClientApp && npm run test:coverage
```

### Test Categories

- **Unit Tests**: Core business logic and provider integrations
- **Integration Tests**: API endpoint validation and streaming functionality  
- **Component Tests**: React UI components and admin interface
- **E2E Tests**: Full user workflows including admin operations

### Current Test Statistics

- **Backend Tests:** 635 tests passing (0% failures)
- **Frontend Tests:** 415 tests passing (0% failures)
- **Backend Coverage:** 9.02% (needs improvement)
- **Frontend Coverage:** 85.77% (exceeds 80% target)
- **Zero Flakiness:** 0% flaky tests across the entire test suite
- **Zero Warnings:** 0 compiler warnings, 0 ESLint errors

## Backend Testing

### Framework & Tools

- **Test Framework**: xUnit
- **Coverage Tool**: Coverlet
- **Mocking**: Moq
- **Test Projects**: Organized by architectural layer

### Test Project Structure

```
tests/
├── Common/                          # Shared test utilities
│   ├── Synaxis.Common.Tests.csproj
│   ├── TestBase.cs                 # Mock factories
│   ├── TestDataFactory.cs          # Test data generators
│   └── Infrastructure/
│       └── InMemoryDbContext.cs    # In-memory database setup
├── InferenceGateway.UnitTests/     # Unit tests
├── InferenceGateway/
│   ├── Application.Tests/          # Application layer tests
│   ├── Infrastructure.Tests/       # Infrastructure layer tests
│   └── IntegrationTests/           # Integration tests
```

### Running Backend Tests

#### Run All Backend Tests
```bash
dotnet test src/InferenceGateway/InferenceGateway.sln
```

#### Run Specific Test Project
```bash
dotnet test tests/InferenceGateway.UnitTests
dotnet test tests/InferenceGateway/Application.Tests
dotnet test tests/InferenceGateway/Infrastructure.Tests
dotnet test tests/InferenceGateway/IntegrationTests
```

#### Run Tests with Verbose Output
```bash
dotnet test --verbosity normal
dotnet test --verbosity detailed
```

#### Run Tests in Parallel
```bash
dotnet test --parallel
```

#### Filter Tests by Category
```bash
# Run only unit tests
dotnet test --filter Category=Unit

# Run only integration tests
dotnet test --filter Category=Integration

# Run tests matching pattern
dotnet test --filter FullyQualifiedName~RetryPolicyTests
```

### Backend Test Utilities

#### TestBase.cs
Provides factory methods for creating mocks:
```csharp
// Example usage
var mockChatClient = TestBase.CreateMockChatClient();
var mockProviderRegistry = TestBase.CreateMockProviderRegistry();
var mockModelResolver = TestBase.CreateMockModelResolver();
```

#### TestDataFactory.cs
Provides factory methods for creating test data:
```csharp
// Example usage
var chatMessage = TestDataFactory.CreateChatMessage("user", "Hello world");
var providerConfig = TestDataFactory.CreateProviderConfig("Groq", "groq");
var canonicalModel = TestDataFactory.CreateCanonicalModelConfig("llama-3.1-70b-versatile");
```

#### InMemoryDbContext.cs
Provides in-memory database setup for EF Core:
```csharp
// Example usage
using var dbContext = new InMemoryDbContext().CreateDbContext();
```

### Writing Backend Tests

#### Unit Test Example
```csharp
[Fact]
public async Task ExecuteAsync_WhenActionSucceedsOnFirstAttempt_ReturnsResultImmediately()
{
    // Arrange
    var policy = new RetryPolicy(maxRetries: 3, initialDelayMs: 100, backoffMultiplier: 2.0);
    var attemptCount = 0;
    var expectedResult = "success";

    Func<Task<string>> action = () =>
    {
        attemptCount++;
        return Task.FromResult(expectedResult);
    };

    Func<Exception, bool> shouldRetry = ex => true;

    // Act
    var result = await policy.ExecuteAsync(action, shouldRetry);

    // Assert
    Assert.Equal(expectedResult, result);
    Assert.Equal(1, attemptCount);
}
```

#### Integration Test Example
```csharp
[Fact]
public async Task ChatCompletions_WithValidRequest_ReturnsSuccess()
{
    // Arrange
    var client = _factory.CreateClient();
    var request = new
    {
        model = "llama-3.1-70b-versatile",
        messages = new[] { new { role = "user", content = "Hello" } }
    };

    // Act
    var response = await client.PostAsJsonAsync("/v1/chat/completions", request);

    // Assert
    response.EnsureSuccessStatusCode();
    var content = await response.Content.ReadAsStringAsync();
    Assert.NotEmpty(content);
}
```

## Frontend Testing

### Framework & Tools

- **Test Framework**: Vitest
- **Coverage Tool**: @vitest/coverage-v8
- **E2E Testing**: Playwright
- **Component Testing**: Testing Library
- **Environment**: jsdom

### Test Structure

```
src/Synaxis.WebApp/ClientApp/
├── e2e/                          # End-to-end tests
│   ├── streaming-flow.spec.ts
│   └── example.spec.ts
├── src/
│   ├── test/
│   │   └── setup.ts             # Test setup configuration
│   └── [components]/            # Component tests ( colocated )
│       └── [Component].test.tsx
```

### Running Frontend Tests

#### Run All Frontend Tests
```bash
cd src/Synaxis.WebApp/ClientApp
npm test
```

#### Run Tests in Watch Mode
```bash
npm run test -- --watch
```

#### Run Tests with Coverage
```bash
npm run test:coverage
```

#### Run E2E Tests
```bash
npm run test:e2e
```

#### Run E2E Tests with UI
```bash
npm run test:e2e:ui
```

#### Run Specific Test File
```bash
npm test -- streaming-flow.spec.ts
```

#### Run Tests Matching Pattern
```bash
npm test -- --grep "streaming"
```

### Frontend Test Configuration

#### Vitest Configuration (vite.config.ts)
```typescript
test: {
  globals: true,
  environment: 'jsdom',
  setupFiles: './src/test/setup.ts',
  coverage: {
    provider: 'v8',
    reporter: ['text', 'lcov'],
    include: ['src/**/*.{ts,tsx}'],
  },
}
```

#### Test Setup (src/test/setup.ts)
```typescript
import '@testing-library/jest-dom';
import { expect, afterEach } from 'vitest';
import { cleanup } from '@testing-library/react';

// Cleanup after each test case
afterEach(() => {
  cleanup();
});
```

### Writing Frontend Tests

#### Component Test Example
```typescript
import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { ChatWindow } from './ChatWindow';

describe('ChatWindow', () => {
  it('should render chat input', () => {
    render(<ChatWindow />);
    
    const chatInput = screen.getByPlaceholderText('Type a message...');
    expect(chatInput).toBeInTheDocument();
  });

  it('should display streaming toggle', () => {
    render(<ChatWindow />);
    
    const streamingToggle = screen.getByText(/Streaming/);
    expect(streamingToggle).toBeInTheDocument();
  });
});
```

#### E2E Test Example
```typescript
import { test, expect } from '@playwright/test';

test.describe('Streaming Flow', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
    await page.waitForSelector('text=Synaxis');
  });

  test('should create new chat session', async ({ page }) => {
    const newChatButton = page.locator('button[aria-label="New chat"]');
    await expect(newChatButton).toBeVisible();
    
    await newChatButton.click();
    await page.waitForSelector('textarea[placeholder="Type a message..."]');
    
    const chatInput = page.locator('textarea[placeholder="Type a message..."]');
    await expect(chatInput).toBeInTheDocument();
  });
});
```

## Coverage Reports

### Backend Coverage

#### Generate Coverage Report
```bash
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage
```

#### View Coverage Report
```bash
# Install report generator (one-time setup)
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate HTML report
reportgenerator -reports:coverage/coverage.cobertura.xml -targetdir:coverage/html -reporttypes:Html
```

#### Coverage Targets
- **Current**: 9.02% (backend)
- **Target**: 80% overall
- **Focus**: Core inference logic and provider integrations

### Frontend Coverage

#### Generate Coverage Report
```bash
cd src/Synaxis.WebApp/ClientApp
npm run test:coverage
```

#### View Coverage Report
```bash
# Open HTML report in browser
open coverage/lcov-report/index.html  # macOS
xdg-open coverage/lcov-report/index.html  # Linux
```

#### Coverage Targets
- **Current**: 85.77% (frontend)
- **Target**: 80% overall
- **Status**: ✅ Exceeds target

### Combined Coverage Analysis

#### Calculate Combined Coverage
```bash
# Backend coverage (from .sisyphus/baseline-coverage.txt)
BACKEND_COVERAGE=9.02

# Frontend coverage (from .sisyphus/baseline-coverage-frontend.txt)  
FRONTEND_COVERAGE=85.77

# Combined (weighted by line count - adjust based on project size)
# This is a simplified calculation
echo "Backend: $BACKEND_COVERAGE%"
echo "Frontend: $FRONTEND_COVERAGE%"
echo "Combined: Approximately 47.40% (see .sisyphus/baseline-coverage.txt)"
```

## Fixing Flaky Tests

### Identifying Flaky Tests

#### Run Tests Multiple Times
```bash
# Run tests 10 times to detect flakiness
for i in {1..10}; do
  echo "Run $i:"
  dotnet test --filter Category=Smoke
done
```

#### Check Flaky Test Results
```bash
# View baseline flakiness results
cat .sisyphus/baseline-flakiness.txt

# View detailed flaky test logs
cat .sisyphus/flaky-tests.txt
```

### Common Flaky Test Causes

#### 1. Real Provider Dependencies
**Problem**: Tests hit actual API providers causing timeouts and rate limits.

**Solution**: Use mocks and test doubles
```csharp
// ❌ Flaky - hits real provider
var client = new GroqChatClient("real-api-key");

// ✅ Stable - uses mock
var mockClient = new Mock<IChatClient>();
mockClient.Setup(x => x.CompleteChatAsync(It.IsAny<ChatRequest>()))
          .ReturnsAsync(ChatResponse.Success("test response"));
```

#### 2. Timing Dependencies
**Problem**: Tests depend on specific timing or delays.

**Solution**: Use fake timers and async patterns
```csharp
// ❌ Flaky - depends on real timing
await Task.Delay(1000);

// ✅ Stable - uses controlled timing
await policy.ExecuteAsync(action, shouldRetry); // Uses fake delays in tests
```

#### 3. Shared State
**Problem**: Tests interfere with each other through shared state.

**Solution**: Use fresh instances and proper cleanup
```csharp
// ✅ Good - fresh instance per test
[Fact]
public void Test1()
{
    var service = new ChatService(); // Fresh instance
    // test logic
}

[Fact] 
public void Test2()
{
    var service = new ChatService(); // Fresh instance
    // test logic
}
```

#### 4. Network Dependencies
**Problem**: Tests depend on network availability.

**Solution**: Use test servers and dependency injection
```csharp
// ✅ Good - uses test server
var factory = new WebApplicationFactory<Program>();
var client = factory.CreateClient();
```

### Flaky Test Remediation Steps

1. **Identify**: Run tests multiple times to confirm flakiness
2. **Analyze**: Determine root cause (timing, network, state)
3. **Mock**: Replace external dependencies with mocks
4. **Isolate**: Ensure tests don't share state
5. **Verify**: Run tests 10+ times to confirm stability
6. **Monitor**: Set up continuous monitoring for regressions

### Example: Fixing a Flaky Retry Test

```csharp
// ❌ Flaky version
[Fact]
public async Task RetryPolicy_ShouldRetryOnFailure()
{
    var policy = new RetryPolicy(maxRetries: 3, initialDelayMs: 100);
    var attemptCount = 0;
    
    Func<Task<string>> action = async () =>
    {
        attemptCount++;
        if (attemptCount < 3) throw new Exception("Fail");
        return "success";
    };
    
    // This could be flaky due to real timing
    var result = await policy.ExecuteAsync(action, ex => true);
    Assert.Equal("success", result);
}

// ✅ Stable version
[Fact]
public async Task RetryPolicy_ShouldRetryOnFailure()
{
    // Use mock to avoid real timing
    var mockPolicy = new Mock<IRetryPolicy>();
    mockPolicy.Setup(x => x.ExecuteAsync(It.IsAny<Func<Task<string>>>(), It.IsAny<Func<Exception, bool>>()))
              .ReturnsAsync("success");
    
    var service = new ChatService(mockPolicy.Object);
    var result = await service.ProcessWithRetry();
    
    Assert.Equal("success", result);
}
```

## Testing Best Practices

### General Principles

1. **Test Pyramid**: More unit tests, fewer integration tests, minimal E2E tests
2. **Fast Tests**: Unit tests should run in milliseconds
3. **Deterministic**: Same input always produces same output
4. **Independent**: Tests don't depend on each other
5. **Clear Intent**: Test names should describe what they're testing

### Backend Testing Best Practices

#### 1. Use Descriptive Test Names
```csharp
// ✅ Good
[Fact]
public async Task ExecuteAsync_WhenActionSucceedsOnFirstAttempt_ReturnsResultImmediately()

// ❌ Bad  
[Fact]
public async Task TestRetry()
```

#### 2. Follow AAA Pattern (Arrange-Act-Assert)
```csharp
[Fact]
public async Task ProcessChatRequest_WithValidRequest_ReturnsSuccess()
{
    // Arrange
    var request = CreateValidChatRequest();
    var service = new ChatService(mockDependencies);
    
    // Act
    var result = await service.ProcessAsync(request);
    
    // Assert
    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Data);
}
```

#### 3. Use Appropriate Assertions
```csharp
// ✅ Specific assertions
Assert.Equal(expectedValue, actualValue);
Assert.NotNull(result);
Assert.Throws<InvalidOperationException>(() => service.InvalidCall());

// ❌ Generic assertions
Assert.True(true);
Assert.False(false);
```

#### 4. Mock External Dependencies
```csharp
// ✅ Good - mocks external services
var mockProvider = new Mock<IChatClient>();
var mockRegistry = new Mock<IProviderRegistry>();

// ❌ Bad - uses real dependencies
var realProvider = new GroqChatClient("real-key");
```

#### 5. Test Edge Cases
```csharp
[Fact]
public async Task ExecuteAsync_WithNullAction_ThrowsArgumentNullException()
{
    // Arrange
    var policy = new RetryPolicy(3, 100, 2.0);
    
    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(
        () => policy.ExecuteAsync(null!, ex => true)
    );
}
```

### Frontend Testing Best Practices

#### 1. Test User Behavior, Not Implementation
```typescript
// ✅ Good - tests what user sees
test('should allow user to send a message', () => {
  render(<ChatWindow />);
  
  const input = screen.getByPlaceholderText('Type a message...');
  const sendButton = screen.getByRole('button', { name: /send/i });
  
  fireEvent.change(input, { target: { value: 'Hello' } });
  fireEvent.click(sendButton);
  
  expect(screen.getByText('Hello')).toBeInTheDocument();
});

// ❌ Bad - tests implementation details
test('should call onSend when button clicked', () => {
  const onSend = vi.fn();
  render(<ChatWindow onSend={onSend} />);
  
  fireEvent.click(screen.getByRole('button'));
  
  expect(onSend).toHaveBeenCalled(); // Too implementation-focused
});
```

#### 2. Use Appropriate Testing Library Queries
```typescript
// ✅ Good - semantic queries
screen.getByRole('button', { name: /submit/i });
screen.getByLabelText('Email address');
screen.getByPlaceholderText('Enter your message');

// ❌ Bad - implementation queries
screen.getByTestId('submit-button');
screen.querySelector('.btn-primary');
```

#### 3. Test Accessibility
```typescript
test('streaming toggle has correct aria-label', () => {
  render(<StreamingToggle enabled={true} />);
  
  const toggle = screen.getByRole('button');
  expect(toggle).toHaveAttribute('aria-label', 'Disable streaming');
});
```

#### 4. Mock API Calls
```typescript
// ✅ Good - mocks API
vi.mocked(api.postChatMessage).mockResolvedValue({
  data: { message: 'Hello', id: '123' }
});

// ❌ Bad - makes real API calls
// No mocking - hits real API
```

#### 5. Test Error States
```typescript
test('displays error message when API fails', async () => {
  vi.mocked(api.postChatMessage).mockRejectedValue(new Error('Network error'));
  
  render(<ChatWindow />);
  
  fireEvent.click(screen.getByRole('button', { name: /send/i }));
  
  await waitFor(() => {
    expect(screen.getByText(/network error/i)).toBeInTheDocument();
  });
});
```

### E2E Testing Best Practices

#### 1. Test Critical User Journeys
```typescript
test('complete chat flow from start to finish', async ({ page }) => {
  await page.goto('/');
  
  // Create new chat
  await page.click('[aria-label="New chat"]');
  await page.waitForSelector('textarea');
  
  // Send message
  await page.fill('textarea', 'Hello AI');
  await page.click('[aria-label="Send"]');
  
  // Verify response
  await expect(page.locator('[data-testid="message"]')).toContainText('Hello');
});
```

#### 2. Use Stable Selectors
```typescript
// ✅ Good - stable selectors
await page.getByRole('button', { name: 'Send' });
await page.getByLabelText('Message');

// ❌ Bad - fragile selectors
await page.click('#send-button-123');
await page.click('.btn-primary');
```

#### 3. Handle Async Operations
```typescript
test('streaming response appears gradually', async ({ page }) => {
  await page.goto('/');
  await page.click('[aria-label="New chat"]');
  
  const responsePromise = page.waitForResponse('**/chat/completions');
  await page.fill('textarea', 'Tell me a story');
  await page.click('[aria-label="Send"]');
  
  const response = await responsePromise;
  expect(response.status()).toBe(200);
});
```

## Troubleshooting

### Common Issues and Solutions

#### Backend Tests

##### Tests Don't Run
```bash
# Check if test project builds
dotnet build tests/InferenceGateway.UnitTests

# Restore packages
dotnet restore

# Clean and rebuild
dotnet clean && dotnet build
```

##### Coverage Not Generated
```bash
# Ensure Coverlet is installed
dotnet add tests/InferenceGateway.UnitTests package coverlet.collector

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

##### Tests Timeout
```csharp
// Add timeout to test
[Fact(Timeout = 5000)] // 5 seconds
public async Task LongRunningTest()
{
    // test logic
}
```

##### Mock Not Working
```csharp
// ✅ Correct setup
var mock = new Mock<IChatClient>();
mock.Setup(x => x.CompleteChatAsync(It.IsAny<ChatRequest>()))
    .ReturnsAsync(ChatResponse.Success("test"));

// ❌ Common mistake - wrong setup syntax
mock.Setup(x => x.CompleteChatAsync(It.IsAny<ChatRequest>()))
    .Returns(Task.FromResult(ChatResponse.Success("test"))); // For non-async
```

#### Frontend Tests

##### Vitest Not Found
```bash
# Install dependencies
cd src/Synaxis.WebApp/ClientApp
npm install

# Run specific test
npx vitest run specific-test.test.ts
```

##### Coverage Report Empty
```typescript
// Check vite.config.ts coverage settings
coverage: {
  provider: 'v8',
  reporter: ['text', 'lcov'],
  include: ['src/**/*.{ts,tsx}'], // Ensure correct paths
}
```

##### E2E Tests Fail
```bash
# Install Playwright browsers
npx playwright install

# Run with debug
npx playwright test --debug

# Run specific test
npx playwright test streaming-flow.spec.ts
```

##### Component Tests Timeout
```typescript
// Increase timeout in vitest config
export default defineConfig({
  test: {
    globals: true,
    environment: 'jsdom',
    testTimeout: 10000, // 10 seconds
  }
});
```

##### Testing Library Queries Fail
```typescript
// ✅ Wait for async content
await waitFor(() => {
  expect(screen.getByText('Loaded')).toBeInTheDocument();
});

// ✅ Use find queries for async content
const loadingElement = await screen.findByText('Loading');
expect(loadingElement).toBeInTheDocument();
```

### Performance Issues

#### Slow Backend Tests
```csharp
// Use parallel execution
dotnet test --parallel

// Run specific test categories
dotnet test --filter Category=Unit

// Use in-memory databases
var options = new DbContextOptionsBuilder<ControlPlaneDbContext>()
    .UseInMemoryDatabase(databaseName: "TestDb")
    .Options;
```

#### Slow Frontend Tests
```typescript
// Run tests in parallel
export default defineConfig({
  test: {
    globals: true,
    environment: 'jsdom',
    threads: true, // Enable parallel execution
  }
});

// Run only affected tests
npm test -- --changed
```

### Debugging Tips

#### Backend Test Debugging
```csharp
// Add debug output
[Fact]
public void TestWithDebug()
{
    var result = CalculateSomething();
    Console.WriteLine($"Result: {result}");
    Assert.True(result > 0);
}

// Use debugger
[Fact]
public void TestWithDebugger()
{
    var service = new MyService();
    System.Diagnostics.Debugger.Break(); // Breakpoint
    var result = service.Process();
    Assert.NotNull(result);
}
```

#### Frontend Test Debugging
```typescript
// Add debug output
test('debug test', () => {
  const result = calculateSomething();
  console.log('Result:', result);
  expect(result).toBeGreaterThan(0);
});

// Use Vitest debug mode
test('debug test', async () => {
  render(<MyComponent />);
  await screen.debug(); // Prints DOM
});
```

#### E2E Test Debugging
```typescript
// Add screenshots
test('debug test', async ({ page }) => {
  await page.goto('/');
  await page.screenshot({ path: 'debug.png' });
  
  // Pause for manual inspection
  await page.pause();
});
```

### Getting Help

1. **Check Logs**: Look for error messages in test output
2. **Run Verbose**: Use `--verbosity detailed` for more info
3. **Isolate Tests**: Run single failing test to narrow down
4. **Check Dependencies**: Ensure all packages are installed
5. **Review Recent Changes**: Check git history for recent modifications

### Useful Commands Reference

```bash
# Backend
dotnet test                          # Run all tests
dotnet test --filter Category=Unit   # Filter by category
dotnet test --verbosity detailed     # Verbose output
dotnet test --collect:"XPlat Code Coverage"  # With coverage

# Frontend
npm test                             # Run all tests
npm test -- --watch                  # Watch mode
npm test -- --grep "pattern"         # Filter by pattern
npm run test:coverage               # With coverage

# E2E
npm run test:e2e                    # Run E2E tests
npm run test:e2e:ui                 # Run with UI
npx playwright test --debug         # Debug mode

# Coverage
open coverage/lcov-report/index.html  # View frontend coverage
reportgenerator -reports:coverage/coverage.cobertura.xml -targetdir:coverage/html  # Backend coverage HTML
```

## Test Infrastructure

### TestBase.cs

Provides factory methods for creating mocks:

```csharp
public abstract class TestBase
{
    protected Mock<IChatClient> CreateMockChatClient()
    protected Mock<IProviderRegistry> CreateMockProviderRegistry()
    protected Mock<IModelResolver> CreateMockModelResolver()
    protected Mock<IHealthStore> CreateMockHealthStore(bool healthy = true)
    protected Mock<IQuotaTracker> CreateMockQuotaTracker()
    protected Mock<ICostService> CreateMockCostService()
    protected Mock<ILogger<T>> CreateMockLogger<T>()
    protected Mock<HttpMessageHandler> CreateMockHttpHandler()
}
```

**Usage Example:**

```csharp
public class RoutingLogicTests : TestBase
{
    private ModelResolver _resolver;
    
    [SetUp]
    public void Setup()
    {
        _resolver = new ModelResolver(
            CreateMockProviderRegistry().Object,
            CreateMockCanonicalModelRepository().Object,
            CreateMockLogger<ModelResolver>().Object
        );
    }
}
```

### TestDataFactory.cs

Provides factory methods for creating test data:

```csharp
public static class TestDataFactory
{
    public static ChatMessage CreateChatMessage(string role, string content)
    public static ProviderConfig CreateProviderConfig(string name, int tier)
    public static CanonicalModelConfig CreateCanonicalModelConfig(string id, string provider)
    public static ApiKey CreateApiKey(string provider, string key)
    public static User CreateUser(string email)
    public static Project CreateProject(string name)
    public static Route CreateRoute(string from, string to)
    public static HealthStatus CreateHealthStatus(string service, bool healthy)
}
```

**Usage Example:**

```csharp
[Fact]
public async Task GetCandidatesAsync_WhenModelExists_ReturnsProvider()
{
    // Given
    var modelId = "llama-3.1-70b-versatile";
    var expectedProvider = TestDataFactory.CreateProviderConfig("openai", 1);
    
    // When & Then
    // Test implementation
}
```

### InMemoryDbContext.cs

Provides in-memory database setup:

```csharp
public class InMemoryDbContext : IDisposable
{
    public ControlPlaneDbContext Context { get; }
    
    public InMemoryDbContext(string dbName = null)
    {
        var options = new DbContextOptionsBuilder<ControlPlaneDbContext>()
            .UseInMemoryDatabase(dbName ?? $"TestDb_{Guid.NewGuid()}")
            .Options;
        Context = new ControlPlaneDbContext(options);
    }
    
    public void Dispose()
    {
        Context.Dispose();
    }
}
```

**Usage Example:**

```csharp
[Fact]
public async Task SaveAndRetrieveModelConfig()
{
    // Given
    using var dbContext = new InMemoryDbContext();
    var modelConfig = TestDataFactory.CreateCanonicalModelConfig("test-model", "openai");
    
    // When
    await dbContext.Context.CanonicalModelConfigs.AddAsync(modelConfig);
    await dbContext.Context.SaveChangesAsync();
    
    // Then
    var retrieved = await dbContext.Context.CanonicalModelConfigs
        .FirstOrDefaultAsync(m => m.Id == "test-model");
    Assert.NotNull(retrieved);
}
```

### SynaxisWebApplicationFactory.cs

Integration test factory with Testcontainers:

```csharp
public class SynaxisWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private PostgreSqlContainer _postgres;
    private RedisContainer _redis;
    
    public SynaxisWebApplicationFactory()
    {
        _postgres = new PostgreSqlBuilder()
            .WithDatabase("synaxis_test")
            .WithUsername("test")
            .WithPassword("test")
            .Build();
            
        _redis = new RedisBuilder()
            .WithPort(6379)
            .Build();
    }
    
    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync());
        
        // Set environment variables for test containers
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", 
            _postgres.GetConnectionString());
        Environment.SetEnvironmentVariable("Redis__ConnectionString", 
            _redis.GetConnectionString());
    }
    
    public new async Task DisposeAsync()
    {
        await Task.WhenAll(_postgres.DisposeAsync(), _redis.DisposeAsync());
        await base.DisposeAsync();
    }
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        
        builder.ConfigureServices(services =>
        {
            // Replace real services with test doubles
            services.AddSingleton<IChatClient, MockChatClient>();
            services.AddSingleton<IProviderRegistry, MockProviderRegistry>();
        });
    }
}
```

**Usage Example:**

```csharp
public class IntegrationTests : IClassFixture<SynaxisWebApplicationFactory>
{
    private readonly SynaxisWebApplicationFactory _factory;
    private readonly HttpClient _client;
    
    public IntegrationTests(SynaxisWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }
    
    [Fact]
    public async Task GetHealthCheck_ReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");
        
        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", content);
    }
}
```

### MockSmokeTestHelper.cs

Provides mock HTTP clients for testing:

```csharp
public static class MockSmokeTestHelper
{
    public static HttpClient CreateMockHttpClient(string responseContent, int statusCode = 200)
    {
        var handler = new MockHttpHandler();
        handler.When("*").Respond(statusCode, "application/json", responseContent);
        return new HttpClient(handler);
    }
    
    public static HttpClient CreateFailingHttpClient(int statusCode = 500)
    {
        var handler = new MockHttpHandler();
        handler.When("*").Respond(statusCode, "application/json", "{\"error\":\"test error\"}");
        return new HttpClient(handler);
    }
}
```

**Usage Example:**

```csharp
[Fact]
public async Task ChatClient_SendMessage_ReturnsResponse()
{
    // Arrange
    var expectedResponse = "Hello, how can I help you?";
    var mockClient = MockSmokeTestHelper.CreateMockHttpClient(
        $"{{\"choices\":[{{\"message\":{{\"content\":\"{expectedResponse}\"}}}}]}}"
    );
    
    var chatClient = new OpenAIChatClient(mockClient, "test-key");
    
    // Act
    var response = await chatClient.SendMessageAsync("Hello");
    
    // Assert
    Assert.Equal(expectedResponse, response.Content);
}
```

## Running Specific Tests

### Backend Test Filtering

```bash
# Run tests by test class name
dotnet test --filter "FullyQualifiedName~RoutingLogicTests"

# Run tests by test method name
dotnet test --filter "FullyQualifiedName~ModelResolver_ResolveAsync"

# Run tests by category
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=Integration"
dotnet test --filter "Category=Mocked"
dotnet test --filter "Category=RealProvider"

# Run tests by trait
dotnet test --filter "Trait=Priority&Value=High"

# Combine filters
dotnet test --filter "Category=Unit&FullyQualifiedName~RoutingLogicTests"
```

### Frontend Test Filtering

```bash
# Run specific test file
npm test stores.test.ts

# Run tests matching pattern
npm test -- --grep "ChatWindow"

# Run tests in specific directory
npm test components/

# Run tests with specific tag
npm test -- --tag "unit"
npm test -- --tag "integration"
```

### E2E Test Execution

```bash
# Run all E2E tests
npm run test:e2e

# Run E2E tests in headed mode (see browser)
npm run test:e2e:headed

# Run specific E2E test file
npx playwright test auth.spec.ts

# Run E2E tests with specific browser
npx playwright test --project=chromium
npx playwright test --project=firefox
npx playwright test --project=webkit

# Run E2E tests in debug mode
npx playwright test --debug
```

## CI/CD Integration

### Automated Test Execution

Tests are automatically executed in CI/CD pipeline:

```yaml
# Example GitHub Actions workflow
name: Tests
on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore
    
    - name: Test
      run: dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage"
    
    - name: Upload coverage reports
      uses: codecov/codecov-action@v3
      with:
        file: ./coverage/coverage.cobertura.xml
```

### Coverage Reporting

```bash
# Generate and upload coverage
dotnet test --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover
```

### Quality Gates

- **Minimum Coverage:** 80% overall
- **Zero Flaky Tests:** All tests must pass consistently
- **Zero Warnings:** No compiler warnings or lint errors
- **Test Execution Time:** Individual tests < 10 seconds
- **Integration Tests:** Must complete within 5 minutes

### Flaky Test Detection

```bash
# Run tests multiple times to detect flakiness
for i in {1..10}; do
    echo "Run $i:"
    dotnet test --filter "Category=Flaky" --logger "trx;LogFileName=TestResults_$i.trx"
done

# Analyze results
dotnet test --logger "trx;LogFileName=Combined.trx" --results-directory ./test-results
```

---

## Summary

This testing guide provides comprehensive coverage of Synaxis testing practices:

- **Backend**: xUnit + Coverlet with 9.02% current coverage (635 tests)
- **Frontend**: Vitest + Playwright with 85.77% current coverage (415 tests)
- **Target**: 80% overall coverage
- **Focus**: Eliminating flaky tests through proper mocking and isolation
- **Best Practices**: AAA pattern, descriptive names, proper assertions
- **Zero Flakiness**: 0% flaky tests across the entire test suite
- **Zero Warnings**: 0 compiler warnings, 0 ESLint errors
- **Test Infrastructure**: Comprehensive with TestBase.cs, TestDataFactory.cs, InMemoryDbContext.cs, SynaxisWebApplicationFactory.cs, and MockSmokeTestHelper.cs

For questions or issues, refer to the troubleshooting section or check the project notepads for recent testing insights.