# Synaxis.Providers.Anthropic

This package implements provider interfaces for Anthropic's Claude API.

## Installation

```bash
dotnet add reference Synaxis.Providers.Anthropic
```

## Usage

### Basic Setup

```csharp
using Microsoft.Extensions.DependencyInjection;
using Synaxis.Providers.Anthropic.DependencyInjection;

// Simple registration with API key
services.AddAnthropicChatProvider("your-api-key-here");

// Or with configuration
services.AddAnthropicChatProvider(options =>
{
    options.ApiKey = "your-api-key-here";
    options.AnthropicVersion = "2023-06-01"; // Optional
    options.BaseUrl = "https://api.anthropic.com/v1/"; // Optional
});
```

### Using the Provider

```csharp
using Synaxis.Abstractions.Providers;
using Synaxis.Contracts.V1.Messages;

public class MyService
{
    private readonly IChatProvider _chatProvider;

    public MyService(IChatProvider chatProvider)
    {
        _chatProvider = chatProvider;
    }

    public async Task<ChatResponse> GetCompletionAsync()
    {
        var messages = new[]
        {
            new ChatMessage
            {
                Role = "user",
                Content = "Hello, Claude!"
            }
        };

        var response = await _chatProvider.ChatAsync(
            messages,
            model: "claude-3-opus-20240229",
            options: new { MaxTokens = 1024, Temperature = 0.7 },
            cancellationToken: default
        );

        return (ChatResponse)response;
    }
}
```

## Key Features

- ✅ Chat completions with Claude models
- ✅ Streaming support
- ✅ System message handling (extracted from messages array)
- ✅ Anthropic-specific message formatting
- ✅ Comprehensive error mapping to SynaxisError
- ✅ Rate limit and authentication error handling

## API Differences from OpenAI

1. **System Messages**: Anthropic uses a separate `system` parameter instead of including it in the messages array
2. **Required `max_tokens`**: Anthropic requires specifying max_tokens (defaults to 1024)
3. **Message Format**: Different response structure with `content` array containing text blocks
4. **Authentication**: Uses `x-api-key` header instead of Bearer token

## Configuration Options

| Option | Description | Default |
|--------|-------------|---------|
| `ApiKey` | Your Anthropic API key | Required |
| `BaseUrl` | API base URL | `https://api.anthropic.com/v1/` |
| `AnthropicVersion` | API version header | `null` (optional) |

## Error Handling

The provider maps Anthropic error codes to `SynaxisError` categories:

- 401/403 → `AUTH_INVALID` / `AUTH_FORBIDDEN`
- 429 → `RATE_LIMIT_EXCEEDED`
- 400 → `VALIDATION_ERROR`
- Other → `PROVIDER_ERROR`
