# Synaxis.Adapters.SignalR

SignalR hub adapter for real-time AI capabilities in .NET applications.

## When to Use

Use this package when you want to:
- Add real-time AI to web applications
- Use SignalR for bidirectional communication
- Build interactive chat UIs
- Support browser and .NET clients
- Leverage SignalR's connection management
- Scale with SignalR backplanes (Redis, Azure)

## Installation

```bash
dotnet add package Synaxis.Adapters.SignalR
```

## Quick Start

### Server Setup

```csharp
using Synaxis.Adapters.SignalR.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add Synaxis core
builder.Services.AddSynaxis();

// Add SignalR adapter
builder.Services.AddSynaxisAdapterSignalR(options =>
{
    options.EnableDetailedErrors = false;
    options.MaxMessageSize = 32 * 1024; // 32KB
});

var app = builder.Build();

// Map SignalR hub
app.MapSynaxisSignalRHub("/hubs/synaxis");

app.Run();
```

### Client Usage (JavaScript)

```javascript
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/synaxis")
    .withAutomaticReconnect()
    .build();

// Handle streaming responses
connection.stream("ChatStream", {
    messages: [
        { role: "user", content: "Hello!" }
    ],
    model: "gpt-4",
    temperature: 0.7
})
.subscribe({
    next: (chunk) => console.log(chunk.content),
    complete: () => console.log("Done!"),
    error: (err) => console.error(err)
});

await connection.start();
```

### Client Usage (C#)

```csharp
using Microsoft.AspNetCore.SignalR.Client;
using Synaxis.Contracts.V1.Messages;

var connection = new HubConnectionBuilder()
    .WithUrl("http://localhost:5000/hubs/synaxis")
    .WithAutomaticReconnect()
    .Build();

await connection.StartAsync();

// Non-streaming chat
var response = await connection.InvokeAsync<ChatResponse>("Chat", new
{
    messages = new[]
    {
        new ChatMessage { Role = "user", Content = "Hello!" }
    },
    model = "gpt-4"
});

Console.WriteLine(response.Content);

// Streaming chat
var stream = connection.StreamAsync<ChatStreamChunk>("ChatStream", new
{
    messages = new[] { new ChatMessage { Role = "user", Content = "Hello!" } },
    model = "gpt-4"
});

await foreach (var chunk in stream)
{
    Console.Write(chunk.Content);
}
```

### React Hook Example

```typescript
import { useEffect, useState } from 'react';
import * as signalR from '@microsoft/signalr';

function useAIChat() {
    const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
    const [response, setResponse] = useState('');

    useEffect(() => {
        const conn = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/synaxis')
            .withAutomaticReconnect()
            .build();

        conn.start().then(() => setConnection(conn));

        return () => { conn.stop(); };
    }, []);

    const chat = async (message: string) => {
        if (!connection) return;

        setResponse('');
        
        connection.stream('ChatStream', {
            messages: [{ role: 'user', content: message }],
            model: 'gpt-4'
        }).subscribe({
            next: (chunk) => setResponse(prev => prev + chunk.content),
            complete: () => console.log('Done'),
            error: (err) => console.error(err)
        });
    };

    return { chat, response };
}
```

## Features

- **Streaming Methods** - Real-time token streaming
- **Invoke Methods** - Request/response pattern
- **Automatic Reconnection** - Built-in connection resilience
- **TypeScript Support** - Strongly-typed client
- **Backplane Support** - Redis, Azure Service Bus, etc.
- **Authentication** - JWT, API keys, custom auth

## Available Hub Methods

**Streaming:**
- `ChatStream(request)` - Streaming chat completion
- `GenerateImageStream(request)` - Image generation with progress

**Invoke:**
- `Chat(request)` - Non-streaming chat
- `GenerateEmbedding(request)` - Generate embeddings
- `Transcribe(request)` - Audio transcription
- `Rerank(request)` - Document reranking

## Configuration

```csharp
services.AddSynaxisAdapterSignalR(options =>
{
    options.EnableDetailedErrors = false;           // Production: false
    options.MaxMessageSize = 32 * 1024;            // 32KB
    options.StreamBufferCapacity = 10;             // Stream buffer
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
});
```

## Scaling with Backplanes

```csharp
// Redis backplane
builder.Services.AddSignalR()
    .AddStackExchangeRedis("localhost:6379");

// Azure SignalR Service
builder.Services.AddSignalR()
    .AddAzureSignalR(builder.Configuration["Azure:SignalR:ConnectionString"]);
```

## Dependencies

- .NET 10.0
- Synaxis.Abstractions
- Synaxis.Contracts
- Synaxis (core library)
- ASP.NET Core SignalR

## Documentation

Full documentation at [docs/packages/Synaxis.Adapters.SignalR.md](/docs/packages/Synaxis.Adapters.SignalR.md)
