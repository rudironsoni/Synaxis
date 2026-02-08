# Synaxis SaaS Client Sample

A console application demonstrating how to use Synaxis via HTTP API with proper error handling and retry logic.

## Features

- Simple chat requests
- Streaming responses
- Comprehensive error handling
- Exponential backoff retry logic
- Production-ready patterns

## Prerequisites

- .NET 10.0 SDK or later
- Synaxis API key (from your SaaS provider)
- Active Synaxis endpoint

## Configuration

### Option 1: Environment Variable

Set your API key as an environment variable:

```bash
export SYNAXIS_API_KEY="your-api-key-here"
```

### Option 2: Modify Code

Update the API key in `Program.cs`:

```csharp
var apiKey = "your-api-key-here";
```

### Configure Endpoint

Update the base URL in `Program.cs` to match your Synaxis endpoint:

```csharp
BaseAddress = new Uri("https://api.synaxis.io")
```

For local development, use:
```csharp
BaseAddress = new Uri("http://localhost:5000")
```

## Running the Application

1. Navigate to the sample directory:
   ```bash
   cd samples/SaaSClient
   ```

2. Run the application:
   ```bash
   dotnet run
   ```

## Examples

### Example 1: Simple Chat Request

Demonstrates basic chat completion with error handling:

```csharp
var request = new ChatRequest
{
    Messages = new[]
    {
        new ChatMessage { Role = "user", Content = "What is Synaxis?" }
    },
    Model = "gpt-3.5-turbo",
    Temperature = 0.7,
    MaxTokens = 500
};

var response = await client.PostAsJsonAsync("/v1/chat/completions", request);
var chatResponse = await response.Content.ReadFromJsonAsync<ChatResponse>();
```

**Output:**
```
Response: Synaxis is a unified AI inference gateway...
Tokens: 245
```

### Example 2: Streaming Chat Request

Demonstrates real-time streaming responses:

```csharp
var request = new ChatRequest
{
    Messages = new[]
    {
        new ChatMessage { Role = "user", Content = "Count from 1 to 10." }
    },
    Model = "gpt-3.5-turbo",
    Stream = true
};
```

**Output:**
```
Streaming response: 1, 2, 3, 4, 5, 6, 7, 8, 9, 10.
[Stream completed]
```

### Example 3: Error Handling

Demonstrates handling API errors:

```csharp
// Invalid request
var request = new ChatRequest
{
    Messages = Array.Empty<ChatMessage>(),
    Model = "gpt-3.5-turbo"
};
```

**Output:**
```
API Error (400): {"error":{"code":"invalid_request","message":"Messages array cannot be empty"}}
Error Code: invalid_request
Error Message: Messages array cannot be empty
```

### Example 4: Retry Logic

Demonstrates exponential backoff for transient failures:

```csharp
const int maxRetries = 3;
const int baseDelayMs = 1000;

for (int attempt = 0; attempt <= maxRetries; attempt++)
{
    try
    {
        // Make request
        // ...
        
        // Retry on 5xx or 429
        if ((int)response.StatusCode >= 500 || (int)response.StatusCode == 429)
        {
            var delay = baseDelayMs * (int)Math.Pow(2, attempt);
            await Task.Delay(delay);
            continue;
        }
    }
    catch (HttpRequestException ex)
    {
        // Exponential backoff
    }
}
```

**Output:**
```
Attempt 1 of 4...
Server error 503. Retrying in 1000ms...
Attempt 2 of 4...
Server error 503. Retrying in 2000ms...
Attempt 3 of 4...
Request succeeded!
```

## API Reference

### Chat Request

```csharp
public record ChatRequest
{
    public ChatMessage[] Messages { get; init; }
    public string Model { get; init; } = "gpt-3.5-turbo";
    public double? Temperature { get; init; }
    public int? MaxTokens { get; init; }
    public bool Stream { get; init; }
}
```

### Chat Response

```csharp
public record ChatResponse
{
    public string Id { get; init; }
    public string Object { get; init; }
    public long Created { get; init; }
    public string Model { get; init; }
    public ChatChoice[] Choices { get; init; }
    public ChatUsage? Usage { get; init; }
}
```

## Error Handling

### HTTP Status Codes

| Code | Meaning | Action |
|------|---------|--------|
| 400 | Bad Request | Fix request payload |
| 401 | Unauthorized | Check API key |
| 429 | Rate Limited | Implement backoff |
| 500 | Server Error | Retry with backoff |
| 503 | Service Unavailable | Retry with backoff |

### Error Response Format

```json
{
  "error": {
    "code": "invalid_request",
    "message": "Detailed error message",
    "type": "invalid_request_error"
  }
}
```

## Best Practices

### 1. Always Use Retry Logic

Implement exponential backoff for transient failures:

```csharp
var delay = baseDelayMs * (int)Math.Pow(2, attempt);
await Task.Delay(delay);
```

### 2. Handle Timeouts

Set appropriate timeouts based on expected response time:

```csharp
httpClient.Timeout = TimeSpan.FromMinutes(2);
```

### 3. Validate Responses

Always check HTTP status and parse errors:

```csharp
if (!response.IsSuccessStatusCode)
{
    var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
    // Handle error
}
```

### 4. Use Streaming for Long Responses

For real-time applications or long responses:

```csharp
var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
await using var stream = await response.Content.ReadAsStreamAsync();
```

### 5. Secure API Keys

Never hardcode API keys. Use:
- Environment variables
- Configuration files (with .gitignore)
- Secret management services (Azure Key Vault, AWS Secrets Manager)

## Integration Patterns

### Dependency Injection

```csharp
services.AddHttpClient<ISynaxisClient, SynaxisClient>(client =>
{
    client.BaseAddress = new Uri("https://api.synaxis.io");
})
.AddPolicyHandler(GetRetryPolicy());
```

### Polly Resilience

```csharp
using Polly;
using Polly.Extensions.Http;

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(3, retryAttempt => 
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}
```

## Performance Considerations

### Connection Pooling

Reuse HttpClient instances:

```csharp
// Good: Single instance
private static readonly HttpClient _httpClient = new HttpClient();

// Bad: Creates new instance per request
var client = new HttpClient(); // Don't do this
```

### Async/Await

Always use async patterns:

```csharp
var response = await client.PostAsJsonAsync(...); // Good
var response = client.PostAsJsonAsync(...).Result; // Bad - blocks thread
```

## Troubleshooting

### Connection Errors

```bash
# Test endpoint connectivity
curl https://api.synaxis.io/health/ready
```

### Authentication Errors

Verify your API key:
```bash
curl -H "Authorization: Bearer your-api-key" \
  https://api.synaxis.io/v1/chat/completions
```

### Timeout Issues

Increase timeout for long-running requests:
```csharp
httpClient.Timeout = TimeSpan.FromMinutes(5);
```

## Production Deployment

### Logging

Integrate with logging frameworks:

```csharp
using Microsoft.Extensions.Logging;

_logger.LogInformation("Making chat request with {MessageCount} messages", 
    request.Messages.Length);
```

### Metrics

Track key metrics:
- Request latency
- Error rates
- Token usage
- API costs

### Rate Limiting

Implement client-side rate limiting:

```csharp
using System.Threading.RateLimiting;

var rateLimiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
{
    PermitLimit = 10,
    Window = TimeSpan.FromSeconds(1)
});
```

## Next Steps

- Explore the [MinimalApi](../MinimalApi/README.md) sample for server-side integration
- Check out the [SelfHosted](../SelfHosted/README.md) sample for self-hosting
- Read the main [documentation](../../README.md) for advanced features

## Support

For issues and questions, see the main [documentation](../../README.md).
