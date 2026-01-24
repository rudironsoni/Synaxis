# Synaplexer: The Intelligent LLM Gateway

A unified interface for managing multiple LLM providers with smart routing.

## Key Features
- **Multi-Key Load Balancing**: Distribute requests across multiple API keys per provider.
- **Priority-Based Routing**: Tiered routing to optimize for cost and reliability.
- **Automatic Failover**: Seamlessly switches to the next available provider on failure.

## Supported Providers
1. **Cohere**
2. **Groq**
3. **Gemini**
4. **Cloudflare**
5. **NVIDIA**
6. **HuggingFace**
7. **Pollinations**
8. **OpenRouter**
9. **DeepInfra**

## Configuration
Configure your providers in `appsettings.json`:

```json
"Providers": {
  "Cohere": { "Priority": 1, "ApiKeys": ["YOUR_KEY_HERE"] },
  "Groq": { "Priority": 1, "ApiKeys": ["YOUR_KEY_HERE"] }
}
```

## Usage

### HTTP API
The service exposes an OpenAI-compatible endpoint:

```bash
POST /v1/chat/completions
{
  "model": "llama-3",
  "messages": [{"role": "user", "content": "Hello!"}]
}
```

### gRPC
High-performance gRPC endpoints are available for internal service communication.
