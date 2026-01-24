# LlmProviders

LlmProviders is a tiered LLM provider service that implements a sophisticated routing strategy to manage multiple AI model providers. It acts as an orchestration layer, providing a unified API for chat completions while intelligently delegating requests to various backends.

## Features

- **Tiered Routing Strategy**: Organizes providers into logical tiers (Tier 1 through Tier 4) to optimize for cost, performance, and reliability.
- **Unified Chat API**: Provides a consistent interface for chat completions, shielding consumers from the complexities of individual provider implementations.
- **gRPC Integration**: Built-in support for high-performance gRPC services, making it suitable for microservices architectures.
- **Provider Orchestration**: Manages provider pools, accounts, and sessions to ensure high availability and efficient resource utilization.
- **Ultra-Miser Mode**: Prioritizes free/fast Tier 1 providers.

## Project Structure

The service follows a Clean Architecture approach:

- **`ContextSavvy.LlmProviders.API`**: The entry point for the service, containing both REST and gRPC endpoints.
- **`ContextSavvy.LlmProviders.Application`**: Implements the business logic, command/query handlers, and orchestration flows.
- **`ContextSavvy.LlmProviders.Domain`**: Contains the core domain entities (`ProviderPool`, `ProviderAccount`, `ProviderSession`), value objects (`ProviderTier`, `ChatModels`), and repository interfaces.
- **`ContextSavvy.LlmProviders.Infrastructure`**: Handles data persistence, external service communication, and the concrete implementation of provider adapters.
- **`Shared/`**: Contains shared components like `EventBus`, `Core` abstractions, and `Contracts` used across the service.

## Dependencies

- **.NET 10**: The project targets the latest .NET runtime for performance and feature benefits.

## Getting Started

### Build

To build the service, run:

```bash
dotnet build
```

### Configuration

Ensure that your `appsettings.json` or environment variables are configured with the necessary provider credentials.

### Running the Service

Start the API project:

```bash
dotnet run --project src/LlmProviders/ContextSavvy.LlmProviders.API/ContextSavvy.LlmProviders.API.csproj
```

## Architecture Overview

LlmProviders uses a "Provider Pool" concept. When a request comes in, the service evaluates the requested model and tier, selects an appropriate `ProviderAccount` from the pool, and initiates a `ProviderSession` to handle the interaction. This allows for seamless rotation of accounts and load balancing across different LLM backends.

The service follows a strict tiering strategy:
- **Tier 1 (Free/Fast)**: Gemini Flash, Groq, OpenRouter (Free), HuggingFace, Pollinations, Cloudflare.
- **Tier 2 (Standard/Paid)**: Paid APIs like Cohere, DeepInfra, Perplexity, TogetherAI, etc.

## License

This project is licensed under the MIT License - see the LICENSE file for details.
