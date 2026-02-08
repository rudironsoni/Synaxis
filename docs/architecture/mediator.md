# Mediator Pattern & CQRS

> **TL;DR**: Synaxis uses the Mediator pattern to decouple transport mechanisms from business logic, enabling streaming-first CQRS with composable pipeline behaviors.

## 🎯 Why Mediator?

Traditional approaches tightly couple request handling to transport:

```csharp
// ❌ Traditional: Tight coupling to HTTP
[HttpPost("/chat")]
public async Task<IActionResult> Chat([FromBody] ChatRequest request)
{
    var provider = _serviceProvider.GetService<IChatProvider>();
    var result = await provider.ChatAsync(request.Messages);
    return Ok(result);
}
```

**Problems**:
- Business logic embedded in controllers
- Hard to test (requires HTTP context)
- Can't reuse logic across transports (gRPC, WebSocket, in-process)
- No uniform pipeline for logging, validation, metrics

**Mediator solves this** by introducing a single abstraction layer between transport and business logic.

## 📐 CQRS Architecture

Synaxis implements **Command Query Responsibility Segregation** with three request types:

| Type | Purpose | Return | Example |
|------|---------|--------|---------|
| **Command** | State-changing operations | `Task<TResponse>` | Create, Update, Delete |
| **Query** | Read-only operations | `Task<TResponse>` | Get, List, Search |
| **Stream Query** | Streaming operations | `IAsyncEnumerable<TItem>` | Real-time inference, SSE |

### Request Flow

```
┌──────────────┐
│   Transport  │  HTTP / gRPC / WebSocket / In-Process
└──────┬───────┘
       │
       ▼
┌──────────────────────────────────────────────────┐
│              ICommandExecutor                     │
│              IQueryExecutor                       │
│              IStreamExecutor                      │
└──────┬───────────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────────────┐
│         Pipeline Behaviors (Composable)          │
│  ┌─────────┐  ┌─────────┐  ┌─────────┐         │
│  │ Logging │→ │Validation│→ │ Metrics │→ ...   │
│  └─────────┘  └─────────┘  └─────────┘         │
└──────┬───────────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────────────┐
│              Handler (Business Logic)            │
│         ICommandHandler<TRequest, TResponse>     │
│         IQueryHandler<TRequest, TResponse>       │
│         IStreamQueryHandler<TQuery, TItem>       │
└──────┬───────────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────────────┐
│          Provider Layer (External APIs)          │
│       IChatProvider / IEmbeddingProvider         │
└──────────────────────────────────────────────────┘
```

## 🏗️ Core Interfaces

### 1. Executor Abstractions

```csharp
namespace Synaxis.Abstractions.Execution
{
    /// <summary>
    /// Executes commands (state-changing operations).
    /// </summary>
    public interface ICommandExecutor
    {
        Task<TResponse> ExecuteAsync<TResponse>(
            ICommand<TResponse> command,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Executes queries (read-only operations).
    /// </summary>
    public interface IQueryExecutor
    {
        Task<TResponse> ExecuteAsync<TResponse>(
            IQuery<TResponse> query,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Executes streaming queries (real-time data).
    /// </summary>
    public interface IStreamExecutor
    {
        IAsyncEnumerable<TItem> ExecuteAsync<TItem>(
            IStreamQuery<TItem> query,
            CancellationToken cancellationToken = default);
    }
}
```

**Key Design**: Executors are **transport-agnostic**. They work identically whether called from HTTP, gRPC, or in-process.

### 2. Request Markers

```csharp
namespace Synaxis.Abstractions.Messaging
{
    /// <summary>
    /// Marker interface for commands.
    /// </summary>
    public interface ICommand<TResponse> : IMessage<TResponse> { }

    /// <summary>
    /// Marker interface for queries.
    /// </summary>
    public interface IQuery<TResponse> : IMessage<TResponse> { }

    /// <summary>
    /// Marker interface for streaming queries.
    /// </summary>
    public interface IStreamQuery<TItem> : IMessage { }
}
```

### 3. Handler Contracts

```csharp
namespace Synaxis.Abstractions.Handlers
{
    /// <summary>
    /// Handles commands.
    /// </summary>
    public interface ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        Task<TResponse> HandleAsync(
            TCommand command,
            CancellationToken cancellationToken);
    }

    /// <summary>
    /// Handles streaming queries.
    /// </summary>
    public interface IStreamQueryHandler<TQuery, TItem>
        where TQuery : IStreamQuery<TItem>
    {
        IAsyncEnumerable<TItem> HandleAsync(
            TQuery query,
            CancellationToken cancellationToken);
    }
}
```

## 🔄 Request/Response Flow

### Example: Chat Completion

#### 1. Define Request & Response (Contracts)
```csharp
namespace Synaxis.Contracts.V1.Chat
{
    public sealed record ChatCompletionRequest : ICommand<ChatCompletionResponse>
    {
        public required string Model { get; init; }
        public required IReadOnlyList<ChatMessage> Messages { get; init; }
        public float? Temperature { get; init; }
        public int? MaxTokens { get; init; }
    }

    public sealed record ChatCompletionResponse
    {
        public required string Id { get; init; }
        public required string Content { get; init; }
        public required Usage Usage { get; init; }
    }
}
```

#### 2. Implement Handler (Mediator)
```csharp
namespace Synaxis.Application.Handlers
{
    public class ChatCompletionHandler 
        : ICommandHandler<ChatCompletionRequest, ChatCompletionResponse>
    {
        private readonly IChatProvider _chatProvider;
        private readonly ILogger<ChatCompletionHandler> _logger;

        public ChatCompletionHandler(
            IChatProvider chatProvider,
            ILogger<ChatCompletionHandler> logger)
        {
            _chatProvider = chatProvider;
            _logger = logger;
        }

        public async Task<ChatCompletionResponse> HandleAsync(
            ChatCompletionRequest request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Processing chat completion for model {Model}", 
                request.Model);

            // Delegate to provider
            var result = await _chatProvider.ChatAsync(
                request.Messages,
                request.Model,
                new { temperature = request.Temperature, max_tokens = request.MaxTokens },
                cancellationToken);

            // Map provider response to contract
            return new ChatCompletionResponse
            {
                Id = result.Id,
                Content = result.Choices[0].Message.Content,
                Usage = new Usage
                {
                    PromptTokens = result.Usage.PromptTokens,
                    CompletionTokens = result.Usage.CompletionTokens
                }
            };
        }
    }
}
```

#### 3. Execute via Transport (HTTP Controller)
```csharp
namespace Synaxis.Transport.Http.Controllers
{
    [ApiController]
    [Route("v1/chat")]
    public class ChatController : ControllerBase
    {
        private readonly ICommandExecutor _executor;

        public ChatController(ICommandExecutor executor)
        {
            _executor = executor;
        }

        [HttpPost("completions")]
        public async Task<IActionResult> CreateCompletion(
            [FromBody] ChatCompletionRequest request,
            CancellationToken cancellationToken)
        {
            var response = await _executor.ExecuteAsync<ChatCompletionResponse>(
                request, 
                cancellationToken);

            return Ok(response);
        }
    }
}
```

**Notice**: Controller has **zero business logic**. It's a thin adapter between HTTP and Mediator.

## 🔗 Pipeline Behaviors

Pipeline behaviors are middleware that wrap handler execution. They compose like Russian dolls:

```
Request → [Logging → [Validation → [Metrics → [Handler] ] ] ] → Response
```

### Built-in Behaviors

#### 1. Logging Behavior
```csharp
public sealed class LoggingBehavior<TMessage, TResponse> 
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    private readonly ILogger<LoggingBehavior<TMessage, TResponse>> _logger;

    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        var messageType = typeof(TMessage).Name;
        _logger.LogInformation("Handling {MessageType}", messageType);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var response = await next(message, cancellationToken);
            stopwatch.Stop();

            _logger.LogInformation(
                "Handled {MessageType} in {ElapsedMs}ms",
                messageType,
                stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "Error handling {MessageType} after {ElapsedMs}ms",
                messageType,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
```

**Output**:
```
[INFO] Handling ChatCompletionRequest
[INFO] Handled ChatCompletionRequest in 1247ms
```

#### 2. Validation Behavior
```csharp
public sealed class ValidationBehavior<TMessage, TResponse> 
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    private readonly IEnumerable<IValidator<TMessage>> _validators;

    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next(message, cancellationToken);
        }

        var context = new ValidationContext<TMessage>(message);
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count != 0)
        {
            throw new ValidationException(failures);
        }

        return await next(message, cancellationToken);
    }
}
```

#### 3. Metrics Behavior
```csharp
public sealed class MetricsBehavior<TMessage, TResponse> 
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    private readonly IMeterFactory _meterFactory;
    private static readonly Histogram<double> _requestDuration;

    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        var messageType = typeof(TMessage).Name;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next(message, cancellationToken);
            stopwatch.Stop();

            _requestDuration.Record(
                stopwatch.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("message_type", messageType),
                new KeyValuePair<string, object?>("status", "success"));

            return response;
        }
        catch
        {
            stopwatch.Stop();
            _requestDuration.Record(
                stopwatch.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("message_type", messageType),
                new KeyValuePair<string, object?>("status", "failure"));
            throw;
        }
    }
}
```

#### 4. Authorization Behavior
```csharp
public sealed class AuthorizationBehavior<TMessage, TResponse> 
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    private readonly ICurrentUserService _currentUser;
    private readonly IAuthorizationService _authService;

    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        var authorizeAttributes = message.GetType()
            .GetCustomAttributes<AuthorizeAttribute>()
            .ToList();

        if (!authorizeAttributes.Any())
        {
            return await next(message, cancellationToken);
        }

        foreach (var attribute in authorizeAttributes)
        {
            var result = await _authService.AuthorizeAsync(
                _currentUser.User,
                attribute.Policy);

            if (!result.Succeeded)
            {
                throw new UnauthorizedException(
                    $"User not authorized to execute {typeof(TMessage).Name}");
            }
        }

        return await next(message, cancellationToken);
    }
}
```

### Custom Behaviors

Create your own by implementing `IPipelineBehavior<TMessage, TResponse>`:

```csharp
public class CachingBehavior<TMessage, TResponse> 
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage, ICacheable
{
    private readonly IDistributedCache _cache;

    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        var cacheKey = message.GetCacheKey();
        
        // Try cache first
        var cached = await _cache.GetAsync<TResponse>(cacheKey, cancellationToken);
        if (cached != null)
        {
            return cached;
        }

        // Execute handler
        var response = await next(message, cancellationToken);

        // Cache result
        await _cache.SetAsync(cacheKey, response, message.CacheDuration, cancellationToken);

        return response;
    }
}
```

## 🌊 Streaming Support

Synaxis treats streaming as a first-class citizen using `IAsyncEnumerable<T>`:

### Streaming Request
```csharp
public sealed record StreamInferenceQuery : IStreamQuery<ChatChunk>
{
    public required string Model { get; init; }
    public required IReadOnlyList<ChatMessage> Messages { get; init; }
}

public sealed record ChatChunk
{
    public required string Id { get; init; }
    public required string Delta { get; init; }
    public bool IsComplete { get; init; }
}
```

### Streaming Handler
```csharp
public class StreamInferenceHandler 
    : IStreamQueryHandler<StreamInferenceQuery, ChatChunk>
{
    private readonly IChatProvider _chatProvider;

    public async IAsyncEnumerable<ChatChunk> HandleAsync(
        StreamInferenceQuery query,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var streamId = Guid.NewGuid().ToString();

        await foreach (var chunk in _chatProvider.StreamAsync(
            query.Messages,
            query.Model,
            cancellationToken))
        {
            yield return new ChatChunk
            {
                Id = streamId,
                Delta = chunk.Delta,
                IsComplete = chunk.FinishReason != null
            };

            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }
        }
    }
}
```

### Streaming Transport (HTTP SSE)
```csharp
[HttpPost("chat/stream")]
public async Task StreamCompletion(
    [FromBody] StreamInferenceQuery query,
    CancellationToken cancellationToken)
{
    Response.ContentType = "text/event-stream";

    await foreach (var chunk in _streamExecutor.ExecuteAsync<ChatChunk>(
        query, 
        cancellationToken))
    {
        await Response.WriteAsync(
            $"data: {JsonSerializer.Serialize(chunk)}\n\n",
            cancellationToken);

        await Response.Body.FlushAsync(cancellationToken);
    }
}
```

**Benefits**:
- **Backpressure**: Natural flow control via async iteration
- **Cancellation**: Built-in `CancellationToken` support
- **Composability**: LINQ-style operations (`Where`, `Select`, `Take`)
- **Memory Efficient**: Streams items one-at-a-time

## 🔧 Registration

### Automatic Registration (Recommended)
```csharp
using Mediator;

var builder = WebApplication.CreateBuilder(args);

// Add mediator with source generation
builder.Services.AddMediator(options =>
{
    options.ServiceLifetime = ServiceLifetime.Scoped;
});

// Register pipeline behaviors (order matters!)
builder.Services.AddSingleton(
    typeof(IPipelineBehavior<,>), 
    typeof(LoggingBehavior<,>));
    
builder.Services.AddSingleton(
    typeof(IPipelineBehavior<,>), 
    typeof(ValidationBehavior<,>));
    
builder.Services.AddSingleton(
    typeof(IPipelineBehavior<,>), 
    typeof(MetricsBehavior<,>));

builder.Services.AddSingleton(
    typeof(IPipelineBehavior<,>), 
    typeof(AuthorizationBehavior<,>));
```

**Execution Order**: Behaviors run in registration order (logging → validation → metrics → auth → handler)

### Manual Registration
```csharp
// Register specific handlers
builder.Services.AddScoped<ICommandHandler<ChatCompletionRequest, ChatCompletionResponse>, 
    ChatCompletionHandler>();

builder.Services.AddScoped<IStreamQueryHandler<StreamInferenceQuery, ChatChunk>, 
    StreamInferenceHandler>();
```

## 🎭 In-Process vs Transport Execution

### In-Process (SDK)
```csharp
var services = new ServiceCollection();
services.AddMediator();
services.AddSingleton<IChatProvider, OpenAIChatProvider>();

var provider = services.BuildServiceProvider();
var executor = provider.GetRequiredService<ICommandExecutor>();

// Direct, in-memory execution
var response = await executor.ExecuteAsync<ChatCompletionResponse>(
    new ChatCompletionRequest { Model = "gpt-4o", Messages = [...] });
```

### HTTP Transport
```csharp
// Client side
var httpClient = new HttpClient { BaseAddress = new Uri("https://api.synaxis.io") };
var request = new ChatCompletionRequest { Model = "gpt-4o", Messages = [...] };

var response = await httpClient.PostAsJsonAsync("/v1/chat/completions", request);
var result = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>();
```

**Same handler, different transport!** Business logic is identical.

## 📊 Performance

### Overhead
- **In-Process Mediator**: ~5-10µs per request (source generated, zero reflection)
- **Pipeline Behaviors**: ~1-2µs per behavior
- **Serialization (HTTP)**: ~100-500µs depending on payload size

### Optimization Tips
1. Use source generators (martinothamar/Mediator) for zero-reflection dispatch
2. Keep behaviors lightweight (avoid I/O in sync path)
3. Use `ValueTask<T>` for hot paths
4. Profile with BenchmarkDotNet for critical flows

## 🚫 Anti-Patterns

❌ **Don't**: Put business logic in behaviors (keep them cross-cutting)  
✅ **Do**: Keep logic in handlers, behaviors for infrastructure concerns

❌ **Don't**: Return concrete classes from handlers  
✅ **Do**: Return DTOs defined in Contracts package

❌ **Don't**: Reference transport-specific types in handlers  
✅ **Do**: Keep handlers transport-agnostic

❌ **Don't**: Use Service Locator pattern in handlers  
✅ **Do**: Use constructor injection for dependencies

## 📚 Related Documentation

- [ADR-001: Stream-Native CQRS](../adr/001-stream-native-cqrs.md) - Why CQRS was chosen
- [ADR-013: Transport Abstraction with Mediator](../adr/013-transport-abstraction-mediator.md) - Full design rationale
- [Transport Layer](./transports.md) - How transports integrate with mediator
- [Package Architecture](./packages.md) - Where mediator fits in the package hierarchy

---

**Next**: Read [Transport Layer](./transports.md) to see how HTTP, gRPC, and WebSocket use the mediator.
