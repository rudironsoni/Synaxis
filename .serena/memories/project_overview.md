# Synaxis Project Overview

## Purpose
Synaxis is an SDK-first, enterprise AI Gateway that solves fragmentation in modern AI applications. It provides a composable SDK that can be embedded directly into applications, with support for multiple transports (HTTP, gRPC, WebSocket, SignalR) and AI providers (OpenAI, Azure, Anthropic, Google).

## Key Features
- **SDK-First Architecture**: Embed AI capabilities without HTTP overhead
- **Multi-Transport Support**: HTTP REST, gRPC, WebSocket, SignalR
- **Provider Agnostic**: Unified interface for multiple AI providers with failover
- **CQRS Architecture**: Clean separation using Mediator pattern
- **"Ultra Miser Mode"**: Optimized for free-tier AI provider rotation

## Tech Stack
- **Framework**: .NET 10 (net10.0)
- **Language**: C# with nullable reference types enabled
- **Architecture**: CQRS with Mediator pattern
- **AI Integration**: Microsoft Agents Framework, Microsoft.Extensions.AI
- **Databases**: PostgreSQL, Redis, Qdrant
- **Authentication**: JWT Bearer tokens, ASP.NET Core Identity
- **Testing**: xUnit, Moq, FluentAssertions, Testcontainers

## Codebase Structure
```
src/
  Synaxis.Core/             # Domain models & abstractions
  Synaxis.Api/              # Main ASP.NET Core API
  Synaxis.Infrastructure/   # Infrastructure concerns
  Synaxis.Providers.*/      # Provider implementations
  InferenceGateway/         # Main inference gateway
  Agents/                   # Agents bounded context
  Identity/                 # Identity bounded context

tests/
  Synaxis.*.UnitTests/
  Synaxis.*.IntegrationTests/
  Synaxis.TestUtilities/    # Shared test utilities

apps/
  studio-web/               # Web studio (Vite + React)
  studio-desktop/           # Desktop studio
  studio-mobile/            # Mobile studio
```

## Entry Points
1. **Synaxis.Api** - Main ASP.NET Core API host
2. **Synaxis.InferenceGateway.WebApi** - Inference gateway
3. **Synaxis.Server** - Standalone server host
4. **Identity.Api** - Identity service
5. **Agents.Api** - Agent management service

## Key Configuration Files
- `Synaxis.sln` - Main solution file
- `Directory.Build.props` - Global MSBuild properties
- `Directory.Packages.props` - Centralized NuGet versions
- `.editorconfig` - Code style rules
- `docker-compose.yml` - Local development infrastructure
