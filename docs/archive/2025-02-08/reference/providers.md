# Provider Reference

> **ULTRA MISER MODE™ Pro Tip**: This page lists every provider you can exploit for free inference. Memorize it. Love it. Never pay for tokens again.

Complete reference for all AI providers supported by Synaxis. Each provider has been carefully selected for its generosity (or gullibility) in offering free API access.

---

## Table of Contents

- [Native Providers](#native-providers)
  - [OpenAI](#openai)
  - [Groq](#groq)
  - [Cohere](#cohere)
  - [Cloudflare](#cloudflare)
  - [Gemini](#gemini)
  - [OpenRouter](#openrouter)
  - [NVIDIA](#nvidia)
  - [HuggingFace](#huggingface)
  - [Pollinations](#pollinations)
  - [Antigravity](#antigravity)
  - [GitHubCopilot](#githubcopilot)
  - [DuckDuckGo](#duckduckgo)
  - [AiHorde](#aihorde)
  - [KiloCode](#kilocode)
- [OpenAI-Compatible Providers](#openai-compatible-providers)
- [Provider Comparison Matrix](#provider-comparison-matrix)
- [ULTRA MISER MODE™ Tier Strategy](#ultra-miser-mode-tier-strategy)

---

## Native Providers

### OpenAI

The original. The standard. The one that started it all (and charges for it).

| Property | Value |
|----------|-------|
| **Type** | `openai` |
| **Key Required** | Yes |
| **Free Tier** | Limited ($5-18 credit) |
| **Rate Limits** | Varies by tier |
| **Streaming** | ✅ Full support |
| **Tools/Functions** | ✅ Native support |

**Configuration Example:**
```json
{
  "OpenAI": {
    "Enabled": true,
    "Type": "openai",
    "Key": "sk-...",
    "Tier": 2,
    "Models": ["gpt-4o", "gpt-4o-mini", "gpt-3.5-turbo"]
  }
}
```

**Best For**: When you absolutely need the real thing and have credits to burn.

---

### Groq

Blazing fast inference that makes other providers look like they're running on potatoes.

| Property | Value |
|----------|-------|
| **Type** | `groq` |
| **Key Required** | Yes |
| **Free Tier** | Generous (check current limits) |
| **Rate Limits** | 60 RPM / 100K TPM typical |
| **Streaming** | ✅ Full support |
| **Specialty** | Speed demon |

**Configuration Example:**
```json
{
  "Groq": {
    "Enabled": true,
    "Type": "groq",
    "Key": "gsk_...",
    "Tier": 1,
    "Models": [
      "llama-3.3-70b-versatile",
      "llama-3.1-8b-instant",
      "mixtral-8x7b-32768"
    ],
    "RateLimitRPM": 60,
    "RateLimitTPM": 100000
  }
}
```

**Best For**: When you need responses faster than you can blink.

**ULTRA MISER MODE™ Note**: Groq's free tier is genuinely generous. Treat it with respect, or it'll rate-limit you into oblivion.

---

### Cohere

Enterprise-grade models with a focus on business use cases.

| Property | Value |
|----------|-------|
| **Type** | `cohere` |
| **Key Required** | Yes |
| **Free Tier** | Available (trial credits) |
| **Rate Limits** | Varies by plan |
| **Streaming** | ✅ Supported |
| **Specialty** | Enterprise, multilingual |

**Configuration Example:**
```json
{
  "Cohere": {
    "Enabled": true,
    "Type": "cohere",
    "Key": "...",
    "Tier": 1,
    "Models": [
      "c4ai-aya-expanse-32b",
      "c4ai-aya-expanse-8b",
      "command-r",
      "command-r-plus"
    ]
  }
}
```

**Best For**: Multilingual applications and enterprise contexts.

---

### Cloudflare

Serverless AI inference on Cloudflare's edge network. Because why not run LLMs in 200+ data centers?

| Property | Value |
|----------|-------|
| **Type** | `cloudflare` |
| **Key Required** | Yes + AccountId |
| **Free Tier** | Workers AI free tier |
| **Rate Limits** | Workers AI limits apply |
| **Streaming** | ✅ Supported |
| **Specialty** | Edge deployment |

**Configuration Example:**
```json
{
  "Cloudflare": {
    "Enabled": true,
    "Type": "cloudflare",
    "Key": "...",
    "AccountId": "your-account-id",
    "Tier": 1,
    "Models": [
      "@cf/meta/llama-3.1-8b-instruct",
      "@cf/meta/llama-3.2-3b-instruct",
      "@cf/mistral/mistral-7b-instruct-v0.1",
      "@cf/microsoft/phi-2"
    ]
  }
}
```

**Best For**: Edge deployments and when you already use Cloudflare.

---

### Gemini

Google's AI models. Sometimes brilliant, sometimes... well, it's Google.

| Property | Value |
|----------|-------|
| **Type** | `gemini` |
| **Key Required** | Yes |
| **Free Tier** | Generous free tier |
| **Rate Limits** | 60 RPM free tier |
| **Streaming** | ✅ Supported |
| **Specialty** | Long context, multimodal |

**Configuration Example:**
```json
{
  "Gemini": {
    "Enabled": true,
    "Type": "gemini",
    "Key": "...",
    "Tier": 1,
    "Models": [
      "gemini-2.0-flash",
      "gemini-1.5-pro",
      "gemini-1.5-flash",
      "gemini-1.5-flash-8b"
    ]
  }
}
```

**Best For**: Long context windows and multimodal applications.

---

### OpenRouter

The universal model aggregator. One API key, access to dozens of providers.

| Property | Value |
|----------|-------|
| **Type** | `openrouter` |
| **Key Required** | Yes |
| **Free Tier** | Many `:free` models |
| **Rate Limits** | Varies by model |
| **Streaming** | ✅ Supported |
| **Specialty** | Model variety |

**Configuration Example:**
```json
{
  "OpenRouter": {
    "Enabled": true,
    "Type": "openrouter",
    "Key": "sk-or-...",
    "Tier": 2,
    "Models": [
      "meta-llama/llama-3.3-70b-instruct:free",
      "mistralai/mistral-7b-instruct:free",
      "huggingfaceh4/zephyr-7b-beta:free"
    ]
  }
}
```

**Best For**: Accessing many models with one key. Look for `:free` suffix models.

**ULTRA MISER MODE™ Tip**: OpenRouter's `:free` models are a goldmine. Use them liberally.

---

### NVIDIA

NIM inference microservices. Enterprise-grade, GPU-accelerated inference.

| Property | Value |
|----------|-------|
| **Type** | `nvidia` |
| **Key Required** | Yes |
| **Free Tier** | Limited trial |
| **Rate Limits** | Varies by tier |
| **Streaming** | ✅ Supported |
| **Specialty** | GPU-optimized |

**Configuration Example:**
```json
{
  "NVIDIA": {
    "Enabled": true,
    "Type": "nvidia",
    "Key": "...",
    "Tier": 1,
    "Models": [
      "meta/llama-3.3-70b-instruct",
      "nvidia/llama-3.1-nemotron-70b-instruct",
      "deepseek-ai/deepseek-v3"
    ]
  }
}
```

**Best For**: When you need GPU-optimized inference.

---

### HuggingFace

The open-source model hub. Thousands of models, one API.

| Property | Value |
|----------|-------|
| **Type** | `huggingface` |
| **Key Required** | Yes |
| **Free Tier** | Inference API free tier |
| **Rate Limits** | Varies by model |
| **Streaming** | ✅ Supported |
| **Specialty** | Open source models |

**Configuration Example:**
```json
{
  "HuggingFace": {
    "Enabled": true,
    "Type": "huggingface",
    "Key": "hf_...",
    "Tier": 1,
    "Models": [
      "microsoft/Phi-3.5-mini-instruct",
      "HuggingFaceTB/SmolLM2-1.7B-Instruct",
      "Qwen/Qwen2.5-7B-Instruct"
    ]
  }
}
```

**Best For**: Open-source models and research.

---

### Pollinations

**The Holy Grail of ULTRA MISER MODE™**

| Property | Value |
|----------|-------|
| **Type** | `pollinations` |
| **Key Required** | **NO** |
| **Free Tier** | **100% FREE** |
| **Rate Limits** | Fair use |
| **Streaming** | ✅ Supported |
| **Specialty** | Zero authentication |

**Configuration Example:**
```json
{
  "Pollinations": {
    "Enabled": true,
    "Type": "pollinations",
    "Tier": 0,
    "Models": ["openai", "mistral"]
  }
}
```

**Best For**: When you have no API keys and zero budget.

**ULTRA MISER MODE™ Alert**: This provider requires NO API key. Zero. Nada. Just pure, unadulterated free inference. This is the provider that makes accountants weep with joy.

---

### Antigravity

Google's cloud code integration. OAuth-based authentication.

| Property | Value |
|----------|-------|
| **Type** | `antigravity` |
| **Key Required** | OAuth |
| **Free Tier** | Google Cloud credits |
| **Rate Limits** | GCP quotas |
| **Streaming** | ✅ Supported |
| **Specialty** | Google Cloud integration |

**Configuration Example:**
```json
{
  "Antigravity": {
    "Enabled": true,
    "Type": "antigravity",
    "ProjectId": "your-project-id",
    "Tier": 1,
    "Models": ["gemini-2.0-flash"]
  }
}
```

**Best For**: Google Cloud users with existing GCP credits.

---

### GitHubCopilot

GitHub Copilot integration for when you already pay for Copilot.

| Property | Value |
|----------|-------|
| **Type** | `githubcopilot` |
| **Key Required** | OAuth |
| **Free Tier** | Requires Copilot subscription |
| **Rate Limits** | Copilot limits |
| **Streaming** | ✅ Supported |
| **Specialty** | IDE integration |

**Configuration Example:**
```json
{
  "GitHubCopilot": {
    "Enabled": true,
    "Type": "githubcopilot",
    "Tier": 1,
    "Models": ["copilot"]
  }
}
```

**Best For**: When you already have GitHub Copilot.

---

### DuckDuckGo

Anonymous AI chat. No account, no tracking, no problem.

| Property | Value |
|----------|-------|
| **Type** | `duckduckgo` |
| **Key Required** | **NO** |
| **Free Tier** | **100% FREE** |
| **Rate Limits** | Fair use |
| **Streaming** | ✅ Supported |
| **Specialty** | Privacy-focused |

**Configuration Example:**
```json
{
  "DuckDuckGo": {
    "Enabled": true,
    "Type": "duckduckgo",
    "Tier": 0,
    "Models": [
      "gpt-4o-mini",
      "claude-3-haiku",
      "llama-3.1-70b"
    ]
  }
}
```

**Best For**: Privacy-conscious applications and zero-setup inference.

---

### AiHorde

Distributed AI workers. Community-powered inference.

| Property | Value |
|----------|-------|
| **Type** | `aihorde` |
| **Key Required** | Optional (anon key works) |
| **Free Tier** | **100% FREE** |
| **Rate Limits** | Priority-based |
| **Streaming** | ✅ Supported |
| **Specialty** | Distributed, community |

**Configuration Example:**
```json
{
  "AiHorde": {
    "Enabled": true,
    "Type": "aihorde",
    "Key": "0000000000",
    "Tier": 0,
    "Models": ["aihorde"]
  }
}
```

**Best For**: Supporting the distributed AI community.

---

### KiloCode

Specialized endpoints for specific use cases.

| Property | Value |
|----------|-------|
| **Type** | `kilocode` |
| **Key Required** | Yes |
| **Free Tier** | Check provider |
| **Rate Limits** | Varies |
| **Streaming** | ✅ Supported |
| **Specialty** | Specialized models |

**Configuration Example:**
```json
{
  "KiloCode": {
    "Enabled": true,
    "Type": "kilocode",
    "Key": "...",
    "Tier": 1,
    "Models": ["kilocode-model"]
  }
}
```

---

## OpenAI-Compatible Providers

Any provider with an OpenAI-compatible API can be configured with `Type: "openai"` and a custom endpoint:

| Provider | Endpoint | Free Tier | Notes |
|----------|----------|-----------|-------|
| **SiliconFlow** | `https://api.siliconflow.cn/v1` | Yes | Chinese provider, good for Asian markets |
| **SambaNova** | `https://api.sambanova.ai/v1` | Yes | Fast inference |
| **Zai** | `https://open.bigmodel.cn/api/paas/v4` | Yes | GLM models |
| **GitHubModels** | `https://models.inference.ai.azure.com` | Yes | GitHub's free model inference |
| **Hyperbolic** | `https://api.hyperbolic.xyz/v1` | Yes | Various open models |
| **DeepSeek** | `https://api.deepseek.com/v1` | Yes | DeepSeek models |

**Configuration Example (DeepSeek):**
```json
{
  "DeepSeek": {
    "Enabled": true,
    "Type": "openai",
    "Endpoint": "https://api.deepseek.com/v1",
    "Key": "...",
    "Tier": 1,
    "Models": ["deepseek-chat", "deepseek-reasoner"]
  }
}
```

---

## Provider Comparison Matrix

| Provider | Key Required | Free Tier | Streaming | Tools | Vision | Best For |
|----------|--------------|-----------|-----------|-------|--------|----------|
| **OpenAI** | ✅ | Limited | ✅ | ✅ | ✅ | Quality, compatibility |
| **Groq** | ✅ | Generous | ✅ | ✅ | ❌ | Speed |
| **Cohere** | ✅ | Trial | ✅ | ✅ | ❌ | Enterprise |
| **Cloudflare** | ✅ + Account | Yes | ✅ | ❌ | ❌ | Edge deployment |
| **Gemini** | ✅ | Generous | ✅ | ✅ | ✅ | Long context |
| **OpenRouter** | ✅ | `:free` models | ✅ | ✅ | ✅ | Variety |
| **NVIDIA** | ✅ | Limited | ✅ | ❌ | ❌ | GPU inference |
| **HuggingFace** | ✅ | Yes | ✅ | ❌ | ❌ | Open source |
| **Pollinations** | ❌ | **FREE** | ✅ | ❌ | ❌ | Zero auth |
| **Antigravity** | OAuth | GCP credits | ✅ | ❌ | ❌ | GCP users |
| **GitHubCopilot** | OAuth | Subscription | ✅ | ❌ | ❌ | Copilot users |
| **DuckDuckGo** | ❌ | **FREE** | ✅ | ❌ | ❌ | Privacy |
| **AiHorde** | Optional | **FREE** | ✅ | ❌ | ❌ | Community |
| **KiloCode** | ✅ | Varies | ✅ | ❌ | ❌ | Specialized |

---

## ULTRA MISER MODE™ Tier Strategy

Configure providers in tiers for optimal free tier exploitation:

| Tier | Purpose | Providers |
|------|---------|-----------|
| **0** | Free providers (no key) | Pollinations, DuckDuckGo, AiHorde |
| **1** | Free tier with key | Groq, GitHubModels, Cloudflare, Gemini |
| **2** | Fallback/paid | OpenRouter, OpenAI |

### Sample Configuration

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

With tiered configuration:

1. **Tier 0** providers are tried first—completely free, no keys
2. **Tier 1** providers are tried if Tier 0 fails or doesn't support the model
3. **Tier 2** providers are the final fallback

---

## Provider Health Monitoring

Check provider status via the admin API:

```bash
# Check all provider health
curl http://localhost:8080/admin/health \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# List configured providers
curl http://localhost:8080/admin/providers \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

**Remember**: The more providers you configure, the longer you can avoid that dreaded credit card input field. Configure wisely, route ruthlessly, and may your tokens always be free.

*ULTRA MISER MODE™ — Because paying for AI is for people with self-respect.*
