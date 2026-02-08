# Configuration Guide

> **ULTRA MISER MODE™ Pro Tip**: Configuration is just telling the gateway which free tiers to exploit. The more providers you configure, the longer you can avoid that dreaded credit card input field.

Synaxis is configured primarily through `appsettings.json` (and environment variables for Docker deployments). This guide covers provider setup, model routing, and the sacred art of maximizing free inference.

---

## Table of Contents

- [Configuration Structure](#configuration-structure)
- [Supported Providers](#supported-providers)
- [Provider Configuration](#provider-configuration)
- [Canonical Models](#canonical-models)
- [Model Aliases](#model-aliases)
- [Environment Variables](#environment-variables)
- [Docker Compose Configuration](#docker-compose-configuration)
- [ULTRA MISER MODE™ Optimization](#ultra-miser-mode-optimization)

---

## Configuration Structure

The configuration is nested under `Synaxis:InferenceGateway`:

```json
{
  "Synaxis": {
    "InferenceGateway": {
      "MasterKey": "your-master-key",
      "JwtSecret": "your-jwt-secret",
      "Providers": { },
      "CanonicalModels": [ ],
      "Aliases": { }
    }
  }
}
```

### Core Properties

| Property | Description | Required |
|----------|-------------|----------|
| `MasterKey` | Master API key for admin operations | Yes |
| `JwtSecret` | Secret for JWT token signing (min 32 chars) | Yes |
| `Providers` | Provider configurations | Yes |
| `CanonicalModels` | Model mappings and capabilities | Yes |
| `Aliases` | Model aliases for routing | Yes |

---

## Supported Providers

Synaxis supports **15+ providers** out of the box, because one free tier is never enough:

### Native Providers

| Provider | Type | Key Required | Special Features |
|----------|------|--------------|------------------|
| **OpenAI** | `openai` | Yes | Generic OpenAI-compatible endpoint |
| **Groq** | `groq` | Yes | Blazing fast inference |
| **Cohere** | `cohere` | Yes | Enterprise-grade models |
| **Cloudflare** | `cloudflare` | Yes + AccountId | Workers AI serverless |
| **Gemini** | `gemini` | Yes | Google's AI models |
| **OpenRouter** | `openrouter` | Yes | Universal model aggregator |
| **NVIDIA** | `nvidia` | Yes | NIM inference microservices |
| **HuggingFace** | `huggingface` | Yes | Open source model hub |
| **Pollinations** | `pollinations` | **No** | Truly free, no signup |
| **Antigravity** | `antigravity` | OAuth | Google's cloud code |
| **GitHubCopilot** | `githubcopilot` | OAuth | Copilot integration |
| **DuckDuckGo** | `duckduckgo` | No | Anonymous AI chat |
| **AiHorde** | `aihorde` | Optional | Distributed AI workers |
| **KiloCode** | `kilocode` | Yes | Specialized endpoints |

### OpenAI-Compatible Providers

Any provider with an OpenAI-compatible API can be configured with `Type: "openai"` and a custom endpoint:

| Provider | Endpoint | Free Tier |
|----------|----------|-----------|
| **SiliconFlow** | `https://api.siliconflow.cn/v1` | Yes |
| **SambaNova** | `https://api.sambanova.ai/v1` | Yes |
| **Zai** | `https://open.bigmodel.cn/api/paas/v4` | Yes |
| **GitHubModels** | `https://models.inference.ai.azure.com` | Yes |
| **Hyperbolic** | `https://api.hyperbolic.xyz/v1` | Yes |
| **DeepSeek** | `https://api.deepseek.com/v1` | Yes |

---

## Provider Configuration

### Common Provider Properties

```json
{
  "ProviderName": {
    "Enabled": true,
    "Type": "ProviderType",
    "Key": "API_KEY",
    "Tier": 1,
    "Models": ["model-1", "model-2"],
    "RateLimitRPM": 60,
    "RateLimitTPM": 10000,
    "IsFree": false,
    "Endpoint": "https://custom.endpoint.com/v1",
    "CustomHeaders": {
      "X-Custom-Header": "value"
    }
  }
}
```

| Property | Description | Required |
|----------|-------------|----------|
| `Enabled` | Activate this provider | Yes |
| `Type` | Provider type (see table above) | Yes |
| `Key` | API key for authentication | Usually |
| `Tier` | Routing priority (lower = tried first) | Yes |
| `Models` | Array of supported model IDs | Yes |
| `RateLimitRPM` | Requests per minute limit | No |
| `RateLimitTPM` | Tokens per minute limit | No |
| `IsFree` | Mark as free tier provider | No |
| `Endpoint` | Custom API endpoint URL | No |
| `AccountId` | Cloudflare account ID | Cloudflare only |
| `ProjectId` | Antigravity project ID | Antigravity only |

### Provider Examples

#### 1. Groq (High-Speed Inference)

```json
"Groq": {
  "Enabled": true,
  "Type": "Groq",
  "Key": "gsk_your_groq_key_here",
  "Tier": 1,
  "Models": [
    "llama-3.3-70b-versatile",
    "llama-3.1-8b-instant",
    "mixtral-8x7b-32768"
  ],
  "RateLimitRPM": 60,
  "RateLimitTPM": 100000
}
```

#### 2. Cohere (Enterprise Models)

```json
"Cohere": {
  "Enabled": true,
  "Type": "Cohere",
  "Key": "your_cohere_key_here",
  "Tier": 1,
  "Models": [
    "c4ai-aya-expanse-32b",
    "c4ai-aya-expanse-8b",
    "command-r",
    "command-r-plus"
  ]
}
```

#### 3. Cloudflare Workers AI

```json
"Cloudflare": {
  "Enabled": true,
  "Type": "Cloudflare",
  "Key": "your_cf_api_token",
  "AccountId": "your_account_id",
  "Tier": 1,
  "Models": [
    "@cf/meta/llama-3.1-8b-instruct",
    "@cf/meta/llama-3.2-3b-instruct",
    "@cf/mistral/mistral-7b-instruct-v0.1",
    "@cf/microsoft/phi-2"
  ]
}
```

#### 4. Gemini (Google AI)

```json
"Gemini": {
  "Enabled": true,
  "Type": "Gemini",
  "Key": "your_gemini_api_key",
  "Tier": 1,
  "Models": [
    "gemini-2.0-flash",
    "gemini-1.5-pro",
    "gemini-1.5-flash",
    "gemini-1.5-flash-8b"
  ]
}
```

#### 5. OpenRouter (Aggregator)

```json
"OpenRouter": {
  "Enabled": true,
  "Type": "OpenRouter",
  "Key": "sk-or-your-openrouter-key",
  "Tier": 2,
  "Models": [
    "meta-llama/llama-3.3-70b-instruct:free",
    "mistralai/mistral-7b-instruct:free",
    "huggingfaceh4/zephyr-7b-beta:free"
  ]
}
```

#### 6. NVIDIA NIM

```json
"NVIDIA": {
  "Enabled": true,
  "Type": "Nvidia",
  "Key": "your_nvidia_api_key",
  "Tier": 1,
  "Models": [
    "meta/llama-3.3-70b-instruct",
    "nvidia/llama-3.1-nemotron-70b-instruct",
    "deepseek-ai/deepseek-v3"
  ]
}
```

#### 7. HuggingFace

```json
"HuggingFace": {
  "Enabled": true,
  "Type": "HuggingFace",
  "Key": "hf_your_token_here",
  "Tier": 1,
  "Models": [
    "microsoft/Phi-3.5-mini-instruct",
    "HuggingFaceTB/SmolLM2-1.7B-Instruct",
    "Qwen/Qwen2.5-7B-Instruct"
  ]
}
```

#### 8. Pollinations (No API Key!)

```json
"Pollinations": {
  "Enabled": true,
  "Type": "Pollinations",
  "Tier": 1,
  "Models": [
    "openai",
    "mistral"
  ]
}
```

> **ULTRA MISER MODE™ Alert**: Pollinations requires NO API key. Zero. Nada. Just pure, unadulterated free inference. This is the provider that makes accountants weep with joy.

#### 9. Antigravity (Google Cloud Code)

```json
"Antigravity": {
  "Enabled": true,
  "Type": "Antigravity",
  "ProjectId": "your-project-id",
  "Tier": 1,
  "Models": [
    "gemini-2.0-flash"
  ]
}
```

#### 10. OpenAI-Compatible Custom Provider

```json
"DeepSeek": {
  "Enabled": true,
  "Type": "OpenAI",
  "Endpoint": "https://api.deepseek.com/v1",
  "Key": "your_deepseek_key",
  "Tier": 1,
  "Models": [
    "deepseek-chat",
    "deepseek-reasoner"
  ]
}
```

#### 11. GitHub Models (Free Tier)

```json
"GitHubModels": {
  "Enabled": true,
  "Type": "OpenAI",
  "Endpoint": "https://models.inference.ai.azure.com",
  "Key": "your_github_pat",
  "IsFree": true,
  "Tier": 1,
  "Models": [
    "gpt-4o",
    "Llama-3.1-405B-Instruct"
  ]
}
```

#### 12. DuckDuckGo (Anonymous)

```json
"DuckDuckGo": {
  "Enabled": true,
  "Type": "DuckDuckGo",
  "Tier": 1,
  "Models": [
    "gpt-4o-mini",
    "claude-3-haiku",
    "llama-3.1-70b"
  ]
}
```

#### 13. AiHorde (Distributed)

```json
"AiHorde": {
  "Enabled": true,
  "Type": "AiHorde",
  "Key": "0000000000",
  "Tier": 1,
  "Models": [
    "aihorde"
  ]
}
```

---

## Canonical Models

Canonical models map provider-specific model IDs to unified Synaxis identifiers:

```json
"CanonicalModels": [
  {
    "Id": "groq/llama-3.3-70b",
    "Provider": "Groq",
    "ModelPath": "llama-3.3-70b-versatile",
    "Streaming": true,
    "Tools": true,
    "Vision": false,
    "StructuredOutput": false,
    "LogProbs": false
  },
  {
    "Id": "nvidia/llama-3.3-70b",
    "Provider": "NVIDIA",
    "ModelPath": "meta/llama-3.3-70b-instruct",
    "Streaming": true,
    "Tools": false,
    "Vision": false,
    "StructuredOutput": false,
    "LogProbs": false
  }
]
```

### Canonical Model Properties

| Property | Description |
|----------|-------------|
| `Id` | Unique canonical identifier |
| `Provider` | Provider name (must match config key) |
| `ModelPath` | Provider-specific model ID |
| `Streaming` | Supports SSE streaming |
| `Tools` | Supports function calling |
| `Vision` | Supports image input |
| `StructuredOutput` | Supports JSON mode |
| `LogProbs` | Supports log probabilities |

---

## Model Aliases

Aliases provide user-friendly names that map to multiple provider candidates:

```json
"Aliases": {
  "llama-3.3-70b": {
    "Candidates": [
      "groq/llama-3.3-70b",
      "nvidia/llama-3.3-70b",
      "openrouter/llama-3.3-free"
    ]
  },
  "gpt-4o-mini": {
    "Candidates": [
      "pollinations/openai",
      "ddg/gpt-4o-mini"
    ]
  }
}
```

### ULTRA MISER MODE™ Aliases

Pre-configured aliases for maximum free tier exploitation:

```json
"miser-intelligence": {
  "Candidates": [
    "sambanova/llama-405b",
    "github/gpt-4o",
    "hyperbolic/llama-405b"
  ]
},
"miser-fast": {
  "Candidates": [
    "siliconflow/deepseek-v3",
    "zai/glm-4-flash",
    "groq/llama-8b"
  ]
},
"miser-coding": {
  "Candidates": [
    "siliconflow/qwen-2.5",
    "github/gpt-4o"
  ]
}
```

Use them like any other model:

```bash
curl http://localhost:8080/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "miser-intelligence",
    "messages": [{"role": "user", "content": "Hello!"}]
  }'
```

---

## Environment Variables

For Docker deployments, all configuration can be set via environment variables using double-underscore notation:

### Core Settings

| Variable | Description |
|----------|-------------|
| `Synaxis__InferenceGateway__MasterKey` | Master API key |
| `Synaxis__InferenceGateway__JwtSecret` | JWT signing secret |
| `Synaxis__ControlPlane__ConnectionString` | PostgreSQL connection string |
| `ConnectionStrings__Redis` | Redis connection string |

### Provider API Keys

| Variable | Provider |
|----------|----------|
| `GROQ_API_KEY` | Groq |
| `COHERE_API_KEY` | Cohere |
| `CLOUDFLARE_API_KEY` | Cloudflare |
| `CLOUDFLARE_ACCOUNT_ID` | Cloudflare Account ID |
| `GEMINI_API_KEY` | Gemini |
| `OPENROUTER_API_KEY` | OpenRouter |
| `NVIDIA_API_KEY` | NVIDIA |
| `HUGGINGFACE_API_KEY` | HuggingFace |
| `DEEPSEEK_API_KEY` | DeepSeek |
| `OPENAI_API_KEY` | OpenAI |
| `ANTIGRAVITY_PROJECT_ID` | Antigravity |
| `KILOCODE_API_KEY` | KiloCode |

### Docker-Specific Provider Mapping

```bash
Synaxis__InferenceGateway__Providers__Groq__Key=${GROQ_API_KEY}
Synaxis__InferenceGateway__Providers__Groq__Type=Groq
Synaxis__InferenceGateway__Providers__Groq__Enabled=true
Synaxis__InferenceGateway__Providers__Groq__Tier=1
```

---

## Docker Compose Configuration

### Complete docker-compose.yml Example

```yaml
services:
  inference-gateway:
    image: synaxis/synaxis-inferencegateway:latest
    restart: unless-stopped
    build:
      context: .
      dockerfile: src/InferenceGateway/WebApi/Dockerfile
    ports:
      - "8080:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      Synaxis__ControlPlane__ConnectionString: Host=postgres;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}
      ConnectionStrings__Redis: redis:6379,password=${REDIS_PASSWORD},abortConnect=false
      Synaxis__InferenceGateway__JwtSecret: ${JWT_SECRET}
      # Provider configurations
      Synaxis__InferenceGateway__Providers__Groq__Key: ${GROQ_API_KEY}
      Synaxis__InferenceGateway__Providers__Cohere__Key: ${COHERE_API_KEY}
      Synaxis__InferenceGateway__Providers__Cloudflare__Key: ${CLOUDFLARE_API_KEY}
      Synaxis__InferenceGateway__Providers__Cloudflare__AccountId: ${CLOUDFLARE_ACCOUNT_ID}
      Synaxis__InferenceGateway__Providers__Gemini__Key: ${GEMINI_API_KEY}
      Synaxis__InferenceGateway__Providers__HuggingFace__Key: ${HUGGINGFACE_API_KEY}
      Synaxis__InferenceGateway__Providers__OpenRouter__Key: ${OPENROUTER_API_KEY}
      Synaxis__InferenceGateway__Providers__NVIDIA__Key: ${NVIDIA_API_KEY}
    depends_on:
      postgres:
        condition: service_healthy
      redis:
        condition: service_healthy

  postgres:
    image: postgres:15-alpine
    restart: unless-stopped
    environment:
      POSTGRES_DB: ${POSTGRES_DB}
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER} -d ${POSTGRES_DB}"]
      interval: 10s
      timeout: 5s
      retries: 5
    ports:
      - "5432:5432"
    volumes:
      - pgdata:/var/lib/postgresql/data

  redis:
    image: redis:alpine
    restart: unless-stopped
    command: redis-server --requirepass ${REDIS_PASSWORD}
    healthcheck:
      test: ["CMD-SHELL", "redis-cli -a ${REDIS_PASSWORD} ping | grep PONG"]
      interval: 10s
      timeout: 5s
      retries: 5
    ports:
      - "6379:6379"

volumes:
  pgdata:
```

### .env File Template

```bash
# Database
POSTGRES_DB=synaxis
POSTGRES_USER=synaxis
POSTGRES_PASSWORD=REPLACE_WITH_POSTGRES_PASSWORD

# Redis
REDIS_PASSWORD=REPLACE_WITH_REDIS_PASSWORD

# Security
JWT_SECRET=REPLACE_WITH_JWT_SECRET_AT_LEAST_32_CHARACTERS

# Provider API Keys (add only the ones you use)
GROQ_API_KEY=REPLACE_WITH_GROQ_API_KEY
COHERE_API_KEY=REPLACE_WITH_COHERE_API_KEY
CLOUDFLARE_API_KEY=REPLACE_WITH_CLOUDFLARE_API_KEY
CLOUDFLARE_ACCOUNT_ID=REPLACE_WITH_CLOUDFLARE_ACCOUNT_ID
GEMINI_API_KEY=REPLACE_WITH_GEMINI_API_KEY
OPENROUTER_API_KEY=REPLACE_WITH_OPENROUTER_API_KEY
NVIDIA_API_KEY=REPLACE_WITH_NVIDIA_API_KEY
HUGGINGFACE_API_KEY=REPLACE_WITH_HUGGINGFACE_API_KEY
DEEPSEEK_API_KEY=REPLACE_WITH_DEEPSEEK_API_KEY
OPENAI_API_KEY=REPLACE_WITH_OPENAI_API_KEY
```

### Running with Docker

```bash
# Copy environment template
cp .env.example .env

# Edit with your API keys
nano .env

# Start all services
docker compose up -d

# Or with dev profile (includes pgAdmin)
docker compose --profile dev up -d
```

---

## ULTRA MISER MODE™ Optimization

### The Philosophy

> **ULTRA MISER MODE™** is not just a configuration strategy—it's a lifestyle. It's the dignified art of routing prompts through every free inference crumb on the internet before anyone dares ask you for a credit card.

### Tier Strategy

Configure providers in tiers for optimal free tier exploitation:

| Tier | Purpose | Examples |
|------|---------|----------|
| **0** | Free providers (no key) | Pollinations, DuckDuckGo |
| **1** | Free tier with key | Groq, GitHubModels, Cloudflare |
| **2** | Fallback/paid | OpenRouter, OpenAI |

### Sample ULTRA MISER MODE™ Configuration

```json
{
  "Synaxis": {
    "InferenceGateway": {
      "Providers": {
        "Pollinations": {
          "Enabled": true,
          "Type": "Pollinations",
          "Tier": 0,
          "Models": ["openai", "mistral"]
        },
        "DuckDuckGo": {
          "Enabled": true,
          "Type": "DuckDuckGo",
          "Tier": 0,
          "Models": ["gpt-4o-mini"]
        },
        "Groq": {
          "Enabled": true,
          "Type": "Groq",
          "Key": "gsk_...",
          "Tier": 1,
          "Models": ["llama-3.3-70b-versatile"]
        },
        "GitHubModels": {
          "Enabled": true,
          "Type": "OpenAI",
          "Endpoint": "https://models.inference.ai.azure.com",
          "Key": "ghp_...",
          "IsFree": true,
          "Tier": 1,
          "Models": ["gpt-4o"]
        },
        "OpenRouter": {
          "Enabled": true,
          "Type": "OpenRouter",
          "Key": "sk-or-...",
          "Tier": 2,
          "Models": ["meta-llama/llama-3.3-70b-instruct:free"]
        }
      }
    }
  }
}
```

### Routing Behavior

With this configuration:

1. **Tier 0** providers (Pollinations, DuckDuckGo) are tried first—completely free
2. **Tier 1** providers (Groq, GitHubModels) are tried if Tier 0 fails or doesn't support the model
3. **Tier 2** providers (OpenRouter) are the final fallback

### Pro Tips for Maximum Miserliness

1. **Enable every free provider**: The more providers, the more resilience
2. **Use `:free` models on OpenRouter**: They explicitly mark free models
3. **Monitor rate limits**: Set `RateLimitRPM` and `RateLimitTPM` to avoid hitting caps
4. **Use aliases**: Create aliases that prioritize free providers
5. **Check provider status**: Use the admin UI to monitor provider health

### The Golden Rule

> If a provider doesn't require a credit card, it belongs in your configuration. If it does require a credit card, it belongs in Tier 2 as a last resort.

---

## Configuration Validation

After making changes, validate your configuration:

```bash
# Check provider health
curl http://localhost:8080/admin/health \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# List configured providers
curl http://localhost:8080/admin/providers \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Test a specific model
curl http://localhost:8080/v1/chat/completions \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_API_KEY" \
  -d '{
    "model": "llama-3.3-70b",
    "messages": [{"role": "user", "content": "Test"}]
  }'
```

---

## Troubleshooting

### Provider Not Found

Ensure the `Type` matches exactly (case-insensitive):
- ✅ `groq`, `Groq`, `GROQ`
- ❌ `groq.com`, `api.groq`

### Authentication Errors

- Verify API keys are correct and not expired
- Check if the provider requires additional fields (AccountId, ProjectId)
- Ensure keys have the necessary permissions/scopes

### Model Not Available

- Verify the model ID matches the provider's expected format
- Check if the model is available in your region
- Ensure the model is included in the provider's `Models` array

### Rate Limiting

- Set appropriate `RateLimitRPM` and `RateLimitTPM` values
- Use multiple providers to distribute load
- Enable `IsFree` flag for proper free tier prioritization

---

**Remember**: Every properly configured provider is another step toward financial independence from AI API bills. Configure wisely, route ruthlessly, and may your tokens always be free.

*ULTRA MISER MODE™ — Because paying for AI is for people with self-respect.*
