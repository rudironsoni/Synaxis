# ADR-012: SDK-First Package Architecture

## Status
Accepted

## Context
Synaxis needs to support multiple consumption patterns to serve different user needs:

1. **SDK Users**: Developers building applications that consume Synaxis as a library
2. **Self-Hosters**: Organizations running their own Synaxis instances
3. **Direct API Users**: Clients interacting with Synaxis via HTTP/gRPC without SDK

The challenge is designing a package structure that:
- Minimizes dependencies for SDK consumers
- Provides clean abstractions for testing and extensibility
- Allows independent versioning of contracts vs. implementation
- Supports multiple transport mechanisms (in-process, HTTP, gRPC)

A monolithic package approach would force all consumers to pull in unnecessary dependencies (e.g., ASP.NET Core for SDK-only users). A fragmented approach without clear boundaries would create confusion and maintenance burden.

## Decision
Split Synaxis into four core packages with explicit dependency boundaries:

### 1. Synaxis.Abstractions (Zero Dependencies)
- Core interfaces and base types
- `ICommandExecutor`, `IStreamExecutor`, `IProviderClient`
- No external dependencies beyond .NET runtime
- Target: Stable, rarely changes

### 2. Synaxis.Contracts (Versioned Messages)
- Request/Response DTOs
- Command and Query message definitions
- Versioned namespaces (V1, V2, etc.)
- Minimal dependencies (System.Text.Json, protobuf-net)
- Target: Independent versioning from implementation

### 3. Synaxis.Mediator (Transport Abstraction)
- Mediator pattern implementation
- In-process command/query handling
- Transport-agnostic streaming via `IAsyncEnumerable<T>`
- Depends on: Abstractions, Contracts
- Target: Pluggable transports

### 4. Synaxis.Client (High-Level SDK)
- Fluent API for common scenarios
- Builder patterns for configuration
- Convenience methods over mediator
- Depends on: Abstractions, Contracts, Mediator
- Target: Developer-friendly experience

### Dependency Graph
```
Synaxis.Client
    ↓
Synaxis.Mediator
    ↓
Synaxis.Contracts
    ↓
Synaxis.Abstractions
```

### Package Consumption Patterns
- **SDK Users**: Install `Synaxis.Client` (pulls all layers)
- **Custom Transport**: Install `Synaxis.Contracts` + `Synaxis.Abstractions`, implement own executor
- **Testing**: Reference `Synaxis.Abstractions` only, mock interfaces
- **Self-Hosting**: Install all packages, register services via DI

## Consequences

### Positive
- **Minimal Dependencies**: SDK users only pull what they need
- **Clear Boundaries**: Each package has single responsibility
- **Testability**: Interfaces in Abstractions enable easy mocking
- **Flexibility**: Multiple transport implementations (HTTP, gRPC, in-process)
- **Independent Versioning**: Contracts can evolve without breaking SDK
- **Tree-Shaking**: Unused packages not included in final deployments

### Negative
- **More Packages**: Four packages to maintain instead of one
- **Version Coordination**: Breaking changes require coordinated releases
- **Documentation Overhead**: Need clear guidance on which packages to use
- **Initial Complexity**: Setup requires understanding package roles

### Mitigation Strategies
- Use central package management (`Directory.Packages.props`)
- Lock-step versioning for Synaxis 1.x (all packages same version)
- Comprehensive documentation with consumption examples
- CLI tooling (`synaxis init`) to scaffold correct package references

## Related
- [ADR-013: Transport Abstraction with Mediator](./013-transport-abstraction-mediator.md)
- [ADR-014: Explicit Registration Pattern](./014-explicit-registration-pattern.md)
- [ADR-015: Contracts Versioning Strategy](./015-contracts-versioning-strategy.md)
- [ADR-001: Stream-Native CQRS](./001-stream-native-cqrs.md)
