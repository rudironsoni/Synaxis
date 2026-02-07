# Agent Instructions

This project uses **bd** (beads) for issue tracking. Run `bd onboard` to get started.

## Quick Reference

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

## Build Commands

```bash
# Build entire solution
dotnet build

# Build in Release mode
dotnet build -c Release

# Run all tests
dotnet test

# Run a specific test project
dotnet test tests/Synaxis.Tests/

# Run a single test class
dotnet test --filter "FullyQualifiedName~RetryPolicyTests"

# Run a single test method
dotnet test --filter "RetryPolicyTests.ExecuteAsync_WhenActionSucceedsOnFirstAttempt_ReturnsResultImmediately"

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings

# Run tests in specific project with verbosity
dotnet test tests/InferenceGateway.UnitTests/ -v n
```

## Code Style Guidelines

### Project Configuration
- **Target Framework**: .NET 10.0
- **Nullable**: Enabled (`#nullable enable` or project-level)
- **Implicit Usings**: Enabled
- **TreatWarningsAsErrors**: True (all warnings are build errors)
- **Central Package Management**: Enabled via Directory.Packages.props

### Naming Conventions
- **Private fields**: Use underscore prefix (`_fieldName`) - SA1309 disabled
- **Public members**: PascalCase
- **Parameters**: camelCase
- **Local variables**: camelCase
- **Constants**: PascalCase or UPPER_SNAKE for public constants
- **Interfaces**: Prefix with `I` (e.g., `IUserService`)
- **Generic type parameters**: Prefix with `T` (e.g., `TContainer`)

### Formatting
- **Indentation**: 4 spaces (no tabs)
- **Encoding**: UTF-8
- **Line endings**: CRLF for .cs files (Git handles conversion)
- **Maximum line length**: Not strictly enforced, but keep under 120 characters

### Imports and Using Statements
```csharp
// Use file-scoped namespaces when possible
namespace Synaxis.Core.Models;

// Prefer using directives inside namespace declaration
namespace Synaxis.Core.Models
{
    using System;
    using System.Collections.Generic;
}

// Or at top with file-scoped namespace
using System;
using Microsoft.Extensions.Logging;
```

### Documentation
- XML documentation required for public APIs
- Copyright headers allowed but not enforced (SA1636 disabled)
- Keep comments meaningful and current

### Type Organization
- Multiple DTOs/models allowed per file in `Contracts/`, `Models/`, `Entities/` directories
- One public class per file elsewhere (SA1402 disabled for specific directories)
- Member ordering not strictly enforced (SA1201/SA1202/SA1204 disabled)

### Testing Conventions
```csharp
// Use xUnit with Fact for sync, async Task for async
public class RetryPolicyTests
{
    [Fact]
    public async Task ExecuteAsync_WhenActionSucceedsOnFirstAttempt_ReturnsResultImmediately()
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

### Error Handling
- Use specific exception types, not generic Exception
- Prefer async/await over ContinueWith
- Use `ConfigureAwait(false)` in library code
- Always handle IDisposable properly (IDisposableAnalyzers enabled)

### Async Patterns
- Method names should end with `Async` for async methods
- Return `Task` or `Task<T>`, not `void` for async
- Use `CancellationToken` parameters where appropriate

### Analyzers Enabled
- StyleCop (code style)
- SonarAnalyzer (code quality)
- Meziantou.Analyzer (best practices)
- AsyncFixer (async/await patterns)
- IDisposableAnalyzers (resource management)
- BannedApiAnalyzers (API restrictions)

Use 'bd' for task tracking
