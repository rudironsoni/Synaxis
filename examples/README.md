# Synaxis Examples

This directory contains code examples demonstrating various ways to use Synaxis.

## Examples

### [Simple Chat (curl)](./simple-chat-curl.md)
Basic chat completion example using curl command-line tool.

### [Streaming (JavaScript)](./streaming-javascript.md)
Streaming chat completion example using JavaScript and Fetch API.

### [Multi-Modal (Python)](./multimodal-python.md)
Multi-modal example with text, images, and audio using Python.

### [Batch Processing (C#)](./batch-processing-csharp.md)
Batch processing example using C# SDK.

## Running Examples

### Prerequisites

1. **Install Synaxis** (if running SDK examples):
   ```bash
   dotnet add package Synaxis
   dotnet add package Synaxis.Providers.OpenAI
   ```

2. **Set up environment variables**:
   ```bash
   export OPENAI_API_KEY=sk-your-key
   export AZURE_OPENAI_ENDPOINT=your-endpoint
   ```

3. **Start the gateway** (for HTTP examples):
   ```bash
   docker run -d -p 8080:8080 -e OPENAI_API_KEY=sk-your-key synaxis/gateway:latest
   ```

### Running HTTP Examples

```bash
# Simple chat
curl -X POST http://localhost:8080/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d @examples/simple-chat.json
```

### Running SDK Examples

```bash
# Navigate to example directory
cd examples/SimpleChat

# Restore dependencies
dotnet restore

# Run the example
dotnet run
```

## Sample Applications

For complete, runnable applications, see the [samples](../samples/) directory:

- **MinimalApi**: Minimal API example
- **SelfHosted**: Self-hosted gateway
- **SaaSClient**: SaaS client example
- **Microservices**: Microservices architecture example

## Contributing

Have an example you'd like to share? Please submit a pull request!

## Support

- **Documentation**: [https://docs.synaxis.io](https://docs.synaxis.io)
- **GitHub Issues**: [https://github.com/rudironsoni/Synaxis/issues](https://github.com/rudironsoni/Synaxis/issues)
- **Discord**: [Join our Discord](https://discord.gg/synaxis)
