# Quickstart: Synaxis SDK

This guide helps you integrate the Synaxis SDK into your .NET application in about 15 minutes.

## Prerequisites

- **.NET 10 SDK** or later ([Download](https://dotnet.microsoft.com/download))
- **IDE**: Visual Studio 2022, VS Code, or Rider
- **API Key**: OpenAI, Anthropic, or another supported provider

## Installation

Add the Synaxis SDK packages to your project:

```bash
dotnet add package Synaxis
dotnet add package Synaxis.Providers.OpenAI
```

For other providers:

```bash
# Anthropic
dotnet add package Synaxis.Providers.Anthropic

# Azure OpenAI
dotnet add package Synaxis.Providers.Azure
```

## Configuration

Configure Synaxis in your `Program.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Synaxis;
using Synaxis.Providers.OpenAI;

var builder = WebApplication.CreateBuilder(args);

// Add Synaxis with OpenAI provider
builder.Services.AddSynaxis(options =>
{
    options.AddOpenAIProvider(builder.Configuration["OpenAI:ApiKey"]!);
});

builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();
```

**Configuration via appsettings.json:**

```json
{
  "OpenAI": {
    "ApiKey": "your-api-key-here"
  }
}
```

> **Security Note**: Never commit API keys to source control. Use environment variables or Azure Key Vault in production.

## First Chat Completion

Create a simple controller to execute chat completions:

```csharp
using Microsoft.AspNetCore.Mvc;
using Synaxis.Abstractions;
using Synaxis.Contracts;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace YourApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatExecutor _executor;

    public ChatController(IChatExecutor executor)
    {
        _executor = executor;
    }

    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        try
        {
            var messages = new List<ChatMessage>
            {
                new ChatMessage { Role = "user", Content = request.Message }
            };

            var command = new ChatCommand
            {
                Model = "gpt-4",
                Messages = messages
            };

            var response = await _executor.ExecuteAsync(command);

            return Ok(new
            {
                message = response.Choices[0].Message.Content,
                usage = response.Usage
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public record ChatRequest(string Message);
```

**Test with curl:**

```bash
curl -X POST http://localhost:5000/api/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "Hello, how are you?"}'
```

**Expected output:**

```json
{
  "message": "I'm doing well, thank you! How can I help you today?",
  "usage": {
    "promptTokens": 12,
    "completionTokens": 15,
    "totalTokens": 27
  }
}
```

## Adding Streaming

Enable real-time streaming responses:

```csharp
[HttpPost("stream")]
public async Task StreamChat([FromBody] ChatRequest request)
{
    Response.ContentType = "text/event-stream";

    var messages = new List<ChatMessage>
    {
        new ChatMessage { Role = "user", Content = request.Message }
    };

    var command = new ChatCommand
    {
        Model = "gpt-4",
        Messages = messages,
        Stream = true
    };

    await foreach (var chunk in _executor.ExecuteStreamAsync(command))
    {
        var delta = chunk.Choices[0].Delta?.Content;
        if (!string.IsNullOrEmpty(delta))
        {
            await Response.WriteAsync($"data: {delta}\n\n");
            await Response.Body.FlushAsync();
        }
    }

    await Response.WriteAsync("data: [DONE]\n\n");
}
```

**Test streaming:**

```bash
curl -N http://localhost:5000/api/chat/stream \
  -H "Content-Type: application/json" \
  -d '{"message": "Write a short poem"}'
```

## Adding Multiple Providers

Configure multiple AI providers with automatic fallback:

```csharp
builder.Services.AddSynaxis(options =>
{
    // Primary provider
    options.AddOpenAIProvider(builder.Configuration["OpenAI:ApiKey"]!);

    // Fallback providers
    options.AddAnthropicProvider(builder.Configuration["Anthropic:ApiKey"]!);
    options.AddAzureProvider(
        endpoint: builder.Configuration["Azure:Endpoint"]!,
        apiKey: builder.Configuration["Azure:ApiKey"]!
    );

    // Configure routing strategy
    options.RoutingStrategy = RoutingStrategy.CostOptimized;
    options.EnableAutomaticFallback = true;
});
```

**appsettings.json:**

```json
{
  "OpenAI": {
    "ApiKey": "sk-..."
  },
  "Anthropic": {
    "ApiKey": "sk-ant-..."
  },
  "Azure": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "..."
  }
}
```

**Using specific providers:**

```csharp
var command = new ChatCommand
{
    Model = "claude-3-opus",  // Will route to Anthropic
    Messages = messages
};

var response = await _executor.ExecuteAsync(command);
```

## Next Steps

- **[Configuration Guide](../configuration.md)**: Advanced settings and options
- **[Providers](../providers/overview.md)**: Detailed provider configurations
- **[Error Handling](../error-handling.md)**: Robust error management
- **[Rate Limiting](../rate-limiting.md)**: Manage quotas and limits
- **[Monitoring](../monitoring.md)**: Telemetry and observability

## Troubleshooting

### Provider Authentication Failed

**Error:** `401 Unauthorized` or `API key not valid`

**Solution:**
1. Verify your API key is correct
2. Check that the key has proper permissions
3. Ensure the key isn't expired
4. Confirm configuration is loaded: `builder.Configuration["OpenAI:ApiKey"]`

### Model Not Found

**Error:** `Model 'gpt-4' not found`

**Solution:**
1. Verify the model name is correct
2. Check your API key has access to this model
3. Use `gpt-3.5-turbo` if GPT-4 access is not available

### Dependency Injection Error

**Error:** `Unable to resolve service for type 'IChatExecutor'`

**Solution:**
Ensure `AddSynaxis()` is called in `Program.cs` before `Build()`:

```csharp
builder.Services.AddSynaxis(options => {
    options.AddOpenAIProvider(apiKey);
});

var app = builder.Build(); // Must come after AddSynaxis
```

### Rate Limit Exceeded

**Error:** `429 Too Many Requests`

**Solution:**
Configure rate limiting and retries:

```csharp
builder.Services.AddSynaxis(options =>
{
    options.AddOpenAIProvider(apiKey);
    options.MaxRetries = 3;
    options.RetryDelay = TimeSpan.FromSeconds(2);
    options.EnableExponentialBackoff = true;
});
```

### Streaming Not Working

**Issue:** No data received when streaming

**Solution:**
1. Ensure `Response.ContentType = "text/event-stream"`
2. Call `Response.Body.FlushAsync()` after each write
3. Verify `Stream = true` in ChatCommand
4. Check that client supports SSE (Server-Sent Events)

## Sample Project

Clone a complete working example:

```bash
git clone https://github.com/synaxis/samples
cd samples/quickstart-sdk
dotnet restore
dotnet run
```

## Support

- **Documentation**: [https://docs.synaxis.io](https://docs.synaxis.io)
- **GitHub Issues**: [https://github.com/synaxis/synaxis/issues](https://github.com/synaxis/synaxis/issues)
- **Community**: [Discord](https://discord.gg/synaxis)
