# Synaxis 2.0 (Next Generation)

This is the new Agentic Gateway implementation using the **Microsoft Agent Framework** (`Microsoft.Extensions.AI` / `Microsoft.Agents`).

## Architecture

The solution is split into three focused layers:

1.  **`Synaxis.Connectors`** (`src/Synaxis.Connectors`)
    *   Pure implementations of `IChatClient`.
    *   **Gemini**: Uses the official `Google.GenAI` SDK.
    *   **Groq**: Uses a custom adapter for `TryAGI.Groq`.
    *   **Pattern**: Implements `client.AsIChatClient(modelId)` for easy registration.

2.  **`Synaxis.Brain`** (`src/Synaxis.Brain`)
    *   **Routing**: `RoutingChatClient` selects the provider based on the model name (e.g., `gemini*` -> Google, `llama*` -> Groq).
    *   **Telemetry**: `UsageTrackingChatClient` intercepts calls to log token usage and estimated costs.
    *   **Orchestration**: Wired via `BrainExtensions`.

3.  **`Synaxis.Gateway`** (`src/Synaxis.Gateway`)
    *   **API**: ASP.NET Core 9.0 Web API.
    *   **Endpoints**:
        *   `POST /v1/chat/completions`: OpenAI-compatible endpoint supporting SSE streaming.
    *   **Configuration**: Loads keys from `GROQ_API_KEY` and `GEMINI_API_KEY` environment variables.

## Getting Started

### Prerequisites
*   .NET 9.0 SDK
*   API Keys for Groq and Google Gemini

### Running the Gateway

```bash
# Set your API keys
export GROQ_API_KEY="gsk_..."
export GEMINI_API_KEY="AIza..."

# Run the project
dotnet run --project src/Synaxis.Gateway/Synaxis.Gateway.csproj
```

### Testing with Curl

```bash
curl http://localhost:5000/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "gemini-2.0-flash",
    "messages": [
      { "role": "user", "content": "Explain quantum computing in one sentence." }
    ],
    "stream": true
  }'
```

## Testing

The solution includes comprehensive unit and integration tests (>87% coverage).

```bash
dotnet test Synaxis.Next.sln
```
