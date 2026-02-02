# API Reference

> **ULTRA MISER MODE™ API Design**: Every endpoint is engineered to maximize free tier utilization. Why pay for API documentation when you can read this for free? That's the spirit.

Synaxis provides a unified, OpenAI-compatible API for accessing multiple AI inference providers through a single endpoint. The API supports real-time streaming, comprehensive error handling, and intelligent provider routing with automatic failover.

---

## Overview

### OpenAI Compatibility

Synaxis implements the OpenAI API specification, allowing you to use existing OpenAI clients with minimal configuration changes. Simply point your client to the Synaxis gateway URL instead of OpenAI's endpoints.

### Base URL

```
http://localhost:8080/openai/v1
```

### API Version

Current API version: `v1`

---

## Authentication

### JWT Authentication

Admin and user endpoints require JWT (JSON Web Token) authentication. Include the token in the Authorization header:

```
Authorization: Bearer <your-jwt-token>
```

### Development Login

For development and testing, use the dev-login endpoint to obtain a JWT token:

**Endpoint:** `POST /auth/dev-login`

**Request:**
```json
{
  "email": "developer@example.com"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

### Standard Authentication

**Register:** `POST /auth/register`
```json
{
  "email": "user@example.com",
  "password": "secure-password"
}
```

**Login:** `POST /auth/login`
```json
{
  "email": "user@example.com",
  "password": "secure-password"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": "user-id",
    "email": "user@example.com"
  }
}
```

---

## Endpoints

### Chat Completions

#### POST /openai/v1/chat/completions

Creates a model response for the given chat conversation. Supports both streaming and non-streaming modes.

**Authentication:** None required

**Request:**
```json
{
  "model": "llama-3.1-70b-versatile",
  "messages": [
    { "role": "system", "content": "You are a helpful assistant." },
    { "role": "user", "content": "Hello, world!" }
  ],
  "stream": false,
  "temperature": 0.7,
  "max_tokens": 1000,
  "top_p": 1.0,
  "tools": [
    {
      "type": "function",
      "function": {
        "name": "get_weather",
        "description": "Get weather information",
        "parameters": {
          "type": "object",
          "properties": {
            "location": {
              "type": "string",
              "description": "The location to get weather for"
            }
          },
          "required": ["location"]
        }
      }
    }
  ],
  "tool_choice": "auto"
}
```

**Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `model` | string | Yes | Model identifier or alias |
| `messages` | array | Yes | Array of message objects |
| `stream` | boolean | No | Enable streaming (default: false) |
| `temperature` | number | No | Sampling temperature (0-2) |
| `max_tokens` | integer | No | Maximum tokens to generate |
| `top_p` | number | No | Nucleus sampling (0-1) |
| `tools` | array | No | Available tools/functions |
| `tool_choice` | string | No | Tool selection strategy |
| `response_format` | object | No | Response format specification |
| `stop` | string/array | No | Stop sequences |

**Response (non-streaming):**
```json
{
  "id": "chatcmpl-123",
  "object": "chat.completion",
  "created": 1699012345,
  "model": "llama-3.1-70b-versatile",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": "Hello! How can I help you today?"
      },
      "finish_reason": "stop"
    }
  ],
  "usage": {
    "prompt_tokens": 10,
    "completion_tokens": 20,
    "total_tokens": 30
  }
}
```

**Response (streaming):**
```
data: {"id":"chatcmpl-123","object":"chat.completion.chunk","created":1699012345,"model":"llama-3.1-70b-versatile","choices":[{"index":0,"delta":{"role":"assistant","content":"Hello"},"finish_reason":null}]}

data: {"id":"chatcmpl-123","object":"chat.completion.chunk","created":1699012345,"model":"llama-3.1-70b-versatile","choices":[{"index":0,"delta":{"content":"!"},"finish_reason":null}]}

data: [DONE]
```

---

### Responses

#### POST /openai/v1/responses

OpenAI-compatible responses endpoint supporting both streaming and non-streaming modes.

**Authentication:** None required

**Request:** Same as chat completions

**Response (non-streaming):**
```json
{
  "id": "resp_123",
  "object": "response",
  "created": 1699012345,
  "model": "llama-3.1-70b-versatile",
  "output": [
    {
      "type": "message",
      "role": "assistant",
      "content": [
        {
          "type": "output_text",
          "text": "Hello! How can I help you today?"
        }
      ]
    }
  ]
}
```

**Response (streaming):**
```
data: {"id":"resp_123","object":"response.output_item.delta","created":1699012345,"model":"llama-3.1-70b-versatile","delta":{"content":"Hello"}}

data: {"id":"resp_123","object":"response.completed","created":1699012345,"model":"llama-3.1-70b-versatile","delta":{}}

data: [DONE]
```

---

### Models

#### GET /openai/v1/models

Returns a list of all available models with their capabilities and provider information.

**Authentication:** None required

**Response:**
```json
{
  "object": "list",
  "data": [
    {
      "id": "llama-3.1-70b-versatile",
      "object": "model",
      "created": 1699012345,
      "owned_by": "groq",
      "provider": "groq",
      "model_path": "llama-3.1-70b-versatile",
      "capabilities": {
        "streaming": true,
        "tools": true,
        "vision": false,
        "structured_output": false,
        "log_probs": false
      }
    }
  ],
  "providers": [
    {
      "id": "groq",
      "type": "groq",
      "enabled": true,
      "tier": 0
    }
  ]
}
```

#### GET /openai/v1/models/{id}

Returns detailed information about a specific model.

**Authentication:** None required

**Response:**
```json
{
  "id": "llama-3.1-70b-versatile",
  "object": "model",
  "created": 1699012345,
  "owned_by": "groq",
  "provider": "groq",
  "model_path": "llama-3.1-70b-versatile",
  "capabilities": {
    "streaming": true,
    "tools": true,
    "vision": false,
    "structured_output": false,
    "log_probs": false
  }
}
```

**Error Response (404):**
```json
{
  "error": {
    "message": "The model 'invalid-model' does not exist",
    "type": "invalid_request_error",
    "param": "model",
    "code": "model_not_found"
  }
}
```

---

### Admin Endpoints

All admin endpoints require JWT authentication.

#### GET /admin/providers

Returns a list of all configured AI providers with their settings and status.

**Authentication:** JWT required

**Response:**
```json
[
  {
    "id": "groq",
    "name": "groq",
    "type": "groq",
    "enabled": true,
    "tier": 0,
    "endpoint": null,
    "key_configured": true,
    "models": [
      {
        "id": "llama-3.1-70b-versatile",
        "name": "llama-3.1-70b-versatile",
        "enabled": true
      }
    ],
    "status": "online",
    "latency": 45
  }
]
```

#### PUT /admin/providers/{providerId}

Update provider settings including enabled status, API key, endpoint, and tier.

**Authentication:** JWT required

**Request:**
```json
{
  "enabled": true,
  "key": "new-api-key",
  "endpoint": "https://api.example.com/v1",
  "tier": 1
}
```

**Response:**
```json
{
  "success": true,
  "message": "Provider 'groq' updated successfully"
}
```

#### GET /admin/health

Returns detailed health information about services and AI providers.

**Authentication:** JWT required

**Response:**
```json
{
  "services": [
    {
      "name": "API Gateway",
      "status": "healthy",
      "last_checked": "2024-01-15T10:30:00Z"
    },
    {
      "name": "PostgreSQL",
      "status": "healthy",
      "latency": 15,
      "last_checked": "2024-01-15T10:30:00Z"
    },
    {
      "name": "Redis",
      "status": "healthy",
      "latency": 5,
      "last_checked": "2024-01-15T10:30:00Z"
    }
  ],
  "providers": [
    {
      "id": "groq",
      "name": "groq",
      "status": "online",
      "last_checked": "2024-01-15T10:30:00Z",
      "success_rate": 98.5,
      "latency": 45
    }
  ],
  "overall_status": "healthy",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

---

### Health Check Endpoints

#### GET /health/liveness

Simple liveness check that returns healthy if the service is running.

**Authentication:** None required

**Response:**
```json
{
  "status": "healthy"
}
```

#### GET /health/readiness

Comprehensive readiness check including database and Redis connectivity.

**Authentication:** None required

**Response:**
```json
{
  "status": "healthy",
  "checks": {
    "database": "healthy",
    "redis": "healthy"
  }
}
```

---

### Identity Endpoints

#### POST /api/identity/{provider}/start

Start OAuth authentication flow for a specific provider (google, github).

**Authentication:** None required

**Response:**
```json
{
  "auth_url": "https://provider.com/oauth/authorize?...",
  "state": "random-state-string"
}
```

#### POST /api/identity/{provider}/complete

Complete OAuth authentication flow.

**Authentication:** None required

**Request:**
```json
{
  "code": "authorization-code",
  "state": "state-from-start"
}
```

**Response:**
```json
{
  "success": true,
  "user": {
    "id": "user-id",
    "email": "user@example.com"
  }
}
```

#### GET /api/identity/accounts

List all configured identity accounts (with masked tokens).

**Authentication:** None required

**Response:**
```json
[
  {
    "id": "account-id",
    "provider": "google",
    "email": "user@example.com",
    "access_token": "abcd....1234"
  }
]
```

---

### OAuth Endpoints

#### GET /oauth/antigravity/callback

OAuth callback endpoint for Antigravity authentication.

**Authentication:** None required

**Response:** HTML page with authentication result

#### POST /antigravity/auth/start

Start Antigravity authentication flow.

**Authentication:** None required

**Request:**
```json
{
  "redirect_url": "http://localhost:51121/oauth/antigravity/callback"
}
```

**Response:**
```json
{
  "auth_url": "https://antigravity.com/auth?...",
  "redirect_url": "http://localhost:51121/oauth/antigravity/callback",
  "instructions": "Open AuthUrl in your browser. After login, you will be redirected to the callback."
}
```

#### POST /antigravity/auth/complete

Complete Antigravity authentication flow.

**Authentication:** None required

**Request:**
```json
{
  "code": "authorization-code",
  "state": "state-string",
  "redirect_url": "http://localhost:51121/oauth/antigravity/callback"
}
```

**Response:**
```json
{
  "message": "Authentication successful. Account added."
}
```

#### GET /antigravity/accounts

List all Antigravity accounts.

**Authentication:** None required

---

## Streaming (SSE)

Synaxis uses Server-Sent Events (SSE) for streaming responses, following the OpenAI specification.

### SSE Headers

```
Content-Type: text/event-stream
Cache-Control: no-cache
Connection: keep-alive
```

### Data Format

Each chunk is sent as a separate data frame:
```
data: {"id":"chatcmpl-123","object":"chat.completion.chunk",...}

```

### End Marker

The stream ends with:
```
data: [DONE]

```

### Complete Streaming Example

```
data: {"id":"chatcmpl-123","object":"chat.completion.chunk","created":1699012345,"model":"llama-3.1-70b-versatile","choices":[{"index":0,"delta":{"role":"assistant","content":"Hello"},"finish_reason":null}]}

data: {"id":"chatcmpl-123","object":"chat.completion.chunk","created":1699012345,"model":"llama-3.1-70b-versatile","choices":[{"index":0,"delta":{"content":" world"},"finish_reason":null}]}

data: {"id":"chatcmpl-123","object":"chat.completion.chunk","created":1699012345,"model":"llama-3.1-70b-versatile","choices":[{"index":0,"delta":{"content":"!"},"finish_reason":null}]}

data: {"id":"chatcmpl-123","object":"chat.completion.chunk","created":1699012345,"model":"llama-3.1-70b-versatile","choices":[{"index":0,"delta":{},"finish_reason":"stop"}]}

data: [DONE]

```

---

## Error Handling

All errors follow the OpenAI error format with Synaxis-specific error codes.

### Error Format

```json
{
  "error": {
    "message": "Error description",
    "type": "error_type",
    "param": "parameter_name",
    "code": "error_code"
  }
}
```

### Error Codes

| Error Code | HTTP Status | Description |
|------------|-------------|-------------|
| `invalid_request_error` | 400 | Invalid request format or parameters |
| `invalid_value` | 400 | Specific parameter has invalid value |
| `authentication_error` | 401 | Invalid or missing authentication |
| `authorization_error` | 403 | Insufficient permissions |
| `not_found` | 404 | Resource not found |
| `rate_limit_exceeded` | 429 | Rate limit exceeded |
| `upstream_routing_failure` | 502 | All providers failed |
| `provider_error` | 502 | Specific provider error |
| `service_unavailable` | 503 | Service temporarily unavailable |
| `internal_error` | 500 | Unexpected server error |

For detailed error troubleshooting, see [Error Reference](reference/errors.md).

### Common Error Examples

**Invalid Model:**
```json
{
  "error": {
    "message": "The model 'invalid-model' does not exist",
    "type": "invalid_request_error",
    "param": "model",
    "code": "model_not_found"
  }
}
```

**Rate Limit Exceeded:**
```json
{
  "error": {
    "message": "Rate limit exceeded. Please try again later.",
    "type": "rate_limit_error",
    "code": "rate_limit_exceeded"
  }
}
```

**Provider Error:**
```json
{
  "error": {
    "message": "An error occurred while processing your request.",
    "type": "api_error",
    "code": "provider_error"
  }
}
```

---

## Rate Limiting

Synaxis implements rate limiting to protect against abuse and ensure fair usage.

### Rate Limit Headers

```
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 999
X-RateLimit-Reset: 1642428789
X-RateLimit-Window: 60
```

### Rate Limit Tiers

| Endpoint Type | Limit |
|---------------|-------|
| OpenAI-Compatible | 1000 requests/minute |
| Admin | 100 requests/minute |
| Identity | 50 requests/minute |

When rate limits are exceeded, the response includes:
```
Retry-After: 60
```

---

## SDK Examples

### JavaScript/Node.js

```javascript
const response = await fetch('http://localhost:8080/openai/v1/chat/completions', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    model: 'llama-3.1-70b-versatile',
    messages: [{ role: 'user', content: 'Hello!' }],
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
      if (data === '[DONE]') return;
      
      try {
        const parsed = JSON.parse(data);
        console.log('Received:', parsed.choices[0].delta.content);
      } catch (e) {
        console.error('Parse error:', data);
      }
    }
  }
}
```

### Python

```python
import requests
import json

response = requests.post(
    'http://localhost:8080/openai/v1/chat/completions',
    json={
        'model': 'llama-3.1-70b-versatile',
        'messages': [{'role': 'user', 'content': 'Hello!'}],
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
            try:
                chunk = json.loads(data)
                print(chunk['choices'][0]['delta'].get('content', ''))
            except json.JSONDecodeError:
                pass
```

### cURL

```bash
# Non-streaming
curl -X POST http://localhost:8080/openai/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "llama-3.1-70b-versatile",
    "messages": [{"role": "user", "content": "Hello!"}]
  }'

# Streaming
curl -X POST http://localhost:8080/openai/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "llama-3.1-70b-versatile",
    "messages": [{"role": "user", "content": "Tell me a story"}],
    "stream": true
  }'
```

### OpenAI Python SDK

```python
from openai import OpenAI

client = OpenAI(
    base_url="http://localhost:8080/openai/v1",
    api_key="not-needed"  # Synaxis doesn't require API keys for public endpoints
)

# Non-streaming
response = client.chat.completions.create(
    model="llama-3.1-70b-versatile",
    messages=[{"role": "user", "content": "Hello!"}]
)
print(response.choices[0].message.content)

# Streaming
for chunk in client.chat.completions.create(
    model="llama-3.1-70b-versatile",
    messages=[{"role": "user", "content": "Hello!"}],
    stream=True
):
    content = chunk.choices[0].delta.content
    if content:
        print(content, end="")
```

---

## Schemas

### Chat Completion Request

```json
{
  "type": "object",
  "required": ["model", "messages"],
  "properties": {
    "model": { "type": "string" },
    "messages": {
      "type": "array",
      "items": {
        "type": "object",
        "required": ["role", "content"],
        "properties": {
          "role": { "type": "string", "enum": ["system", "user", "assistant", "tool"] },
          "content": { "type": ["string", "array"] },
          "name": { "type": "string" },
          "tool_calls": { "type": "array" }
        }
      }
    },
    "stream": { "type": "boolean" },
    "temperature": { "type": "number", "minimum": 0, "maximum": 2 },
    "max_tokens": { "type": "integer", "minimum": 1 },
    "top_p": { "type": "number", "minimum": 0, "maximum": 1 },
    "tools": { "type": "array" },
    "tool_choice": { "type": ["string", "object"] },
    "response_format": { "type": "object" },
    "stop": { "type": ["string", "array"] }
  }
}
```

### Chat Completion Response

```json
{
  "type": "object",
  "properties": {
    "id": { "type": "string" },
    "object": { "type": "string", "enum": ["chat.completion"] },
    "created": { "type": "integer" },
    "model": { "type": "string" },
    "choices": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "index": { "type": "integer" },
          "message": {
            "type": "object",
            "properties": {
              "role": { "type": "string", "enum": ["assistant"] },
              "content": { "type": "string" }
            }
          },
          "finish_reason": { "type": "string", "enum": ["stop", "length", "content_filter", "tool_calls"] }
        }
      }
    },
    "usage": {
      "type": "object",
      "properties": {
        "prompt_tokens": { "type": "integer" },
        "completion_tokens": { "type": "integer" },
        "total_tokens": { "type": "integer" }
      }
    }
  }
}
```

---

## ULTRA MISER MODE™ Tips

1. **Use Aliases**: Configure model aliases to abstract provider-specific model names
2. **Enable Streaming**: Streaming responses feel faster and use the same quota
3. **Monitor Health**: Check `/admin/health` to see which providers are currently healthy
4. **Tier Strategy**: Configure providers by tier—free tiers (0-1) are tried before paid tiers (2-3)
5. **Token Efficiency**: Use `max_tokens` to prevent runaway generation from consuming quotas

> *"The best API call is the one that costs nothing. The second best is the one that costs almost nothing."* — ULTRA MISER MODE™ Principle #42

---

## See Also

- [Architecture Overview](ARCHITECTURE.md) — Clean Architecture and routing deep dive
- [Configuration Guide](CONFIGURATION.md) — Provider setup and configuration
- [Error Reference](reference/errors.md) — Detailed error codes and troubleshooting
- [Providers Reference](reference/providers.md) — Provider-specific details and capabilities
