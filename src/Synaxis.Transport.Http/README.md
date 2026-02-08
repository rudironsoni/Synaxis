# Synaxis.Transport.Http

REST API transport layer for exposing Synaxis capabilities over HTTP.

## When to Use

Use this package when you want to:
- Expose Synaxis as a REST API
- Build HTTP-based AI gateways
- Create OpenAI-compatible endpoints
- Support standard HTTP clients
- Deploy as a web service

## Installation

```bash
dotnet add package Synaxis.Transport.Http
```

## Quick Start

### Add HTTP Transport

```csharp
using Synaxis.Transport.Http.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add Synaxis core
builder.Services.AddSynaxis();

// Add HTTP transport
builder.Services.AddSynaxisTransportHttp(options =>
{
    options.BasePath = "/api/v1";
    options.EnableSwagger = true;
    options.EnableCors = true;
});

var app = builder.Build();

// Map Synaxis HTTP endpoints
app.MapSynaxisHttpEndpoints();

app.Run();
```

### Making HTTP Requests

```bash
# Chat completion
curl -X POST http://localhost:5000/api/v1/chat \
  -H "Content-Type: application/json" \
  -d '{
    "messages": [
      {"role": "user", "content": "Hello!"}
    ],
    "model": "gpt-4",
    "temperature": 0.7
  }'

# Streaming chat
curl -X POST http://localhost:5000/api/v1/chat/stream \
  -H "Content-Type: application/json" \
  -d '{
    "messages": [{"role": "user", "content": "Hello!"}],
    "model": "gpt-4"
  }'

# Embeddings
curl -X POST http://localhost:5000/api/v1/embeddings \
  -H "Content-Type: application/json" \
  -d '{
    "input": "Text to embed",
    "model": "text-embedding-3-small"
  }'
```

### Custom Middleware

```csharp
using Synaxis.Transport.Http;

public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;

    public RateLimitMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Rate limiting logic
        await _next(context);
    }
}

app.UseMiddleware<RateLimitMiddleware>();
```

## Features

- **REST Endpoints** - Standard HTTP POST/GET endpoints
- **Streaming Support** - Server-Sent Events (SSE) for streaming
- **OpenAPI/Swagger** - Automatic API documentation
- **CORS Support** - Cross-origin resource sharing
- **Authentication** - API key and bearer token support
- **Error Handling** - Standard HTTP status codes

## Configuration Options

```csharp
services.AddSynaxisTransportHttp(options =>
{
    options.BasePath = "/api/v1";           // Base path for endpoints
    options.EnableSwagger = true;           // Enable Swagger UI
    options.EnableCors = true;              // Enable CORS
    options.AllowedOrigins = new[] { "*" }; // CORS origins
    options.RequireAuthentication = false;  // API key requirement
});
```

## Available Endpoints

- `POST /chat` - Chat completion (non-streaming)
- `POST /chat/stream` - Chat completion (streaming)
- `POST /embeddings` - Generate embeddings
- `POST /images/generations` - Generate images
- `POST /audio/speech` - Text-to-speech
- `POST /audio/transcriptions` - Speech-to-text
- `POST /rerank` - Document reranking

## Dependencies

- .NET 10.0
- Synaxis.Abstractions
- Synaxis.Contracts
- Synaxis (core library)
- ASP.NET Core

## Documentation

Full documentation at [docs/packages/Synaxis.Transport.Http.md](/docs/packages/Synaxis.Transport.Http.md)
