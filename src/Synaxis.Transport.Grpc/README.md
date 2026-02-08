# Synaxis.Transport.Grpc

gRPC transport layer for high-performance, low-latency AI orchestration.

## When to Use

Use this package when you need:
- High-performance communication
- Low-latency streaming
- Efficient binary protocol
- Service-to-service communication
- Strongly-typed contracts
- HTTP/2 multiplexing

## Installation

```bash
dotnet add package Synaxis.Transport.Grpc
```

## Quick Start

### Server Setup

```csharp
using Synaxis.Transport.Grpc.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add Synaxis core
builder.Services.AddSynaxis();

// Add gRPC transport
builder.Services.AddSynaxisTransportGrpc();

var app = builder.Build();

// Map gRPC services
app.MapSynaxisGrpcServices();

app.Run();
```

### Client Usage

```csharp
using Grpc.Net.Client;
using Synaxis.Transport.Grpc.V1;

var channel = GrpcChannel.ForAddress("https://localhost:5001");
var client = new ChatService.ChatServiceClient(channel);

var request = new ChatRequest
{
    Messages = 
    {
        new Message { Role = "user", Content = "Hello!" }
    },
    Model = "gpt-4",
    Temperature = 0.7
};

var response = await client.ChatAsync(request);
Console.WriteLine(response.Content);
```

### Streaming

```csharp
using var call = client.ChatStream(new ChatStreamRequest
{
    Messages = { new Message { Role = "user", Content = "Hello!" } },
    Model = "gpt-4"
});

await foreach (var chunk in call.ResponseStream.ReadAllAsync())
{
    Console.Write(chunk.Content);
}
```

## Features

- **Binary Protocol** - Efficient Protobuf serialization
- **HTTP/2 Multiplexing** - Multiple concurrent streams
- **Bidirectional Streaming** - Full-duplex communication
- **Deadline/Timeout Support** - Request timeout handling
- **Metadata Support** - Headers and trailers
- **Interceptors** - Request/response middleware

## Configuration

```csharp
services.AddSynaxisTransportGrpc(options =>
{
    options.MaxReceiveMessageSize = 4 * 1024 * 1024; // 4MB
    options.MaxSendMessageSize = 4 * 1024 * 1024;    // 4MB
    options.EnableDetailedErrors = false;            // Production setting
});
```

## Available Services

- **ChatService** - Chat completions (unary and streaming)
- **EmbeddingService** - Text embeddings
- **ImageService** - Image generation
- **AudioService** - Speech synthesis and transcription
- **RerankService** - Document reranking

## Proto Definitions

Proto files are located in `Protos/synaxis/v1/`:
- `chat.proto` - Chat service
- `embedding.proto` - Embedding service
- `common.proto` - Shared message types

## Performance

gRPC provides significant performance benefits:
- **30-50% faster** than REST for small messages
- **Lower latency** due to HTTP/2
- **Smaller payload** with Protobuf
- **Better streaming** with backpressure support

## Dependencies

- .NET 10.0
- Synaxis.Abstractions
- Synaxis.Contracts
- Synaxis (core library)
- Grpc.AspNetCore
- Google.Protobuf

## Documentation

Full documentation at [docs/packages/Synaxis.Transport.Grpc.md](/docs/packages/Synaxis.Transport.Grpc.md)
