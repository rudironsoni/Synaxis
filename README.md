# Synaxis – Because Paying for AI Is for People With Self-Respect

<p align="center">
  <img src="https://img.shields.io/badge/.NET%2010-the%20future%20is%20free(ish)-blue?style=for-the-badge" alt=".NET 10">
  <img src="https://img.shields.io/badge/mindset-ULTRA%20MISER%20MODE™-orange?style=for-the-badge" alt="Ultra Miser Mode">
  <img src="https://img.shields.io/badge/spending%20money-never-brightgreen?style=for-the-badge" alt="Zero Spend Energy">
</p>

**Synaxis** — the dignified art project of routing prompts through every free inference crumb on the internet before anyone dares ask you for a credit card.

This isn't just software.
It's **ULTRA MISER MODE™**: a lifestyle choice. A philosophy. A quiet rebellion against subscription fatigue.
A lovingly architected reminder that tokens should be free, and if they're not, we'll just rotate until they are.

## The Core Gag (aka Why This Exists)

> Craving Claude-3.5 / GPT-4o / Llama-405B quality but your wallet is practicing minimalism?
> Welcome home, fellow connoisseur of other people's free tiers.

ULTRA MISER MODE™ exists for:

- People who flinch at $5 the way normal humans flinch at spiders
- Devs whose monthly burn rate is measured in leftover API credits
- Anyone who has ever whispered "just one more prompt" while watching Groq's counter tick toward zero
- broke geniuses, caffeine addicts, and professional quota evaders worldwide

## Features (87% of which exist purely to provide free usage)

- One beautiful OpenAI-compatible `/v1` endpoint (so you don't have to touch your client code ever again)
- **Real-time streaming responses** with Server-Sent Events (SSE) for that premium, instant-gratification feeling
- **Admin Web UI** for provider configuration, health monitoring, and system management
- **Comprehensive security hardening** with JWT validation, rate limiting, input validation, and security headers
- **Intelligent error handling** with graceful degradation and automatic retry logic
- Automatic, merciless rotation when any provider inevitably says "you've had enough generosity for today"
- Failover choreography so smooth it almost feels ethical
- Priority-based routing (burn the highest-quota ones first, obviously)
- Natively supports Groq, Cloudflare Workers AI, Together AI, DeepInfra, Fireworks, Cohere, Lepton, and whatever sad little free tier appears next Tuesday
- Zero euros spent until the heat death of the universe (or until someone actually sponsors this chaos)

## Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker](https://docs.docker.com/get-docker/)

### 1. Installation – Costs: €0.00 (electricity not included)

```bash
# Clone before the guilt sets in
git clone https://github.com/rudironsoni/Synaxis.git
cd Synaxis

# Restore packages on someone else's electricity bill
dotnet restore

# Build — because even misers deserve snappy startup
dotnet build
```

### 2. Configuring Your Miser Empire

Update `src/InferenceGateway/WebApi/appsettings.json` with your provider keys.
For Docker, copy `.env.example` to `.env` and fill in the placeholders.

See [Configuration Guide](docs/CONFIGURATION.md) for detailed provider setup.

Quick configuration example:

```json
{
  "Synaxis": {
    "InferenceGateway": {
      "JwtSecret": "REPLACE_WITH_JWT_SECRET",
      "Providers": {
        "Groq": {
          "Enabled": true,
          "Type": "groq",
          "Key": "GROQ_API_KEY",
          "Tier": 0,
          "Models": ["llama-3.1-70b-versatile"]
        },
        "DeepSeek": {
          "Enabled": true,
          "Type": "openai",
          "Endpoint": "https://api.deepseek.com/v1",
          "Key": "DEEPSEEK_API_KEY",
          "Tier": 1,
          "Models": ["deepseek-chat"]
        }
      },
      "CanonicalModels": [
        {
          "Id": "deepseek-chat",
          "Provider": "DeepSeek",
          "ModelPath": "deepseek-chat",
          "Streaming": true,
          "Tools": true,
          "Vision": false,
          "StructuredOutput": false,
          "LogProbs": false
        }
      ],
      "Aliases": {
        "default": {
          "Candidates": ["deepseek-chat"]
        }
      }
    },
    "ControlPlane": {
      "ConnectionString": "Host=localhost;Database=synaxis;Username=postgres;Password=postgres"
    }
  },
  "ConnectionStrings": {
    "Redis": "localhost:6379,abortConnect=false"
  }
}
```

### 3. Running the Beast (While Mentally Thanking Providers for Their Charity)

```bash
# Normal person way
dotnet run --project src/InferenceGateway/WebApi

# Miser-optimized (skip build if you're feeling extra cheap)
dotnet run --project src/InferenceGateway/WebApi --configuration Release
```

Then point any OpenAI client to:

```
http://localhost:5000/v1/chat/completions
```

And bask in the warm glow of 16k tokens costing literally nothing.

## Usage

Send an OpenAI-compatible request to the gateway:

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

### Streaming Support

Enable real-time streaming with `stream: true`:

```bash
curl http://localhost:5000/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "llama-3.3-70b-versatile",
    "stream": true,
    "messages": [
      { "role": "user", "content": "Tell me a story about AI..." }
    ]
  }'
```

Synaxis routes requests based on the `model` parameter. Streaming responses use Server-Sent Events (SSE) with `data: {json}` frames followed by `data: [DONE]`.

See [API Documentation](docs/API.md) for complete endpoint reference.

## Docker Compose (Dev)

```bash
# Create your env file
cp .env.example .env

# Start dependencies + API + WebApp (pgAdmin is in the dev profile)
docker compose --profile dev up --build
```

Default ports:
- **Web API:** http://localhost:8080
- **Web App (Admin UI):** http://localhost:8080/admin
- **Postgres:** localhost:5432
- **Redis:** localhost:6379
- **pgAdmin:** http://localhost:5050

### API Endpoints

#### Core OpenAI-Compatible Endpoints
- **Chat Completions:** `http://localhost:8080/v1/chat/completions`
- **Streaming:** `http://localhost:8080/v1/chat/completions` (with `stream: true`)
- **Models Listing:** `http://localhost:8080/v1/models`

#### Admin API Endpoints (JWT Required)
- **Provider Management:** `http://localhost:8080/admin/providers`
- **Health Monitoring:** `http://localhost:8080/admin/health`

#### Health & Monitoring
- **Liveness Check:** `http://localhost:8080/health/liveness`
- **Readiness Check:** `http://localhost:8080/health/readiness`

## Documentation

### Core Documentation

- **[Architecture Overview](docs/ARCHITECTURE.md)** — Clean Architecture deep dive, CQRS pipeline, and tiered routing
- **[API Reference](docs/API.md)** — Complete OpenAI-compatible endpoint documentation
- **[Configuration Guide](docs/CONFIGURATION.md)** — Provider setup, environment variables, and Docker configuration
- **[Deployment Guide](docs/DEPLOYMENT.md)** — Production deployment, Docker Compose, and infrastructure setup
- **[Security Guide](docs/SECURITY.md)** — Authentication, authorization, rate limiting, and security best practices
- **[Testing Guide](docs/TESTING.md)** — Test infrastructure, running tests, and writing new tests
- **[Contributing Guide](docs/CONTRIBUTING.md)** — Development setup, PR process, and code standards

### Reference Documentation

- **[Providers](docs/reference/providers.md)** — Provider-specific details, capabilities, and limitations
- **[Models](docs/reference/models.md)** — Supported models, feature matrix, and context windows
- **[Errors](docs/reference/errors.md)** — Error codes, troubleshooting, and debugging

### Architecture Decisions (ADRs)

- **[ADR-001: Stream-Native CQRS](docs/adr/001-stream-native-cqrs.md)** — Why we built streaming into the core
- **[ADR-002: Tiered Routing Strategy](docs/adr/002-tiered-routing-strategy.md)** — Routing algorithm and failover design
- **[ADR-003: Authentication Architecture](docs/adr/003-authentication-architecture.md)** — OAuth and JWT implementation decisions

### Operational Guides

- **[Troubleshooting](docs/ops/troubleshooting.md)** — Common issues and solutions
- **[Monitoring](docs/ops/monitoring.md)** — Health checks, metrics, and observability
- **[Performance](docs/ops/performance.md)** — Benchmarks, optimization tips, and profiling

## Admin Web UI

Synaxis includes a beautiful React-based admin interface for managing your AI provider ecosystem:

### Access the Admin UI

1. **Start the application** (see Docker Compose section above)
2. **Navigate to** `http://localhost:8080/admin`
3. **Authenticate** using your JWT token

### Admin Features

- **Provider Configuration** — Configure API keys, endpoints, tiers, and model availability
- **Health Monitoring Dashboard** — Monitor Redis, database, and provider status in real-time
- **Authentication & Security** — JWT-based access with role-based permissions

## Testing & Quality

Synaxis maintains comprehensive test coverage:

- **Backend:** 635 tests (xUnit with integration tests)
- **Frontend:** 415 tests (Vitest + React Testing Library, 85.77% coverage)
- **Zero Flakiness:** All tests are deterministic
- **Zero Warnings:** Clean builds with no compiler or ESLint warnings

```bash
# Backend tests
dotnet test src/InferenceGateway/InferenceGateway.sln

# Frontend tests
cd src/Synaxis.WebApp/ClientApp && npm test

# E2E tests
npm run test:e2e
```

See [Testing Guide](docs/TESTING.md) for detailed test documentation.

## Recent Updates

- **Streaming Support** — Real-time SSE streaming for chat completions
- **Admin Web UI** — React-based interface for provider management
- **Security Hardening** — JWT validation, rate limiting, and security headers
- **Comprehensive Testing** — 635 backend + 415 frontend tests
- **Documentation Suite** — Complete documentation with ULTRA MISER MODE™ personality

## License

MIT — because monetizing a free-tier proxy would be performance art, not software.

**ULTRA MISER MODE™** — not a feature. A way of life. Built with spite, clean architecture pride, and the tears of expired API keys.

Enjoy (or hoard — we don't judge).

---

<p align="center">
  <em>Proud .NET 10 energy all the way.</em>
</p>
