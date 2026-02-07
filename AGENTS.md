# AGENTS.md - AI Coding Assistant Guidelines

Guidelines for AI coding assistants working in the Synaxis codebase.

## Task Tracking

This project uses **bd** (beads) for issue tracking. Run `bd onboard` to get started.

### Quick Reference

```bash
bd ready              # Find available work
bd show <id>          # View issue details
bd update <id> --status in_progress  # Claim work
bd close <id>         # Complete work
bd sync               # Sync with git
```

## Landing the Plane (Session Completion)

**When ending a work session**, you MUST complete ALL steps below. Work is NOT complete until `git push` succeeds.

**MANDATORY WORKFLOW:**

1. **File issues for remaining work** - Create issues for anything that needs follow-up
2. **Run quality gates** (if code changed) - Tests, linters, builds
3. **Update issue status** - Close finished work, update in-progress items
4. **PUSH TO REMOTE** - This is MANDATORY:
   ```bash
   git pull --rebase
   bd sync
   git push
   git status  # MUST show "up to date with origin"
   ```
5. **Clean up** - Clear stashes, prune remote branches
6. **Verify** - All changes committed AND pushed
7. **Hand off** - Provide context for next session

**CRITICAL RULES:**
- Work is NOT complete until `git push` succeeds
- NEVER stop before pushing - that leaves work stranded locally
- NEVER say "ready to push when you are" - YOU must push
- If push fails, resolve and retry until it succeeds

Use 'bd' for task tracking

## Build, Test, and Lint Commands

### Build
```bash
# Restore dependencies
dotnet restore Synaxis.sln

# Build solution (Debug)
dotnet build Synaxis.sln

# Build (Release)
dotnet build Synaxis.sln --configuration Release

# Clean build artifacts
find . -type d -name "obj" -exec rm -rf {} + 2>/dev/null
find . -type d -name "bin" -exec rm -rf {} + 2>/dev/null
```

### Test
```bash
# Run all tests
dotnet test Synaxis.sln

# Run specific test project
dotnet test tests/Synaxis.Tests/Synaxis.Tests.csproj

# Run specific test class
dotnet test --filter "FullyQualifiedName~RetryPolicyTests"

# Run single test method
dotnet test --filter "FullyQualifiedName=RetryPolicyTests.ExecuteAsync_WhenActionSucceedsOnFirstAttempt_ReturnsResultImmediately"

# Run with coverage (requires coverlet)
dotnet test Synaxis.sln --collect:"XPlat Code Coverage" --results-directory ./coverage
```

### Lint/Format
```bash
# Format code
dotnet format Synaxis.sln

# Verify formatting without changes
dotnet format Synaxis.sln --verify-no-changes
```

## Code Style Guidelines

### C# Standards

**Framework:** .NET 10 with central package management (Directory.Packages.props)

**Build Configuration:** (from Directory.Build.props)
- `TreatWarningsAsErrors: true`
- `Nullable: enable`
- `ImplicitUsings: enable`

**Naming Conventions:**
- Classes: `PascalCase` (e.g., `ProviderRoutingService`)
- Interfaces: `PascalCase` with `I` prefix (e.g., `IProviderClient`)
- Methods: `PascalCase` (e.g., `SendChatRequestAsync`)
- Private fields: `_camelCase` (e.g., `_logger`)
- Constants: `PascalCase` (e.g., `MaxRetries`)
- Properties: `PascalCase` (e.g., `ProviderName`)
- Async methods: Must end with `Async` suffix

**Code Style Rules:**
- Use `var` when type is obvious
- Prefer `is` pattern matching: `if (provider is null)` not `if (provider == null)`
- Always use braces (even for single-line blocks)
- Use expression-bodied members for simple cases
- Avoid `async void` (except event handlers)

**Architecture:**
- Clean Architecture: Domain → Application → Infrastructure → WebApi
- CQRS pattern with MediatR for commands/queries
- Constructor dependency injection
- Result pattern for operations that can fail

### Testing Standards

**Framework:** xUnit with FluentAssertions, NSubstitute/Moq

**Coverage Requirements:** (from coverlet.runsettings)
- Minimum 80% line coverage threshold
- Format: Cobertura
- Excludes: `[*]Tests*`

**Test Structure:**
```csharp
[Fact]
public async Task MethodName_Scenario_ExpectedResult()
{
    // Arrange
    var mock = new Mock<IService>();
    var sut = new Service(mock.Object);

    // Act
    var result = await sut.DoSomethingAsync();

    // Assert
    result.IsSuccess.Should().BeTrue();
}
```

## Project Structure

```
Synaxis.sln
├── src/
│   ├── Synaxis.Core/           # Domain models, contracts
│   ├── Synaxis.Api/            # API controllers
│   ├── Synaxis.Infrastructure/ # External services, persistence
│   └── InferenceGateway/
│       ├── WebApi/             # Controllers, middleware
│       ├── Application/        # Use cases, commands, queries
│       └── Infrastructure/     # Provider clients, persistence
├── tests/
│   ├── Synaxis.Tests/          # Core tests
│   └── InferenceGateway/
│       ├── UnitTests/
│       ├── Application.Tests/
│       ├── Infrastructure.Tests/
│       └── IntegrationTests/
└── Directory.Packages.props    # Central package versions
```

## Error Handling

- Use Result pattern for operations that can fail
- Return structured errors with codes
- Log errors with appropriate levels
- Never expose sensitive data in error messages

## Commit Convention

Follow Conventional Commits:
- `feat(scope): description`
- `fix(scope): description`
- `test(scope): description`
- `refactor(scope): description`
- `docs(scope): description`

Common scopes: `routing`, `providers`, `streaming`, `auth`, `api`, `tests`

## Important Notes

- Warnings are treated as errors - fix all warnings
- All tests must pass before merging
- 80% minimum code coverage required
- No breaking changes without explicit approval
- ULTRA MISER MODE™: Optimize for efficiency and cost
