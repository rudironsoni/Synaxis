# ABSOLUTE MANDATORY RULES

**You MUST follow these rules. No exceptions.**

## Before Any Action
- **READ** these instructions in **FULL** before executing any steps
- **UNDERSTAND** the complete guidelines before making changes
- **VERIFY** you have all context needed before starting

## During Work
- **NO** verbose explanations or commentary unless explicitly required
- **NO** comments in code unless asked
- **NO** status updates while processing (e.g., "Now I'm going to...", "Let me check...")
- **DO NOT** repeat yourself
- **DO NOT** ask clarifying questions after work has started
- **DO NOT** keep saying what you're doing step-by-step

## After Work
- **VERIFY** the solution builds and passes tests
- **FIX** underlying issues, never disable checks to make things pass
- **DO NOT** commit until verification succeeds
- **DO NOT** claim completion unless verification succeeded or the work is explicitly marked **NOT VERIFIED** with a concrete reason
- **DO NOT** close tracking issues until verification evidence is captured
- **DO NOT** proceed to new work while any build warning, build error, or test failure remains unresolved for the current change

## Agent Execution Contract (Mandatory)

For every non-trivial task, follow this sequence exactly:

1. `bd ready --json`
2. Claim work: `bd update <id> --status in_progress --json`
3. Implement a minimal, focused change set
4. Run mandatory verification commands from repo root
5. Publish verification evidence (exact commands + pass/fail)
6. Close completed work: `bd close <id> --reason "Completed" --json`

If any required check fails, stop, fix root cause, and rerun all required checks.

## GIT WORKFLOW - ABSOLUTE PROHIBITIONS

### ❌ NEVER USE `git stash` TO AVOID CONFLICTS ❌

**THIS IS FORBIDDEN. NO EXCEPTIONS. EVER.**

Using `git stash` to hide local changes, avoid merge conflicts, or "clean up" the working directory is **STRICTLY PROHIBITED**. This behavior:
- Hides work that may be lost or forgotten
- Prevents proper conflict resolution
- Breaks trust by making changes invisible
- Creates technical debt that compounds over time

**YOU MUST NEVER:**
- Run `git stash` to "temporarily" save work
- Run `git stash pop` without explicit user request
- Use `git stash` before switching branches
- Use `git stash` to avoid dealing with conflicts
- Use `git stash` during rebase operations

**WHAT TO DO INSTEAD:**
1. **If you have uncommitted changes:**
   - Commit them: `git add <files> && git commit -m "WIP: description"`
   - Or discard properly: `git restore <files>` (only if truly disposable)

2. **If you encounter merge conflicts:**
   - STOP and resolve them properly
   - Use `git status` to see conflicted files
   - Edit files to resolve conflicts manually
   - Use `git add <file>` to mark resolved
   - NEVER abort with `git merge --abort` unless explicitly instructed

3. **If you need to switch branches:**
   - Commit current work first
   - Or create a new branch: `git checkout -b new-branch-name`

4. **If rebase has conflicts:**
   - Resolve the conflicts manually
   - Use `git rebase --continue` after resolving
   - NEVER use `git rebase --abort` unless explicitly instructed

**ENFORCEMENT:**
- Before any git operation, check: `git status`
- If local changes exist, COMMIT THEM or DISCARD PROPERLY
- If stash exists: `git stash list` - DO NOT IGNORE THIS OUTPUT
- **If you stash work, you must restore and handle it before any other work**

**VIOLATION CONSEQUENCE:**
Stashing work without immediate restoration and proper handling is a **CRITICAL ERROR** that breaks trust and risks data loss.

---

# Project Instructions for AI Agents

This file provides instructions and context for AI coding agents working on this project.

<!-- BEGIN BEADS INTEGRATION -->
## Issue Tracking with bd (beads)

**IMPORTANT**: This project uses **bd (beads)** for ALL issue tracking. Do NOT use markdown TODOs, task lists, or other tracking methods.

### Why bd?

- Dependency-aware: Track blockers and relationships between issues
- Git-friendly: Auto-syncs to JSONL for version control
- Agent-optimized: JSON output, ready work detection, discovered-from links
- Prevents duplicate tracking systems and confusion

### Quick Start

**Check for ready work:**

```bash
bd ready --json
```

**Create new issues:**

```bash
bd create "Issue title" --description="Detailed context" -t bug|feature|task -p 0-4 --json
bd create "Issue title" --description="What this issue is about" -p 1 --deps discovered-from:bd-123 --json
```

**Claim and update:**

```bash
bd update bd-42 --status in_progress --json
bd update bd-42 --priority 1 --json
```

**Complete work:**

```bash
bd close bd-42 --reason "Completed" --json
```

### Issue Types

- `bug` - Something broken
- `feature` - New functionality
- `task` - Work item (tests, docs, refactoring)
- `epic` - Large feature with subtasks
- `chore` - Maintenance (dependencies, tooling)

### Priorities

- `0` - Critical (security, data loss, broken builds)
- `1` - High (major features, important bugs)
- `2` - Medium (default, nice-to-have)
- `3` - Low (polish, optimization)
- `4` - Backlog (future ideas)

### Workflow for AI Agents

1. **Check ready work**: `bd ready` shows unblocked issues
2. **Claim your task**: `bd update <id> --status in_progress`
3. **Work on it**: Implement, test, document
4. **Discover new work?** Create linked issue:
   - `bd create "Found bug" --description="Details about what was found" -p 1 --deps discovered-from:<parent-id>`
5. **Complete**: `bd close <id> --reason "Done"`

### Hard Gate

- Do not start code edits before an issue is in `in_progress`, unless the user explicitly requests a one-off change and waives beads tracking.
- Do not close an issue until verification evidence is included in the final summary.

### Auto-Sync

bd automatically syncs with git:

- Exports to `.beads/issues.jsonl` after changes (5s debounce)
- Imports from JSONL when newer (e.g., after `git pull`)
- No manual export/import needed!

### Important Rules

- ✅ Use bd for ALL task tracking
- ✅ Always use `--json` flag for programmatic use
- ✅ Link discovered work with `discovered-from` dependencies
- ✅ Check `bd ready` before asking "what should I work on?"
- ❌ Do NOT create markdown TODO lists
- ❌ Do NOT use external issue trackers
- ❌ Do NOT duplicate tracking systems

For more details, see README.md and docs/QUICKSTART.md.

<!-- END BEADS INTEGRATION -->

---

## Build Verification & Quality Gates

### EXTREMELY REQUIRED: Verify After Every Change

**You MUST prove the solution is correct after developing.**

#### Mandatory Commands (run from repo root)

Run these commands and ensure they succeed:

```bash
# Format verification (EditorConfig-driven):
dotnet format --verify-no-changes

# Build the solution (warnings treated as errors):
dotnet build <Solution.sln> -c Release -warnaserror

# Test the solution:
dotnet test <Solution.sln> --no-build
```

`dotnet build` and `dotnet test` results are acceptable only when there are **zero warnings** and **zero errors**.
Any warning is treated as a failed verification even if tooling/configuration accidentally allows it.

Replace `<Solution.sln>` with the actual solution file name.

If the repo uses multiple solution files, run these commands for the solution impacted by the change, and prefer the primary solution used by CI.

#### Verification Decision Matrix

- **Code changes**: format + build + test are required.
- **Test-only changes**: format + build + test are required.
- **Docs-only changes**: verification commands are recommended; if not run, mark output as **NOT VERIFIED (docs-only change)**.
- **Behavioral code changes**: add or update automated tests is mandatory unless technically impossible; if impossible, create and link a follow-up issue before completion.
- **Any non-doc change**: running tests is mandatory; skipping tests is non-compliant.

If `dotnet test ... --no-build` fails due to missing artifacts, use:
```bash
dotnet test <Solution.sln>
```

#### Test Command Guardrails (Strict)

For test execution, these flags are **forbidden** because they can hide race conditions or create misleading pass results:

- `--configuration Release`
- `-c Release`
- `--maxcpucount:1`
- `-maxcpucount:1`
- `-nodeReuse:false`
- `-nodereuse:false`
- `/nr:false`

Do not serialize test execution or tune MSBuild process reuse to "make tests pass". Fix the flaky/concurrency issue instead.
These flags are forbidden in local invocations, scripts, and CI snippets.

If extra diagnostics are needed, prefer adding observability rather than changing execution semantics, for example:

```bash
dotnet test <Solution.sln> --no-build --logger "trx;LogFileName=test-results.trx"
dotnet test <Solution.sln> --no-build --blame-hang --blame-hang-timeout 5m
```

#### Rules About Failures

- **DO NOT** "fix" failures by weakening correctness gates (disabling analyzers, removing `-warnaserror`, lowering severity)
- **FIX** the underlying cause
- If you cannot run these commands, you **MUST** state that clearly and mark the change as **NOT VERIFIED**
- If a failure is intermittent, treat it as a correctness issue and stabilize it (no retries-until-green behavior)
- If any test fails, stop immediately, fix the root cause, and rerun the full required verification commands
- It is forbidden to claim completion while any build warning, build error, or test failure exists
- It is forbidden to bypass failures by skipping tests, filtering out failing tests, quarantining tests, or changing runtime semantics to force green

#### Evidence Required

Include in your PR or change summary:
- The exact commands you ran
- Pass/fail results
- Any deviations and why (only when explicitly requested or technically unavoidable)

Use this format:

```text
Verification Evidence
- Command: <exact command>
  Result: PASS|FAIL
  Notes: <optional short note>
```

Any missing verification evidence means the change is **NOT VERIFIED**.

---

## Before ANY Git Operation - MANDATORY CHECK

**You MUST run these commands BEFORE any git checkout, switch, rebase, merge, or pull:**

```bash
# 1. Check for stashes
if git stash list | grep -q 'stash@'; then
    echo "ERROR: Stashes exist! Handle them before proceeding."
    echo "Run: git stash pop && git add -A && git commit -m 'Restored stashed work'"
    exit 1
fi

# 2. Check for local changes
if ! git diff --quiet || ! git diff --cached --quiet; then
    echo "WARNING: Local changes exist."
    git status
    echo "Commit them or discard them properly before proceeding."
fi
```

**DO NOT PROCEED** with any git operation until:
- `git stash list` returns empty
- You have explicitly decided what to do with local changes (commit or restore)

---

## Pre-Completion Checklist (CRITICAL)

Before claiming work is complete, you MUST run this checklist:

### 1. Check for Stashed Work

```bash
git stash list
```

- **CRITICAL: If stashes exist, STOP and handle them immediately**
- Run: `git stash list`
- If stashes exist: `git stash pop` to restore the most recent
- Commit the restored changes: `git add -A && git commit -m "Restored stashed work"`
- Repeat until `git stash list` returns empty
- **NEVER** leave work in stashes
- **NEVER** use `git stash` to avoid conflicts or "clean" working directory

### 2. Check for Unstaged Changes

```bash
git status
```

- Stage and commit all work files
- Do NOT leave feature code in unstaged/uncommitted state
- Only documentation/notes may remain uncommitted

### 3. Verify All Changes Are Committed

```bash
git diff --name-only
```

- Should return empty for work files
- Only non-code files (notes, docs) may remain

### 4. Final Verification

```bash
dotnet format --verify-no-changes
dotnet build <Solution.sln> -c Release -warnaserror
dotnet test <Solution.sln> --no-build
```

All must pass. No exceptions.

### 5. Review Commit History

```bash
git log --oneline -5
```

- Ensure commits are meaningful and complete
- Each commit should represent a logical unit of work

### 6. Beads Sync Check

```bash
bd sync
```

- Ensure all issue tracking is up to date
- Close completed issues with `bd close <id>`

**FAILURE TO FOLLOW THIS CHECKLIST IS A CRITICAL ERROR**

### Stash Warning

When using git worktrees, remember:
- Stashes are **global** to the repository
- Stashing in a worktree affects all worktrees
- Before switching worktrees, check `git stash list`
- Consider committing instead of stashing when possible

---

## .NET 10 Execution Standard

### SDK and Language Baseline

- Target .NET 10 SDK/tooling for all active development paths
- Keep `global.json` pinned to an approved .NET 10 SDK band for reproducible local/CI behavior
- Enable modern defaults in project files (`Nullable`, implicit usings as appropriate, analyzers on)
- Do not silently upgrade SDK bands in feature branches; coordinate SDK jumps as explicit maintenance work

### Restore Determinism

- Prefer deterministic restore behavior aligned with CI lock-file policy
- If lock files are enabled in the repo, use locked restore mode in CI and local verification paths
- Do not bypass restore determinism to resolve transient package issues; fix source/lock configuration instead

### CI-Equivalent Local Validation

When validating locally, prefer CI-equivalent behavior over custom local shortcuts:

- Run format, build, and tests from repo root
- Use the same solution(s) and test entrypoints used in CI
- Do not weaken analyzers, warning policy, or test runtime behavior to bypass failures

### Strict Agent Compliance

- If a required command fails, stop and fix root cause before continuing
- If verification cannot be executed, explicitly state what is blocked and why
- Never report "done" without either successful verification evidence or an explicit **NOT VERIFIED** declaration
- Never trade reliability for speed by reducing parallelism or changing runtime semantics to force green tests

---

## Compiler Warnings & Static Analysis

### Warnings Are Errors

Compiler warnings indicate something is potentially wrong. **Treat warnings as errors.**

- Pre-existing warnings cause AI agents to miss new warnings from their changes
- Get to 0 warnings before treating them as errors
- Configure in `.editorconfig`, `Directory.Build.props`, or project files

### Recommended Analyzers

Add these to your projects for better code quality:

- **Microsoft.CodeAnalysis.NetAnalyzers** - Standard .NET analyzers
- **StyleCop.Analyzers** - Style consistency
- **Meziantou.Analyzer** - Additional best practices

### Baseline Configuration

Add to `Directory.Build.props`:

```xml
<Project>
  <PropertyGroup>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

---

## Code Formatting Automation

### Problem

AI-generated code often has inconsistent formatting (wrong indentation, brace placement, etc.), making it feel alien in the codebase.

### Solution: Auto-Format on Build

Add a `Directory.Build.targets` file to auto-format code during build. This ensures all code meets your team's standards—even when written by AI.

See [docs/Directory.Build.targets.example](docs/Directory.Build.targets.example) for the complete implementation.

**Key points:**
- Runs `dotnet format` before compilation
- Uses a lock file to run only once per build
- Respects `.editorconfig` rules
- Can be disabled with `EnableAutoFormat=false`

### Alternative: Pre-Commit Hook

Configure git to run `dotnet format` before each commit to ensure consistency.

---

## C# Project Structure Patterns

### Global Using Statements

Create a `GlobalUsings.cs` file in each project's root to centralize common namespaces:

```csharp
global using System.Diagnostics;
global using System.Text;
global using System.Text.Json;
global using Microsoft.Extensions.Logging;
```

**Benefits:**
- AI agents don't need to worry about adding `using` statements for common types
- Reduces repetitive boilerplate
- Prevents missing using statement errors

**Caution:** Including too many namespaces increases the chance of type name conflicts. Be selective.

### Directory-Based Package Management

Use `Directory.Packages.props` for centralized NuGet package version management:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="Package.Name" Version="1.0.0" />
  </ItemGroup>
</Project>
```

**Benefits:**
- Single source of truth for package versions
- Prevents version mismatches across projects
- AI agents can't accidentally add different versions of the same package

### NuGet Configuration

Define a `NuGet.config` file to explicitly declare package sources:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
```

**Benefits:**
- Eliminates issues from multiple package sources
- Consistent package resolution across environments

---

## C# Language Best Practices

### Prefer `var` Keyword

While explicit types improve human readability, AI agents struggle with:
- Specifying exact type names
- Generic type parameters
- Missing using statements for referenced types

**Preferred:**
```csharp
var builder = GameFactory.CreateBuilder();
var result = await service.GetDataAsync();
```

**Trade-off:** Type information is less visible in code review. Most IDEs show inferred types, but diff tools may not.

### Use `required` Keyword and Nullability

Define immutable properties with `required`:

```csharp
public required string Name { get; init; }
```

**Benefits:**
- Compiler enforces initialization
- AI agents can't forget to set required properties
- Nullability analysis catches null reference issues

**Enable nullability:**
```xml
<Nullable>enable</Nullable>
```

### Use `with` Keyword for Records

When working with records, use `with` for immutable modifications:

```csharp
Point newPos = originalPos with { X = originalPos.X + 1 };
```

**Benefits:**
- Focus on what's changed
- Minimizes properties AI needs to worry about
- Clean, expressive syntax

### Async/Await Patterns

- **Avoid blocking** on async (`.Result`, `.Wait()`) in request-handling paths
- Prefer **end-to-end async**
- In library code, use `ConfigureAwait(false)` unless context is required

### Dependency Injection

- Prefer **constructor injection**
- Avoid hidden global state and stateful statics
- Choose correct lifetimes (avoid captive dependencies)
- Prefer thread-safe services for concurrent use

### HTTP Client Usage

- **Never** create `new HttpClient()` per request
- Prefer `IHttpClientFactory`
- Or use `PooledConnectionLifetime` when configuring handlers

### Logging

- Use `ILogger` abstractions
- Use consistent event names and log levels
- **Never** log secrets, tokens, or sensitive data
- Prefer structured logging

### Security Requirements

- **Never** commit secrets (API keys, tokens, passwords, connection strings)
- Avoid insecure serialization (**never** use `BinaryFormatter`)
- Use Data Protection APIs for sensitive data in ASP.NET Core
- Avoid ad hoc cryptography

---

## ASP.NET Core Patterns (when applicable)

- Avoid blocking calls on the request thread
- Be mindful of hot paths, allocations, and pagination for large results
- Prefer built-in platform patterns over custom reinventions
- Validate inputs for public APIs; throw `ArgumentNullException` when appropriate
- Do not allow public methods to fail with incidental `NullReferenceException`

---

## Testing Expectations

### Core Testing Philosophy

- Add or update tests for all behavioral changes
- Keep tests deterministic and resilient (no dependencies on real time, random data, or external services unless required)
- Isolate external dependencies and label accordingly
- Tests should serve as executable documentation

### Mandatory Test Rules (Enforced)

- Do not introduce or keep flaky tests; fix instability before completion
- Do not force single-threaded execution to hide concurrency defects
- Do not depend on machine-local state, timezone, locale, or clock timing unless explicitly required and controlled
- Prefer behavioral assertions over implementation-detail assertions
- For any behavioral code change, tests must be implemented or updated in the same change set unless explicitly waived by maintainers
- Running the relevant test suites is mandatory before claiming completion
- Any failing test is a release-blocking condition for the change until fixed

### Flaky Test Handling Protocol

1. Reproduce under normal parallel execution.
2. Capture diagnostics (`trx`, `--blame-hang`, targeted logs).
3. Fix root cause (shared mutable state, race, ordering, timing assumptions).
4. Re-run the full required verification commands.

### Testing Stack

This project uses the following testing frameworks and tools:

#### xUnit (Primary Testing Framework)

**Attributes:**
- `[Fact]` - Single test case
- `[Theory]` with `[InlineData]` or `[MemberData]` - Parameterized tests
- `[Trait("Category", "Unit")]` - Test categorization for filtering
- `IClassFixture<T>` - Shared test context across tests in a class
- `ICollectionFixture<T>` - Shared test context across test classes

**Async Support:**
- Test methods can be `async Task`
- Use `IAsyncLifetime` for async initialization and cleanup

**Example:**
```csharp
[Trait("Category", "Unit")]
public class ServiceTests : IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Method_WhenValidInput_ReturnsExpectedResult()
    {
        // Test implementation
    }

    [Theory]
    [InlineData("input1", true)]
    [InlineData("input2", false)]
    public void Method_WithParameters_ValidatesInput(string input, bool expected)
    {
        // Parameterized test
    }
}
```

#### Moq (Mocking Framework)

**Setup Patterns:**
```csharp
private readonly Mock<IDependency> _dependencyMock = new();

// Basic setup
_dependencyMock.Setup(x => x.Method()).Returns(expectedValue);

// Async setup
_dependencyMock.Setup(x => x.MethodAsync()).ReturnsAsync(expectedValue);

// Argument matchers
_dependencyMock.Setup(x => x.Method(It.IsAny<string>())).Returns(true);
_dependencyMock.Setup(x => x.Method(It.Is<string>(s => s.Length > 5))).Returns(false);

// Verify calls
_dependencyMock.Verify(x => x.Method(), Times.Once);
_dependencyMock.Verify(x => x.Method(It.IsAny<string>()), Times.Never);
```

**Best Practices:**
- Mock at the interface level, not implementation
- Verify behavior only when the interaction itself is important
- Use `MockBehavior.Strict` to catch unexpected calls during refactoring

#### FluentAssertions (Assertion Library)

**Common Assertions:**
```csharp
// Equality
result.Should().Be(expected);
result.Should().NotBe(unexpected);

// Null checking
result.Should().BeNull();
result.Should().NotBeNull();

// Collections
collection.Should().Contain(item);
collection.Should().HaveCount(3);
collection.Should().BeEquivalentTo(expected);

// Exceptions
Action act = () => service.Method(null);
act.Should().Throw<ArgumentNullException>()
    .WithMessage("*parameter*");

// Async exceptions
Func<Task> act = async () => await service.MethodAsync(null);
await act.Should().ThrowAsync<ArgumentNullException>();

// Strings
result.Should().Contain("substring");
result.Should().StartWith("prefix");
result.Should().MatchRegex("pattern");
```

#### TestContainers (Integration Testing)

**Container Types Used:**
- **PostgreSQL** - Database integration tests
- **Redis** - Caching and session storage tests
- **Qdrant** - Vector database tests

**Pattern with IAsyncLifetime:**
```csharp
public class DatabaseTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("test_db")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        // Run migrations, seed data
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    private string GetConnectionString() => _postgres.GetConnectionString();
}
```

**Parallel Container Startup:**
```csharp
var postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();
var redis = new RedisBuilder("redis:7-alpine").Build();

await Task.WhenAll(
    postgres.StartAsync(),
    redis.StartAsync()
).ConfigureAwait(false);
```

#### WebApplicationFactory (ASP.NET Core Integration Tests)

**Factory Pattern:**
```csharp
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();
    private readonly RedisContainer _redis = new RedisBuilder("redis:7-alpine").Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove real implementations
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            
            // Add test implementations
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));
        });
    }

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync());
    }

    public async Task DisposeAsync()
    {
        await Task.WhenAll(_postgres.DisposeAsync(), _redis.DisposeAsync());
    }
}
```

**HTTP Client Testing:**
```csharp
public class ApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetEndpoint_ReturnsSuccess()
    {
        var response = await _client.GetAsync("/api/endpoint");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

### Test Organization Patterns

#### Arrange-Act-Assert (AAA)
```csharp
[Fact]
public async Task ProcessOrder_WhenStockAvailable_ReducesInventory()
{
    // Arrange
    var product = new Product { Stock = 10 };
    var order = new Order { ProductId = product.Id, Quantity = 3 };
    _productRepoMock.Setup(x => x.GetById(product.Id)).ReturnsAsync(product);

    // Act
    await _orderService.ProcessOrderAsync(order);

    // Assert
    product.Stock.Should().Be(7);
    _productRepoMock.Verify(x => x.Update(product), Times.Once);
}
```

#### Test Naming Conventions
```
MethodName_StateUnderTest_ExpectedBehavior

Examples:
- ProcessOrder_StockAvailable_ReducesInventory
- ProcessOrder_InsufficientStock_ThrowsOutOfStockException
- CalculatePrice_EmptyCart_ReturnsZero
- SendEmail_InvalidAddress_ThrowsValidationException
```

#### Test Categories
Use `[Trait]` to categorize tests:
```csharp
[Trait("Category", "Unit")]           // Fast, isolated, in-memory
[Trait("Category", "Integration")]    // Database, external services
[Trait("Category", "Security")]       // Auth, encryption, validation
[Trait("Category", "Smoke")]          // Critical path health checks
```

**Run specific categories:**
```bash
dotnet test --filter "Category=Unit"
dotnet test --filter "Category!=Integration"
```

### BDD with Reqnroll (Optional)

When implementing Behavior Driven Development:

**Feature File (.feature):**
```gherkin
Feature: User Authentication
  As a user
  I want to authenticate with my credentials
  So that I can access protected resources

  Scenario: Successful login with valid credentials
    Given a registered user with email "user@example.com" and password "password123"
    When the user attempts to login
    Then the login should succeed
    And an authentication token should be returned
```

**Step Definitions:**
```csharp
[Binding]
public class AuthenticationSteps
{
    private readonly AuthenticationContext _context;

    public AuthenticationSteps(AuthenticationContext context)
    {
        _context = context;
    }

    [Given(@"a registered user with email ""(.*)"" and password ""(.*)""")]
    public void GivenARegisteredUser(string email, string password)
    {
        _context.User = new User { Email = email, Password = password };
    }

    [When(@"the user attempts to login")]
    public async Task WhenTheUserAttemptsToLogin()
    {
        _context.Result = await _authService.LoginAsync(_context.User);
    }

    [Then(@"the login should succeed")]
    public void ThenTheLoginShouldSucceed()
    {
        _context.Result.IsSuccess.Should().BeTrue();
    }
}
```

### TDD Workflow (Red-Green-Refactor)

1. **Red** - Write a failing test that defines the expected behavior
2. **Green** - Write the minimum code to make the test pass
3. **Refactor** - Improve the code while keeping tests green
4. **Repeat** - Move to the next behavior

**Example TDD Cycle:**
```csharp
// Step 1: RED - Write failing test
[Fact]
public void CalculateDiscount_EmptyCart_ReturnsZero()
{
    var calculator = new DiscountCalculator();
    var result = calculator.Calculate(new Cart());
    result.Should().Be(0);  // Fails - method doesn't exist yet
}

// Step 2: GREEN - Minimum implementation
public decimal Calculate(Cart cart)
{
    return 0;  // Simplest code to pass
}

// Step 3: REFACTOR - Improve while keeping test green
// (Add more tests, extract methods, improve naming)
```

### Testing Anti-Patterns to Avoid

❌ **Logic in tests** - Tests should be straightforward, no conditionals or loops  
❌ **Multiple assertions** - One concept per test (use multiple tests or Assert.Multiple)  
❌ **External dependencies** - Mock everything external; use TestContainers for databases  
❌ **Shared mutable state** - Tests should be independent and parallelizable  
❌ **Testing implementation details** - Test behavior, not internal structure  
❌ **Ignored/flaky tests** - Fix or delete, don't leave commented out

✅ **Fast tests** - Unit tests should run in milliseconds  
✅ **Deterministic tests** - Same input always produces same output  
✅ **Readable tests** - Clear arrange/act/assert, descriptive names  
✅ **Isolated tests** - No dependencies between tests

---

## Documentation

- Keep XML docs updated for public APIs when the repo expects it
- Ensure new public types and members have meaningful summaries and parameter docs

---

## Change Guidelines

- Keep changes **minimal** and reviewable
- Do **not** reformat unrelated files
- Do **not** introduce new warnings
- Do **not** add secrets to source control
- Prefer **small, focused commits** that match the intent of the change

---

## Conflict Resolution

If guidance conflicts:
1. Follow the repo for the current change
2. Propose improvements separately if needed
3. Document rationale in the PR description

---

## Architecture Overview

Maintainer-owned section. Keep this updated with the current solution layout (entry points, boundaries, infrastructure, and test strategy) when architecture changes.

## Project-Specific Conventions

Maintainer-owned section. Add concrete naming, layering, and dependency conventions specific to this repository; avoid leaving placeholders.
