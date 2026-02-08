# Synaxis

Core implementation of the Synaxis AI orchestration framework with Mediator pattern support.

## When to Use

Use this package when you want to:
- Build Synaxis-enabled applications
- Orchestrate multiple AI providers
- Implement command/query patterns for AI operations
- Add middleware pipelines (logging, validation, metrics)
- Route requests across providers with strategies

## Installation

```bash
dotnet add package Synaxis
```

## Quick Start

### Basic Setup

```csharp
using Synaxis.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add Synaxis with default configuration
builder.Services.AddSynaxis(options =>
{
    options.DefaultRoutingStrategy = "RoundRobin";
    options.EnablePipelineBehaviors = true;
    options.EnableValidation = true;
    options.EnableMetrics = true;
});

// Add providers (e.g., OpenAI, Anthropic)
builder.Services.AddSynaxisProviderOpenAI(config =>
{
    config.ApiKey = builder.Configuration["OpenAI:ApiKey"];
});

var app = builder.Build();
```

### Execute Chat Command

```csharp
using Synaxis.Abstractions.Execution;
using Synaxis.Commands.Chat;
using Synaxis.Contracts.V1.Messages;

public class ChatService
{
    private readonly ICommandExecutor<ChatCommand, ChatResponse> _executor;

    public ChatService(ICommandExecutor<ChatCommand, ChatResponse> executor)
    {
        _executor = executor;
    }

    public async Task<string> GetCompletionAsync(string userMessage)
    {
        var command = new ChatCommand
        {
            Messages = new[]
            {
                new ChatMessage { Role = "user", Content = userMessage }
            },
            Model = "gpt-4",
            Temperature = 0.7
        };

        var response = await _executor.ExecuteAsync(command, CancellationToken.None);
        return response.Content;
    }
}
```

### Streaming Responses

```csharp
using Synaxis.Abstractions.Execution;
using Synaxis.Commands.Chat;

public async Task StreamChatAsync(string userMessage)
{
    var streamExecutor = serviceProvider.GetRequiredService<IStreamExecutor<ChatStreamCommand, ChatStreamChunk>>();
    
    var command = new ChatStreamCommand
    {
        Messages = new[] { new ChatMessage { Role = "user", Content = userMessage } },
        Model = "gpt-4"
    };

    await foreach (var chunk in streamExecutor.ExecuteAsync(command, CancellationToken.None))
    {
        Console.Write(chunk.Content);
    }
}
```

## Features

### Pipeline Behaviors

Synaxis includes built-in middleware behaviors:

- **LoggingBehavior** - Logs all requests and responses
- **ValidationBehavior** - Validates commands before execution
- **MetricsBehavior** - Collects performance metrics
- **AuthorizationBehavior** - Handles authentication/authorization

### Routing Strategies

Multiple routing strategies are available:

- **RoundRobinRoutingStrategy** - Distributes load evenly
- **LeastLoadedRoutingStrategy** - Routes to least busy provider
- **PriorityRoutingStrategy** - Uses provider priority ordering

### Configuration Options

```csharp
services.AddSynaxis(options =>
{
    options.DefaultRoutingStrategy = "RoundRobin"; // or "LeastLoaded", "Priority"
    options.EnablePipelineBehaviors = true;        // Enable middleware pipeline
    options.EnableValidation = true;               // Validate commands
    options.EnableMetrics = true;                  // Collect metrics
});
```

## Dependencies

- .NET 10.0
- Synaxis.Abstractions
- Synaxis.Contracts
- Mediator (for CQRS pattern)
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Logging

## Documentation

Full documentation at [docs/packages/Synaxis.md](/docs/packages/Synaxis.md)
