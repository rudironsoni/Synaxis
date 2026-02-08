# Synaxis.Contracts

Versioned data contracts and DTOs for Synaxis API interactions.

## When to Use

Use this package when you need to:
- Consume Synaxis types in client applications
- Build type-safe integrations with Synaxis
- Share data models between services
- Serialize/deserialize Synaxis messages
- Build custom transports or adapters

## Installation

```bash
dotnet add package Synaxis.Contracts
```

## Quick Start

### Using ChatMessage

```csharp
using Synaxis.Contracts.V1.Messages;

var messages = new List<ChatMessage>
{
    new ChatMessage 
    { 
        Role = "system", 
        Content = "You are a helpful assistant." 
    },
    new ChatMessage 
    { 
        Role = "user", 
        Content = "What is the capital of France?" 
    }
};

var response = new ChatResponse
{
    Content = "The capital of France is Paris.",
    Model = "gpt-4",
    Usage = new ChatUsage
    {
        PromptTokens = 15,
        CompletionTokens = 8,
        TotalTokens = 23
    }
};
```

### Working with Commands

```csharp
using Synaxis.Contracts.V1.Commands;

// Commands are defined as interfaces for flexibility
public class MyChatCommand : IChatCommand
{
    public IEnumerable<ChatMessage> Messages { get; init; } = Array.Empty<ChatMessage>();
    public string Model { get; init; } = "gpt-4";
    public string? Provider { get; init; }
    public double Temperature { get; init; } = 0.7;
}
```

### Handling Errors

```csharp
using Synaxis.Contracts.V1.Errors;

try
{
    // Execute command
}
catch (SynaxisException ex)
{
    Console.WriteLine($"Error {ex.ErrorCode}: {ex.Message}");
    // ErrorCodes: ProviderNotFound, ModelNotSupported, InvalidRequest, etc.
}
```

## Key Types

**Messages:**
- `ChatMessage` - Chat message with role and content
- `ChatResponse` - Chat completion response
- `EmbeddingData` - Embedding vectors
- `ImageData` - Generated image data

**Commands:**
- `IChatCommand` - Chat completion request
- `IEmbeddingCommand` - Embedding generation request
- `IImageGenerationCommand` - Image generation request
- `IAudioSynthesisCommand` - Text-to-speech request
- `IAudioTranscriptionCommand` - Speech-to-text request

**Errors:**
- `SynaxisError` - Standard error response
- `ErrorCodes` - Well-known error codes

## Versioning

Contracts use semantic versioning in the namespace (V1, V2, etc.) to maintain backward compatibility.

## Dependencies

- .NET Standard 2.1
- Synaxis.Abstractions
- PolySharp (for C# feature polyfills)

## Documentation

Full documentation at [docs/packages/Synaxis.Contracts.md](/docs/packages/Synaxis.Contracts.md)
