# Synaxis (formerly Synaplexer)

A lightweight, configuration-driven AI Gateway that unifies multiple LLM providers under a single OpenAI-compatible API.

## Key Features

- **Unified API**: Drop-in replacement for OpenAI clients (`/v1/chat/completions`).
- **Smart Routing**: Tiered failover (e.g., try Free Tier first, then Paid).
- **Config-Driven**: No database required. Defined entirely in `appsettings.json`.
- **Broad Support**: Groq, Cohere, Gemini, Cloudflare, Pollinations, OpenRouter.

## Configuration

Synaxis is configured via `appsettings.json`. Below is an example configuration:

```json
{
  "Synaxis": {
    "Providers": {
      "Groq": {
        "Type": "Groq",
        "Key": "YOUR_GROQ_API_KEY",
        "Tier": 1,
        "Models": [
          "llama-3.3-70b-versatile",
          "llama-3.1-8b-instant"
        ]
      },
      "Gemini": {
        "Type": "Gemini",
        "Key": "YOUR_GEMINI_API_KEY",
        "Tier": 1,
        "Models": [
          "gemini-1.5-flash",
          "gemini-1.5-pro"
        ]
      },
      "OpenRouter": {
        "Type": "OpenRouter",
        "Key": "YOUR_OPENROUTER_API_KEY",
        "Tier": 2,
        "Models": [
          "mistralai/mistral-7b-instruct:free"
        ]
      }
    }
  }
}
```

## Running

### Local
To run the API locally:
```bash
dotnet run --project src/Synaxis.Api
```

### Docker
To run using Docker:
```bash
docker-compose up --build
```

The gateway will be available at `http://localhost:8080`.
