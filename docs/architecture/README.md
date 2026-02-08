# Synaxis Architecture Documentation

> **Overview**: Synaxis is an SDK-first, multi-transport AI inference gateway built on CQRS principles with pluggable provider support.

## 📚 Documentation Index

### Core Architecture
- **[Package Architecture](./packages.md)** - 4-tier package structure, dependency rules, and modular design
- **[Mediator Pattern](./mediator.md)** - CQRS implementation, request/response flow, and pipeline behaviors
- **[Transport Layer](./transports.md)** - HTTP, gRPC, WebSocket abstraction and executor pattern
- **[Provider System](./providers.md)** - AI provider integration, routing, and extensibility

### Architecture Decision Records
- **[ADR Index](./adr-index.md)** - Complete list of all architectural decisions with summaries

## 🎯 Quick Start

New to Synaxis architecture? Start here:

1. **Understand the Layers**: Read [Package Architecture](./packages.md) to see how Synaxis is organized
2. **Learn the Flow**: Read [Mediator Pattern](./mediator.md) to understand request handling
3. **Pick a Transport**: Read [Transport Layer](./transports.md) to see how clients connect
4. **Add Providers**: Read [Provider System](./providers.md) to integrate AI services

## 🏗️ High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        Client Layer                          │
│  (SDK, HTTP Client, gRPC Client, WebSocket Client)          │
└───────────────────────┬─────────────────────────────────────┘
                        │
┌───────────────────────▼─────────────────────────────────────┐
│                    Transport Layer                           │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐   │
│  │   HTTP   │  │   gRPC   │  │WebSocket │  │In-Process│   │
│  │(REST/SSE)│  │ Streaming│  │ Bi-Directional│Mediator │   │
│  └─────┬────┘  └─────┬────┘  └─────┬────┘  └─────┬────┘   │
└────────┼─────────────┼─────────────┼─────────────┼─────────┘
         │             │             │             │
         └─────────────┴─────────────┴─────────────┘
                        │
┌───────────────────────▼─────────────────────────────────────┐
│                   Mediator (CQRS Core)                       │
│  ┌────────────────────────────────────────────────────┐     │
│  │  Pipeline: Logging → Validation → Metrics → Auth  │     │
│  └───────────────────┬────────────────────────────────┘     │
│                      │                                       │
│  ┌───────────────────▼────────────────────────────────┐     │
│  │         Command/Query/Stream Handlers              │     │
│  └───────────────────┬────────────────────────────────┘     │
└────────────────────────┼────────────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────────────┐
│                    Provider Layer                            │
│  ┌────────┐  ┌────────┐  ┌────────┐  ┌────────┐           │
│  │ OpenAI │  │Anthropic│  │ Google │  │ Azure  │  ...      │
│  └────────┘  └────────┘  └────────┘  └────────┘           │
└─────────────────────────────────────────────────────────────┘
```

## 🔑 Key Concepts

### SDK-First Design
Synaxis prioritizes SDK consumers with minimal dependencies. Packages are layered to allow tree-shaking and selective imports.

### Transport Independence
Business logic is decoupled from transport mechanisms. The same handlers work across HTTP, gRPC, WebSocket, and in-process calls.

### CQRS Pattern
Commands, Queries, and Streams are first-class citizens with typed request/response contracts and composable pipeline behaviors.

### Provider Abstraction
AI providers implement common interfaces (`IChatProvider`, `IEmbeddingProvider`, etc.) enabling multi-provider routing and failover.

### Explicit Registration
No magic auto-discovery. All components are explicitly registered for predictability, testability, and performance.

## 📦 Package Overview

| Package | Purpose | Dependencies |
|---------|---------|-------------|
| `Synaxis.Abstractions` | Core interfaces | None |
| `Synaxis.Contracts` | Request/Response DTOs | Abstractions |
| `Synaxis.Mediator` | CQRS implementation | Contracts |
| `Synaxis.Client` | High-level SDK | Mediator |
| `Synaxis.Transport.*` | Transport adapters | Mediator |
| `Synaxis.Providers.*` | AI provider integrations | Abstractions |

See [Package Architecture](./packages.md) for detailed dependency rules.

## 🚀 Request Flow Example

```csharp
// 1. Client creates request
var request = new ChatCompletionRequest
{
    Model = "gpt-4o",
    Messages = [new("user", "Hello!")]
};

// 2. Transport sends to executor
var response = await executor.ExecuteAsync(request);

// 3. Mediator runs pipeline behaviors
// → Logging → Validation → Metrics → Authorization

// 4. Handler processes request
var result = await handler.HandleAsync(request);

// 5. Handler delegates to provider
var completion = await chatProvider.ChatAsync(messages, model);

// 6. Response flows back through pipeline
return completion;
```

## 🎨 Design Principles

1. **Zero Dependencies** - Abstractions package has no external dependencies
2. **Explicit Over Implicit** - All registrations are explicit and traceable
3. **Streaming First** - `IAsyncEnumerable<T>` for native streaming support
4. **Type Safety** - Compile-time validation where possible
5. **Testability** - Interfaces at every boundary for easy mocking
6. **Performance** - Source generators and minimal allocations
7. **Extensibility** - Open for extension via interfaces and behaviors

## 🔍 Deep Dive Topics

### Mediator Pipeline
See [Mediator Pattern](./mediator.md) for:
- How pipeline behaviors compose
- Request/response flow
- Custom behavior creation
- Handler registration

### Transport Abstraction
See [Transport Layer](./transports.md) for:
- Executor pattern
- HTTP REST + SSE
- gRPC streaming
- WebSocket bi-directional
- In-process mediator

### Provider Integration
See [Provider System](./providers.md) for:
- Provider interfaces
- Adding new providers
- Routing strategies
- Health tracking
- Quota management

### Architecture Decisions
See [ADR Index](./adr-index.md) for:
- Why CQRS was chosen
- Package structure rationale
- Transport abstraction reasoning
- Explicit registration benefits

## 🛠️ Extension Points

Synaxis is designed for extensibility:

- **Custom Transports**: Implement `ICommandExecutor`, `IQueryExecutor`, `IStreamExecutor`
- **Pipeline Behaviors**: Implement `IPipelineBehavior<TMessage, TResponse>`
- **Providers**: Implement `IChatProvider`, `IEmbeddingProvider`, etc.
- **Routing Strategies**: Implement `IProviderSelector`
- **Middleware**: Add transport-specific middleware

## 📖 Related Documentation

- [Getting Started Guide](../getting-started/) - Quickstart for new developers
- [API Reference](../api/) - Complete API documentation
- [ADR Archive](../adr/) - Detailed architectural decision records
- [Contributing Guide](../../CONTRIBUTING.md) - How to contribute to Synaxis

## 🤝 Contributing

When making architectural changes:

1. Read existing ADRs to understand context
2. Propose changes via RFC issue
3. Create new ADR if architectural decision is made
4. Update this documentation to reflect changes
5. Maintain backwards compatibility where possible

## 📝 Document Conventions

- **Code Examples**: Use C# 12 syntax with modern patterns
- **Diagrams**: ASCII art or Mermaid for version control friendliness
- **Cross-References**: Link to related docs extensively
- **Versioning**: Document when features were added/deprecated

---

**Last Updated**: 2026-02-08  
**Maintainers**: Synaxis Core Team  
**Status**: Living Documentation
