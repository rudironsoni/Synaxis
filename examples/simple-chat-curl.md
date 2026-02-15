# Simple Chat Example (curl)

This example demonstrates how to make a simple chat completion request to Synaxis using curl.

## Prerequisites

- **curl** installed on your system
- **Synaxis Gateway** running (or use the hosted API)
- **API Key** for at least one AI provider

## Setup

### Option 1: Use Hosted API

```bash
export SYNAXIS_API_URL="https://api.synaxis.io"
export SYNAXIS_API_KEY="your-synaxis-api-key"
```

### Option 2: Use Local Gateway

Start the gateway:

```bash
docker run -d \
  -p 8080:8080 \
  -e OPENAI_API_KEY=sk-your-openai-key \
  --name synaxis-gateway \
  synaxis/gateway:latest
```

Set environment variables:

```bash
export SYNAXIS_API_URL="http://localhost:8080"
export SYNAXIS_API_KEY="sk-your-openai-key"
```

## Basic Chat Completion

### Request

```bash
curl -X POST $SYNAXIS_API_URL/v1/chat/completions \
  -H "Authorization: Bearer $SYNAXIS_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "model": "gpt-4",
    "messages": [
      {
        "role": "system",
        "content": "You are a helpful assistant."
      },
      {
        "role": "user",
        "content": "Hello, how are you?"
      }
    ],
    "temperature": 0.7,
    "max_tokens": 150
  }'
```

### Response

```json
{
  "id": "chatcmpl-123",
  "object": "chat.completion",
  "created": 1677652288,
  "model": "gpt-4",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": "I'm doing well, thank you! How can I help you today?"
      },
      "finish_reason": "stop"
    }
  ],
  "usage": {
    "prompt_tokens": 20,
    "completion_tokens": 15,
    "total_tokens": 35
  }
}
```

## Chat with System Prompt

### Request

```bash
curl -X POST $SYNAXIS_API_URL/v1/chat/completions \
  -H "Authorization: Bearer $SYNAXIS_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "model": "gpt-4",
    "messages": [
      {
        "role": "system",
        "content": "You are a technical writer. Explain concepts clearly and concisely."
      },
      {
        "role": "user",
        "content": "What is a REST API?"
      }
    ]
  }'
```

## Multi-Turn Conversation

### Request

```bash
curl -X POST $SYNAXIS_API_URL/v1/chat/completions \
  -H "Authorization: Bearer $SYNAXIS_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "model": "gpt-4",
    "messages": [
      {
        "role": "user",
        "content": "What is the capital of France?"
      },
      {
        "role": "assistant",
        "content": "The capital of France is Paris."
      },
      {
        "role": "user",
        "content": "What is its population?"
      }
    ]
  }'
```

## Using Different Models

### GPT-3.5 Turbo

```bash
curl -X POST $SYNAXIS_API_URL/v1/chat/completions \
  -H "Authorization: Bearer $SYNAXIS_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "model": "gpt-3.5-turbo",
    "messages": [
      {
        "role": "user",
        "content": "Explain quantum computing in simple terms."
      }
    ]
  }'
```

### Claude 3 (Anthropic)

```bash
curl -X POST $SYNAXIS_API_URL/v1/chat/completions \
  -H "Authorization: Bearer $SYNAXIS_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "model": "claude-3-opus",
    "messages": [
      {
        "role": "user",
        "content": "Write a short poem about technology."
      }
    ]
  }'
```

## Controlling Output

### Temperature (Creativity)

Lower temperature (0.0) = more focused, deterministic
Higher temperature (2.0) = more creative, random

```bash
curl -X POST $SYNAXIS_API_URL/v1/chat/completions \
  -H "Authorization: Bearer $SYNAXIS_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "model": "gpt-4",
    "messages": [
      {
        "role": "user",
        "content": "Write a story about a robot."
      }
    ],
    "temperature": 0.9
  }'
```

### Max Tokens

Limit the length of the response:

```bash
curl -X POST $SYNAXIS_API_URL/v1/chat/completions \
  -H "Authorization: Bearer $SYNAXIS_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "model": "gpt-4",
    "messages": [
      {
        "role": "user",
        "content": "Tell me a story."
      }
    ],
    "max_tokens": 50
  }'
```

### Stop Sequences

Stop generation when specific text is encountered:

```bash
curl -X POST $SYNAXIS_API_URL/v1/chat/completions \
  -H "Authorization: Bearer $SYNAXIS_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "model": "gpt-4",
    "messages": [
      {
        "role": "user",
        "content": "List 5 programming languages."
      }
    ],
    "stop": ["\n\n"]
  }'
```

## Error Handling

### Check HTTP Status

```bash
response=$(curl -s -o /dev/null -w "%{http_code}" -X POST $SYNAXIS_API_URL/v1/chat/completions \
  -H "Authorization: Bearer $SYNAXIS_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "model": "gpt-4",
    "messages": [{"role": "user", "content": "Hello!"}]
  }')

if [ $response -eq 200 ]; then
  echo "Success!"
else
  echo "Error: HTTP $response"
fi
```

### Parse Error Response

```bash
curl -X POST $SYNAXIS_API_URL/v1/chat/completions \
  -H "Authorization: Bearer invalid-key" \
  -H "Content-Type: application/json" \
  -d '{
    "model": "gpt-4",
    "messages": [{"role": "user", "content": "Hello!"}]
  }' | jq '.error'
```

## Advanced Examples

### JSON Output

Request JSON-formatted output:

```bash
curl -X POST $SYNAXIS_API_URL/v1/chat/completions \
  -H "Authorization: Bearer $SYNAXIS_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "model": "gpt-4",
    "messages": [
      {
        "role": "system",
        "content": "You are a helpful assistant that responds in JSON format."
      },
      {
        "role": "user",
        "content": "Provide information about Python in JSON format with keys: name, creator, year, popularity."
      }
    ],
    "temperature": 0.3
  }'
```

### Function Calling

```bash
curl -X POST $SYNAXIS_API_URL/v1/chat/completions \
  -H "Authorization: Bearer $SYNAXIS_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "model": "gpt-4",
    "messages": [
      {
        "role": "user",
        "content": "What'\''s the weather in San Francisco?"
      }
    ],
    "functions": [
      {
        "name": "get_weather",
        "description": "Get the current weather in a given location",
        "parameters": {
          "type": "object",
          "properties": {
            "location": {
              "type": "string",
              "description": "The city and state, e.g. San Francisco, CA"
            }
          },
          "required": ["location"]
        }
      }
    ],
    "function_call": "auto"
  }'
```

## Tips and Best Practices

1. **Use system prompts** to set context and behavior
2. **Include conversation history** for multi-turn conversations
3. **Adjust temperature** based on desired creativity
4. **Set max_tokens** to control response length
5. **Handle errors gracefully** with proper status code checking
6. **Use stop sequences** to control output format
7. **Cache responses** for repeated queries

## Next Steps

- [Streaming Example](./streaming-javascript.md) - Learn about streaming responses
- [Multi-Modal Example](./multimodal-python.md) - Work with images and audio
- [Batch Processing Example](./batch-processing-csharp.md) - Process multiple requests

## Support

- **Documentation**: [https://docs.synaxis.io](https://docs.synaxis.io)
- **GitHub Issues**: [https://github.com/rudironsoni/Synaxis/issues](https://github.com/rudironsoni/Synaxis/issues)
- **Discord**: [Join our Discord](https://discord.gg/synaxis)
