# Synaxis API Documentation

**Last Updated**: 2026-02-01
**Version**: 1.0.0

## Overview

Synaxis provides an OpenAI-compatible inference gateway that routes requests to multiple AI providers. This document describes all available API endpoints, authentication requirements, and response formats.

**Total Endpoints Documented**: 15+ endpoints across 6 categories

### Endpoint Categories

| Category | Endpoints | Authentication |
|----------|-----------|----------------|
| OpenAI Compatible | 5 | None |
| Identity Management | 3 | None |
| Antigravity OAuth | 4 | None |
| Health Checks | 2 | None |
| Admin Management | 3 | JWT Required |
| Auth & API Keys | 3 | JWT Required |

## Base URLs

| Environment | WebAPI URL | WebApp URL |
|-------------|------------|------------|
| Development | `http://localhost:5000` | `http://localhost:5001` |
| Production | `https://api.synaxis.io` | `https://app.synaxis.io` |

## Authentication

All admin endpoints require JWT authentication. Use the development login endpoint to obtain a token.

### Obtaining a JWT Token (Development)

```bash
curl -X POST http://localhost:5000/auth/dev-login \
  -H "Content-Type: application/json" \
  -d '{"email": "admin@example.com"}'
```

Response:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2026-02-01T12:00:00Z"
}
```

### Using the Token

Include the token in the Authorization header:
```
Authorization: Bearer <token>
```

## Chat Completions

### POST /openai/v1/chat/completions

Send a chat completion request to generate AI responses.

**Request Headers**:
| Header | Required | Description |
|--------|----------|-------------|
| Content-Type | Yes | Must be `application/json` |
| Authorization | No | Required for admin models |

**Request Body**:
```json
{
  "model": "gpt-4",
  "messages": [
    {
      "role": "user",
      "content": "Hello, how are you?"
    }
  ],
  "temperature": 0.7,
  "max_tokens": 1000,
  "stream": false,
  "tools": []
}
```

**Parameters**:
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| model | string | Yes | - | Model ID (e.g., `gpt-4`, `claude-3-opus`) |
| messages | array | Yes | - | Array of message objects |
| temperature | number | No | 0.7 | Randomness (0-2) |
| max_tokens | number | No | - | Maximum tokens to generate |
| stream | boolean | No | false | Enable streaming response |
| tools | array | No | [] | Tool definitions for function calling |

**Non-Streaming Response**:
```json
{
  "id": "chatcmpl-abc123",
  "object": "chat.completion",
  "created": 1699000000,
  "model": "gpt-4",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": "Hello! I'm doing well, thank you for asking."
      },
      "finish_reason": "stop"
    }
  ],
  "usage": {
    "prompt_tokens": 15,
    "completion_tokens": 12,
    "total_tokens": 27
  }
}
```

**Streaming Response (SSE)**:
```
data: {"id":"chatcmpl-abc123","object":"chat.completion.chunk","created":1699000000,"model":"gpt-4","choices":[{"index":0,"delta":{"content":"Hello"},"finish_reason":null}]}

data: {"id":"chatcmpl-abc123","object":"chat.completion.chunk","created":1699000000,"model":"gpt-4","choices":[{"index":0,"delta":{"content":"!"},"finish_reason":null}]}

data: [DONE]
```

### POST /openai/v1/completions

Legacy completion endpoint (for backward compatibility).

**Request Body**:
```json
{
  "model": "text-davinci-003",
  "prompt": "Once upon a time",
  "max_tokens": 100,
  "temperature": 0.7,
  "stream": false
}
```

## Models

### GET /openai/v1/models

List all available models grouped by provider.

**Response**:
```json
{
  "object": "list",
  "data": [
    {
      "id": "gpt-4",
      "object": "model",
      "created": 1699000000,
      "owned_by": "openai",
      "provider": "openai",
      "model_path": "gpt-4",
      "capabilities": {
        "streaming": true,
        "tools": true,
        "vision": true,
        "structured_output": true,
        "log_probs": true
      }
    }
  ],
  "providers": [
    {
      "id": "openai",
      "type": "OpenAI",
      "enabled": true,
      "tier": 1
    }
  ]
}
```

### GET /openai/v1/models/{id}

Get details for a specific model.

**Response**:
```json
{
  "id": "gpt-4",
  "object": "model",
  "created": 1699000000,
  "owned_by": "openai",
  "provider": "openai",
  "model_path": "gpt-4",
  "capabilities": {
    "streaming": true,
    "tools": true,
    "vision": true,
    "structured_output": true,
    "log_probs": true
  }
}
```

## Responses API

### POST /openai/v1/responses

OpenAI Responses API endpoint (supports both streaming and non-streaming).

**Request Body**:
```json
{
  "input": "Summarize the following text...",
  "model": "gpt-4o",
  "text": {
    "format": {
      "type": "text"
    }
  },
  "max_output_tokens": 1000
}
```

**Response**:
```json
{
  "id": "resp_abc123",
  "object": "response",
  "created": 1699000000,
  "output": [
    {
      "id": "msg_123",
      "object": "message",
      "content": [
        {
          "type": "output_text",
          "text": "Here is the summary..."
        }
      ]
    }
  ]
}
```

**Streaming Response (SSE)**:
```
data: {"id":"resp_abc123","object":"response","created":1699000000,"output":[{"id":"msg_123","object":"message","content":[{"type":"output_text","text":"Here is"}]}]}

data: {"id":"resp_abc123","object":"response","created":1699000000,"output":[{"id":"msg_123","object":"message","content":[{"type":"output_text","text":" the summary"}]}]}

data: [DONE]
```

## Identity Management

### POST /api/identity/{provider}/start

Start authentication flow for an identity provider.

**Path Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| provider | string | Yes | Identity provider name (e.g., "google", "github") |

**Request Body**: None

**Response**:
```json
{
  "authUrl": "https://provider.com/oauth/authorize?...",
  "state": "random_state_string",
  "instructions": "Complete authentication in the opened window"
}
```

### POST /api/identity/{provider}/complete

Complete authentication flow for an identity provider.

**Path Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| provider | string | Yes | Identity provider name |

**Request Body**:
```json
{
  "code": "authorization_code_from_provider",
  "state": "state_from_start_endpoint"
}
```

**Response**:
```json
{
  "success": true,
  "account": {
    "id": "user_123",
    "email": "user@example.com",
    "provider": "google"
  }
}
```

### GET /api/identity/accounts

List all connected identity accounts.

**Response**:
```json
[
  {
    "id": "user_123",
    "email": "user@example.com",
    "provider": "google",
    "connectedAt": "2026-02-01T12:00:00Z"
  }
]
```

## Antigravity OAuth Integration

### GET /oauth/antigravity/callback

OAuth callback endpoint for Antigravity authentication.

**Query Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| code | string | Yes | Authorization code |
| state | string | Yes | State parameter |

**Response**: HTML page with success/failure message

### GET /antigravity/accounts

List all connected Antigravity accounts.

**Response**:
```json
[
  {
    "id": "ag_account_123",
    "email": "user@example.com",
    "connectedAt": "2026-02-01T12:00:00Z"
  }
]
```

### POST /antigravity/auth/start

Start Antigravity authentication flow.

**Request Body**:
```json
{
  "redirectUrl": "https://your-app.com/callback"
}
```

**Response**:
```json
{
  "authUrl": "https://antigravity.com/auth?...",
  "redirectUrl": "https://your-app.com/callback",
  "instructions": "Complete authentication and return to redirectUrl"
}
```

### POST /antigravity/auth/complete

Complete Antigravity authentication flow.

**Request Body**:
```json
{
  "code": "authorization_code",
  "state": "state_parameter",
  "redirectUrl": "https://your-app.com/callback",
  "callbackUrl": "https://your-app.com/api/callback"
}
```

**Response**:
```json
{
  "success": true,
  "message": "Authentication completed successfully"
}
```

## Health Checks

### GET /health/liveness

Liveness probe endpoint for container orchestration.

**Response**:
```json
{
  "status": "healthy",
  "timestamp": "2026-02-01T12:00:00Z"
}
```

### GET /health/readiness

Readiness probe endpoint that checks dependencies.

**Response**:
```json
{
  "status": "ready",
  "timestamp": "2026-02-01T12:00:00Z",
  "checks": {
    "database": "healthy",
    "redis": "healthy",
    "providers": "healthy"
  }
}
```

## Authentication & API Keys

### POST /auth/dev-login

Development-only authentication endpoint for testing.

**Request Body**:
```json
{
  "email": "admin@example.com"
}
```

**Response**:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2026-02-01T12:00:00Z"
}
```

### POST /projects/{projectId}/keys

Create a new API key for a project.

**Path Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| projectId | string | Yes | Project identifier |

**Headers**:
| Header | Required | Description |
|--------|----------|-------------|
| Authorization | Yes | `Bearer <token>` |

**Request Body**:
```json
{
  "name": "Production Key"
}
```

**Response**:
```json
{
  "id": "key_123",
  "key": "sk-proj-abc123...",
  "name": "Production Key",
  "createdAt": "2026-02-01T12:00:00Z"
}
```

### DELETE /projects/{projectId}/keys/{keyId}

Delete an API key from a project.

**Path Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| projectId | string | Yes | Project identifier |
| keyId | string | Yes | API key identifier |

**Headers**:
| Header | Required | Description |
|--------|----------|-------------|
| Authorization | Yes | `Bearer <token>` |

**Response**: 204 No Content

## Admin Endpoints

All admin endpoints require JWT authentication.

### GET /admin/providers

List all configured providers and their status.

**Headers**:
| Header | Required | Description |
|--------|----------|-------------|
| Authorization | Yes | `Bearer <token>` |

**Response**:
```json
[
  {
    "id": "openai",
    "type": "OpenAI",
    "enabled": true,
    "tier": 1,
    "defaultModel": "gpt-4",
    "models": ["gpt-4", "gpt-4-turbo", "gpt-3.5-turbo"]
  },
  {
    "id": "anthropic",
    "type": "Anthropic",
    "enabled": true,
    "tier": 1,
    "defaultModel": "claude-3-opus-20240229",
    "models": ["claude-3-opus-20240229", "claude-3-sonnet-20240229"]
  }
]
```

### PUT /admin/providers/{providerId}

Update provider configuration.

**Request Body**:
```json
{
  "enabled": true,
  "tier": 2,
  "apiKey": "sk-...",
  "defaultModel": "gpt-4-turbo"
}
```

**Response**:
```json
{
  "id": "openai",
  "type": "OpenAI",
  "enabled": true,
  "tier": 2,
  "message": "Provider updated successfully"
}
```

### GET /admin/health

Get detailed health status of all providers.

**Response**:
```json
{
  "status": "healthy",
  "timestamp": "2026-02-01T12:00:00Z",
  "providers": {
    "openai": {
      "status": "healthy",
      "latencyMs": 45,
      "lastChecked": "2026-02-01T12:00:00Z"
    },
    "anthropic": {
      "status": "healthy",
      "latencyMs": 120,
      "lastChecked": "2026-02-01T12:00:00Z"
    }
  },
  "circuitBreakers": {
    "openai": "closed",
    "anthropic": "closed"
  }
}
```

## Streaming Format

Synaxis supports Server-Sent Events (SSE) streaming for real-time responses. When `stream: true` is set in the request, responses are delivered incrementally.

### SSE Format

All streaming responses use the following format:

```
data: {json_chunk_1}

data: {json_chunk_2}

data: [DONE]
```

### Streaming Headers

Streaming responses include these headers:
- `Content-Type: text/event-stream`
- `Cache-Control: no-cache`
- `Connection: keep-alive`

### Streaming Examples

**Chat Completions Streaming**:
```
data: {"id":"chatcmpl-abc123","object":"chat.completion.chunk","created":1699000000,"model":"gpt-4","choices":[{"index":0,"delta":{"content":"Hello"},"finish_reason":null}]}

data: {"id":"chatcmpl-abc123","object":"chat.completion.chunk","created":1699000000,"model":"gpt-4","choices":[{"index":0,"delta":{"content":"!"},"finish_reason":null}]}

data: [DONE]
```

**Responses API Streaming**:
```
data: {"id":"resp_abc123","object":"response","created":1699000000,"output":[{"id":"msg_123","object":"message","content":[{"type":"output_text","text":"Here is"}]}]}

data: {"id":"resp_abc123","object":"response","created":1699000000,"output":[{"id":"msg_123","object":"message","content":[{"type":"output_text","text":" the summary"}]}]}

data: [DONE]
```

## Error Responses

### Standard Error Format

```json
{
  "error": {
    "message": "Invalid model ID",
    "type": "invalid_request_error",
    "param": "model",
    "code": "model_not_found"
  }
}
```

### HTTP Status Codes

| Code | Description |
|------|-------------|
| 200 | Success |
| 201 | Created (for POST endpoints) |
| 204 | No Content (for DELETE endpoints) |
| 400 | Bad Request (invalid parameters) |
| 401 | Unauthorized (missing/invalid token) |
| 403 | Forbidden (insufficient permissions) |
| 404 | Not Found (resource doesn't exist) |
| 429 | Rate Limited |
| 500 | Internal Server Error |
| 503 | Service Unavailable (all providers down) |

### Error Types

| Type | Description |
|------|-------------|
| `invalid_request_error` | Missing or invalid parameters |
| `authentication_error` | Authentication failed |
| `permission_error` | Insufficient permissions |
| `rate_limit_error` | Rate limit exceeded |
| `server_error` | Internal server error |
| `upstream_routing_failure` | All providers failed |
| `provider_error` | Upstream provider error |
| `model_not_found` | Requested model not available |
| `quota_exceeded` | Provider quota exceeded |

### Provider-Specific Errors

When upstream providers return errors, they are wrapped with additional context:

```json
{
  "error": {
    "message": "Provider request failed",
    "type": "provider_error",
    "provider": "openai",
    "original_error": {
      "message": "Rate limit exceeded",
      "type": "rate_limit_error"
    }
  }
}
```

## Rate Limiting

Synaxis implements rate limiting per provider. Limits are configurable per tier.

| Tier | Requests/Minute | Tokens/Minute |
|------|-----------------|---------------|
| 1 (Primary) | 1000 | 100,000 |
| 2 (Secondary) | 500 | 50,000 |
| 3 (Backup) | 100 | 10,000 |

## Supported Providers

| Provider | Type | Models | Tier |
|----------|------|--------|------|
| OpenAI | OpenAI | gpt-4, gpt-4-turbo, gpt-3.5-turbo | 1 |
| Anthropic | Anthropic | claude-3-opus, claude-3-sonnet | 1 |
| Google | VertexAI | gemini-pro, gemini-ultra | 1 |
| Groq | Groq | llama-3.3-70b, mixtral-8x7b | 1 |
| Cohere | Cohere | command-r-plus | 2 |
| Cloudflare | Workers AI | @cf/meta/llama-3.1-8b | 2 |
| HuggingFace | Inference | microsoft/Phi-3.5-mini | 3 |

## Response Headers

All responses include the following headers:

| Header | Description |
|--------|-------------|
| x-request-id | Unique request identifier |
| x-gateway-model-requested | Original model requested |
| x-gateway-model-resolved | Actual model used |
| x-gateway-provider | Provider that handled the request |

## Request/Response Schemas

### Chat Completion Request Schema

```json
{
  "model": "string (required)",
  "messages": [
    {
      "role": "user|assistant|system",
      "content": "string or array",
      "name": "string (optional)",
      "tool_calls": "array (optional)"
    }
  ],
  "temperature": "number (0-2, optional)",
  "max_tokens": "integer (optional)",
  "top_p": "number (0-1, optional)",
  "stream": "boolean (default: false)",
  "tools": [
    {
      "type": "function",
      "function": {
        "name": "string",
        "description": "string (optional)",
        "parameters": "object (optional)"
      }
    }
  ],
  "tool_choice": "string|object (optional)",
  "response_format": "object (optional)",
  "stop": "string|array (optional)"
}
```

### Chat Completion Response Schema

```json
{
  "id": "string",
  "object": "chat.completion",
  "created": "integer (timestamp)",
  "model": "string",
  "choices": [
    {
      "index": "integer",
      "message": {
        "role": "assistant",
        "content": "string",
        "tool_calls": "array (optional)"
      },
      "finish_reason": "stop|length|content_filter|null"
    }
  ],
  "usage": {
    "prompt_tokens": "integer",
    "completion_tokens": "integer",
    "total_tokens": "integer"
  }
}
```

### Provider Admin DTO Schema

```json
{
  "id": "string",
  "name": "string",
  "type": "string",
  "enabled": "boolean",
  "tier": "integer",
  "endpoint": "string (optional)",
  "keyConfigured": "boolean",
  "models": [
    {
      "id": "string",
      "name": "string",
      "enabled": "boolean"
    }
  ],
  "status": "string",
  "latency": "integer (optional)"
}
```

## Changelog

### v1.0.1 (2026-02-01)
- Enhanced API documentation with comprehensive endpoint coverage
- Added identity management endpoints documentation
- Added Antigravity OAuth integration endpoints
- Added health check endpoints documentation
- Added authentication & API key management endpoints
- Enhanced error response documentation
- Added streaming format specifications
- Added request/response schema definitions

### v1.0.0 (2026-02-01)
- Initial API documentation
- OpenAI-compatible endpoints
- Multi-provider routing
- Admin management endpoints
- Streaming support
