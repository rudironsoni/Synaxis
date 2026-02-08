# Synaxis.Adapters.Agents

Integration adapter for Microsoft.Agents SDK (Bot Framework evolution) that enables Synaxis AI capabilities in conversational applications.

## Overview

This adapter allows you to integrate Synaxis with Bot Framework channels (Teams, Web Chat, Direct Line, etc.) by providing:

- **MediatorActivityHandler**: Routes messages from Bot Framework to Synaxis command executor
- **ChatTool**: Exposes chat functionality as a callable tool
- **RoutingTool**: Provides AI provider and model routing information  
- **ConversationStateManager**: Manages conversation history and state
- **Memory/Persistent Storage**: Support for in-memory or custom storage backends

## Installation

```bash
dotnet add package Synaxis.Adapters.Agents
```

## Usage

### Basic Setup

```csharp
using Synaxis.Adapters.Agents.DependencyInjection;

services.AddSynaxisAdapterAgents(options =>
{
    options.AppId = Configuration["MicrosoftAppId"];
    options.AppPassword = Configuration["MicrosoftAppPassword"];
    options.UseAuthentication = true;
    options.MaxHistoryMessages = 20;
    options.DefaultModel = "gpt-4";
});
```

### Custom Storage Provider

```csharp
services.AddSynaxisAdapterAgents(
    sp => new MyCustomConversationStorage(sp.GetRequiredService<IDatabase>()),
    options =>
    {
        options.MaxHistoryMessages = 50;
    });
```

### Using the Activity Handler

```csharp
public class MyController : ControllerBase
{
    private readonly MediatorActivityHandler _handler;

    public MyController(MediatorActivityHandler handler)
    {
        _handler = handler;
    }

    [HttpPost("/api/messages")]
    public async Task<IActionResult> PostAsync([FromBody] Activity activity)
    {
        var response = await _handler.HandleMessageAsync(
            conversationId: activity.Conversation.Id,
            userId: activity.From.Id,
            messageText: activity.Text,
            model: "gpt-4");

        return Ok(MessageFactory.Text(response));
    }
}
```

### Using Tools

```csharp
public class ChatService
{
    private readonly ChatTool _chatTool;
    private readonly RoutingTool _routingTool;

    public ChatService(ChatTool chatTool, RoutingTool routingTool)
    {
        _chatTool = chatTool;
        _routingTool = routingTool;
    }

    public async Task<string> GetCompletionAsync(string message)
    {
        return await _chatTool.GetChatCompletionAsync(
            message: message,
            model: "gpt-4",
            systemPrompt: "You are a helpful assistant.");
    }

    public async Task<string> GetProviderInfoAsync()
    {
        return await _routingTool.ListProvidersAsync();
    }
}
```

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| AppId | string | "" | Microsoft App ID for Bot Framework authentication |
| AppPassword | string | "" | Microsoft App Password for Bot Framework authentication |
| UseAuthentication | bool | true | Whether to use authentication (set false for local dev) |
| MaxHistoryMessages | int | 20 | Maximum number of messages to keep in conversation history |
| DefaultModel | string | "gpt-4" | Default AI model to use for chat completions |
| IncludeDetailedErrors | bool | false | Whether to include detailed errors in responses (use false in production) |

## Architecture

### Components

1. **MediatorActivityHandler**
   - Routes incoming messages to Synaxis command executor
   - Manages conversation history
   - Handles errors and logging

2. **ChatTool**
   - Provides simple chat completion interface
   - Supports conversation history
   - Can be used as a function calling tool

3. **RoutingTool**
   - Returns provider recommendations based on capability and model
   - Lists available providers and their capabilities

4. **ConversationStateManager**
   - Manages conversation state and message history
   - Automatically trims old messages based on MaxHistoryMessages
   - Supports custom storage backends via IConversationStorage

### Storage

#### Built-in: MemoryConversationStorage
- In-memory storage using ConcurrentDictionary
- Suitable for development and testing
- Data is lost when application restarts

#### Custom Storage
Implement `IConversationStorage` interface:

```csharp
public class RedisConversationStorage : IConversationStorage
{
    public Task<ConversationState?> GetAsync(string conversationId, CancellationToken cancellationToken)
    {
        // Retrieve from Redis
    }

    public Task SetAsync(string conversationId, ConversationState state, CancellationToken cancellationToken)
    {
        // Store in Redis
    }

    public Task DeleteAsync(string conversationId, CancellationToken cancellationToken)
    {
        // Delete from Redis
    }
}
```

## Integration with Bot Framework

This adapter is designed to work with:
- **Microsoft Teams**: Direct integration via Bot Framework
- **Web Chat**: Embed in web applications
- **Direct Line**: Custom channels and applications
- **Slack, Telegram, etc.**: Via Bot Framework channel adapters

## Requirements

- .NET 10.0 or later
- Synaxis.Abstractions
- Synaxis.Contracts
- Synaxis (core library)
- Microsoft.Agents.Core

## License

Copyright (c) Synaxis. All rights reserved.
