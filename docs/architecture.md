# Synaxis Architecture

This document provides a comprehensive overview of the Synaxis platform architecture, including system components, data flow, security model, and design principles.

## Table of Contents

- [System Overview](#system-overview)
- [Component Diagram](#component-diagram)
- [Architecture Layers](#architecture-layers)
- [Data Flow](#data-flow)
- [Security Model](#security-model)
- [Design Principles](#design-principles)
- [Scalability](#scalability)
- [Extensibility](#extensibility)

## System Overview

Synaxis is an **SDK-first, multi-transport AI inference gateway** built on CQRS principles with pluggable provider support. The architecture is designed to be:

- **Modular**: Each component can be used independently or together
- **Extensible**: Easy to add new providers, transports, and features
- **Performant**: Minimal overhead with streaming-first design
- **Secure**: Built-in authentication, authorization, and encryption
- **Observable**: Comprehensive logging, metrics, and tracing

### Key Architectural Decisions

1. **SDK-First**: Prioritizes embedded usage over HTTP-only gateways
2. **Transport Independence**: Business logic decoupled from transport mechanisms
3. **CQRS Pattern**: Clear separation of commands, queries, and streams
4. **Provider Abstraction**: Unified interface for multiple AI providers
5. **Explicit Registration**: No magic auto-discovery for predictability

## Component Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                           Client Layer                               │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐              │
│  │   SDK App    │  │ HTTP Client  │  │ gRPC Client  │  ...         │
│  │  (Embedded)  │  │  (REST/SSE)  │  │  (Streaming) │              │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘              │
└─────────┼─────────────────┼─────────────────┼──────────────────────┘
          │                 │                 │
          └─────────────────┴─────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────────────┐
│                        Transport Layer                              │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐              │
│  │ HTTP Server  │  │ gRPC Server  │  │WebSocket Srv │              │
│  │ (ASP.NET)    │  │ (Grpc.Core)  │  │ (SignalR)    │              │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘              │
└─────────┼─────────────────┼─────────────────┼──────────────────────┘
          │                 │                 │
          └─────────────────┴─────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────────────┐
│                      Mediator (CQRS Core)                           │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │              Pipeline Behaviors                             │   │
│  │  Logging → Validation → Metrics → Auth → Rate Limiting     │   │
│  └──────────────────────────┬──────────────────────────────────┘   │
│                             │                                       │
│  ┌──────────────────────────▼──────────────────────────────────┐   │
│  │              Command/Query/Stream Handlers                  │   │
│  │  • ChatCompletionHandler                                    │   │
│  │  • EmbeddingHandler                                         │   │
│  │  • StreamingHandler                                         │   │
│  └──────────────────────────┬──────────────────────────────────┘   │
└─────────────────────────────┼───────────────────────────────────────┘
                              │
┌─────────────────────────────▼───────────────────────────────────────┐
│                       Provider Layer                                │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐          │
│  │  OpenAI  │  │ Anthropic│  │  Azure   │  │  Google  │  ...     │
│  │ Provider │  │ Provider │  │ Provider │  │ Provider │          │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────┘          │
└───────┼────────────┼────────────┼────────────┼────────────────────┘
        │            │            │            │
        └────────────┴────────────┴────────────┘
                           │
┌──────────────────────────▼─────────────────────────────────────────┐
│                     Infrastructure Layer                            │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐          │
│  │PostgreSQL│  │  Redis   │  │ Qdrant   │  │Prometheus│          │
│  │ (Metadata)│  │ (Cache)  │  │ (Vectors)│  │ (Metrics)│          │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘          │
└─────────────────────────────────────────────────────────────────────┘
```

## Architecture Layers

### 1. Client Layer

The client layer provides multiple ways to interact with Synaxis:

#### SDK (Embedded)
- **Package**: `Synaxis`
- **Usage**: Direct in-process calls
- **Benefits**: Zero network overhead, type-safe, maximum performance

```csharp
var chatService = serviceProvider.GetRequiredService<IChatService>();
var response = await chatService.CompleteAsync(request);
```

#### HTTP Client
- **Package**: `Synaxis.Transport.Http`
- **Protocol**: REST + Server-Sent Events (SSE)
- **Benefits**: Language-agnostic, easy integration

```bash
curl -X POST http://localhost:8080/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{"model": "gpt-4", "messages": [...]}'
```

#### gRPC Client
- **Package**: `Synaxis.Transport.Grpc`
- **Protocol**: gRPC streaming
- **Benefits**: Binary protocol, efficient streaming

```csharp
var client = new Chat.ChatClient(channel);
var response = await client.CompleteAsync(request);
```

#### WebSocket Client
- **Package**: `Synaxis.Transport.WebSocket`
- **Protocol**: WebSocket / SignalR
- **Benefits**: Real-time bi-directional communication

```javascript
const connection = new HubConnectionBuilder()
  .withUrl("http://localhost:8080/hubs/chat")
  .build();
```

### 2. Transport Layer

The transport layer handles protocol-specific concerns and delegates to the mediator:

#### HTTP Transport
- Implements OpenAI-compatible REST API
- Supports SSE for streaming responses
- Handles CORS, compression, and content negotiation

#### gRPC Transport
- Implements protobuf-based API
- Supports bidirectional streaming
- Handles connection pooling and load balancing

#### WebSocket Transport
- Implements real-time communication
- Supports SignalR for automatic reconnection
- Handles message queuing and delivery guarantees

### 3. Mediator Layer (CQRS Core)

The mediator layer implements the CQRS pattern with pipeline behaviors:

#### Pipeline Behaviors
Behaviors are executed in order for each request:

1. **Logging**: Logs request/response details
2. **Validation**: Validates request structure and constraints
3. **Metrics**: Records timing and usage metrics
4. **Authentication**: Verifies API keys and JWT tokens
5. **Authorization**: Checks permissions and quotas
6. **Rate Limiting**: Enforces rate limits per client

#### Handlers
Handlers process specific request types:

- **ChatCompletionHandler**: Processes chat completion requests
- **EmbeddingHandler**: Generates text embeddings
- **StreamingHandler**: Handles streaming responses
- **BatchHandler**: Processes batch requests

```csharp
public class ChatCompletionHandler : IRequestHandler<ChatCompletionCommand, ChatCompletionResponse>
{
    private readonly IProviderSelector _providerSelector;
    private readonly IChatProvider _chatProvider;

    public async Task<ChatCompletionResponse> Handle(
        ChatCompletionCommand request,
        CancellationToken cancellationToken)
    {
        // Select appropriate provider
        var provider = await _providerSelector.SelectProviderAsync(request);

        // Execute request
        return await provider.CompleteAsync(request, cancellationToken);
    }
}
```

### 4. Provider Layer

The provider layer implements AI provider integrations:

#### Provider Interface
All providers implement common interfaces:

```csharp
public interface IChatProvider
{
    Task<ChatCompletionResponse> CompleteAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<ChatCompletionChunk> CompleteStreamAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken = default);
}
```

#### Available Providers
- **OpenAI**: GPT-4, GPT-3.5, DALL-E, Whisper
- **Azure OpenAI**: Azure-hosted OpenAI models
- **Anthropic**: Claude 3, Claude 2
- **Google**: Gemini, PaLM
- **Cohere**: Command, Embed
- **And more...**

#### Provider Selection
Synaxis uses intelligent routing to select the best provider:

- **Cost-Optimized**: Routes to cheapest available provider
- **Performance-Optimized**: Routes to fastest provider
- **Quality-Optimized**: Routes to highest quality provider
- **Custom**: User-defined routing rules

### 5. Infrastructure Layer

The infrastructure layer provides supporting services:

#### PostgreSQL
- Stores metadata, configurations, and audit logs
- Supports multi-tenant data isolation
- Provides transactional consistency

#### Redis
- Caches frequently accessed data
- Implements rate limiting
- Supports distributed locking

#### Qdrant
- Stores vector embeddings
- Provides semantic search
- Supports RAG (Retrieval-Augmented Generation)

#### Prometheus
- Collects metrics and performance data
- Enables monitoring and alerting
- Supports Grafana dashboards

## Data Flow

### Request Flow (Chat Completion)

```
1. Client Request
   ↓
2. Transport Layer (HTTP/gRPC/WebSocket)
   ↓
3. Mediator Pipeline
   ├─ Logging
   ├─ Validation
   ├─ Metrics
   ├─ Authentication
   ├─ Authorization
   └─ Rate Limiting
   ↓
4. Handler (ChatCompletionHandler)
   ↓
5. Provider Selector
   ↓
6. Provider (OpenAI/Anthropic/Azure)
   ↓
7. AI Provider API
   ↓
8. Response (back through pipeline)
   ↓
9. Client Response
```

### Streaming Flow

```
1. Client Request (stream: true)
   ↓
2. Transport Layer
   ↓
3. Mediator Pipeline
   ↓
4. StreamingHandler
   ↓
5. Provider (streaming)
   ↓
6. AI Provider API (streaming)
   ↓
7. Stream Chunks (IAsyncEnumerable)
   ↓
8. Transport Layer (SSE/WebSocket)
   ↓
9. Client (real-time)
```

### Error Handling Flow

```
1. Error Occurs
   ↓
2. Error Handler
   ├─ Log Error
   ├─ Record Metrics
   └─ Determine Retry Strategy
   ↓
3. Retry Logic (if applicable)
   ├─ Exponential Backoff
   ├─ Provider Fallback
   └─ Circuit Breaker
   ↓
4. Error Response
   ├─ Standard Error Format
   ├─ Error Code
   └─ Suggested Action
```

## Security Model

### Authentication

#### API Key Authentication
- Simple API key in header: `Authorization: Bearer <api-key>`
- Stored securely in database with hashing
- Supports key rotation and revocation

#### JWT Authentication
- JWT tokens for stateless authentication
- Claims-based authorization
- Configurable token expiration

#### OAuth 2.0
- Support for external OAuth providers
- Token exchange and refresh
- Scope-based permissions

### Authorization

#### Role-Based Access Control (RBAC)
- Define roles (Admin, User, ReadOnly)
- Assign permissions to roles
- Map users to roles

#### Resource-Based Access Control
- Fine-grained permissions per resource
- Owner-based access control
- Team/organization sharing

#### Quota Management
- Per-user request quotas
- Token usage limits
- Cost budgeting

### Data Security

#### Encryption
- TLS 1.3 for all network communication
- AES-256 encryption for data at rest
- Secure key management

#### Data Privacy
- No logging of sensitive data
- PII redaction in logs
- GDPR compliance

#### Audit Logging
- All requests logged with metadata
- Immutable audit trail
- Compliance reporting

### Network Security

#### CORS Configuration
- Configurable allowed origins
- Preflight request handling
- Credential support

#### Rate Limiting
- Per-IP rate limits
- Per-user rate limits
- Distributed rate limiting with Redis

#### DDoS Protection
- Request throttling
- IP blacklisting
- Challenge-response for suspicious traffic

## Design Principles

### 1. SDK-First Design
- Prioritize embedded usage over HTTP-only
- Minimal dependencies for core packages
- Tree-shaking support for bundle size optimization

### 2. Transport Independence
- Business logic decoupled from transport
- Same handlers work across all transports
- Easy to add new transports

### 3. CQRS Pattern
- Clear separation of commands, queries, and streams
- Composable pipeline behaviors
- Testable and maintainable

### 4. Provider Abstraction
- Unified interface for all providers
- Easy to add new providers
- Provider-agnostic application code

### 5. Explicit Registration
- No magic auto-discovery
- All components explicitly registered
- Predictable behavior

### 6. Streaming First
- Native streaming support with `IAsyncEnumerable<T>`
- Efficient memory usage
- Real-time response delivery

### 7. Type Safety
- Compile-time validation where possible
- Strong typing for all contracts
- Minimal runtime errors

### 8. Testability
- Interfaces at every boundary
- Easy mocking for unit tests
- Integration test support

### 9. Performance
- Minimal allocations
- Async/await throughout
- Connection pooling and reuse

### 10. Observability
- Structured logging
- Comprehensive metrics
- Distributed tracing

## Scalability

### Horizontal Scaling
- Stateless design enables horizontal scaling
- Load balancer distributes requests
- Session affinity not required

### Vertical Scaling
- Efficient resource usage
- Connection pooling
- Caching strategies

### Database Scaling
- Read replicas for PostgreSQL
- Redis clustering for cache
- Sharding support for large datasets

### Provider Scaling
- Multiple provider instances
- Provider load balancing
- Automatic failover

## Extensibility

### Custom Transports
Implement `ICommandExecutor`, `IQueryExecutor`, `IStreamExecutor`:

```csharp
public class CustomTransport : ICommandExecutor<ChatCompletionCommand>
{
    public async Task<ChatCompletionResponse> ExecuteAsync(
        ChatCompletionCommand request,
        CancellationToken cancellationToken)
    {
        // Custom transport logic
    }
}
```

### Custom Providers
Implement `IChatProvider`, `IEmbeddingProvider`, etc.:

```csharp
public class CustomProvider : IChatProvider
{
    public async Task<ChatCompletionResponse> CompleteAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken)
    {
        // Custom provider logic
    }
}
```

### Pipeline Behaviors
Implement `IPipelineBehavior<TMessage, TResponse>`:

```csharp
public class CustomBehavior : IPipelineBehavior<ChatCompletionCommand, ChatCompletionResponse>
{
    public async Task<ChatCompletionResponse> Handle(
        ChatCompletionCommand request,
        RequestHandlerDelegate<ChatCompletionResponse> next,
        CancellationToken cancellationToken)
    {
        // Pre-processing
        var response = await next();
        // Post-processing
        return response;
    }
}
```

### Custom Routing
Implement `IProviderSelector`:

```csharp
public class CustomSelector : IProviderSelector
{
    public async Task<IChatProvider> SelectProviderAsync(
        ChatCompletionRequest request)
    {
        // Custom routing logic
    }
}
```

---

**Next**: Learn about the [API Reference](./api-reference.md) or explore [Deployment](./deployment.md) options.
