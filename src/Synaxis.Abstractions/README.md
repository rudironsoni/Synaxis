# Synaxis.Abstractions

Core abstractions and interfaces for building custom Synaxis implementations and extensions.

## When to Use

Use this package when you need to:
- Build custom AI provider implementations
- Create custom routing strategies
- Implement custom command executors
- Extend Synaxis with new capabilities
- Build framework-agnostic integrations

## Installation

```bash
dotnet add package Synaxis.Abstractions
```

## Quick Start

### Custom Provider Implementation

```csharp
using Synaxis.Abstractions.Providers;
using Synaxis.Contracts.V1.Messages;

public class MyCustomChatProvider : IChatProvider
{
    public string Name => "my-custom-provider";
    
    public async Task<ChatResponse> ChatAsync(
        IEnumerable<ChatMessage> messages,
        string model,
        CancellationToken cancellationToken = default)
    {
        // Your implementation
        return new ChatResponse
        {
            Content = "Response from custom provider",
            Model = model
        };
    }
}

// Register in DI
services.AddScoped<IChatProvider, MyCustomChatProvider>();
```

### Custom Routing Strategy

```csharp
using Synaxis.Abstractions.Routing;

public class CustomRoutingStrategy : IRoutingStrategy
{
    public string Name => "CustomRouting";
    
    public Task<string> SelectProviderAsync(
        IEnumerable<string> availableProviders,
        CancellationToken cancellationToken = default)
    {
        // Your selection logic
        return Task.FromResult(availableProviders.First());
    }
}
```

## Key Interfaces

- **IChatProvider** - Chat completion providers
- **IEmbeddingProvider** - Text embedding providers
- **IImageProvider** - Image generation providers
- **IAudioProvider** - Audio synthesis/transcription
- **IRerankProvider** - Document reranking
- **IRoutingStrategy** - Provider selection strategies
- **ICommandExecutor** - Command execution pipeline
- **IStreamExecutor** - Streaming command execution

## Dependencies

- .NET Standard 2.1 (compatible with .NET Core 3.0+, .NET 5+)
- No external dependencies

## Documentation

Full documentation at [docs/packages/Synaxis.Abstractions.md](/docs/packages/Synaxis.Abstractions.md)
