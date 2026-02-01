# Synaxis Testing Guide

This comprehensive guide covers all aspects of testing for the Synaxis project, including backend (.NET) and frontend (React/TypeScript) testing, coverage reporting, and best practices.

## Table of Contents

- [Quick Start](#quick-start)
- [Backend Testing](#backend-testing)
- [Frontend Testing](#frontend-testing)
- [Coverage Reports](#coverage-reports)
- [Fixing Flaky Tests](#fixing-flaky-tests)
- [Testing Best Practices](#testing-best-practices)
- [Troubleshooting](#troubleshooting)

## Quick Start

### Run All Tests

```bash
# Backend tests
dotnet test src/InferenceGateway/InferenceGateway.sln

# Frontend tests (from ClientApp directory)
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
- **Current**: 7.19% (backend)
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
BACKEND_COVERAGE=7.19

# Frontend coverage (from .sisyphus/baseline-coverage-frontend.txt)  
FRONTEND_COVERAGE=85.77

# Combined (weighted by line count - adjust based on project size)
# This is a simplified calculation
echo "Backend: $BACKEND_COVERAGE%"
echo "Frontend: $FRONTEND_COVERAGE%"
echo "Combined: Approximately 46.48% (see .sisyphus/baseline-coverage.txt)"
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

---

## Summary

This testing guide provides comprehensive coverage of Synaxis testing practices:

- **Backend**: xUnit + Coverlet with 7.19% current coverage
- **Frontend**: Vitest + Playwright with 85.77% current coverage  
- **Target**: 80% overall coverage
- **Focus**: Eliminating flaky tests through proper mocking and isolation
- **Best Practices**: AAA pattern, descriptive names, proper assertions

For questions or issues, refer to the troubleshooting section or check the project notepads for recent testing insights.