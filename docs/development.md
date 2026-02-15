# Synaxis Development Guide

This guide covers setting up a development environment, understanding the project structure, running locally, testing, and contributing to Synaxis.

## Table of Contents

- [Development Setup](#development-setup)
- [Project Structure](#project-structure)
- [Running Locally](#running-locally)
- [Testing](#testing)
- [Building](#building)
- [Debugging](#debugging)
- [Contributing](#contributing)
- [Code Style](#code-style)
- [Release Process](#release-process)

## Development Setup

### Prerequisites

- **.NET 10 SDK** ([Download](https://dotnet.microsoft.com/download))
- **Git** ([Download](https://git-scm.com/downloads))
- **Docker** ([Download](https://www.docker.com/products/docker-desktop))
- **Docker Compose** (included with Docker Desktop)
- **IDE**: Visual Studio 2022, VS Code, or Rider

### Clone the Repository

```bash
git clone https://github.com/rudironsoni/Synaxis.git
cd Synaxis
```

### Install Dependencies

```bash
dotnet restore
```

### Set Up Environment Variables

Copy the example environment file:

```bash
cp .env.example .env
```

Edit `.env` and add your API keys:

```bash
# Required
OPENAI_API_KEY=sk-your-openai-key
AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com

# Optional (for full features)
POSTGRES_CONNECTION_STRING=Host=localhost;Database=synaxis;Username=synaxis;Password=your-password
REDIS_CONNECTION_STRING=localhost:6379,password=your-password
```

### Start Supporting Services

```bash
docker-compose up -d postgres redis
```

### Verify Setup

```bash
dotnet build
dotnet test
```

## Project Structure

```
Synaxis/
├── src/                          # Source code
│   ├── Synaxis.Abstractions/     # Core interfaces and abstractions
│   ├── Synaxis.Contracts/        # Request/response DTOs
│   ├── Synaxis.Core/             # Core business logic
│   ├── Synaxis.Infrastructure/   # Infrastructure implementations
│   ├── Synaxis.Api/              # HTTP API layer
│   ├── Synaxis.Server/           # Standalone server
│   ├── Synaxis.Providers/        # Provider base classes
│   ├── Synaxis.Providers.OpenAI/ # OpenAI provider
│   ├── Synaxis.Providers.Azure/  # Azure OpenAI provider
│   ├── Synaxis.Providers.Anthropic/ # Anthropic provider
│   ├── Synaxis.Routing/          # Provider routing logic
│   ├── Synaxis.Webhooks/         # Webhook handling
│   ├── Synaxis.BatchProcessing/  # Batch processing
│   ├── Synaxis.Transport.Http/   # HTTP transport
│   ├── Synaxis.Transport.Grpc/   # gRPC transport
│   ├── Synaxis.Transport.WebSocket/ # WebSocket transport
│   ├── Synaxis.Adapters.SignalR/ # SignalR adapter
│   ├── Synaxis.Adapters.Mcp/     # MCP adapter
│   └── Synaxis.Adapters.Agents/  # Agents adapter
├── tests/                        # Test projects
│   ├── Synaxis.Core.Tests/       # Core unit tests
│   ├── Synaxis.Infrastructure.Tests/ # Infrastructure tests
│   ├── Synaxis.Api.Tests/        # API tests
│   ├── Synaxis.Integration.Tests/ # Integration tests
│   └── Synaxis.Benchmarks/       # Performance benchmarks
├── docs/                         # Documentation
│   ├── getting-started.md
│   ├── architecture.md
│   ├── api-reference.md
│   ├── deployment.md
│   └── development.md
├── samples/                      # Sample applications
│   ├── MinimalApi/               # Minimal API example
│   ├── SelfHosted/               # Self-hosted gateway
│   ├── SaaSClient/               # SaaS client example
│   └── Microservices/            # Microservices example
├── scripts/                      # Utility scripts
│   ├── build.sh
│   ├── test.sh
│   └── release.sh
├── infra/                        # Infrastructure as Code
│   ├── terraform/
│   ├── bicep/
│   └── kubernetes/
├── Synaxis.sln                   # Solution file
├── Directory.Build.props         # Common build properties
├── Directory.Packages.props      # Common package versions
├── .editorconfig                 # Editor configuration
├── .gitignore                    # Git ignore rules
├── .env.example                  # Environment variables template
├── docker-compose.yml            # Docker Compose configuration
├── Dockerfile                    # Docker image definition
└── README.md                     # Project README
```

### Package Dependencies

```
Synaxis.Abstractions (no external dependencies)
    ↓
Synaxis.Contracts
    ↓
Synaxis.Core
    ↓
Synaxis.Infrastructure
    ↓
Synaxis.Api / Synaxis.Server
    ↓
Synaxis.Providers.*
    ↓
Synaxis.Transport.*
```

## Running Locally

### Option 1: Run the Server

```bash
dotnet run --project src/Synaxis.Server/Synaxis.Server.csproj
```

The server will start on `http://localhost:8080`

### Option 2: Run with Docker Compose

```bash
docker-compose up
```

This starts:
- Synaxis Gateway on port 8080
- PostgreSQL on port 5432
- Redis on port 6379

### Option 3: Run in Debug Mode (VS Code)

1. Open the project in VS Code
2. Press `F5` or click "Run and Debug"
3. Select ".NET Core Launch (web)"

### Option 4: Run in Debug Mode (Visual Studio)

1. Open `Synaxis.sln`
2. Set `Synaxis.Server` as startup project
3. Press `F5` to start debugging

### Verify It's Running

```bash
curl http://localhost:8080/health
```

Expected response:

```json
{
  "status": "healthy",
  "version": "1.0.0"
}
```

## Testing

### Run All Tests

```bash
dotnet test
```

### Run Specific Test Project

```bash
dotnet test tests/Synaxis.Core.Tests/Synaxis.Core.Tests.csproj
```

### Run with Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
```

Generate coverage report:

```bash
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coverage-report
```

### Run Integration Tests

```bash
dotnet test tests/Synaxis.Integration.Tests/Synaxis.Integration.Tests.csproj
```

### Run Benchmarks

```bash
dotnet run --project tests/Synaxis.Benchmarks/Synaxis.Benchmarks.csproj
```

### Test Categories

Tests are organized by category:

- **Unit Tests**: Fast, isolated tests
- **Integration Tests**: Tests with external dependencies
- **End-to-End Tests**: Full workflow tests
- **Performance Tests**: Benchmark tests

Run specific category:

```bash
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=Integration"
dotnet test --filter "Category=E2E"
```

## Building

### Build Debug

```bash
dotnet build
```

### Build Release

```bash
dotnet build -c Release
```

### Build with Warnings as Errors

```bash
dotnet build -warnaserror
```

### Build Specific Project

```bash
dotnet build src/Synaxis.Api/Synaxis.Api.csproj
```

### Build Docker Image

```bash
docker build -t synaxis/gateway:latest .
```

### Build Multi-Architecture Image

```bash
docker buildx build --platform linux/amd64,linux/arm64 -t synaxis/gateway:latest .
```

## Debugging

### Attach Debugger

#### VS Code

1. Set breakpoints in your code
2. Press `F5` to start debugging
3. Use the Debug Console for inspection

#### Visual Studio

1. Set breakpoints by clicking in the gutter
2. Press `F5` to start debugging
3. Use the Immediate Window for inspection

#### Rider

1. Set breakpoints by clicking in the gutter
2. Press `F5` to start debugging
3. Use the Evaluate Expression tool

### Remote Debugging

#### Debug Docker Container

```bash
docker run -d \
  -p 8080:8080 \
  -e DOTNET_USE_POLLING_FILE_WATCHER=1 \
  -v $(pwd):/app \
  synaxis/gateway:latest
```

Attach debugger to the container process.

### Logging

Enable debug logging:

```bash
export LOG_LEVEL=Debug
dotnet run
```

Logs are written to:
- Console (structured JSON)
- File (logs/synaxis.log)

### Profiling

Use dotnet-trace:

```bash
dotnet-trace collect --process-id <pid> --output trace.nettrace
```

Analyze with PerfView or SpeedScope.

## Contributing

### Workflow

1. **Fork the repository**
2. **Create a feature branch**: `git checkout -b feature/my-feature`
3. **Make your changes**
4. **Run tests**: `dotnet test`
5. **Commit changes**: `git commit -m "Add my feature"`
6. **Push to branch**: `git push origin feature/my-feature`
7. **Create a Pull Request**

### Commit Message Format

Follow conventional commits:

```
<type>(<scope>): <subject>

<body>

<footer>
```

Types:
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting)
- `refactor`: Code refactoring
- `test`: Test changes
- `chore`: Build process or auxiliary tool changes

Example:

```
feat(providers): add Google Gemini provider

Add support for Google's Gemini models including:
- Gemini Pro
- Gemini Ultra
- Streaming support

Closes #123
```

### Pull Request Guidelines

1. **Title**: Use conventional commit format
2. **Description**: Explain what and why
3. **Linked Issues**: Reference related issues
4. **Tests**: Include tests for new features
5. **Docs**: Update documentation
6. **CI**: Ensure all checks pass

### Code Review Process

1. Automated checks must pass
2. At least one approval required
3. Address all review comments
4. Squash commits if needed
5. Merge after approval

## Code Style

### C# Style Guide

Follow the official [C# coding conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions):

- **PascalCase** for classes, methods, properties
- **camelCase** for parameters, local variables
- **_camelCase** for private fields
- **UPPER_CASE** for constants

### Example

```csharp
public class ChatCompletionHandler
{
    private readonly IProviderSelector _providerSelector;
    private readonly ILogger<ChatCompletionHandler> _logger;

    public ChatCompletionHandler(
        IProviderSelector providerSelector,
        ILogger<ChatCompletionHandler> logger)
    {
        _providerSelector = providerSelector;
        _logger = logger;
    }

    public async Task<ChatCompletionResponse> HandleAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken)
    {
        var provider = await _providerSelector.SelectProviderAsync(request);
        return await provider.CompleteAsync(request, cancellationToken);
    }
}
```

### Formatting

Run the formatter before committing:

```bash
dotnet format
```

Verify no changes:

```bash
dotnet format --verify-no-changes
```

### EditorConfig

The project includes `.editorconfig` for consistent formatting across IDEs.

### Naming Conventions

| Type | Convention | Example |
|------|------------|---------|
| Classes | PascalCase | `ChatCompletionHandler` |
| Interfaces | PascalCase with I prefix | `IChatProvider` |
| Methods | PascalCase | `CompleteAsync` |
| Properties | PascalCase | `Model` |
| Fields | _camelCase | `_providerSelector` |
| Constants | UPPER_CASE | `MAX_RETRIES` |
| Parameters | camelCase | `request` |
| Local Variables | camelCase | `provider` |

### Async/Await Guidelines

- Use `async`/`await` for all async methods
- Use `CancellationToken` for long-running operations
- Configure `await` using: `await task.ConfigureAwait(false)`

### Exception Handling

- Use specific exception types
- Include meaningful error messages
- Log exceptions with context

```csharp
try
{
    return await provider.CompleteAsync(request, cancellationToken);
}
catch (ProviderException ex)
{
    _logger.LogError(ex, "Provider error: {Message}", ex.Message);
    throw;
}
catch (OperationCanceledException)
{
    _logger.LogWarning("Request cancelled");
    throw;
}
```

### Dependency Injection

- Use constructor injection
- Register services in `Program.cs`
- Use interfaces for dependencies

```csharp
public class ChatCompletionHandler
{
    private readonly IProviderSelector _providerSelector;
    private readonly ILogger<ChatCompletionHandler> _logger;

    public ChatCompletionHandler(
        IProviderSelector providerSelector,
        ILogger<ChatCompletionHandler> logger)
    {
        _providerSelector = providerSelector;
        _logger = logger;
    }
}
```

## Release Process

### Versioning

Follow [Semantic Versioning](https://semver.org/):

- **MAJOR**: Incompatible API changes
- **MINOR**: Backwards-compatible functionality
- **PATCH**: Backwards-compatible bug fixes

### Release Checklist

1. **Update version** in `Directory.Build.props`
2. **Update CHANGELOG.md**
3. **Run all tests**: `dotnet test`
4. **Build release**: `dotnet build -c Release`
5. **Create Git tag**: `git tag v1.0.0`
6. **Push tag**: `git push origin v1.0.0`
7. **Create GitHub release**
8. **Publish NuGet packages**
9. **Publish Docker image**

### Publishing NuGet Packages

```bash
dotnet pack -c Release
dotnet nuget push src/**/*.nupkg --api-key your-key --source https://api.nuget.org/v3/index.json
```

### Publishing Docker Image

```bash
docker build -t synaxis/gateway:1.0.0 .
docker tag synaxis/gateway:1.0.0 synaxis/gateway:latest
docker push synaxis/gateway:1.0.0
docker push synaxis/gateway:latest
```

### Release Notes Template

```markdown
## [1.0.0] - 2024-02-15

### Added
- New feature 1
- New feature 2

### Changed
- Updated dependency X to version Y

### Fixed
- Bug fix 1
- Bug fix 2

### Deprecated
- Feature X (will be removed in 2.0.0)

### Removed
- Feature Y
```

## Additional Resources

### Documentation

- [Getting Started](./getting-started.md)
- [Architecture](./architecture.md)
- [API Reference](./api-reference.md)
- [Deployment](./deployment.md)

### External Resources

- [.NET Documentation](https://learn.microsoft.com/en-us/dotnet/)
- [ASP.NET Core Documentation](https://learn.microsoft.com/en-us/aspnet/core/)
- [MediatR Documentation](https://github.com/jbogard/MediatR)
- [OpenAI API Documentation](https://platform.openai.com/docs)

### Community

- [GitHub Issues](https://github.com/rudironsoni/Synaxis/issues)
- [GitHub Discussions](https://github.com/rudironsoni/Synaxis/discussions)
- [Discord](https://discord.gg/synaxis)

---

**Happy coding!** 🚀
