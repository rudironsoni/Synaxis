# Synaxis - Enterprise AI Gateway SDK

**SDK-First Architecture for Multi-Provider AI**

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)](https://github.com/rudironsoni/Synaxis)
[![NuGet](https://img.shields.io/badge/nuget-v0.1.0-blue)](https://www.nuget.org/packages/Synaxis)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

## What is Synaxis?

Synaxis is an SDK-first AI gateway that solves the fragmentation problem in modern AI applications. Unlike traditional API gateways that force HTTP dependencies, Synaxis provides a **composable SDK** you can embed directly into your applications, with support for multiple transports (HTTP, gRPC, WebSocket, SignalR) and AI providers (OpenAI, Azure, Anthropic, and more). Whether you need an embedded library, a self-hosted gateway, or a SaaS solution, Synaxis adapts to your architecture—not the other way around.

## Key Features

✨ **SDK-First Architecture** - Embed AI capabilities directly in your applications without HTTP overhead, or deploy as a standalone gateway when needed

🚀 **Multi-Transport Support** - Choose HTTP REST, gRPC, WebSocket, or SignalR based on your requirements—all sharing the same core logic

🔌 **Provider Agnostic** - Unified interface for OpenAI, Azure OpenAI, Anthropic, and other providers with seamless switching and failover

🏗️ **Clean Abstractions** - Mediator-based CQRS architecture ensures maintainable, testable code with clear separation of concerns

📦 **Zero Dependencies Foundation** - Core abstractions have no external dependencies, making it lightweight and easy to extend

## Quick Start

### SDK Usage (Embedded in Your App)

```csharp
using Synaxis;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add Synaxis SDK directly to your application
builder.Services.AddSynaxis(options =>
{
    options.AddOpenAIProvider(config =>
    {
        config.ApiKey = builder.Configuration["OpenAI:ApiKey"];
        config.DefaultModel = "gpt-4";
    });
});

var app = builder.Build();

app.MapPost("/chat", async (IMediator mediator, ChatRequest request) =>
{
    var command = new SendChatCommand(request.Messages, request.Model);
    var response = await mediator.Send(command);
    return Results.Ok(response);
});

app.Run();
```

### Self-Hosted Gateway (Docker)

```bash
# Run Synaxis as a standalone gateway
docker run -d \
  -p 8080:8080 \
  -e OpenAI__ApiKey=your-key-here \
  -e Azure__Endpoint=your-endpoint \
  synaxis/gateway:latest

# Call the gateway from any language
curl -X POST http://localhost:8080/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "gpt-4",
    "messages": [{"role": "user", "content": "Hello!"}]
  }'
```

### SaaS Client (Using Synaxis.Client)

```csharp
using Synaxis.Client;

// Connect to hosted Synaxis instance
var client = new SynaxisClient("https://api.synaxis.io", apiKey: "your-api-key");

var response = await client.Chat.SendAsync(new ChatRequest
{
    Model = "gpt-4",
    Messages = new[]
    {
        new Message { Role = "user", Content = "Explain quantum computing" }
    }
});

Console.WriteLine(response.Content);
```

## Installation

Install the core SDK and your preferred transport:

```bash
# Core SDK (required)
dotnet add package Synaxis

# Choose your transport
dotnet add package Synaxis.Transport.Http
dotnet add package Synaxis.Transport.Grpc
dotnet add package Synaxis.Transport.WebSocket
dotnet add package Synaxis.Transport.SignalR

# Add AI providers
dotnet add package Synaxis.Providers.OpenAI
dotnet add package Synaxis.Providers.Azure
dotnet add package Synaxis.Providers.Anthropic
```

## Architecture Overview

Synaxis is built on a **4-tier architecture** that ensures clean separation of concerns:

1. **Core Layer** (`Synaxis.Core`) - Domain models, abstractions, and business logic with zero external dependencies
2. **Application Layer** (`Synaxis.Application`) - CQRS commands/queries using MediatR, orchestrating core functionality
3. **Infrastructure Layer** (`Synaxis.Providers.*`) - Provider-specific implementations (OpenAI, Azure, Anthropic, etc.)
4. **Presentation Layer** (`Synaxis.Transport.*`) - Transport-specific implementations (HTTP, gRPC, WebSocket, SignalR)

This architecture allows you to:
- Use only the layers you need (embed core + one provider, or deploy full gateway)
- Swap transports without changing business logic
- Add new providers without touching existing code
- Test each layer independently

For detailed architecture documentation, see [docs/architecture/](docs/architecture/).

## Project Structure

```
Synaxis/
├── src/
│   ├── Synaxis.Core/              # Domain models & abstractions
│   ├── Synaxis.Application/       # CQRS handlers & orchestration
│   ├── Synaxis.Providers.OpenAI/  # OpenAI provider implementation
│   ├── Synaxis.Providers.Azure/   # Azure OpenAI provider
│   ├── Synaxis.Transport.Http/    # HTTP REST API
│   ├── Synaxis.Transport.Grpc/    # gRPC implementation
│   └── Synaxis.Gateway/           # Standalone gateway host
├── tests/
│   ├── Synaxis.Core.Tests/
│   └── Synaxis.Integration.Tests/
└── docs/
    ├── architecture/
    ├── providers/
    └── deployment/
```

## Contributing

We welcome contributions! Please read our [Contributing Guide](CONTRIBUTING.md) for details on:
- Code of conduct
- Development setup
- Coding standards
- Pull request process
- Issue reporting

## Roadmap

- [x] Core SDK architecture
- [x] OpenAI provider
- [ ] Azure OpenAI provider
- [ ] Anthropic provider
- [ ] HTTP transport
- [ ] gRPC transport
- [ ] WebSocket transport
- [ ] Docker gateway deployment
- [ ] Kubernetes Helm charts
- [ ] Rate limiting & quotas
- [ ] Request caching
- [ ] Observability (OpenTelemetry)

## License

Synaxis is licensed under the [MIT License](LICENSE). See LICENSE file for details.

---

**Built with ❤️ for developers who need flexible AI integration**

For questions, issues, or feature requests, please visit our [GitHub repository](https://github.com/rudironsoni/Synaxis).
