# Synaxis Coding Standards

## Naming Conventions
- **Classes/Records/Structs**: PascalCase (e.g., `OrderService`)
- **Interfaces**: PascalCase with `I` prefix (e.g., `IOrderRepository`)
- **Methods**: PascalCase (e.g., `GetOrderAsync`)
- **Properties**: PascalCase (e.g., `OrderDate`)
- **Private fields**: `_camelCase` (e.g., `_orderRepository`)
- **Parameters/Locals**: camelCase (e.g., `orderId`)
- **Async methods**: Suffix with `Async` (e.g., `GetOrderAsync`)

## File Organization
- **One type per file**: Each class/record/interface in its own file
- **File-scoped namespaces**: Always use file-scoped namespaces (C# 10+)
- **Using directives**: Place outside namespace, order: System.*, Third-party, Project

## Code Style
- **Braces**: Always use braces for control flow (even single-line)
- **Expression bodies**: Use for single-expression members
- **var usage**: Use when type is obvious from right-hand side
- **Null handling**: Prefer pattern matching (`is not null`)
- **String handling**: Prefer string interpolation

## Access Modifiers
- Always specify explicitly (public, private, internal, protected)
- Modifier order: access -> static -> extern -> new -> virtual/abstract/override/sealed -> readonly -> volatile -> async -> partial

## Async/Await Patterns
- Accept `CancellationToken` as last parameter with default value
- Always forward cancellation token to downstream async calls
- Avoid async void (except for event handlers)

## Quality Rules (CRITICAL)
- **NO Task.Delay in tests** - Use TaskCompletionSource instead
- **NO empty catch blocks** - Handle or propagate exceptions
- **NO warning suppressions** - Fix root causes
- **NO NoWarn in projects** - Address analyzer warnings
- **TreatWarningsAsErrors**: Enabled globally

## Testing Standards
- Use xUnit with FluentAssertions and Moq
- Category traits: `[Trait("Category", "Unit")]`, `[Trait("Category", "Integration")]`
- Use `IAsyncLifetime` for async test setup/teardown
- Mock at interface level, not implementation

## Security
- Never commit secrets (API keys, tokens, passwords)
- Use `ILogger` abstractions, never log secrets
- Prefer constructor injection over hidden global state
