# Synaxis API Documentation

> **Version**: v1
> **Base URL**: `https://api.synaxis.io/v1`
> **Content Type**: `application/json`

## Overview

The Synaxis API provides a unified interface for interacting with multiple AI providers through a single REST API. This documentation covers all available endpoints, request/response formats, and authentication methods.

## Authentication

### API Key Authentication

All API requests require authentication using an API key in the `Authorization` header:

```http
Authorization: Bearer YOUR_API_KEY_HERE
```

**Example**:
```bash
curl https://api.synaxis.io/v1/chat/completions \
  -H "Authorization: Bearer sk-abc123..." \
  -H "Content-Type: application/json" \
  -d '{"model":"gpt-4","messages":[{"role":"user","content":"Hello!"}]}'
```

### Getting an API Key

1. Sign up at [https://cloud.synaxis.io](https://cloud.synaxis.io)
2. Navigate to **API Keys** → **Create New Key**
3. Name your key and select permissions
4. Copy the key immediately (it won't be shown again)

## Endpoints

### Chat Completions

Create a chat completion.

**Endpoint**: `POST /v1/chat/completions`

**Request Body**:
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
  "max_tokens": 500,
  "top_p": 1.0,
  "frequency_penalty": 0.0,
  "presence_penalty": 0.0,
  "stop": ["\n"],
  "stream": false
}
```

**Parameters**:

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `model` | string | Yes | The model to use (e.g., `gpt-4`, `gpt-3.5-turbo`, `claude-3-opus`) |
| `messages` | array | Yes | Array of message objects |
| `temperature` | number | No | Sampling temperature (0.0-2.0). Default: 1.0 |
| `max_tokens` | integer | No | Maximum tokens to generate. Default: model-specific |
| `top_p` | number | No | Nucleus sampling threshold (0.0-1.0). Default: 1.0 |
| `frequency_penalty` | number | No | Frequency penalty (-2.0 to 2.0). Default: 0.0 |
| `presence_penalty` | number | No | Presence penalty (-2.0 to 2.0). Default: 0.0 |
| `stop` | array/string | No | Sequences where generation stops |
| `stream` | boolean | No | Enable streaming. Default: false |

**Message Object**:

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `role` | string | Yes | Message role: `system`, `user`, `assistant` |
| `content` | string | Yes | Message content |
| `name` | string | No | Optional name for the message |

**Response**:
```json
{
  "id": "chatcmpl-abc123",
  "object": "chat.completion",
  "created": 1738972800,
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

**Response Fields**:

| Field | Type | Description |
|-------|------|-------------|
| `id` | string | Unique identifier for the completion |
| `object` | string | Object type: `chat.completion` |
| `created` | integer | Unix timestamp of creation |
| `model` | string | Model used for completion |
| `choices` | array | Array of completion choices |
| `usage` | object | Token usage information |

**Choice Object**:

| Field | Type | Description |
|-------|------|-------------|
| `index` | integer | Choice index |
| `message` | object | Message object with role and content |
| `finish_reason` | string | Reason for completion: `stop`, `length`, `content_filter` |

**Usage Object**:

| Field | Type | Description |
|-------|------|-------------|
| `prompt_tokens` | integer | Number of tokens in prompt |
| `completion_tokens` | integer | Number of tokens in completion |
| `total_tokens` | integer | Total tokens used |

**Example**:
```bash
curl https://api.synaxis.io/v1/chat/completions \
  -H "Authorization: Bearer YOUR_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "model": "gpt-4",
    "messages": [
      {"role": "user", "content": "Explain quantum computing in simple terms"}
    ],
    "max_tokens": 500
  }'
```

### Streaming Chat Completions

Create a streaming chat completion using Server-Sent Events (SSE).

**Endpoint**: `POST /v1/chat/completions`

**Request**: Same as non-streaming, but set `"stream": true`

**Response**: Server-Sent Events stream

```
data: {"id":"chatcmpl-abc123","object":"chat.completion.chunk","created":1738972800,"model":"gpt-4","choices":[{"index":0,"delta":{"role":"assistant"},"finish_reason":null}]}

data: {"id":"chatcmpl-abc123","object":"chat.completion.chunk","created":1738972800,"model":"gpt-4","choices":[{"index":0,"delta":{"content":"Quantum"},"finish_reason":null}]}

data: {"id":"chatcmpl-abc123","object":"chat.completion.chunk","created":1738972800,"model":"gpt-4","choices":[{"index":0,"delta":{"content":" computing"},"finish_reason":null}]}

data: [DONE]
```

**Example**:
```bash
curl https://api.synaxis.io/v1/chat/completions \
  -H "Authorization: Bearer YOUR_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "model": "gpt-4",
    "messages": [{"role": "user", "content": "Write a haiku"}],
    "stream": true
  }'
```

### Embeddings

Create embeddings for text input.

**Endpoint**: `POST /v1/embeddings`

**Request Body**:
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
| `input` | string/array | Yes | Text to embed (string or array of strings) |
| `encoding_format` | string | No | Format: `float` or `base64`. Default: `float` |
| `dimensions` | integer | No | Number of dimensions (for supported models) |

**Response**:
```json
{
  "object": "list",
  "data": [
    {
      "object": "embedding",
      "embedding": [0.0023, -0.0052, 0.0123, ...],
      "index": 0
    }
  ],
  "model": "text-embedding-ada-002",
  "usage": {
    "prompt_tokens": 3,
    "total_tokens": 3
  }
}
```

**Example**:
```bash
curl https://api.synaxis.io/v1/embeddings \
  -H "Authorization: Bearer YOUR_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "model": "text-embedding-ada-002",
    "input": "Hello, world!"
  }'
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
      "created": 1687882410,
      "owned_by": "openai"
    },
    {
      "id": "gpt-3.5-turbo",
      "object": "model",
      "created": 1677610602,
      "owned_by": "openai"
    },
    {
      "id": "claude-3-opus-20240229",
      "object": "model",
      "created": 1708968000,
      "owned_by": "anthropic"
    }
  ]
}
```

**Example**:
```bash
curl https://api.synaxis.io/v1/models \
  -H "Authorization: Bearer YOUR_API_KEY"
```

### Retrieve Model

Get information about a specific model.

**Endpoint**: `GET /v1/models/{model}`

**Response**:
```json
{
  "id": "gpt-4",
  "object": "model",
  "created": 1687882410,
  "owned_by": "openai"
}
```

**Example**:
```bash
curl https://api.synaxis.io/v1/models/gpt-4 \
  -H "Authorization: Bearer YOUR_API_KEY"
```

## Error Responses

All endpoints may return error responses in the following format:

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

| HTTP Status | Error Type | Description |
|-------------|------------|-------------|
| 400 | `invalid_request_error` | Invalid request parameters |
| 401 | `invalid_api_key` | Invalid or missing API key |
| 403 | `permission_error` | Insufficient permissions |
| 404 | `not_found_error` | Resource not found |
| 429 | `rate_limit_error` | Rate limit exceeded |
| 500 | `api_error` | Internal server error |
| 503 | `service_unavailable` | Service temporarily unavailable |

### Error Codes

| Code | Description |
|------|-------------|
| `invalid_api_key` | API key is invalid or expired |
| `insufficient_quota` | Quota exceeded |
| `model_not_found` | Model not found or not accessible |
| `invalid_model` | Invalid model name |
| `context_length_exceeded` | Input exceeds model context length |
| `rate_limit_exceeded` | Rate limit exceeded |

## Rate Limiting

API requests are rate limited based on your plan:

| Plan | Requests/Minute | Tokens/Minute |
|------|-----------------|---------------|
| Free | 3 | 40,000 |
| Basic | 60 | 90,000 |
| Pro | 3,500 | 2,000,000 |
| Enterprise | Custom | Custom |

**Rate Limit Headers**:
```
X-RateLimit-Limit: 60
X-RateLimit-Remaining: 59
X-RateLimit-Reset: 1738972860
```

## Pagination

List endpoints support pagination using query parameters:

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `limit` | integer | 20 | Number of items to return |
| `after` | string | null | Cursor for next page |

**Example**:
```bash
curl "https://api.synaxis.io/v1/models?limit=10&after=cursor-abc123" \
  -H "Authorization: Bearer YOUR_API_KEY"
```

## Streaming

Streaming responses use Server-Sent Events (SSE):

1. Set `stream: true` in request
2. Set `Accept: text/event-stream` header
3. Process each `data:` line as JSON
4. Stream ends with `data: [DONE]`

**Example**:
```bash
curl https://api.synaxis.io/v1/chat/completions \
  -H "Authorization: Bearer YOUR_API_KEY" \
  -H "Content-Type: application/json" \
  -H "Accept: text/event-stream" \
  -d '{
    "model": "gpt-4",
    "messages": [{"role": "user", "content": "Count to 10"}],
    "stream": true
  }'
```

## SDKs

Official SDKs are available for:

- **.NET**: `dotnet add package Synaxis.Client`
- **Python**: `pip install synaxis`
- **JavaScript**: `npm install @synaxis/client`
- **Go**: `go get github.com/synaxis/go-client`

See [Quickstart Guides](../getting-started/) for SDK usage examples.

## Webhooks

Configure webhooks to receive real-time notifications:

**Endpoint**: `POST /v1/webhooks`

**Request**:
```json
{
  "url": "https://your-domain.com/webhook",
  "events": ["completion.created", "error.occurred"],
  "secret": "your-webhook-secret"
}
```

**Webhook Payload**:
```json
{
  "event": "completion.created",
  "data": {
    "id": "chatcmpl-abc123",
    "model": "gpt-4",
    "usage": {
      "total_tokens": 35
    }
  },
  "timestamp": 1738972800
}
```

## Support

- **Documentation**: [https://docs.synaxis.io](https://docs.synaxis.io)
- **Status Page**: [https://status.synaxis.io](https://status.synaxis.io)
- **Support**: [support@synaxis.io](mailto:support@synaxis.io)
- **GitHub**: [https://github.com/synaxis/synaxis](https://github.com/synaxis/synaxis)
