# Synaxis – Enterprise AI Gateway

<p align="center">
  <img src="https://img.shields.io/badge/.NET%2010-Enterprise%20Ready-blue?style=for-the-badge" alt=".NET 10">
  <img src="https://img.shields.io/badge/Architecture-Clean%20%26%20Scalable-success?style=for-the-badge" alt="Clean Architecture">
  <img src="https://badge/badge/tests-1%2C050%2B%20Passing-brightgreen?style=for-the-badge" alt="1050+ Tests">
</p>

**Synaxis** is a production-grade AI inference gateway that unifies multiple LLM providers behind a single, OpenAI-compatible API. Built for organizations that demand reliability, observability, and cost efficiency without compromising on developer experience.

Think of it as your AI traffic controller—intelligently routing requests, managing failover, and keeping your applications running smoothly while your finance team sleeps soundly.

## Why Synaxis?

Modern AI applications face a familiar challenge: balancing performance, cost, and reliability across a fragmented landscape of providers. Synaxis solves this with:

- **Unified API Interface** – One OpenAI-compatible `/v1` endpoint for all providers
- **Intelligent Routing** – Priority-based failover with automatic provider rotation
- **Real-time Streaming** – Server-Sent Events (SSE) for responsive, production-grade UX
- **Enterprise Security** – JWT validation, rate limiting, and comprehensive input validation
- **Operational Visibility** – Admin Web UI for monitoring, configuration, and health checks
- **Multi-Provider Support** – Groq, Cloudflare Workers AI, Together AI, DeepInfra, Fireworks, Cohere, Lepton, and more

*And yes, it happens to be excellent at minimizing API spend. Call it fiscal responsibility with a sense of humor.*

## Features

- OpenAI-compatible `/v1/chat/completions` and `/v1/models` endpoints
- **Streaming responses** via Server-Sent Events (SSE)
- **Admin Web UI** for provider configuration and system management
- **Security hardening** with JWT auth, rate limiting, and security headers
- **Graceful degradation** with intelligent retry and failover logic
- **Priority-based routing** to optimize quota utilization across providers
- **Comprehensive provider ecosystem** supporting major inference platforms
- Clean Architecture with CQRS pattern for maintainability and testability

## Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker](https://docs.docker.com/get-docker/)

### Installation

```bash
# Clone the repository
git clone https://github.com/rudironsoni/Synaxis.git
cd Synaxis

# Restore dependencies
dotnet restore

# Build the solution
dotnet build
```

### Configuration

Update `src/InferenceGateway/WebApi/appsettings.json` with your provider credentials:

```json
{
  "Synaxis": {
    "InferenceGateway": {
      "JwtSecret": "YOUR_JWT_SECRET",
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

For Docker deployment, copy `.env.example` to `.env` and configure your environment variables.

See [Configuration Guide](docs/CONFIGURATION.md) for detailed setup instructions.

### Running the Application

```bash
# Development mode
dotnet run --project src/InferenceGateway/WebApi

# Production mode
dotnet run --project src/InferenceGateway/WebApi --configuration Release
```

The API will be available at `http://localhost:5000/v1/chat/completions`

## Usage

### Basic Request

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

### Streaming Request

```bash
curl http://localhost:5000/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "llama-3.3-70b-versatile",
    "stream": true,
    "messages": [
      { "role": "user", "content": "Tell me about AI infrastructure..." }
    ]
  }'
```

Streaming responses use Server-Sent Events (SSE) with `data: {json}` frames followed by `data: [DONE]`.

See [API Documentation](docs/API.md) for complete endpoint reference.

## Docker Compose

```bash
# Create environment file
cp .env.example .env

# Start with dev profile (includes pgAdmin)
docker compose --profile dev up --build
```

### Default Endpoints

| Service | URL |
|---------|-----|
| Web API | http://localhost:8080 |
| Admin UI | http://localhost:8080/admin |
| Postgres | localhost:5432 |
| Redis | localhost:6379 |
| pgAdmin | http://localhost:5050 |

### API Endpoints

**OpenAI-Compatible:**
- `POST /v1/chat/completions` – Chat completions
- `GET /v1/models` – List available models

**Admin (JWT Required):**
- `GET/POST /admin/providers` – Provider management
- `GET /admin/health` – System health

**Health Checks:**
- `GET /health/liveness` – Liveness probe
- `GET /health/readiness` – Readiness probe

## Documentation

### Core Documentation

- **[Architecture Overview](docs/ARCHITECTURE.md)** – Clean Architecture, CQRS, and routing strategy
- **[API Reference](docs/API.md)** – Complete OpenAI-compatible endpoint documentation
- **[Configuration Guide](docs/CONFIGURATION.md)** – Provider setup and environment configuration
- **[Deployment Guide](docs/DEPLOYMENT.md)** – Production deployment and infrastructure
- **[Security Guide](docs/SECURITY.md)** – Authentication, authorization, and security practices
- **[Testing Guide](docs/TESTING.md)** – Test infrastructure and development practices
- **[Contributing Guide](docs/CONTRIBUTING.md)** – Development workflow and standards

### Reference

- **[Providers](docs/reference/providers.md)** – Provider-specific details and capabilities
- **[Models](docs/reference/models.md)** – Supported models and feature matrix
- **[Errors](docs/reference/errors.md)** – Error codes and troubleshooting

### Architecture Decisions

- **[ADR-001: Stream-Native CQRS](docs/adr/001-stream-native-cqrs.md)**
- **[ADR-002: Tiered Routing Strategy](docs/adr/002-tiered-routing-strategy.md)**
- **[ADR-003: Authentication Architecture](docs/adr/003-authentication-architecture.md)**

### Operations

- **[Troubleshooting](docs/ops/troubleshooting.md)**
- **[Monitoring](docs/ops/monitoring.md)**
- **[Performance](docs/ops/performance.md)**

## Admin Web UI

Synaxis includes a React-based administration interface for managing your AI provider ecosystem:

1. Start the application (see Docker Compose section)
2. Navigate to `http://localhost:8080/admin`
3. Authenticate with your JWT token

### Features

- **Provider Management** – Configure API keys, endpoints, tiers, and models
- **Health Monitoring** – Real-time status of Redis, database, and providers
- **Access Control** – JWT-based authentication with role-based permissions

## Quality Assurance

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

## What's New

- **Streaming Support** – Real-time SSE streaming for chat completions
- **Admin Web UI** – React-based management interface
- **Security Hardening** – JWT validation, rate limiting, and security headers
- **Comprehensive Testing** – 635 backend + 415 frontend tests
- **Documentation Suite** – Complete technical documentation

## License

MIT – See [LICENSE](LICENSE) for details.

---

<p align="center">
  <em>Built with .NET 10, Clean Architecture principles, and a healthy respect for infrastructure budgets.</em>
</p>
