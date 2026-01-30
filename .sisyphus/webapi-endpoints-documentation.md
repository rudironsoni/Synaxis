# Synaxis WebAPI Endpoints Documentation

**Generated:** $(date)  
**Source:** `/src/InferenceGateway/WebApi/`  
**Configuration:** `appsettings.json`, `Program.cs`

## Overview

Synaxis exposes a unified, OpenAI-compatible API gateway that routes requests to 13+ AI providers. The API follows REST principles with OpenAPI 3.0 specification and supports both streaming and non-streaming responses.

## Base Configuration

- **Base Path**: `/` (development: `http://localhost:5000`, production: `https://api.synaxis.io`)
- **Authentication**: JWT Bearer tokens (see Auth section)
- **Content Types**: `application/json`, `text/event-stream` (for streaming)
- **API Version**: `v1` (OpenAI-compatible)

---

## 1. Core AI Endpoints (`/openai/`)

### 1.1 Chat Completions - **PRIMARY ENDPOINT**
```http
POST /openai/v1/chat/completions
```

**Description**: Main OpenAI-compatible chat completion endpoint  
**Authentication**: Bearer token required  
**Streaming**: ✅ Full SSE support  
**Schema**: OpenAI Chat Completions v1 format

**Request Body**:
```json
{
  "model": "llama-3.1-70b-versatile", 
  "messages": [
    {"role": "user", "content": "Hello!"}
  ],
  "stream": false,
  "temperature": 0.7,
  "max_tokens": 1000
}
```

**Streaming Response** (`stream: true`):
```
Content-Type: text/event-stream

data: {"id":"chatcmpl-xxx","object":"chat.completion.chunk",...}
data: {"id":"chatcmpl-xxx","object":"chat.completion.chunk",...}
data: {"id":"chatcmpl-xxx","object":"chat.completion.chunk",...}
data: [DONE]
```

**Non-Streaming Response** (`stream: false`):
```json
{
  "id": "chatcmpl-xxx",
  "object": "chat.completion",
  "created": 1234567890,
  "model": "llama-3.1-70b-versatile",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": "Hello! How can I help you?"
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

**Implementation Details**:
- Uses Mediator pattern for command handling
- Supports both streaming and non-streaming responses
- Routes to 13+ providers based on model selection
- Error handling via `OpenAIErrorHandlerMiddleware`

### 1.2 Legacy Completions (Deprecated)
```http
POST /openai/v1/completions
```

**Status**: ⚠️ DEPRECATED but functional  
**Description**: Simple text completion (pre-ChatGPT format)  
**Streaming**: ✅ SSE support  
**Authentication**: Bearer token required

**Request Body**:
```json
{
  "model": "llama-3.1-70b-versatile",
  "prompt": "Complete this text: The meaning of life is",
  "stream": false,
  "temperature": 0.7,
  "max_tokens": 100
}
```

**Response Format**: Legacy OpenAI completion format with text arrays

### 1.3 Models
```http
GET /openai/v1/models
GET /openai/v1/models/{id}
```

**Description**: List available models and retrieve model details  
**Authentication**: Bearer token required  
**Public**: Can be called without authentication in development

**Models Endpoint Response**:
```json
{
  "object": "list",
  "data": [
    {
      "id": "llama-3.1-70b-versatile",
      "object": "model",
      "created": 1234567890,
      "owned_by": "Groq"
    },
    {
      "id": "deepseek-chat",
      "object": "model",
      "created": 1234567890,
      "owned_by": "DeepSeek"
    }
  ]
}
```

**Individual Model Response**:
```json
{
  "id": "llama-3.1-70b-versatile",
  "object": "model",
  "created": 1234567890,
  "owned_by": "Groq"
}
```

### 1.4 Responses (Not Implemented)
```http
POST /openai/v1/responses
```

**Status**: ❌ 501 Not Implemented  
**Description**: OpenAI Responses API (future enhancement)

---

## 2. Provider Authentication (`/antigravity/`)

### 2.1 OAuth Flow
```http
GET /oauth/antigravity/callback?code=xxx&state=yyy
```

**Description**: OAuth callback handler for Antigravity provider  
**Authentication**: None (OAuth callback)  
**Usage**: Internal callback URL for OAuth flow

### 2.2 Authentication Management
```http
POST /antigravity/auth/start
POST /antigravity/auth/complete
GET /antigravity/accounts
```

**Description**: Manage OAuth authentication for Antigravity provider

**Start Authentication Request**:
```json
{
  "redirectUrl": "http://localhost:51121/oauth/antigravity/callback"
}
```

**Start Authentication Response**:
```json
{
  "authUrl": "https://antigravity.com/oauth/authorize?...",
  "redirectUrl": "http://localhost:51121/oauth/antigravity/callback",
  "instructions": "Open AuthUrl in your browser. After login, you will be redirected..."
}
```

**Complete Authentication Request**:
```json
{
  "code": "authorization_code",
  "state": "oauth_state",
  "redirectUrl": "http://localhost:51121/oauth/antigravity/callback"
}
```

**List Accounts Response**:
```json
[
  {
    "id": "acc_123",
    "provider": "antigravity",
    "email": "user@example.com"
  }
]
```

---

## 3. Generic Identity Management (`/api/identity/`)

### 3.1 Provider Authentication
```http
POST /api/identity/{provider}/start
POST /api/identity/{provider}/complete
```

**Description**: Generic identity provider authentication flow  
**Parameters**: `provider` - Identity provider identifier

**Start Request** (no body):  
**Start Response**: Authentication URL and instructions

**Complete Request**:
```json
{
  "code": "authorization_code",
  "state": "oauth_state"
}
```

### 3.2 Account Management
```http
GET /api/identity/accounts
```

**Description**: List all authenticated accounts with masked tokens  
**Authentication**: Bearer token required  
**Security**: Access tokens are masked (first 4 + last 4 characters)

**Response**:
```json
[
  {
    "id": "acc_123",
    "provider": "openai",
    "email": "user@example.com",
    "accessToken": "abcd....wxyz"
  }
]
```

---

## 4. User Management & Security (`/auth/`)

### 4.1 Developer Login
```http
POST /auth/dev-login
```

**Description**: Simple email-based authentication (dev only)  
**Security**: ⚠️ DEVELOPMENT ONLY - Auto-registers users  
**Rate Limiting**: None

**Request**:
```json
{
  "email": "developer@example.com"
}
```

**Response**:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

**Implementation Details**:
- Creates automatic tenant and user records
- Generates JWT with user claims
- Uses `ControlPlaneDbContext` for persistence

---

## 5. API Key Management (`/projects/{projectId}/keys/`)

### 5.1 Create API Key
```http
POST /projects/{projectId}/keys
```

**Authentication**: Bearer token required (user must own project)  
**Description**: Generate project-specific API keys

**Request**:
```json
{
  "name": "Production Key"
}
```

**Response**:
```json
{
  "id": "key_123",
  "key": "sk_synaxis_1234567890abcdef",
  "name": "Production Key"
}
```

**Security Notes**:
- Raw key only returned once during creation
- Keys are stored as SHA-256 hashes in database
- Supports project-level key management

### 5.2 Revoke API Key
```http
DELETE /projects/{projectId}/keys/{keyId}
```

**Authentication**: Bearer token required (user must own project)  
**Description**: Revoke an API key (soft delete via status flag)

**Response**: `204 No Content`

---

## 6. Health Monitoring (`/health/`)

### 6.1 Liveness Check
```http
GET /health/liveness
```

**Purpose**: Kubernetes/Docker health check endpoint  
**Check**: Basic application health  
**Response**: Simple health status

### 6.2 Readiness Check
```http
GET /health/readiness
```

**Purpose**: Application startup dependency validation  
**Checks**:
- PostgreSQL database connectivity (`ControlPlaneDbContext`)
- Redis connection (`StackExchange.Redis`)
- Configuration validity (`ConfigHealthCheck`)
- Provider connectivity (`ProviderConnectivityHealthCheck`)

**Status Codes**:
- `200 OK`: All dependencies healthy
- `503 Service Unavailable`: One or more dependencies failing

---

## Authentication & Authorization

### JWT Authentication
```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Configuration** (from `Program.cs`):
- **Algorithm**: HS256 (HMAC-SHA256)
- **Secret**: `Synaxis:InferenceGateway:JwtSecret` (configurable)
- **Validation**: 
  - ✅ Issuer signing key
  - ❌ Issuer validation (disabled)
  - ❌ Audience validation (disabled)

**Claims Structure**:
```json
{
  "sub": "user_id",
  "email": "user@example.com",
  "tenantId": "tenant_id",
  "exp": 1234567890
}
```

### Security Features
- **Rate Limiting**: Not implemented (⚠️ TODO)
- **Request Size Limits**: Default ASP.NET Core limits
- **HTTPS Enforcement**: Configured via middleware
- **CORS**: Not configured (⚠️ TODO)
- **Input Validation**: Model binding with validation attributes

---

## Provider Configuration

### Available Providers (13 Total)
From `/src/InferenceGateway/WebApi/Program.cs` environment mapping:

```json
{
  "Providers": {
    "Groq": { "Key": "GROQ_API_KEY" },
    "Cohere": { "Key": "COHERE_API_KEY" },
    "Cloudflare": { 
      "Key": "CLOUDFLARE_API_KEY",
      "AccountId": "CLOUDFLARE_ACCOUNT_ID" 
    },
    "Gemini": { "Key": "GEMINI_API_KEY" },
    "OpenRouter": { "Key": "OPENROUTER_API_KEY" },
    "DeepSeek": { 
      "Key": "DEEPSEEK_API_KEY",
      "Endpoint": "DEEPSEEK_API_ENDPOINT" 
    },
    "OpenAI": { 
      "Key": "OPENAI_API_KEY",
      "Endpoint": "OPENAI_API_ENDPOINT" 
    },
    "Antigravity": { 
      "ProjectId": "ANTIGRAVITY_PROJECT_ID",
      "Endpoint": "ANTIGRAVITY_API_ENDPOINT",
      "FallbackEndpoint": "ANTIGRAVITY_API_ENDPOINT_FALLBACK" 
    },
    "KiloCode": { "Key": "KILOCODE_API_KEY" },
    "NVIDIA": { "Key": "NVIDIA_API_KEY" },
    "HuggingFace": { "Key": "HUGGINGFACE_API_KEY" }
  }
}
```

### Model Routing
- **Configuration**: `Synaxis:InferenceGateway:CanonicalModels`
- **Aliases**: `Synaxis:InferenceGateway:Aliases`
- **Tier System**: Providers configured in tiers for failover
- **Load Balancing**: Round-robin within same tier

---

## Middleware Pipeline

1. **HttpsRedirection** - Force HTTPS in production
2. **Authentication** - JWT Bearer validation
3. **Authorization** - Policy-based access control
4. **Request Buffering** - Enable request body buffering
5. **OpenAIErrorHandler** - Centralized error handling
6. **OpenAIMetadata** - Request/response metadata
7. **Endpoint Routing** - Map to specific handlers

---

## Error Handling

### Error Response Format
```json
{
  "error": {
    "message": "Error description",
    "type": "invalid_request_error",
    "param": "parameter_name",
    "code": "error_code"
  }
}
```

### Common Error Types
- `invalid_request_error`: Malformed requests
- `insufficient_quota`: Provider quota exceeded  
- `rate_limit_exceeded`: Too many requests
- `model_not_found`: Unknown model requested
- `context_length_exceeded`: Input too long

### Middleware Components
- **OpenAIErrorHandlerMiddleware**: Normalizes provider errors
- **OpenAIMetadataMiddleware**: Adds tracing and logging
- **RoutingContext**: Thread-local request context

---

## Streaming Support

### Implementation
- **Protocol**: Server-Sent Events (SSE)
- **Content-Type**: `text/event-stream`
- **Chunk Format**: OpenAI-compatible completion chunks
- **End Signal**: `data: [DONE]\n\n`

### Streaming Features
- ✅ Real-time token streaming
- ✅ Cancellation support
- ✅ Connection keep-alive
- ✅ Error handling during streams

---

## Performance & Monitoring

### Observability
- **OpenTelemetry**: Distributed tracing configured
- **Serilog**: Structured logging to console
- **Health Checks**: Readiness/liveness monitoring
- **Redis**: Caching and session storage
- **PostgreSQL**: Control plane data persistence

### Metrics & Monitoring
- **Tracing**: All requests traced with OpenTelemetry
- **Logging**: Structured logs with Serilog
- **Health**: Multi-layer health checking
- **Performance**: Request/response timing

---

## Development Features

### Development Mode (`Development` environment)
- **OpenAPI**: `/openapi/v1.json` - Full API specification
- **Scalar API**: `/scalar/v1` - Interactive API documentation
- **Debug Endpoints**: Temporary development endpoints

### Database Management
- **Migration**: Automatic database initialization
- **Error Handling**: Non-fatal migration failures
- **Context**: `ControlPlaneDbContext` for user/project data

---

## Gaps & Missing Features

### ❌ **Not Implemented**
1. **Rate Limiting** - No request throttling
2. **CORS Configuration** - Cross-origin requests not configured
3. **API Key Authentication** - Currently only JWT Bearer
4. **Detailed Metrics** - No usage analytics/quotas
5. **Provider Health UI** - No admin dashboard for provider status
6. **Streaming Debugging** - Limited streaming error visibility

### ⚠️ **Partially Implemented**
1. **Error Handling** - Basic normalization, needs enhancement
2. **Testing** - Limited test coverage (51.89% backend)
3. **Documentation** - OpenAPI specs exist but not comprehensive

### 🔮 **Planned/Future**
1. **OpenAI Responses API** - `POST /openai/v1/responses`
2. **Admin Dashboard** - Provider management UI
3. **Usage Analytics** - Token usage tracking
4. **Advanced Routing** - Cost-based routing decisions

---

**Total Endpoints**: 15+ endpoints across 5 main categories  
**Authentication**: JWT Bearer tokens  
**Streaming**: Full SSE support for real-time responses  
**Providers**: 13+ AI providers with intelligent routing  
**Health**: Comprehensive health monitoring with dependency checks