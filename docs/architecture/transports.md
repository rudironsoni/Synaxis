# Transport Layer

> **TL;DR**: Synaxis transports (HTTP, gRPC, WebSocket) are thin adapters that delegate to executor interfaces, enabling transport-independent business logic.

## 🎯 Transport Abstraction

The transport layer's job is simple: **receive requests, delegate to executors, return responses**. No business logic lives in transports.

```
┌────────────────────────────────────────────────────────┐
│              Transport Layer (Adapters)                 │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐            │
│  │   HTTP   │  │   gRPC   │  │WebSocket │            │
│  │(REST/SSE)│  │ Streaming│  │ Bi-Dir   │            │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘            │
│       │             │             │                    │
│       └─────────────┴─────────────┘                    │
│                     │                                  │
│                     ▼                                  │
│  ┌────────────────────────────────────────────┐       │
│  │        Executor Interfaces                 │       │
│  │  - ICommandExecutor                        │       │
│  │  - IQueryExecutor                          │       │
│  │  - IStreamExecutor                         │       │
│  └────────────────────────────────────────────┘       │
└────────────────────────────────────────────────────────┘
                        │
                        ▼
┌────────────────────────────────────────────────────────┐
│              Mediator (Business Logic)                  │
└────────────────────────────────────────────────────────┘
```

## 🌐 HTTP Transport

### REST + Server-Sent Events

**Package**: `Synaxis.Transport.Http`

**Capabilities**:
- RESTful endpoints for unary requests
- Server-Sent Events (SSE) for streaming responses
- Standardized error handling
- Correlation ID middleware
- OpenAPI/Swagger integration

### Architecture

```csharp
namespace Synaxis.Transport.Http
{
    // Thin controller: no business logic
    [ApiController]
    [Route("v1/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly ICommandExecutor _commandExecutor;
        private readonly IStreamExecutor _streamExecutor;

        public ChatController(
            ICommandExecutor commandExecutor,
            IStreamExecutor streamExecutor)
        {
            _commandExecutor = commandExecutor;
            _streamExecutor = streamExecutor;
        }

        // Unary request/response
        [HttpPost("completions")]
        public async Task<ChatCompletionResponse> CreateCompletion(
            [FromBody] ChatCompletionRequest request,
            CancellationToken cancellationToken)
        {
            return await _commandExecutor.ExecuteAsync<ChatCompletionResponse>(
                request, 
                cancellationToken);
        }

        // Streaming via Server-Sent Events
        [HttpPost("stream")]
        public async Task StreamCompletion(
            [FromBody] StreamInferenceQuery query,
            CancellationToken cancellationToken)
        {
            Response.ContentType = "text/event-stream";
            Response.Headers.CacheControl = "no-cache";
            Response.Headers.Connection = "keep-alive";

            await foreach (var chunk in _streamExecutor.ExecuteAsync<ChatChunk>(
                query, 
                cancellationToken))
            {
                var json = JsonSerializer.Serialize(chunk);
                await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }

            await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
        }
    }
}
```

### Middleware Pipeline

```csharp
public static class HttpTransportConfiguration
{
    public static IApplicationBuilder UseSynaxisTransportHttp(
        this IApplicationBuilder app)
    {
        // Correlation ID for request tracing
        app.UseMiddleware<CorrelationIdMiddleware>();

        // Exception handling
        app.UseExceptionHandler("/error");

        // Routing
        app.UseRouting();

        // Authentication/Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // Map controllers
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });

        return app;
    }
}
```

### Error Handling

```csharp
[ApiController]
public class ErrorController : ControllerBase
{
    [Route("/error")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult HandleError()
    {
        var context = HttpContext.Features.Get<IExceptionHandlerFeature>();
        var exception = context?.Error;

        var (statusCode, error) = exception switch
        {
            ValidationException validationEx => (400, new ErrorResponse
            {
                Code = "Validation.Failed",
                Message = "Request validation failed",
                Details = validationEx.Errors.Select(e => e.ErrorMessage).ToList()
            }),

            UnauthorizedException => (401, new ErrorResponse
            {
                Code = "Auth.Unauthorized",
                Message = "Unauthorized request"
            }),

            NotFoundException notFoundEx => (404, new ErrorResponse
            {
                Code = "NotFound",
                Message = notFoundEx.Message
            }),

            _ => (500, new ErrorResponse
            {
                Code = "Internal.Error",
                Message = "An unexpected error occurred"
            })
        };

        return StatusCode(statusCode, error);
    }
}
```

### Registration

```csharp
// In Program.cs or Startup.cs
builder.Services.AddSynaxisTransportHttp(options =>
{
    options.EnableSwagger = true;
    options.EnableCors = true;
    options.EnableCompression = true;
});

// After app.Build()
app.UseSynaxisTransportHttp();
app.MapSynaxisTransportHttp();
```

### Client-Side Usage

```csharp
// Using HttpClient
var httpClient = new HttpClient 
{ 
    BaseAddress = new Uri("https://api.synaxis.io") 
};

var request = new ChatCompletionRequest
{
    Model = "gpt-4o",
    Messages = [new ChatMessage("user", "Hello!")]
};

var response = await httpClient.PostAsJsonAsync(
    "/v1/chat/completions", 
    request);

var result = await response.Content
    .ReadFromJsonAsync<ChatCompletionResponse>();

// Streaming with EventSource (JavaScript)
const eventSource = new EventSource('/v1/chat/stream', {
    method: 'POST',
    body: JSON.stringify(request)
});

eventSource.onmessage = (event) => {
    if (event.data === '[DONE]') {
        eventSource.close();
        return;
    }
    const chunk = JSON.parse(event.data);
    console.log(chunk.delta);
};
```

## 🚀 gRPC Transport

### High-Performance Streaming

**Package**: `Synaxis.Transport.Grpc`

**Capabilities**:
- Binary protocol (Protobuf) for efficiency
- Bidirectional streaming
- HTTP/2 multiplexing
- Built-in deadlines and cancellation

### Service Definition

```protobuf
// inference.proto
syntax = "proto3";

package synaxis.v1;

service InferenceService {
    // Unary RPC
    rpc CreateCompletion(ChatCompletionRequest) returns (ChatCompletionResponse);
    
    // Server streaming
    rpc StreamCompletion(StreamInferenceRequest) returns (stream ChatChunk);
    
    // Bidirectional streaming
    rpc Chat(stream ChatMessage) returns (stream ChatChunk);
}

message ChatCompletionRequest {
    string model = 1;
    repeated ChatMessage messages = 2;
    optional float temperature = 3;
    optional int32 max_tokens = 4;
}

message ChatCompletionResponse {
    string id = 1;
    string content = 2;
    Usage usage = 3;
}

message ChatChunk {
    string id = 1;
    string delta = 2;
    bool is_complete = 3;
}
```

### Implementation

```csharp
namespace Synaxis.Transport.Grpc.Services
{
    public class InferenceGrpcService : InferenceService.InferenceServiceBase
    {
        private readonly ICommandExecutor _commandExecutor;
        private readonly IStreamExecutor _streamExecutor;

        public InferenceGrpcService(
            ICommandExecutor commandExecutor,
            IStreamExecutor streamExecutor)
        {
            _commandExecutor = commandExecutor;
            _streamExecutor = streamExecutor;
        }

        // Unary RPC
        public override async Task<ChatCompletionResponse> CreateCompletion(
            ChatCompletionRequest request,
            ServerCallContext context)
        {
            var command = MapToContractRequest(request);
            var response = await _commandExecutor.ExecuteAsync<
                Contracts.V1.Chat.ChatCompletionResponse>(
                command, 
                context.CancellationToken);

            return MapToGrpcResponse(response);
        }

        // Server streaming
        public override async Task StreamCompletion(
            StreamInferenceRequest request,
            IServerStreamWriter<ChatChunk> responseStream,
            ServerCallContext context)
        {
            var query = MapToStreamQuery(request);

            await foreach (var chunk in _streamExecutor.ExecuteAsync<
                Contracts.V1.Chat.ChatChunk>(
                query, 
                context.CancellationToken))
            {
                await responseStream.WriteAsync(
                    MapToGrpcChunk(chunk), 
                    context.CancellationToken);
            }
        }

        // Bidirectional streaming
        public override async Task Chat(
            IAsyncStreamReader<ChatMessage> requestStream,
            IServerStreamWriter<ChatChunk> responseStream,
            ServerCallContext context)
        {
            await foreach (var message in requestStream.ReadAllAsync(
                context.CancellationToken))
            {
                var query = new StreamInferenceQuery
                {
                    Model = "gpt-4o",
                    Messages = [MapToChatMessage(message)]
                };

                await foreach (var chunk in _streamExecutor.ExecuteAsync<
                    Contracts.V1.Chat.ChatChunk>(
                    query, 
                    context.CancellationToken))
                {
                    await responseStream.WriteAsync(
                        MapToGrpcChunk(chunk), 
                        context.CancellationToken);
                }
            }
        }
    }
}
```

### Registration

```csharp
// Server
builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = true;
    options.MaxReceiveMessageSize = 4 * 1024 * 1024; // 4MB
});

app.MapGrpcService<InferenceGrpcService>();

// Client
var channel = GrpcChannel.ForAddress("https://api.synaxis.io");
var client = new InferenceService.InferenceServiceClient(channel);

var response = await client.CreateCompletionAsync(new ChatCompletionRequest
{
    Model = "gpt-4o",
    Messages = { new ChatMessage { Role = "user", Content = "Hello!" } }
});
```

### Performance Characteristics

| Metric | HTTP/JSON | gRPC/Protobuf |
|--------|-----------|---------------|
| Payload Size | 100% (baseline) | ~40-60% |
| Serialization Time | 100% | ~20-30% |
| Throughput | 100% | ~150-200% |
| Latency | Baseline | -20-30% |

**Use gRPC when**: High throughput, low latency, or large payloads are priorities.

## 🔌 WebSocket Transport

### Bidirectional Real-Time Communication

**Package**: `Synaxis.Transport.WebSocket`

**Capabilities**:
- Full-duplex communication
- Push notifications from server
- Connection persistence
- Client-initiated streaming
- Server-initiated events

### Server Implementation

```csharp
namespace Synaxis.Transport.WebSocket
{
    public class InferenceWebSocketHandler : WebSocketHandler
    {
        private readonly ICommandExecutor _commandExecutor;
        private readonly IStreamExecutor _streamExecutor;

        public override async Task OnConnected(WebSocket socket)
        {
            await base.OnConnected(socket);
            _logger.LogInformation("Client connected: {SocketId}", GetSocketId(socket));
        }

        public override async Task OnDisconnected(WebSocket socket)
        {
            await base.OnDisconnected(socket);
            _logger.LogInformation("Client disconnected: {SocketId}", GetSocketId(socket));
        }

        public override async Task Receive(WebSocket socket, string message)
        {
            var envelope = JsonSerializer.Deserialize<RequestEnvelope>(message);

            switch (envelope.Type)
            {
                case "command":
                    await HandleCommand(socket, envelope);
                    break;

                case "stream":
                    await HandleStream(socket, envelope);
                    break;

                default:
                    await SendError(socket, "Unknown request type");
                    break;
            }
        }

        private async Task HandleCommand(WebSocket socket, RequestEnvelope envelope)
        {
            var request = JsonSerializer.Deserialize<ChatCompletionRequest>(
                envelope.Payload);

            var response = await _commandExecutor.ExecuteAsync<ChatCompletionResponse>(
                request, 
                CancellationToken.None);

            await Send(socket, new ResponseEnvelope
            {
                RequestId = envelope.RequestId,
                Type = "response",
                Payload = JsonSerializer.SerializeToElement(response)
            });
        }

        private async Task HandleStream(WebSocket socket, RequestEnvelope envelope)
        {
            var query = JsonSerializer.Deserialize<StreamInferenceQuery>(
                envelope.Payload);

            await foreach (var chunk in _streamExecutor.ExecuteAsync<ChatChunk>(
                query, 
                CancellationToken.None))
            {
                await Send(socket, new ResponseEnvelope
                {
                    RequestId = envelope.RequestId,
                    Type = "chunk",
                    Payload = JsonSerializer.SerializeToElement(chunk)
                });
            }

            // Send completion marker
            await Send(socket, new ResponseEnvelope
            {
                RequestId = envelope.RequestId,
                Type = "complete"
            });
        }
    }
}
```

### Client Usage (TypeScript)

```typescript
const ws = new WebSocket('wss://api.synaxis.io/ws');

ws.onopen = () => {
    // Send streaming request
    ws.send(JSON.stringify({
        requestId: 'req-123',
        type: 'stream',
        payload: {
            model: 'gpt-4o',
            messages: [{ role: 'user', content: 'Hello!' }]
        }
    }));
};

ws.onmessage = (event) => {
    const envelope = JSON.parse(event.data);
    
    switch (envelope.type) {
        case 'chunk':
            console.log(envelope.payload.delta);
            break;
        case 'complete':
            console.log('Stream complete');
            break;
        case 'error':
            console.error(envelope.payload.message);
            break;
    }
};
```

### Registration

```csharp
var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2)
};

app.UseWebSockets(webSocketOptions);

app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var socket = await context.WebSockets.AcceptWebSocketAsync();
        var handler = context.RequestServices
            .GetRequiredService<InferenceWebSocketHandler>();
        await handler.HandleWebSocket(socket);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});
```

## 🏠 In-Process Transport

### Direct Mediator Execution

**Use Case**: SDK embedded in same process as business logic (no network calls).

```csharp
// Configure services
var services = new ServiceCollection();

// Add mediator
services.AddMediator();

// Add handlers
services.AddScoped<ICommandHandler<ChatCompletionRequest, ChatCompletionResponse>,
    ChatCompletionHandler>();

// Add providers
services.AddOpenAIProvider(options => 
{
    options.ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
});

// Build provider
var serviceProvider = services.BuildServiceProvider();

// Get executor (this IS the mediator)
var executor = serviceProvider.GetRequiredService<ICommandExecutor>();

// Execute directly (no HTTP/gRPC overhead)
var response = await executor.ExecuteAsync<ChatCompletionResponse>(
    new ChatCompletionRequest
    {
        Model = "gpt-4o",
        Messages = [new ChatMessage("user", "Hello!")]
    });
```

**Performance**: ~5-10µs overhead (vs 1-10ms for HTTP, 0.5-5ms for gRPC)

## 🔄 How Transports Use Executors

All transports follow the same pattern:

### 1. Receive Request
```csharp
// HTTP
[HttpPost]
public async Task<IActionResult> Handle([FromBody] TRequest request) { ... }

// gRPC
public override async Task<TResponse> Handle(TRequest request, ServerCallContext ctx) { ... }

// WebSocket
public override async Task Receive(WebSocket socket, string message) { ... }
```

### 2. Validate & Map (Optional)
```csharp
// Validate transport-specific constraints
if (!ModelState.IsValid)
{
    return BadRequest(ModelState);
}

// Map from transport DTO to contract (if needed)
var contractRequest = MapToContract(request);
```

### 3. Delegate to Executor
```csharp
// Unary
var response = await _commandExecutor.ExecuteAsync<TResponse>(
    contractRequest, 
    cancellationToken);

// Streaming
await foreach (var item in _streamExecutor.ExecuteAsync<TItem>(
    contractQuery, 
    cancellationToken))
{
    // Send to client
}
```

### 4. Map & Return
```csharp
// HTTP
return Ok(response);

// gRPC
return MapToGrpcResponse(response);

// WebSocket
await Send(socket, response);
```

**Key Principle**: Transports are **stateless adapters**. All state and logic lives in handlers/providers.

## 📊 Transport Comparison

| Feature | HTTP | gRPC | WebSocket | In-Process |
|---------|------|------|-----------|------------|
| **Protocol** | HTTP/1.1, HTTP/2 | HTTP/2 | HTTP/1.1 Upgrade | N/A |
| **Serialization** | JSON | Protobuf | JSON | None |
| **Streaming** | SSE (server→client) | Bidirectional | Bidirectional | `IAsyncEnumerable<T>` |
| **Browser Support** | ✅ Full | ❌ Limited | ✅ Full | N/A |
| **Firewall Friendly** | ✅ Yes | ⚠️ Sometimes | ✅ Yes | N/A |
| **Latency** | Moderate | Low | Low | Minimal |
| **Throughput** | Moderate | High | Moderate | Highest |
| **Use Case** | Public APIs, Web | Microservices, Mobile | Real-time apps | SDK, Same-process |

## 🔧 Custom Transports

Want to add a new transport? Implement the executor interfaces:

```csharp
public class MqttTransport : ICommandExecutor, IStreamExecutor
{
    private readonly IMqttClient _mqttClient;

    public async Task<TResponse> ExecuteAsync<TResponse>(
        ICommand<TResponse> command,
        CancellationToken cancellationToken)
    {
        // Serialize command
        var payload = JsonSerializer.SerializeToUtf8Bytes(command);

        // Publish to MQTT topic
        await _mqttClient.PublishAsync(
            new MqttApplicationMessage
            {
                Topic = $"synaxis/commands/{typeof(TResponse).Name}",
                Payload = payload
            },
            cancellationToken);

        // Wait for response on reply topic
        var responseMessage = await _mqttClient.WaitForMessageAsync(
            $"synaxis/responses/{correlationId}",
            cancellationToken);

        // Deserialize and return
        return JsonSerializer.Deserialize<TResponse>(responseMessage.Payload);
    }

    public async IAsyncEnumerable<TItem> ExecuteAsync<TItem>(
        IStreamQuery<TItem> query,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Subscribe to stream topic
        await _mqttClient.SubscribeAsync($"synaxis/streams/{streamId}");

        // Yield items as they arrive
        await foreach (var message in _mqttClient.MessagesAsync(cancellationToken))
        {
            yield return JsonSerializer.Deserialize<TItem>(message.Payload);
        }
    }
}
```

Register your custom transport:

```csharp
services.AddSingleton<ICommandExecutor, MqttTransport>();
services.AddSingleton<IStreamExecutor, MqttTransport>();
```

## 🚫 Anti-Patterns

❌ **Don't**: Put business logic in controllers/gRPC services  
✅ **Do**: Delegate to executors immediately

❌ **Don't**: Reference provider implementations in transports  
✅ **Do**: Only reference executor interfaces

❌ **Don't**: Parse/validate complex business rules in transport layer  
✅ **Do**: Use validation behaviors in mediator pipeline

❌ **Don't**: Maintain state in transport handlers  
✅ **Do**: Keep transports stateless, use scoped services for state

## 📚 Related Documentation

- [ADR-013: Transport Abstraction with Mediator](../adr/013-transport-abstraction-mediator.md) - Design rationale
- [Mediator Pattern](./mediator.md) - How executors delegate to handlers
- [Package Architecture](./packages.md) - Where transport packages fit
- [Provider System](./providers.md) - How handlers delegate to providers

---

**Next**: Read [Provider System](./providers.md) to see how handlers integrate with AI providers.
