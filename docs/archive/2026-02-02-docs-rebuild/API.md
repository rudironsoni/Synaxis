# Synaxis API Documentation

## Introduction

Synaxis provides a unified, OpenAI-compatible API for accessing multiple AI inference providers through a single endpoint. The API supports real-time streaming, comprehensive error handling, and intelligent provider routing with automatic failover.

### OpenAI Compatibility

Synaxis is fully compatible with the OpenAI API specification, allowing you to use existing OpenAI clients with minimal configuration changes. Simply point your client to the Synaxis gateway URL instead of OpenAI's endpoints.

### Base URL

```
http://localhost:8080/openai/v1
```

### API Version

Current API version: `v1`

---

## Authentication

### JWT Authentication

Admin endpoints require JWT (JSON Web Token) authentication. Include the token in the Authorization header:

```
Authorization: Bearer <your-jwt-token>
```

### Development Login

For development and testing purposes, use the dev-login endpoint to obtain a JWT token:

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

### Token Generation

Tokens are generated using the configured JWT secret and include user identity and tenant information. Tokens expire after the configured duration.

---

## OpenAI-Compatible Endpoints

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
- `model` (string, required): The model identifier
- `messages` (array, required): Array of message objects with `role` and `content`
- `stream` (boolean, optional): Enable streaming response (default: false)
- `temperature` (number, optional): Sampling temperature (0-2)
- `max_tokens` (integer, optional): Maximum tokens to generate
- `top_p` (number, optional): Nucleus sampling parameter (0-1)
- `tools` (array, optional): Available tools/functions
- `tool_choice` (string, optional): Tool selection strategy
- `response_format` (object, optional): Response format specification
- `stop` (string/array, optional): Stop sequences

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

### Legacy Completions

#### POST /openai/v1/completions

**⚠️ Deprecated:** This endpoint is deprecated and maintained for backward compatibility only.

**Authentication:** None required

**Request:**
```json
{
  "model": "llama-3.1-70b-versatile",
  "prompt": "Once upon a time",
  "max_tokens": 100,
  "temperature": 0.7,
  "stream": false
}
```

**Response:**
```json
{
  "id": "cmpl-123",
  "object": "text_completion",
  "created": 1699012345,
  "model": "llama-3.1-70b-versatile",
  "choices": [
    {
      "text": "Once upon a time, in a land far away...",
      "index": 0,
      "finish_reason": "stop"
    }
  ],
  "usage": {
    "prompt_tokens": 5,
    "completion_tokens": 25,
    "total_tokens": 30
  }
}
```

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

## Admin Endpoints

All admin endpoints require JWT authentication via the Authorization header.

### Provider Management

#### GET /admin/providers

Returns a list of all configured AI providers with their settings and status.

**Authentication:** JWT token required

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
    "keyConfigured": true,
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

**Authentication:** JWT token required

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

### Health Monitoring

#### GET /admin/health

Returns detailed health information about services and AI providers.

**Authentication:** JWT token required

**Response:**
```json
{
  "services": [
    {
      "name": "API Gateway",
      "status": "healthy",
      "lastChecked": "2024-01-15T10:30:00Z"
    },
    {
      "name": "PostgreSQL",
      "status": "healthy",
      "latency": 15,
      "lastChecked": "2024-01-15T10:30:00Z"
    },
    {
      "name": "Redis",
      "status": "healthy",
      "latency": 5,
      "lastChecked": "2024-01-15T10:30:00Z"
    }
  ],
  "providers": [
    {
      "id": "groq",
      "name": "groq",
      "status": "online",
      "lastChecked": "2024-01-15T10:30:00Z",
      "successRate": 98.5,
      "latency": 45
    }
  ],
  "overallStatus": "healthy",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

---

## Health Check Endpoints

### Liveness Check

#### GET /health/liveness

Simple liveness check that always returns healthy if the service is running.

**Authentication:** None required

**Response:**
```json
{
  "status": "healthy"
}
```

### Readiness Check

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

## Identity Management Endpoints

### OAuth Identity

#### POST /api/identity/{provider}/start

Start OAuth authentication flow for a specific provider.

**Authentication:** None required

**Response:**
```json
{
  "authUrl": "https://provider.com/oauth/authorize?...",
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
    "accessToken": "abcd....1234"
  }
]
```

---

## Antigravity OAuth Endpoints

### Authentication Flow

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
  "redirectUrl": "http://localhost:51121/oauth/antigravity/callback"
}
```

**Response:**
```json
{
  "authUrl": "https://antigravity.com/auth?...",
  "redirectUrl": "http://localhost:51121/oauth/antigravity/callback",
  "instructions": "Open AuthUrl in your browser. After login, you will be redirected to the callback; no manual code copy is required."
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
  "redirectUrl": "http://localhost:51121/oauth/antigravity/callback"
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

**Response:**
```json
[
  {
    "id": "account-id",
    "provider": "antigravity",
    "email": "user@example.com"
  }
]
```

---

## Streaming Format

Synaxis uses Server-Sent Events (SSE) for streaming responses. The format follows the OpenAI specification:

### SSE Headers

Streaming responses include these headers:
```
Content-Type: text/event-stream
Cache-Control: no-cache
Connection: keep-alive
```

### Data Frames

Each chunk is sent as a separate data frame:
```
data: {"id":"chatcmpl-123","object":"chat.completion.chunk","created":1699012345,"model":"llama-3.1-70b-versatile","choices":[{"index":0,"delta":{"content":"Hello"},"finish_reason":null}]}

```

### End Marker

The stream ends with a special marker:
```
data: [DONE]

```

### Streaming Example

Complete streaming response example:
```
data: {"id":"chatcmpl-123","object":"chat.completion.chunk","created":1699012345,"model":"llama-3.1-70b-versatile","choices":[{"index":0,"delta":{"role":"assistant","content":"Hello"},"finish_reason":null}]}

data: {"id":"chatcmpl-123","object":"chat.completion.chunk","created":1699012345,"model":"llama-3.1-70b-versatile","choices":[{"index":0,"delta":{"content":" world"},"finish_reason":null}]}

data: {"id":"chatcmpl-123","object":"chat.completion.chunk","created":1699012345,"model":"llama-3.1-70b-versatile","choices":[{"index":0,"delta":{"content":"!"},"finish_reason":null}]}

data: {"id":"chatcmpl-123","object":"chat.completion.chunk","created":1699012345,"model":"llama-3.1-70b-versatile","choices":[{"index":0,"delta":{},"finish_reason":"stop"}]}

data: [DONE]

```

---

## Error Responses

All errors follow the OpenAI error format with additional Synaxis-specific error codes.

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

**Authentication Error:**
```json
{
  "error": {
    "message": "Authentication failed. Please check your credentials.",
    "type": "authentication_error",
    "code": "authentication_error"
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

Synaxis implements rate limiting to protect against abuse and ensure fair usage across all users.

### Rate Limit Headers

Rate limit information is included in response headers:

```
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 999
X-RateLimit-Reset: 1642428789
X-RateLimit-Window: 60
```

### Rate Limit Tiers

Different endpoints may have different rate limits:

- **OpenAI-Compatible Endpoints:** 1000 requests per minute
- **Admin Endpoints:** 100 requests per minute
- **Identity Endpoints:** 50 requests per minute

### Rate Limit Exceeded Response

When rate limits are exceeded:

```json
{
  "error": {
    "message": "Rate limit exceeded. Please try again later.",
    "type": "rate_limit_error",
    "code": "rate_limit_exceeded"
  }
}
```

**Response Headers:**
```
Retry-After: 60
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 0
X-RateLimit-Reset: 1642428789
```

---

## Security Headers

Synaxis implements comprehensive security headers to protect against common web vulnerabilities.

### Implemented Security Headers

| Header | Value | Purpose |
|--------|-------|---------|
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | Force HTTPS connections |
| `Content-Security-Policy` | Configured CSP policy | Prevent XSS attacks |
| `X-Frame-Options` | `DENY` | Prevent clickjacking |
| `X-Content-Type-Options` | `nosniff` | Prevent MIME type sniffing |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | Control referrer information |
| `X-XSS-Protection` | `1; mode=block` | Enable XSS protection |

### CORS Policy

Synaxis implements CORS (Cross-Origin Resource Sharing) with the following policies:

- **PublicAPI:** Allows all origins for OpenAI-compatible endpoints
- **WebApp:** Restricted to admin interface origins
- **Admin:** JWT-authenticated admin endpoints

---

## Request/Response Schemas

### Chat Completion Request Schema

```json
{
  "type": "object",
  "required": ["model", "messages"],
  "properties": {
    "model": {
      "type": "string",
      "description": "Model identifier"
    },
    "messages": {
      "type": "array",
      "items": {
        "type": "object",
        "required": ["role", "content"],
        "properties": {
          "role": {
            "type": "string",
            "enum": ["system", "user", "assistant", "tool"],
            "description": "Message role"
          },
          "content": {
            "type": ["string", "array"],
            "description": "Message content"
          },
          "name": {
            "type": "string",
            "description": "Message sender name"
          },
          "tool_calls": {
            "type": "array",
            "description": "Tool calls made by assistant"
          }
        }
      }
    },
    "stream": {
      "type": "boolean",
      "description": "Enable streaming response"
    },
    "temperature": {
      "type": "number",
      "minimum": 0,
      "maximum": 2,
      "description": "Sampling temperature"
    },
    "max_tokens": {
      "type": "integer",
      "minimum": 1,
      "description": "Maximum tokens to generate"
    },
    "top_p": {
      "type": "number",
      "minimum": 0,
      "maximum": 1,
      "description": "Nucleus sampling parameter"
    },
    "tools": {
      "type": "array",
      "description": "Available tools/functions"
    },
    "tool_choice": {
      "type": ["string", "object"],
      "description": "Tool selection strategy"
    },
    "response_format": {
      "type": "object",
      "description": "Response format specification"
    },
    "stop": {
      "type": ["string", "array"],
      "description": "Stop sequences"
    }
  }
}
```

### Chat Completion Response Schema

```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "description": "Unique identifier for the completion"
    },
    "object": {
      "type": "string",
      "enum": ["chat.completion"],
      "description": "Object type"
    },
    "created": {
      "type": "integer",
      "description": "Unix timestamp of creation"
    },
    "model": {
      "type": "string",
      "description": "Model used for completion"
    },
    "choices": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "index": {
            "type": "integer",
            "description": "Choice index"
          },
          "message": {
            "type": "object",
            "properties": {
              "role": {
                "type": "string",
                "enum": ["assistant"],
                "description": "Message role"
              },
              "content": {
                "type": "string",
                "description": "Message content"
              }
            }
          },
          "finish_reason": {
            "type": "string",
            "enum": ["stop", "length", "content_filter", "tool_calls"],
            "description": "Reason for completion"
          }
        }
      }
    },
    "usage": {
      "type": "object",
      "properties": {
        "prompt_tokens": {
          "type": "integer",
          "description": "Tokens in prompt"
        },
        "completion_tokens": {
          "type": "integer",
          "description": "Tokens in completion"
        },
        "total_tokens": {
          "type": "integer",
          "description": "Total tokens used"
        }
      }
    }
  }
}
```

### Model Schema

```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "description": "Model identifier"
    },
    "object": {
      "type": "string",
      "enum": ["model"],
      "description": "Object type"
    },
    "created": {
      "type": "integer",
      "description": "Unix timestamp of creation"
    },
    "owned_by": {
      "type": "string",
      "description": "Model owner"
    },
    "provider": {
      "type": "string",
      "description": "Provider name"
    },
    "model_path": {
      "type": "string",
      "description": "Provider-specific model path"
    },
    "capabilities": {
      "type": "object",
      "properties": {
        "streaming": {
          "type": "boolean",
          "description": "Supports streaming responses"
        },
        "tools": {
          "type": "boolean",
          "description": "Supports function calling"
        },
        "vision": {
          "type": "boolean",
          "description": "Supports image input"
        },
        "structured_output": {
          "type": "boolean",
          "description": "Supports structured output"
        },
        "log_probs": {
          "type": "boolean",
          "description": "Supports log probability"
        }
      }
    }
  }
}
```

---

## SDK Integration Examples

### JavaScript/Node.js

```javascript
const response = await fetch('http://localhost:8080/openai/v1/chat/completions', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
  },
  body: JSON.stringify({
    model: 'llama-3.1-70b-versatile',
    messages: [
      { role: 'user', content: 'Hello, world!' }
    ],
    stream: true
  })
});

// Handle streaming response
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
      if (data === '[DONE]') {
        console.log('Stream completed');
        return;
      }
      
      try {
        const parsed = JSON.parse(data);
        console.log('Received chunk:', parsed);
      } catch (e) {
        console.error('Failed to parse chunk:', data);
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
        'messages': [
            {'role': 'user', 'content': 'Hello, world!'}
        ],
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
                print('Stream completed')
                break
            
            try:
                chunk = json.loads(data)
                print('Received chunk:', chunk)
            except json.JSONDecodeError:
                print(f'Failed to parse chunk: {data}')
```

### cURL

```bash
# Non-streaming request
curl -X POST http://localhost:8080/openai/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "llama-3.1-70b-versatile",
    "messages": [
      { "role": "user", "content": "Hello, world!" }
    ]
  }'

# Streaming request
curl -X POST http://localhost:8080/openai/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "llama-3.1-70b-versatile",
    "messages": [
      { "role": "user", "content": "Tell me a story..." }
    ],
    "stream": true
  }'
```

---

## Changelog

### Version 1.0.0

- Initial release
- OpenAI-compatible chat completions endpoint
- Streaming support via Server-Sent Events
- Admin endpoints for provider management
- Health check endpoints
- Identity management endpoints
- Comprehensive error handling
- Rate limiting and security headers

---

## Support

For questions, issues, or contributions:

- **Documentation:** This file and inline code comments
- **Issues:** GitHub Issues (if available)
- **API Testing:** Use the provided examples and test with your preferred HTTP client

---

*Last updated: January 2024*
*API Version: v1*