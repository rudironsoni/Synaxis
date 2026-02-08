# AGENTS.md - AI Coding Assistant Guidelines

Guidelines for AI coding assistants working in the Synaxis codebase.

## Task Tracking

This project uses **bd** (beads) for issue tracking.

```bash
bd ready              # Find available work
bd show <id>          # View issue details
bd update <id> --status in_progress  # Claim work
bd close <id>         # Complete work
bd sync               # Sync with git and push
```

## Session Completion Protocol

Work is NOT complete until `git push` succeeds:

```bash
git status              # Check what changed
git add <files>         # Stage changes
bd sync                 # Commit beads changes
git commit -m "..."     # Commit code
bd sync                 # Sync again
git push                # Push to remote
git status              # MUST show "up to date"
```

## Build, Test, and Lint Commands

### Build
```bash
dotnet restore Synaxis.sln
dotnet build Synaxis.sln
dotnet build Synaxis.sln --configuration Release
```

### Test
```bash
# All tests
dotnet test Synaxis.sln

# Specific test project
dotnet test tests/Synaxis.Tests/

# Single test class
dotnet test --filter "FullyQualifiedName~RetryPolicyTests"

# Single test method
dotnet test --filter "FullyQualifiedName=RetryPolicyTests.ExecuteAsync_WhenActionSucceedsOnFirstAttempt_ReturnsResultImmediately"

# With coverage
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings
```

### Lint/Format
```bash
dotnet format Synaxis.sln
dotnet format Synaxis.sln --verify-no-changes
```

## Code Style Guidelines

### Project Configuration
- **Target Framework**: .NET 10.0
- **Nullable**: Enabled (`<Nullable>enable</Nullable>`)
- **Implicit Usings**: Enabled
- **TreatWarningsAsErrors**: True
- **Central Package Management**: Enabled via Directory.Packages.props

### Naming Conventions
- **Classes**: `PascalCase` (e.g., `ProviderRoutingService`)
- **Interfaces**: `PascalCase` with `I` prefix (e.g., `IProviderClient`)
- **Methods**: `PascalCase`, async methods end with `Async` suffix
- **Private fields**: `_camelCase` with underscore prefix (e.g., `_logger`)
- **Properties**: `PascalCase` (e.g., `ProviderName`)
- **Parameters**: `camelCase`
- **Constants**: `PascalCase`

### Formatting
- **Indentation**: 4 spaces (no tabs)
- **Encoding**: UTF-8
- **Line endings**: CRLF for .cs files (Git handles conversion)
- Use `var` when type is obvious
- Always use braces (even for single-line blocks)
- Keep lines under 120 characters

### Imports and Namespaces
```csharp
// File-scoped namespace with usings INSIDE namespace
namespace Synaxis.InferenceGateway.WebApi.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Synaxis.Core.Models;

    public class MyController : ControllerBase
    {
        private readonly IService _service;

        public MyController(IService service)
        {
            this._service = service;
        }
    }
}
```

### Key Patterns
- Use `this.` prefix for instance members (fields, methods, properties)
- Prefer `is` pattern matching: `if (provider is null)` not `if (provider == null)`
- Use expression-bodied members for simple cases
- Avoid `async void` (except event handlers)
- XML documentation required for public APIs
- Copyright headers optional but present in existing files

### Architecture
- Clean Architecture: Domain → Application → Infrastructure → WebApi
- CQRS pattern with Mediator for commands/queries
- Constructor dependency injection
- Result pattern for operations that can fail
- Multiple DTOs/models allowed per file in `Contracts/`, `Models/`, `Entities/`

### Testing Conventions
```csharp
public class RetryPolicyTests
{
    [Fact]
    public async Task ExecuteAsync_WhenActionSucceeds_ReturnsResultImmediately()
    {
        // Arrange
        var policy = new RetryPolicy(maxRetries: 3);

        // Act
        var result = await policy.ExecuteAsync(action, shouldRetry);

        // Assert
        Assert.Equal(expected, result);
    }
}
```

**Test Framework**: xUnit with FluentAssertions, NSubstitute/Moq  
**Coverage**: Minimum 80% line coverage required

### Error Handling
- Use specific exception types, not generic Exception
- Prefer async/await over ContinueWith
- Use `ConfigureAwait(false)` in library code
- Return structured errors with codes
- Never expose sensitive data in error messages

### Analyzers Enabled
- StyleCop (code style)
- SonarAnalyzer (code quality)
- Meziantou.Analyzer (best practices)
- AsyncFixer (async/await patterns)
- IDisposableAnalyzers (resource management)
- BannedApiAnalyzers (API restrictions)

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

## Commit Convention

Follow Conventional Commits:
- `feat(scope): description`
- `fix(scope): description`
- `test(scope): description`
- `refactor(scope): description`
- `docs(scope): description`

Common scopes: `routing`, `providers`, `streaming`, `auth`, `api`, `tests`

## Important Notes

- **Warnings are treated as errors** - fix all warnings before committing
- **All tests must pass** before merging
- **80% minimum code coverage** required
- No breaking changes without explicit approval
- ULTRA MISER MODE: Optimize for efficiency and cost
