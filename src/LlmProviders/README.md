# LlmProviders Service

The **LlmProviders** microservice is a centralized gateway for Large Language Model interactions. It implements the **"Ultra-Miser"** routing logic to minimize costs while maintaining high availability and performance.

## 🚀 Ultra-Miser Architecture

The service routes requests through a tiered system, prioritizing free and fast providers before falling back to paid or slower options.

### Provider Tiers

1.  **Tier 1 (Free & Fast)**:
    *   **Cloudflare Workers AI** (@cf/meta/llama-3-8b-instruct, etc.)
    *   **DeepInfra** (Free models)
    *   **LambdaChat**
    *   **OpenAI FM** (Fine-tuned free models)
    *   **Pollinations**

2.  **Tier 2 (Paid & Reliable)**:
    *   **Groq** (Ultra-fast inference)
    *   **OpenRouter** (Aggregator)
    *   **Anthropic** (Claude)
    *   **Cohere**
    *   **Replicate**

3.  **Tier 3 (Browser/Ghost Mode)**:
    *   Uses browser automation to access web-based chat interfaces (e.g., Gemini Web, HuggingChat).
    *   *Note: Higher latency due to browser overhead.*

4.  **Tier 4 (Fallback)**:
    *   Experimental or lower-reliability browser-based providers.

### TieredProviderRouter

The `TieredProviderRouter` is the core orchestrator. For every request:
1.  It identifies eligible providers based on the requested model.
2.  It sorts them by **Tier** (Tier 1 > Tier 2 > Tier 3...).
3.  It attempts to execute the request with the first provider.
4.  If a provider fails, it logs the error and immediately attempts the next provider in the chain.
5.  It records success/failure metrics to optimize future routing.

## 🔌 API Endpoints

The service exposes an **OpenAI-compatible API**, allowing it to be used as a drop-in replacement for standard OpenAI clients.

### Chat Completions

**POST** `/v1/chat/completions`

**Request Body:**
```json
{
  "model": "llama-3-8b",
  "messages": [
    { "role": "user", "content": "Explain quantum computing." }
  ],
  "temperature": 0.7,
  "max_tokens": 1000,
  "stream": true
}
```

**Supported Models (Mapped automatically):**
*   `llama-3-8b` -> Cloudflare/Groq Llama 3 8B
*   `llama-3-70b` -> Cloudflare/Groq Llama 3 70B
*   `mistral-7b` -> Cloudflare Mistral
*   And many others.

## 🛠️ Configuration

Providers are configured via `appsettings.json` or Environment Variables.

```json
"Cloudflare": {
  "AccountId": "...",
  "ApiToken": "..."
}
```
