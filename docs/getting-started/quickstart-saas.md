# Quickstart: Synaxis Cloud (SaaS)

This guide helps you start using Synaxis Cloud API in about 15 minutes.

## Prerequisites

- **Synaxis Cloud Account** ([Sign up](https://cloud.synaxis.io/signup))
- **.NET 10 SDK** or later ([Download](https://dotnet.microsoft.com/download))
- **IDE**: Visual Studio 2022, VS Code, or Rider

## Get API Key

1. Log in to [Synaxis Cloud Dashboard](https://cloud.synaxis.io)
2. Navigate to **API Keys** → **Create New Key**
3. Name your key (e.g., "Development")
4. Select permissions: `read` and `write`
5. Copy the key immediately (it won't be shown again)

**Security Note**: Store your API key securely. Never commit it to source control.

## Installation

### Option 1: Using Synaxis.Client SDK

Add the official Synaxis Cloud client:

```bash
dotnet add package Synaxis.Client
```

### Option 2: Using HttpClient

No additional packages needed - use the standard .NET `HttpClient`.

## First Request (SDK)

Create a console application:

```csharp
using Synaxis.Client;
using System;
using System.Threading.Tasks;

namespace SynaxisQuickstart;

class Program
{
    static async Task Main(string[] args)
    {
        // Initialize client
        var client = new SynaxisClient("your-api-key-here");

        // Create chat completion
        var request = new ChatCompletionRequest
        {
            Model = "gpt-4",
            Messages = new[]
            {
                new Message
                {
                    Role = "user",
                    Content = "Hello, how are you?"
                }
            }
        };

        try
        {
            var response = await client.ChatCompletions.CreateAsync(request);
            
            Console.WriteLine($"Response: {response.Choices[0].Message.Content}");
            Console.WriteLine($"Tokens: {response.Usage.TotalTokens}");
        }
        catch (SynaxisException ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Status: {ex.StatusCode}");
        }
    }
}
```

**Run the application:**

```bash
dotnet run
```

**Expected output:**

```
Response: I'm doing well, thank you! How can I help you today?
Tokens: 27
```

## First Request (HttpClient)

For environments without SDK support:

```csharp
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace SynaxisQuickstart;

class Program
{
    static async Task Main(string[] args)
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.synaxis.io")
        };
        
        httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer your-api-key-here");

        var request = new
        {
            model = "gpt-4",
            messages = new[]
            {
                new { role = "user", content = "Hello, how are you?" }
            }
        };

        try
        {
            var response = await httpClient.PostAsJsonAsync("/v1/chat/completions", request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>();
            
            Console.WriteLine($"Response: {result.Choices[0].Message.Content}");
            Console.WriteLine($"Tokens: {result.Usage.TotalTokens}");
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}

record ChatCompletionResponse(
    string Id,
    string Object,
    long Created,
    string Model,
    Choice[] Choices,
    Usage Usage
);

record Choice(int Index, Message Message, string FinishReason);
record Message(string Role, string Content);
record Usage(int PromptTokens, int CompletionTokens, int TotalTokens);
```

**Run:**

```bash
dotnet run
```

## Handle Errors

Robust error handling:

```csharp
using Synaxis.Client;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        var client = new SynaxisClient(
            apiKey: Environment.GetEnvironmentVariable("SYNAXIS_API_KEY") 
                ?? throw new InvalidOperationException("SYNAXIS_API_KEY not set")
        );

        var request = new ChatCompletionRequest
        {
            Model = "gpt-4",
            Messages = new[]
            {
                new Message { Role = "user", Content = "Hello!" }
            },
            MaxTokens = 100,
            Temperature = 0.7
        };

        try
        {
            var response = await client.ChatCompletions.CreateAsync(request);
            Console.WriteLine(response.Choices[0].Message.Content);
        }
        catch (SynaxisAuthenticationException ex)
        {
            Console.WriteLine($"Authentication failed: {ex.Message}");
            Console.WriteLine("Check your API key at https://cloud.synaxis.io/keys");
        }
        catch (SynaxisRateLimitException ex)
        {
            Console.WriteLine($"Rate limit exceeded: {ex.Message}");
            Console.WriteLine($"Retry after: {ex.RetryAfter} seconds");
            Console.WriteLine("Consider upgrading your plan");
        }
        catch (SynaxisQuotaException ex)
        {
            Console.WriteLine($"Quota exceeded: {ex.Message}");
            Console.WriteLine($"Usage: {ex.CurrentUsage}/{ex.QuotaLimit}");
            Console.WriteLine("Visit https://cloud.synaxis.io/billing to add credits");
        }
        catch (SynaxisValidationException ex)
        {
            Console.WriteLine($"Invalid request: {ex.Message}");
            foreach (var error in ex.Errors)
            {
                Console.WriteLine($"  - {error.Field}: {error.Message}");
            }
        }
        catch (SynaxisException ex)
        {
            Console.WriteLine($"API error: {ex.Message}");
            Console.WriteLine($"Status: {ex.StatusCode}");
            Console.WriteLine($"Request ID: {ex.RequestId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
        }
    }
}
```

## Streaming Responses

Real-time streaming:

```csharp
using Synaxis.Client;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        var client = new SynaxisClient("your-api-key-here");

        var request = new ChatCompletionRequest
        {
            Model = "gpt-4",
            Messages = new[]
            {
                new Message
                {
                    Role = "user",
                    Content = "Write a short poem about technology"
                }
            },
            Stream = true
        };

        Console.Write("Response: ");

        try
        {
            await foreach (var chunk in client.ChatCompletions.CreateStreamAsync(request))
            {
                var delta = chunk.Choices[0].Delta?.Content;
                if (!string.IsNullOrEmpty(delta))
                {
                    Console.Write(delta);
                }
            }
            
            Console.WriteLine();
        }
        catch (SynaxisException ex)
        {
            Console.WriteLine($"\nError: {ex.Message}");
        }
    }
}
```

## Advanced Configuration

### Retry Policy

```csharp
var client = new SynaxisClient("your-api-key-here", options =>
{
    options.MaxRetries = 3;
    options.RetryDelay = TimeSpan.FromSeconds(1);
    options.Timeout = TimeSpan.FromSeconds(30);
});
```

### Custom Base URL

For enterprise or regional endpoints:

```csharp
var client = new SynaxisClient("your-api-key-here", options =>
{
    options.BaseUrl = "https://eu-west-1.synaxis.io";
});
```

### Request Tracking

```csharp
var request = new ChatCompletionRequest
{
    Model = "gpt-4",
    Messages = messages,
    Metadata = new Dictionary<string, string>
    {
        { "user_id", "user-123" },
        { "session_id", "session-456" },
        { "environment", "production" }
    }
};

var response = await client.ChatCompletions.CreateAsync(request);

// Track usage in dashboard
Console.WriteLine($"Request ID: {response.Id}");
```

## Using Multiple Models

```csharp
var client = new SynaxisClient("your-api-key-here");

// Use GPT-4 for complex reasoning
var complexRequest = new ChatCompletionRequest
{
    Model = "gpt-4",
    Messages = new[] { new Message { Role = "user", Content = "Explain quantum computing" } }
};

// Use GPT-3.5-turbo for simple tasks
var simpleRequest = new ChatCompletionRequest
{
    Model = "gpt-3.5-turbo",
    Messages = new[] { new Message { Role = "user", Content = "Hello!" } }
};

// Use Claude for long-context tasks
var longContextRequest = new ChatCompletionRequest
{
    Model = "claude-3-opus",
    Messages = new[] { new Message { Role = "user", Content = "Analyze this document..." } }
};

var responses = await Task.WhenAll(
    client.ChatCompletions.CreateAsync(complexRequest),
    client.ChatCompletions.CreateAsync(simpleRequest),
    client.ChatCompletions.CreateAsync(longContextRequest)
);

foreach (var response in responses)
{
    Console.WriteLine($"Model: {response.Model}");
    Console.WriteLine($"Cost: ${response.Usage.TotalTokens * 0.0001:F4}");
}
```

## Next Steps

- **[API Reference](../api-reference.md)**: Complete API documentation
- **[Authentication](../authentication.md)**: API keys and OAuth
- **[Models](../models.md)**: Available models and pricing
- **[Rate Limits](../rate-limits.md)**: Understanding quotas and limits
- **[Billing](../billing.md)**: Usage tracking and billing
- **[Examples](../examples/)**: Sample applications and use cases

## Troubleshooting

### Authentication Failed

**Error:** `401 Unauthorized`

**Solution:**
1. Verify API key: https://cloud.synaxis.io/keys
2. Check key permissions (needs `read` and `write`)
3. Ensure key hasn't expired
4. Confirm proper header: `Authorization: Bearer your-key`

**Test authentication:**

```bash
curl https://api.synaxis.io/v1/models \
  -H "Authorization: Bearer your-api-key-here"
```

### Rate Limit Exceeded

**Error:** `429 Too Many Requests`

**Solution:**
1. Check your rate limits: https://cloud.synaxis.io/limits
2. Implement exponential backoff
3. Upgrade plan if needed: https://cloud.synaxis.io/billing

**Implement retry logic:**

```csharp
int maxRetries = 3;
int retryDelay = 1000; // milliseconds

for (int i = 0; i < maxRetries; i++)
{
    try
    {
        var response = await client.ChatCompletions.CreateAsync(request);
        return response;
    }
    catch (SynaxisRateLimitException ex)
    {
        if (i == maxRetries - 1) throw;
        
        await Task.Delay(retryDelay * (int)Math.Pow(2, i));
    }
}
```

### Quota Exceeded

**Error:** `402 Payment Required - Quota exceeded`

**Solution:**
1. Check usage: https://cloud.synaxis.io/usage
2. Add credits: https://cloud.synaxis.io/billing
3. Optimize token usage (shorter prompts, lower max_tokens)

### Invalid Model

**Error:** `400 Bad Request - Model not found`

**Solution:**
1. List available models:

```csharp
var models = await client.Models.ListAsync();
foreach (var model in models)
{
    Console.WriteLine($"{model.Id} - {model.Description}");
}
```

2. Verify spelling and availability
3. Check plan includes this model

### Timeout

**Error:** `Request timeout after 30 seconds`

**Solution:**
Increase timeout for long-running requests:

```csharp
var client = new SynaxisClient("your-api-key-here", options =>
{
    options.Timeout = TimeSpan.FromMinutes(2);
});
```

### Network Errors

**Issue:** `Connection refused` or `DNS resolution failed`

**Solution:**
1. Check internet connectivity
2. Verify firewall allows HTTPS (443)
3. Try alternate endpoint:

```csharp
var client = new SynaxisClient("your-api-key-here", options =>
{
    options.BaseUrl = "https://api-backup.synaxis.io";
});
```

## Monitoring Usage

Track usage in your dashboard:

```bash
# View current usage
curl https://api.synaxis.io/v1/usage \
  -H "Authorization: Bearer your-api-key-here"
```

**Response:**

```json
{
  "usage": {
    "requests": 1234,
    "tokens": 567890,
    "cost": 12.34
  },
  "limits": {
    "requestsPerMinute": 60,
    "tokensPerMonth": 1000000
  },
  "period": {
    "start": "2026-02-01T00:00:00Z",
    "end": "2026-03-01T00:00:00Z"
  }
}
```

## Support

- **Documentation**: [https://docs.synaxis.io](https://docs.synaxis.io)
- **Status Page**: [https://status.synaxis.io](https://status.synaxis.io)
- **Support Portal**: [https://cloud.synaxis.io/support](https://cloud.synaxis.io/support)
- **Email**: support@synaxis.io
- **Community**: [Discord](https://discord.gg/synaxis)

## API Endpoints

- **Production**: `https://api.synaxis.io`
- **EU Region**: `https://eu-west-1.synaxis.io`
- **US Region**: `https://us-east-1.synaxis.io`
- **SA Region**: `https://sa-east-1.synaxis.io`
