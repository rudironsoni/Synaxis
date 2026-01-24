# Synaxis

**Synaxis** is a robust, clean-architecture AI Gateway built on .NET 9. It serves as a unified interface for multiple LLM providers (OpenAI, Groq, Cohere, Cloudflare, etc.), offering intelligent routing, failover protection, and load balancing.

## Key Features

*   **Unified API:** Access multiple LLM providers through a single, OpenAI-compatible interface.
*   **Intelligent Routing ("The Brain"):** Requests are routed based on the requested model ID.
*   **Tiered Failover:** Configure providers in tiers. If a Tier 1 provider fails, Synaxis automatically fails over to Tier 2, and so on.
*   **Load Balancing:** Requests within the same tier are shuffled to distribute load across available providers.
*   **Clean Architecture:** Structured for maintainability and testability (`Api`, `Application`, `Infrastructure`).
*   **Extensible:** Easily add new providers via the `IProviderRegistry` and `IChatClient` interface.

## Quick Start

### Prerequisites

*   [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### Installation

1.  **Clone the repository:**
    ```bash
    git clone https://github.com/rudironsoni/Synaxis.git
    cd Synaxis
    ```

2.  **Configure Providers:**
    *   Open `src/Synaxis.WebApi/appsettings.json`.
    *   Add your API keys for the providers you wish to use.
    *   See [Configuration Guide](docs/CONFIGURATION.md) for details.

3.  **Run Locally:**
    ```bash
    dotnet run --project src/Synaxis.WebApi/Synaxis.WebApi.csproj
    ```

    The API will start (default is typically `http://localhost:5000` or `https://localhost:5001`).

## Usage

Send an OpenAI-compatible request to the gateway. For example, using `curl`:

```bash
curl http://localhost:5000/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "llama-3.3-70b-versatile",
    "messages": [
      { "role": "user", "content": "Hello, world!" }
    ]
  }'
```

Synaxis will inspect the `model` parameter, find the configured provider (e.g., Groq), and route the request accordingly.

## Documentation

*   [Architecture Overview](docs/ARCHITECTURE.md)
*   [Configuration Guide](docs/CONFIGURATION.md)
