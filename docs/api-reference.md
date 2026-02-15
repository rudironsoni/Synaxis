# Synaxis API Reference

This document provides a comprehensive reference for the Synaxis API, including endpoints, request/response formats, authentication, and error codes.

## Table of Contents

- [Overview](#overview)
- [Authentication](#authentication)
- [Base URL](#base-url)
- [Endpoints](#endpoints)
- [Request/Response Formats](#requestresponse-formats)
- [Error Codes](#error-codes)
- [Rate Limiting](#rate-limiting)
- [Examples](#examples)

## Overview

Synaxis provides an **OpenAI-compatible API** that works with existing OpenAI clients and SDKs. This makes it easy to migrate from OpenAI to Synaxis without changing your application code.

### Key Features

- **OpenAI-Compatible**: Drop-in replacement for OpenAI API
- **Multiple Providers**: Route requests to OpenAI, Anthropic, Azure, and more
- **Streaming Support**: Real-time streaming responses via SSE
- **Multi-Modal**: Support for text, images, and audio
- **Batch Processing**: Process multiple requests efficiently

## Authentication

### API Key Authentication

Include your API key in the `Authorization` header:

```bash
Authorization: Bearer your-api-key-here
```

### JWT Authentication

For stateless authentication, use JWT tokens:

```bash
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Example

```bash
curl -X POST https://api.synaxis.io/v1/chat/completions \
  -H "Authorization: Bearer sk-your-api-key" \
  -H "Content-Type: application/json" \
  -d '{"model": "gpt-4", "messages": [...]}'
```

## Base URL

### Production
```
https://api.synaxis.io
```

### Development
```
http://localhost:8080
```

### Self-Hosted
```
https://your-domain.com
```

## Endpoints

### Chat Completions

Create a chat completion.

**Endpoint**: `POST /v1/chat/completions`

**Request**:

```json
{
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
  "max_tokens": 150,
  "stream": false,
  "top_p": 1.0,
  "frequency_penalty": 0.0,
  "presence_penalty": 0.0,
  "stop": ["\n", "User:"],
  "user": "user-123"
}
```

**Parameters**:

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `model` | string | Yes | Model to use (e.g., "gpt-4", "claude-3-opus") |
| `messages` | array | Yes | Array of message objects |
| `temperature` | number | No | Sampling temperature (0-2) |
| `max_tokens` | integer | No | Maximum tokens to generate |
| `stream` | boolean | No | Enable streaming (default: false) |
| `top_p` | number | No | Nucleus sampling threshold (0-1) |
| `frequency_penalty` | number | No | Frequency penalty (-2.0 to 2.0) |
| `presence_penalty` | number | No | Presence penalty (-2.0 to 2.0) |
| `stop` | array/string | No | Stop sequences |
| `user` | string | No | User identifier for tracking |

**Response**:

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
    "prompt_tokens": 12,
    "completion_tokens": 15,
    "total_tokens": 27
  }
}
```

**Streaming Response**:

When `stream: true`, the response is sent as Server-Sent Events (SSE):

```
data: {"id":"chatcmpl-123","object":"chat.completion.chunk","created":1677652288,"model":"gpt-4","choices":[{"index":0,"delta":{"role":"assistant"},"finish_reason":null}]}

data: {"id":"chatcmpl-123","object":"chat.completion.chunk","created":1677652288,"model":"gpt-4","choices":[{"index":0,"delta":{"content":"I'm"},"finish_reason":null}]}

data: {"id":"chatcmpl-123","object":"chat.completion.chunk","created":1677652288,"model":"gpt-4","choices":[{"index":0,"delta":{"content":" doing"},"finish_reason":null}]}

data: [DONE]
```

### Completions (Legacy)

Create a text completion (legacy endpoint).

**Endpoint**: `POST /v1/completions`

**Request**:

```json
{
  "model": "gpt-3.5-turbo-instruct",
  "prompt": "Write a haiku about programming:",
  "max_tokens": 50,
  "temperature": 0.7
}
```

**Response**:

```json
{
  "id": "cmpl-123",
  "object": "text_completion",
  "created": 1677652288,
  "model": "gpt-3.5-turbo-instruct",
  "choices": [
    {
      "index": 0,
      "text": "Code flows like water\nBugs hide in the shadows\nDebug brings the light",
      "finish_reason": "stop"
    }
  ],
  "usage": {
    "prompt_tokens": 6,
    "completion_tokens": 20,
    "total_tokens": 26
  }
}
```

### Embeddings

Create text embeddings.

**Endpoint**: `POST /v1/embeddings`

**Request**:

```json
{
  "model": "text-embedding-ada-002",
  "input": "Hello, world!",
  "encoding_format": "float"
}
```

**Parameters**:

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `model` | string | Yes | Embedding model to use |
| `input` | string/array | Yes | Text to embed |
| `encoding_format` | string | No | Format: "float" or "base64" |
| `dimensions` | integer | No | Number of dimensions (for supported models) |

**Response**:

```json
{
  "object": "list",
  "data": [
    {
      "object": "embedding",
      "index": 0,
      "embedding": [0.0023, -0.0234, 0.1234, ...]
    }
  ],
  "model": "text-embedding-ada-002",
  "usage": {
    "prompt_tokens": 3,
    "total_tokens": 3
  }
}
```

### Models

List available models.

**Endpoint**: `GET /v1/models`

**Response**:

```json
{
  "object": "list",
  "data": [
    {
      "id": "gpt-4",
      "object": "model",
      "created": 1677610602,
      "owned_by": "openai"
    },
    {
      "id": "gpt-3.5-turbo",
      "object": "model",
      "created": 1677610602,
      "owned_by": "openai"
    },
    {
      "id": "claude-3-opus",
      "object": "model",
      "created": 1677610602,
      "owned_by": "anthropic"
    }
  ]
}
```

Retrieve a specific model.

**Endpoint**: `GET /v1/models/{model}`

**Response**:

```json
{
  "id": "gpt-4",
  "object": "model",
  "created": 1677610602,
  "owned_by": "openai"
}
```

### Moderation

Check text for policy violations.

**Endpoint**: `POST /v1/moderations`

**Request**:

```json
{
  "input": "I want to hurt someone.",
  "model": "text-moderation-latest"
}
```

**Response**:

```json
{
  "id": "modr-123",
  "model": "text-moderation-latest",
  "results": [
    {
      "flagged": true,
      "categories": {
        "violence": true,
        "self_harm": false,
        "sexual": false,
        "hate": false
      },
      "category_scores": {
        "violence": 0.95,
        "self_harm": 0.01,
        "sexual": 0.02,
        "hate": 0.03
      }
    }
  ]
}
```

### Images (DALL-E)

Generate images.

**Endpoint**: `POST /v1/images/generations`

**Request**:

```json
{
  "model": "dall-e-3",
  "prompt": "A futuristic city with flying cars",
  "n": 1,
  "size": "1024x1024",
  "quality": "standard",
  "response_format": "url"
}
```

**Parameters**:

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `model` | string | Yes | Model to use (dall-e-2, dall-e-3) |
| `prompt` | string | Yes | Image description |
| `n` | integer | No | Number of images (1-10) |
| `size` | string | No | Image size (256x256, 512x512, 1024x1024, etc.) |
| `quality` | string | No | Quality (standard, hd) |
| `response_format` | string | No | Format (url, b64_json) |

**Response**:

```json
{
  "created": 1677652288,
  "data": [
    {
      "url": "https://oaidalleapiprodscus.blob.core.windows.net/private/..."
    }
  ]
}
```

### Audio (Speech)

Generate speech from text.

**Endpoint**: `POST /v1/audio/speech`

**Request**:

```json
{
  "model": "tts-1",
  "input": "Hello, world!",
  "voice": "alloy"
}
```

**Parameters**:

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `model` | string | Yes | Model to use (tts-1, tts-1-hd) |
| `input` | string | Yes | Text to convert |
| `voice` | string | Yes | Voice (alloy, echo, fable, onyx, nova, shimmer) |

**Response**: Audio file (MP3)

### Audio (Transcription)

Transcribe audio to text.

**Endpoint**: `POST /v1/audio/transcriptions`

**Request**: Multipart form data

```
model: whisper-1
file: [audio file]
language: en
```

**Response**:

```json
{
  "text": "Hello, world!",
  "task": "transcribe",
  "language": "english",
  "duration": 1.5,
  "words": [
    {"word": "Hello", "start": 0.0, "end": 0.5},
    {"word": "world", "start": 0.6, "end": 1.0}
  ]
}
```

### Batch Processing

Process multiple requests in a batch.

**Endpoint**: `POST /v1/batch`

**Request**:

```json
{
  "requests": [
    {
      "custom_id": "req-1",
      "method": "POST",
      "url": "/v1/chat/completions",
      "body": {
        "model": "gpt-4",
        "messages": [{"role": "user", "content": "Hello!"}]
      }
    },
    {
      "custom_id": "req-2",
      "method": "POST",
      "url": "/v1/chat/completions",
      "body": {
        "model": "gpt-4",
        "messages": [{"role": "user", "content": "How are you?"}]
      }
    }
  ]
}
```

**Response**:

```json
{
  "id": "batch_123",
  "object": "batch",
  "endpoint": "/v1/chat/completions",
  "errors": null,
  "status": "in_progress",
  "created_at": 1677652288,
  "completed_at": null,
  "request_counts": {
    "total": 2,
    "completed": 0,
    "failed": 0
  }
}
```

Retrieve batch status:

**Endpoint**: `GET /v1/batch/{batch_id}`

## Request/Response Formats

### Message Object

```json
{
  "role": "user|system|assistant",
  "content": "string",
  "name": "optional-name",
  "tool_calls": [...],
  "tool_call_id": "optional-id"
}
```

### Choice Object

```json
{
  "index": 0,
  "message": {...},
  "finish_reason": "stop|length|content_filter|function_call",
  "logprobs": null
}
```

### Usage Object

```json
{
  "prompt_tokens": 10,
  "completion_tokens": 20,
  "total_tokens": 30
}
```

## Error Codes

### HTTP Status Codes

| Code | Description |
|------|-------------|
| 200 | Success |
| 400 | Bad Request |
| 401 | Unauthorized |
| 403 | Forbidden |
| 404 | Not Found |
| 429 | Rate Limit Exceeded |
| 500 | Internal Server Error |
| 502 | Bad Gateway |
| 503 | Service Unavailable |
| 504 | Gateway Timeout |

### Error Response Format

```json
{
  "error": {
    "message": "Invalid API key provided",
    "type": "invalid_request_error",
    "param": null,
    "code": "invalid_api_key"
  }
}
```

### Error Types

| Type | Description |
|------|-------------|
| `invalid_request_error` | Invalid request parameters |
| `invalid_api_key` | Invalid or missing API key |
| `rate_limit_error` | Rate limit exceeded |
| `insufficient_quota` | Insufficient quota or credits |
| `model_not_found` | Model not found or not accessible |
| `content_filter` | Content filtered by safety policies |
| `server_error` | Internal server error |
| `service_unavailable` | Service temporarily unavailable |

### Common Errors

#### Invalid API Key
```json
{
  "error": {
    "message": "Invalid API key provided",
    "type": "invalid_request_error",
    "code": "invalid_api_key"
  }
}
```

**Solution**: Verify your API key is correct and has proper permissions.

#### Rate Limit Exceeded
```json
{
  "error": {
    "message": "Rate limit exceeded. Please try again later.",
    "type": "rate_limit_error",
    "code": "rate_limit_exceeded"
  }
}
```

**Solution**: Implement retry logic with exponential backoff.

#### Model Not Found
```json
{
  "error": {
    "message": "Model 'gpt-5' not found",
    "type": "invalid_request_error",
    "code": "model_not_found"
  }
}
```

**Solution**: Verify the model name and check your API key has access.

#### Insufficient Quota
```json
{
  "error": {
    "message": "Insufficient quota. Please upgrade your plan.",
    "type": "insufficient_quota",
    "code": "insufficient_quota"
  }
}
```

**Solution**: Upgrade your plan or add credits.

## Rate Limiting

### Rate Limits

| Plan | Requests/Minute | Tokens/Day |
|------|-----------------|------------|
| Free | 60 | 150,000 |
| Basic | 3,000 | 2,000,000 |
| Pro | 10,000 | 10,000,000 |
| Enterprise | Custom | Custom |

### Rate Limit Headers

Synaxis includes rate limit information in response headers:

```
X-RateLimit-Limit: 3000
X-RateLimit-Remaining: 2999
X-RateLimit-Reset: 1677652800
```

### Handling Rate Limits

Implement exponential backoff:

```javascript
async function makeRequest(url, options, retries = 3) {
  try {
    const response = await fetch(url, options);

    if (response.status === 429) {
      const retryAfter = parseInt(response.headers.get('Retry-After') || '5');
      await new Promise(resolve => setTimeout(resolve, retryAfter * 1000));
      return makeRequest(url, options, retries - 1);
    }

    return response;
  } catch (error) {
    if (retries > 0) {
      await new Promise(resolve => setTimeout(resolve, 1000 * (4 - retries)));
      return makeRequest(url, options, retries - 1);
    }
    throw error;
  }
}
```

## Examples

### cURL Examples

#### Simple Chat Completion
```bash
curl -X POST https://api.synaxis.io/v1/chat/completions \
  -H "Authorization: Bearer sk-your-api-key" \
  -H "Content-Type: application/json" \
  -d '{
    "model": "gpt-4",
    "messages": [{"role": "user", "content": "Hello!"}]
  }'
```

#### Streaming Chat Completion
```bash
curl -X POST https://api.synaxis.io/v1/chat/completions \
  -H "Authorization: Bearer sk-your-api-key" \
  -H "Content-Type: application/json" \
  -d '{
    "model": "gpt-4",
    "messages": [{"role": "user", "content": "Tell me a story"}],
    "stream": true
  }'
```

#### Embeddings
```bash
curl -X POST https://api.synaxis.io/v1/embeddings \
  -H "Authorization: Bearer sk-your-api-key" \
  -H "Content-Type: application/json" \
  -d '{
    "model": "text-embedding-ada-002",
    "input": "Hello, world!"
  }'
```

### JavaScript Examples

#### Using Fetch API
```javascript
const response = await fetch('https://api.synaxis.io/v1/chat/completions', {
  method: 'POST',
  headers: {
    'Authorization': 'Bearer sk-your-api-key',
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    model: 'gpt-4',
    messages: [{ role: 'user', content: 'Hello!' }]
  })
});

const data = await response.json();
console.log(data.choices[0].message.content);
```

#### Streaming with Fetch API
```javascript
const response = await fetch('https://api.synaxis.io/v1/chat/completions', {
  method: 'POST',
  headers: {
    'Authorization': 'Bearer sk-your-api-key',
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    model: 'gpt-4',
    messages: [{ role: 'user', content: 'Tell me a story' }],
    stream: true
  })
});

const reader = response.body.getReader();
const decoder = new TextDecoder();

while (true) {
  const { done, value } = await reader.read();
  if (done) break;

  const chunk = decoder.decode(value);
  const lines = chunk.split('\n');

  for (const line of lines) {
    if (line.startsWith('data: ')) {
      const data = line.slice(6);
      if (data === '[DONE]') break;

      const parsed = JSON.parse(data);
      const content = parsed.choices[0]?.delta?.content;
      if (content) process.stdout.write(content);
    }
  }
}
```

### Python Examples

#### Using Requests
```python
import requests

response = requests.post(
    'https://api.synaxis.io/v1/chat/completions',
    headers={
        'Authorization': 'Bearer sk-your-api-key',
        'Content-Type': 'application/json'
    },
    json={
        'model': 'gpt-4',
        'messages': [{'role': 'user', 'content': 'Hello!'}]
    }
)

data = response.json()
print(data['choices'][0]['message']['content'])
```

#### Streaming with Requests
```python
import requests

response = requests.post(
    'https://api.synaxis.io/v1/chat/completions',
    headers={
        'Authorization': 'Bearer sk-your-api-key',
        'Content-Type': 'application/json'
    },
    json={
        'model': 'gpt-4',
        'messages': [{'role': 'user', 'content': 'Tell me a story'}],
        'stream': True
    },
    stream=True
)

for line in response.iter_lines():
    if line:
        line = line.decode('utf-8')
        if line.startswith('data: '):
            data = line[6:]
            if data == '[DONE]':
                break
            parsed = json.loads(data)
            content = parsed['choices'][0]['delta'].get('content', '')
            if content:
                print(content, end='', flush=True)
```

### C# Examples

#### Using HttpClient
```csharp
using System.Net.Http;
using System.Text;
using System.Text.Json;

var client = new HttpClient();
client.DefaultRequestHeaders.Add("Authorization", "Bearer sk-your-api-key");

var request = new
{
    model = "gpt-4",
    messages = new[]
    {
        new { role = "user", content = "Hello!" }
    }
};

var json = JsonSerializer.Serialize(request);
var content = new StringContent(json, Encoding.UTF8, "application/json");

var response = await client.PostAsync(
    "https://api.synaxis.io/v1/chat/completions",
    content);

var responseJson = await response.Content.ReadAsStringAsync();
var data = JsonSerializer.Deserialize<JsonElement>(responseJson);

Console.WriteLine(data.GetProperty("choices")[0]
    .GetProperty("message")
    .GetProperty("content")
    .GetString());
```

---

**Next**: Learn about [Deployment](./deployment.md) or [Development](./development.md).
