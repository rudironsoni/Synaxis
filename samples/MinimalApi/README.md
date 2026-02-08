# Synaxis Minimal API Sample

A simple ASP.NET Core minimal API demonstrating basic Synaxis usage with OpenAI provider.

## Features

- Single-file application
- OpenAI chat completions
- Simple HTTP endpoint
- Configuration-based API key management

## Prerequisites

- .NET 10.0 SDK or later
- OpenAI API key

## Configuration

Update the `appsettings.json` file with your OpenAI API key:

```json
{
  "OpenAI": {
    "ApiKey": "your-openai-api-key-here"
  }
}
```

Alternatively, set the API key using environment variables:

```bash
export OpenAI__ApiKey="your-openai-api-key-here"
```

## Running the Application

1. Navigate to the sample directory:
   ```bash
   cd samples/MinimalApi
   ```

2. Run the application:
   ```bash
   dotnet run
   ```

3. The API will be available at `http://localhost:5000` (or `https://localhost:5001`).

## Usage

### Send a Chat Request

```bash
curl -X POST http://localhost:5000/chat \
  -H "Content-Type: application/json" \
  -d '{
    "messages": [
      {
        "role": "user",
        "content": "What is Synaxis?"
      }
    ],
    "model": "gpt-3.5-turbo",
    "temperature": 0.7,
    "maxTokens": 500
  }'
```

### Example Response

```json
{
  "id": "chatcmpl-123",
  "object": "chat.completion",
  "created": 1677652288,
  "model": "gpt-3.5-turbo",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": "Synaxis is a unified AI inference gateway..."
      },
      "finishReason": "stop"
    }
  ],
  "usage": {
    "promptTokens": 10,
    "completionTokens": 50,
    "totalTokens": 60
  }
}
```

## Project Structure

- `Program.cs` - Main application file with endpoint definitions
- `appsettings.json` - Configuration file with API keys
- `MinimalApi.csproj` - Project file with dependencies

## Key Concepts

### 1. Service Registration

```csharp
builder.Services.AddSynaxis();
builder.Services.AddOpenAIProvider(apiKey);
```

### 2. Chat Command

```csharp
var command = new ChatCommand(
    Messages: request.Messages,
    Model: "gpt-3.5-turbo",
    Temperature: 0.7);
```

### 3. Command Execution

```csharp
var response = await mediator.Send(command);
```

## Next Steps

- Explore the [SelfHosted](../SelfHosted/README.md) sample for a complete server setup
- Check out the [SaaSClient](../SaaSClient/README.md) sample for client usage
- See the main [documentation](../../README.md) for advanced features

## Troubleshooting

### Build Errors

Ensure all dependencies are restored:
```bash
dotnet restore
```

### API Key Issues

Verify your OpenAI API key is valid and has sufficient credits.

### Runtime Errors

Check the logs for detailed error messages. Increase logging level in `appsettings.json` if needed.
