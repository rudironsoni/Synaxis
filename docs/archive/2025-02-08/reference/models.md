# Model Reference

> **ULTRA MISER MODE™ Pro Tip**: Context windows are like free tier quotas—bigger is better, and you'll never have enough. Choose models with 128K+ context and thank me later.

Complete reference for all AI models supported by Synaxis, including feature matrices, context windows, and provider mappings.

---

## Table of Contents

- [Model Feature Matrix](#model-feature-matrix)
- [Context Window Comparison](#context-window-comparison)
- [Provider-Specific Models](#provider-specific-models)
- [Canonical Model Mapping](#canonical-model-mapping)
- [Model Aliases](#model-aliases)
- [ULTRA MISER MODE™ Model Selection](#ultra-miser-mode-model-selection)

---

## Model Feature Matrix

Quick reference for model capabilities across all providers:

| Model | Provider | Streaming | Tools | Vision | JSON Mode | Context |
|-------|----------|-----------|-------|--------|-----------|---------|
| **GPT-4o** | OpenAI | ✅ | ✅ | ✅ | ✅ | 128K |
| **GPT-4o-mini** | OpenAI | ✅ | ✅ | ✅ | ✅ | 128K |
| **GPT-3.5-Turbo** | OpenAI | ✅ | ✅ | ❌ | ✅ | 16K |
| **Llama-3.3-70B** | Groq | ✅ | ✅ | ❌ | ❌ | 128K |
| **Llama-3.1-8B** | Groq | ✅ | ✅ | ❌ | ❌ | 128K |
| **Mixtral-8x7B** | Groq | ✅ | ✅ | ❌ | ❌ | 32K |
| **Command-R** | Cohere | ✅ | ✅ | ❌ | ❌ | 128K |
| **Command-R+** | Cohere | ✅ | ✅ | ❌ | ❌ | 128K |
| **Aya-32B** | Cohere | ✅ | ✅ | ❌ | ❌ | 128K |
| **Llama-3.1-8B** | Cloudflare | ✅ | ❌ | ❌ | ❌ | 8K |
| **Llama-3.2-3B** | Cloudflare | ✅ | ❌ | ❌ | ❌ | 8K |
| **Mistral-7B** | Cloudflare | ✅ | ❌ | ❌ | ❌ | 8K |
| **Phi-2** | Cloudflare | ✅ | ❌ | ❌ | ❌ | 2K |
| **Gemini-2.0-Flash** | Gemini | ✅ | ✅ | ✅ | ✅ | 1M |
| **Gemini-1.5-Pro** | Gemini | ✅ | ✅ | ✅ | ✅ | 2M |
| **Gemini-1.5-Flash** | Gemini | ✅ | ✅ | ✅ | ✅ | 1M |
| **Gemini-1.5-Flash-8B** | Gemini | ✅ | ✅ | ✅ | ✅ | 1M |
| **Llama-3.3-70B** | OpenRouter | ✅ | ✅ | ❌ | ❌ | 128K |
| **Mistral-7B** | OpenRouter | ✅ | ✅ | ❌ | ❌ | 32K |
| **Zephyr-7B** | OpenRouter | ✅ | ❌ | ❌ | ❌ | 32K |
| **Llama-3.3-70B** | NVIDIA | ✅ | ❌ | ❌ | ❌ | 128K |
| **Nemotron-70B** | NVIDIA | ✅ | ❌ | ❌ | ❌ | 128K |
| **DeepSeek-V3** | NVIDIA | ✅ | ❌ | ❌ | ❌ | 128K |
| **Phi-3.5-Mini** | HuggingFace | ✅ | ❌ | ❌ | ❌ | 128K |
| **SmolLM2-1.7B** | HuggingFace | ✅ | ❌ | ❌ | ❌ | 8K |
| **Qwen2.5-7B** | HuggingFace | ✅ | ❌ | ❌ | ❌ | 128K |
| **OpenAI** | Pollinations | ✅ | ❌ | ❌ | ❌ | 4K |
| **Mistral** | Pollinations | ✅ | ❌ | ❌ | ❌ | 4K |
| **Gemini-2.0-Flash** | Antigravity | ✅ | ❌ | ❌ | ❌ | 1M |
| **Copilot** | GitHubCopilot | ✅ | ❌ | ❌ | ❌ | 8K |
| **GPT-4o-mini** | DuckDuckGo | ✅ | ❌ | ❌ | ❌ | 8K |
| **Claude-3-Haiku** | DuckDuckGo | ✅ | ❌ | ❌ | ❌ | 200K |
| **Llama-3.1-70B** | DuckDuckGo | ✅ | ❌ | ❌ | ❌ | 128K |
| **AiHorde** | AiHorde | ✅ | ❌ | ❌ | ❌ | Varies |
| **DeepSeek-Chat** | DeepSeek | ✅ | ✅ | ❌ | ✅ | 64K |
| **DeepSeek-Reasoner** | DeepSeek | ✅ | ❌ | ❌ | ❌ | 64K |
| **GPT-4o** | GitHubModels | ✅ | ✅ | ✅ | ✅ | 128K |
| **Llama-405B** | GitHubModels | ✅ | ✅ | ❌ | ❌ | 128K |

---

## Context Window Comparison

### Massive Context (1M+ tokens)

| Model | Context | Provider | Use Case |
|-------|---------|----------|----------|
| **Gemini-1.5-Pro** | 2M | Gemini | Analyzing entire codebases, books |
| **Gemini-2.0-Flash** | 1M | Gemini | Long document processing |
| **Gemini-1.5-Flash** | 1M | Gemini | Cost-effective long context |
| **Gemini-1.5-Flash-8B** | 1M | Gemini | Fast long context |

### Large Context (100K-500K tokens)

| Model | Context | Provider | Use Case |
|-------|---------|----------|----------|
| **Claude-3-Haiku** | 200K | DuckDuckGo | Long conversations |
| **Llama-3.3-70B** | 128K | Groq/NVIDIA/OpenRouter | General purpose |
| **Command-R** | 128K | Cohere | RAG applications |
| **Command-R+** | 128K | Cohere | Complex RAG |
| **Aya-32B** | 128K | Cohere | Multilingual |
| **Qwen2.5-7B** | 128K | HuggingFace | Chinese/English |
| **Phi-3.5-Mini** | 128K | HuggingFace | Edge devices |
| **Llama-405B** | 128K | GitHubModels | Maximum quality |
| **GPT-4o** | 128K | OpenAI/GitHubModels | General purpose |
| **GPT-4o-mini** | 128K | OpenAI | Cost-effective |

### Medium Context (10K-100K tokens)

| Model | Context | Provider | Use Case |
|-------|---------|----------|----------|
| **DeepSeek-Chat** | 64K | DeepSeek | Coding, reasoning |
| **DeepSeek-Reasoner** | 64K | DeepSeek | Step-by-step reasoning |
| **Mixtral-8x7B** | 32K | Groq | MoE architecture |
| **Mistral-7B** | 32K | Cloudflare/OpenRouter | Efficient inference |
| **Zephyr-7B** | 32K | OpenRouter | Fine-tuned chat |

### Small Context (<10K tokens)

| Model | Context | Provider | Use Case |
|-------|---------|----------|----------|
| **Llama-3.1-8B** | 8K | Cloudflare | Fast responses |
| **Llama-3.2-3B** | 8K | Cloudflare | Mobile/edge |
| **SmolLM2-1.7B** | 8K | HuggingFace | Tiny models |
| **Copilot** | 8K | GitHubCopilot | IDE integration |
| **GPT-4o-mini** | 8K | DuckDuckGo | Quick tasks |
| **Phi-2** | 2K | Cloudflare | Simple tasks |
| **OpenAI** | 4K | Pollinations | Basic inference |
| **Mistral** | 4K | Pollinations | Basic inference |

---

## Provider-Specific Models

### OpenAI Models

| Model | Context | Features | Cost |
|-------|---------|----------|------|
| `gpt-4o` | 128K | Streaming, Tools, Vision, JSON | $$$ |
| `gpt-4o-mini` | 128K | Streaming, Tools, Vision, JSON | $$ |
| `gpt-3.5-turbo` | 16K | Streaming, Tools, JSON | $ |

### Groq Models

| Model | Context | Features | Speed |
|-------|---------|----------|-------|
| `llama-3.3-70b-versatile` | 128K | Streaming, Tools | ⚡⚡⚡ |
| `llama-3.1-8b-instant` | 128K | Streaming, Tools | ⚡⚡⚡⚡ |
| `mixtral-8x7b-32768` | 32K | Streaming, Tools | ⚡⚡⚡ |

### Cohere Models

| Model | Context | Features | Specialty |
|-------|---------|----------|-----------|
| `c4ai-aya-expanse-32b` | 128K | Streaming, Tools | Multilingual |
| `c4ai-aya-expanse-8b` | 128K | Streaming, Tools | Multilingual |
| `command-r` | 128K | Streaming, Tools | RAG |
| `command-r-plus` | 128K | Streaming, Tools | Advanced RAG |

### Cloudflare Models

| Model | Context | Features | Deployment |
|-------|---------|----------|------------|
| `@cf/meta/llama-3.1-8b-instruct` | 8K | Streaming | Edge |
| `@cf/meta/llama-3.2-3b-instruct` | 8K | Streaming | Edge/Mobile |
| `@cf/mistral/mistral-7b-instruct-v0.1` | 8K | Streaming | Edge |
| `@cf/microsoft/phi-2` | 2K | Streaming | Edge |

### Gemini Models

| Model | Context | Features | Specialty |
|-------|---------|----------|-----------|
| `gemini-2.0-flash` | 1M | Streaming, Tools, Vision, JSON | Speed + context |
| `gemini-1.5-pro` | 2M | Streaming, Tools, Vision, JSON | Maximum context |
| `gemini-1.5-flash` | 1M | Streaming, Tools, Vision, JSON | Cost-effective |
| `gemini-1.5-flash-8b` | 1M | Streaming, Tools, Vision, JSON | Fastest |

### OpenRouter Models (Free Tier)

| Model | Context | Features | Note |
|-------|---------|----------|------|
| `meta-llama/llama-3.3-70b-instruct:free` | 128K | Streaming, Tools | Add `:free` suffix |
| `mistralai/mistral-7b-instruct:free` | 32K | Streaming, Tools | Add `:free` suffix |
| `huggingfaceh4/zephyr-7b-beta:free` | 32K | Streaming | Add `:free` suffix |

### NVIDIA Models

| Model | Context | Features | GPU |
|-------|---------|----------|-----|
| `meta/llama-3.3-70b-instruct` | 128K | Streaming | H100 |
| `nvidia/llama-3.1-nemotron-70b-instruct` | 128K | Streaming | H100 |
| `deepseek-ai/deepseek-v3` | 128K | Streaming | H100 |

### HuggingFace Models

| Model | Context | Features | Size |
|-------|---------|----------|------|
| `microsoft/Phi-3.5-mini-instruct` | 128K | Streaming | 3.8B |
| `HuggingFaceTB/SmolLM2-1.7B-Instruct` | 8K | Streaming | 1.7B |
| `Qwen/Qwen2.5-7B-Instruct` | 128K | Streaming | 7B |

### Pollinations Models

| Model | Context | Features | Cost |
|-------|---------|----------|------|
| `openai` | 4K | Streaming | FREE |
| `mistral` | 4K | Streaming | FREE |

### DuckDuckGo Models

| Model | Context | Features | Privacy |
|-------|---------|----------|---------|
| `gpt-4o-mini` | 8K | Streaming | Anonymous |
| `claude-3-haiku` | 200K | Streaming | Anonymous |
| `llama-3.1-70b` | 128K | Streaming | Anonymous |

---

## Canonical Model Mapping

Canonical models map provider-specific model IDs to unified Synaxis identifiers:

```json
{
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
    },
    {
      "Id": "gemini/flash",
      "Provider": "Gemini",
      "ModelPath": "gemini-2.0-flash",
      "Streaming": true,
      "Tools": true,
      "Vision": true,
      "StructuredOutput": true,
      "LogProbs": false
    }
  ]
}
```

### Canonical Model Properties

| Property | Description | Required |
|----------|-------------|----------|
| `Id` | Unique canonical identifier | Yes |
| `Provider` | Provider name (must match config key) | Yes |
| `ModelPath` | Provider-specific model ID | Yes |
| `Streaming` | Supports SSE streaming | Yes |
| `Tools` | Supports function calling | Yes |
| `Vision` | Supports image input | Yes |
| `StructuredOutput` | Supports JSON mode | Yes |
| `LogProbs` | Supports log probabilities | Yes |

---

## Model Aliases

Aliases provide user-friendly names that map to multiple provider candidates:

### Standard Aliases

```json
{
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
    },
    "smart": {
      "Candidates": [
        "gemini/flash",
        "groq/llama-3.3-70b",
        "openai/gpt-4o-mini"
      ]
    },
    "fast": {
      "Candidates": [
        "groq/llama-3.1-8b",
        "gemini/flash-8b",
        "cloudflare/llama-3.2-3b"
      ]
    }
  }
}
```

### ULTRA MISER MODE™ Aliases

Pre-configured aliases for maximum free tier exploitation:

```json
{
  "Aliases": {
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
    },
    "miser-vision": {
      "Candidates": [
        "gemini/flash",
        "github/gpt-4o"
      ]
    },
    "miser-long-context": {
      "Candidates": [
        "gemini/pro",
        "ddg/claude-haiku"
      ]
    }
  }
}
```

**Usage:**

```bash
curl http://localhost:8080/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "miser-intelligence",
    "messages": [{"role": "user", "content": "Hello!"}]
  }'
```

---

## ULTRA MISER MODE™ Model Selection

### The Strategy

1. **Start with free providers** (Tier 0): Pollinations, DuckDuckGo, AiHorde
2. **Fall back to free-tier providers** (Tier 1): Groq, GitHubModels, Gemini
3. **Use OpenRouter `:free` models** as bridge
4. **Last resort**: Paid providers (Tier 2)

### Model Selection by Use Case

| Use Case | Recommended Model | Why |
|----------|-------------------|-----|
| **Quick chat** | `pollinations/openai` | Zero auth, instant |
| **Coding** | `deepseek-chat` | Code-optimized |
| **Reasoning** | `deepseek-reasoner` | Step-by-step |
| **Long documents** | `gemini-1.5-pro` | 2M context |
| **Vision** | `gemini-2.0-flash` | Native vision + 1M context |
| **Speed** | `groq/llama-3.1-8b` | Fastest inference |
| **Quality** | `github/llama-405b` | Largest open model |
| **Privacy** | `ddg/gpt-4o-mini` | Anonymous |
| **Multilingual** | `cohere/aya-32b` | 23 languages |
| **RAG** | `cohere/command-r` | Built for retrieval |

### Cost-Optimized Routing

```json
{
  "Aliases": {
    "default": {
      "Candidates": [
        "pollinations/openai",
        "ddg/gpt-4o-mini",
        "groq/llama-3.3-70b",
        "gemini/flash"
      ]
    }
  }
}
```

This configuration tries completely free providers first, then falls back to generous free tiers.

---

## Model Discovery

Synaxis can discover available models from providers:

```bash
# List all available models
curl http://localhost:8080/v1/models \
  -H "Authorization: Bearer YOUR_API_KEY"

# Response includes all canonical models and their capabilities
```

---

## Feature Support Details

### Streaming (SSE)

All native providers support Server-Sent Events (SSE) streaming:

```bash
curl http://localhost:8080/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "llama-3.3-70b",
    "stream": true,
    "messages": [{"role": "user", "content": "Hello"}]
  }'
```

### Tools / Function Calling

Supported by: OpenAI, Groq, Cohere, Gemini, OpenRouter (some models)

```json
{
  "model": "gpt-4o",
  "messages": [...],
  "tools": [
    {
      "type": "function",
      "function": {
        "name": "get_weather",
        "description": "Get weather for a location",
        "parameters": {...}
      }
    }
  ]
}
```

### Vision

Supported by: OpenAI (GPT-4o), Gemini (all models)

```json
{
  "model": "gpt-4o",
  "messages": [
    {
      "role": "user",
      "content": [
        {"type": "text", "text": "What's in this image?"},
        {"type": "image_url", "image_url": {"url": "https://example.com/image.jpg"}}
      ]
    }
  ]
}
```

### JSON Mode / Structured Output

Supported by: OpenAI, Gemini, DeepSeek

```json
{
  "model": "gpt-4o",
  "messages": [...],
  "response_format": {"type": "json_object"}
}
```

---

**Remember**: Context windows are like free tier quotas—you'll always want more than you have. Plan accordingly, summarize aggressively, and may your tokens always fit within the window.

*ULTRA MISER MODE™ — Because paying for AI is for people with self-respect.*
