# Synaxis.Transport.WebSocket

WebSocket transport layer for real-time, bidirectional AI communication.

## When to Use

Use this package when you need:
- Real-time streaming responses
- Bidirectional communication
- Long-lived connections
- Low overhead for frequent messages
- Browser-based AI interactions
- Interactive chat applications

## Installation

```bash
dotnet add package Synaxis.Transport.WebSocket
```

## Quick Start

### Server Setup

```csharp
using Synaxis.Transport.WebSocket.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add Synaxis core
builder.Services.AddSynaxis();

// Add WebSocket transport
builder.Services.AddSynaxisTransportWebSocket(options =>
{
    options.Path = "/ws";
    options.KeepAliveInterval = TimeSpan.FromSeconds(30);
    options.ReceiveBufferSize = 4096;
});

var app = builder.Build();

// Enable WebSockets
app.UseWebSockets();

// Map WebSocket endpoint
app.MapSynaxisWebSocket();

app.Run();
```

### Client Usage (JavaScript)

```javascript
const ws = new WebSocket('ws://localhost:5000/ws');

ws.onopen = () => {
    // Send chat request
    ws.send(JSON.stringify({
        type: 'chat',
        messages: [
            { role: 'user', content: 'Hello!' }
        ],
        model: 'gpt-4',
        stream: true
    }));
};

ws.onmessage = (event) => {
    const data = JSON.parse(event.data);
    
    if (data.type === 'chunk') {
        // Handle streaming chunk
        console.log(data.content);
    } else if (data.type === 'complete') {
        // Stream completed
        console.log('Done!');
    }
};
```

### Client Usage (C#)

```csharp
using System.Net.WebSockets;

var ws = new ClientWebSocket();
await ws.ConnectAsync(new Uri("ws://localhost:5000/ws"), CancellationToken.None);

var request = new
{
    type = "chat",
    messages = new[]
    {
        new { role = "user", content = "Hello!" }
    },
    model = "gpt-4",
    stream = true
};

var json = JsonSerializer.Serialize(request);
var bytes = Encoding.UTF8.GetBytes(json);
await ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);

var buffer = new byte[4096];
while (ws.State == WebSocketState.Open)
{
    var result = await ws.ReceiveAsync(buffer, CancellationToken.None);
    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
    Console.WriteLine(message);
}
```

## Features

- **Full-Duplex Communication** - Send and receive simultaneously
- **Streaming Support** - Real-time token streaming
- **Connection Persistence** - Long-lived connections
- **Automatic Reconnection** - Built-in retry logic
- **Message Framing** - Efficient binary/text frames
- **Ping/Pong** - Keep-alive mechanism

## Message Format

```json
// Request
{
    "type": "chat",
    "id": "unique-request-id",
    "messages": [
        { "role": "user", "content": "Hello!" }
    ],
    "model": "gpt-4",
    "stream": true
}

// Response (streaming)
{
    "type": "chunk",
    "id": "unique-request-id",
    "content": "Hello",
    "index": 0
}

// Complete
{
    "type": "complete",
    "id": "unique-request-id"
}

// Error
{
    "type": "error",
    "id": "unique-request-id",
    "error": "Error message"
}
```

## Configuration Options

```csharp
services.AddSynaxisTransportWebSocket(options =>
{
    options.Path = "/ws";                                    // WebSocket path
    options.KeepAliveInterval = TimeSpan.FromSeconds(30);   // Ping interval
    options.ReceiveBufferSize = 4096;                       // Buffer size
    options.AllowedOrigins = new[] { "*" };                 // CORS origins
});
```

## Supported Operations

- **chat** - Chat completions (streaming/non-streaming)
- **embeddings** - Text embeddings
- **images** - Image generation
- **audio** - Speech synthesis/transcription

## Dependencies

- .NET 10.0
- Synaxis.Abstractions
- Synaxis.Contracts
- Synaxis (core library)
- ASP.NET Core WebSockets

## Documentation

Full documentation at [docs/packages/Synaxis.Transport.WebSocket.md](/docs/packages/Synaxis.Transport.WebSocket.md)
