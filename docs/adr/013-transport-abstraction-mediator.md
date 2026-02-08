# ADR-013: Transport Abstraction with Mediator

## Status
Accepted

## Context
Synaxis commands and queries need to execute across different deployment scenarios:

1. **In-Process**: SDK embedded directly in application (no network calls)
2. **HTTP**: Remote Synaxis instance via REST API
3. **gRPC**: Remote instance via gRPC for performance-critical scenarios
4. **Testing**: Mock implementations for unit tests

The challenge is decoupling command execution logic from transport mechanism without:
- Exposing transport details to business logic
- Duplicating command handling code per transport
- Breaking streaming semantics (e.g., SSE vs. in-memory streams)
- Complicating testing and extensibility

Traditional approaches:
- **Direct HTTP Clients**: Tight coupling to transport, hard to test
- **Repository Pattern**: Doesn't handle commands/queries uniformly
- **Service Locator**: Runtime coupling, difficult to trace dependencies

## Decision
Use the **Mediator pattern** with explicit transport abstractions:

### Core Abstractions

```csharp
namespace Synaxis.Abstractions
{
    // Command execution (request/response)
    public interface ICommandExecutor
    {
        Task<TResponse> ExecuteAsync<TResponse>(
            ICommand<TResponse> command,
            CancellationToken cancellationToken = default);
    }

    // Query execution (request/response)
    public interface IQueryExecutor
    {
        Task<TResponse> ExecuteAsync<TResponse>(
            IQuery<TResponse> query,
            CancellationToken cancellationToken = default);
    }

    // Streaming execution (server-sent events, streaming responses)
    public interface IStreamExecutor
    {
        IAsyncEnumerable<TItem> ExecuteAsync<TItem>(
            IStreamQuery<TItem> query,
            CancellationToken cancellationToken = default);
    }
}
```

### Unified Handling
Commands and queries handled uniformly:
- **Commands**: State-changing operations (e.g., `CreateInferenceCommand`)
- **Queries**: Read-only operations (e.g., `GetModelQuery`)
- **Stream Queries**: Streaming operations (e.g., `StreamInferenceQuery`)

### Transport Implementations

#### In-Process Mediator
```csharp
public class InProcessMediator : ICommandExecutor, IQueryExecutor, IStreamExecutor
{
    private readonly IServiceProvider _services;

    public async Task<TResponse> ExecuteAsync<TResponse>(
        ICommand<TResponse> command,
        CancellationToken cancellationToken)
    {
        var handler = _services.GetRequiredService<ICommandHandler<TResponse>>();
        return await handler.HandleAsync(command, cancellationToken);
    }

    public async IAsyncEnumerable<TItem> ExecuteAsync<TItem>(
        IStreamQuery<TItem> query,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var handler = _services.GetRequiredService<IStreamQueryHandler<TItem>>();
        await foreach (var item in handler.HandleAsync(query, cancellationToken))
        {
            yield return item;
        }
    }
}
```

#### HTTP Transport
```csharp
public class HttpCommandExecutor : ICommandExecutor
{
    private readonly HttpClient _httpClient;

    public async Task<TResponse> ExecuteAsync<TResponse>(
        ICommand<TResponse> command,
        CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/commands")
        {
            Content = JsonContent.Create(command)
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken);
    }
}
```

#### gRPC Transport
```csharp
public class GrpcStreamExecutor : IStreamExecutor
{
    private readonly InferenceService.InferenceServiceClient _client;

    public async IAsyncEnumerable<TItem> ExecuteAsync<TItem>(
        IStreamQuery<TItem> query,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var grpcRequest = MapToGrpcRequest(query);
        var stream = _client.StreamInference(grpcRequest, cancellationToken: cancellationToken);

        await foreach (var item in stream.ResponseStream.ReadAllAsync(cancellationToken))
        {
            yield return MapFromGrpc<TItem>(item);
        }
    }
}
```

### Streaming via IAsyncEnumerable
- **Language-Native**: C# 8+ `IAsyncEnumerable<T>` for streaming
- **Backpressure**: Natural flow control via async iteration
- **Cancellation**: Built-in `CancellationToken` support
- **Composability**: LINQ-style operations (`Where`, `Select`, `Take`)

### Registration Pattern
```csharp
// In-process (self-hosting)
services.AddSynaxisMediator(options => {
    options.UseInProcess();
});

// Remote HTTP
services.AddSynaxisClient(options => {
    options.UseHttp("https://synaxis.example.com");
});

// Testing
services.AddSingleton<ICommandExecutor, MockCommandExecutor>();
```

## Consequences

### Positive
- **Transport Independence**: Business logic agnostic to transport
- **Pluggable Transports**: Swap implementations without code changes
- **Testability**: Easy to mock `ICommandExecutor` in tests
- **Uniform API**: All commands/queries follow same pattern
- **Streaming Support**: First-class streaming via `IAsyncEnumerable<T>`
- **Composition**: Multiple transports in same application (e.g., local + remote)

### Negative
- **Slight Performance Overhead**: Indirection through mediator (~5-10µs per call)
- **Generic Constraints**: Type erasure requires runtime type inspection for routing
- **Learning Curve**: Developers need to understand mediator pattern
- **Serialization Costs**: HTTP/gRPC transports require serialization

### Mitigation Strategies
- **Performance**: Use source generators for zero-allocation routing
- **Type Safety**: Compile-time validation via analyzers
- **Documentation**: Clear examples for each transport scenario
- **Optimization**: In-process mediator has direct handler invocation (no serialization)

## Related
- [ADR-012: SDK-First Package Architecture](./012-sdk-first-package-architecture.md)
- [ADR-015: Contracts Versioning Strategy](./015-contracts-versioning-strategy.md)
- [ADR-001: Stream-Native CQRS](./001-stream-native-cqrs.md)
