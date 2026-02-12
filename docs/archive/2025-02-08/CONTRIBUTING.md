# Contributing to Synaxis

<p align="center">
  <img src="https://img.shields.io/badge/mindset-ULTRA%20MISER%20MODE™-orange?style=for-the-badge" alt="Ultra Miser Mode">
  <img src="https://img.shields.io/badge/contributions-free%20labor%20welcome-brightgreen?style=for-the-badge" alt="Free Labor Welcome">
</p>

**Welcome, fellow miser!** So you've decided to contribute to Synaxis. We're honored—and slightly suspicious that you're trying to get free compute out of this somehow. But we respect the hustle.

This guide exists to help you contribute code that meets our standards while spending the absolute minimum amount of your own resources. Because **ULTRA MISER MODE™** applies to development too.

---

## Table of Contents

- [Development Philosophy](#development-philosophy)
- [Prerequisites](#prerequisites)
- [Development Setup](#development-setup)
- [Code Standards](#code-standards)
- [Testing Requirements](#testing-requirements)
- [Commit Message Conventions](#commit-message-conventions)
- [Pull Request Process](#pull-request-process)
- [Issue Reporting](#issue-reporting)
- [Code Review Guidelines](#code-review-guidelines)
- [Questions?](#questions)

---

## Development Philosophy

### ULTRA MISER MODE™ Principles

Before you write a single line of code, internalize these principles:

1. **Efficiency Over Elegance** — If it works and uses fewer resources, it's better than clever code that wastes cycles
2. **Explicit Over Implicit** — Code should be readable by someone running on 3 hours of sleep and their last free API credit
3. **Test Everything** — We don't have money for production incidents. Tests are cheaper than downtime
4. **No Breaking Changes** — Someone, somewhere, is relying on this to route their prompts for free. Don't break their flow
5. **Document Relentlessly** — Future you (and future contributors) will thank present you

---

## Prerequisites

### Required Tools (All Free, Obviously)

| Tool | Version | Purpose | Cost |
|------|---------|---------|------|
| [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) | 10.0.x | Backend development | €0.00 |
| [Node.js](https://nodejs.org/) | 20.x LTS | Frontend tooling | €0.00 |
| [Docker](https://docs.docker.com/get-docker/) | Latest | Local infrastructure | €0.00 |
| [Git](https://git-scm.com/) | 2.40+ | Version control | €0.00 |
| [VS Code](https://code.visualstudio.com/) (recommended) | Latest | IDE | €0.00 |

### Optional But Recommended

- **JetBrains Rider** — If you have a license (or their free tier)
- **Postman/Insomnia** — For API testing
- **pgAdmin** — Included in our Docker Compose for database browsing

---

## Development Setup

### 1. Fork and Clone (The Free Way)

```bash
# Fork the repo on GitHub (costs nothing but pride)
# Then clone your fork
git clone https://github.com/YOUR_USERNAME/Synaxis.git
cd Synaxis

# Add upstream remote (for staying in sync)
git remote add upstream https://github.com/rudironsoni/Synaxis.git
```

### 2. Backend Setup (.NET 10)

```bash
# Restore packages (on Microsoft's dime)
dotnet restore src/InferenceGateway/InferenceGateway.sln

# Build the solution
dotnet build src/InferenceGateway/InferenceGateway.sln

# Run tests to verify everything works
dotnet test src/InferenceGateway/InferenceGateway.sln --no-build
```

### 3. Frontend Setup (React 19 + TypeScript)

```bash
# Navigate to the ClientApp
cd src/Synaxis.WebApp/ClientApp

# Install dependencies (npm's bandwidth, not yours)
npm install

# Run type checking
npm run typecheck

# Run the dev server
npm run dev
```

### 4. Infrastructure Setup (Docker)

```bash
# Copy environment template
cp .env.example .env

# Edit .env with your free-tier API keys (see CONFIGURATION.md)
# Then start infrastructure
docker compose up postgres redis -d

# Or start everything including the app
docker compose --profile dev up --build
```

### 5. Verify Your Setup

```bash
# Backend health check
curl http://localhost:8080/health/liveness

# Frontend check (if running separately)
curl http://localhost:5173

# Run all tests
dotnet test src/InferenceGateway/InferenceGateway.sln
cd src/Synaxis.WebApp/ClientApp && npm test
```

---

## Code Standards

### C# / .NET Standards

#### Naming Conventions

```csharp
// Classes: PascalCase
public class ProviderRoutingService { }

// Interfaces: PascalCase with 'I' prefix
public interface IProviderClient { }

// Methods: PascalCase
public async Task<ChatResponse> SendChatRequestAsync() { }

// Private fields: _camelCase
private readonly ILogger<ProviderRoutingService> _logger;

// Constants: PascalCase
public const int MaxRetries = 3;

// Properties: PascalCase
public string ProviderName { get; set; }
```

#### Code Style Rules

```csharp
// Use 'var' when type is obvious
var response = await _client.SendAsync(request); // Good
HttpResponseMessage response = await _client.SendAsync(request); // Redundant

// Prefer 'is' pattern matching
if (provider is null) // Good
if (provider == null) // Avoid

// Use expression-bodied members for simple cases
public bool IsEnabled => _configuration.Enabled;

// Always use braces (even for single-line blocks)
if (condition)
{
    DoSomething(); // Good
}

// Async methods must end with 'Async'
public async Task ProcessRequestAsync() { }

// Avoid async void (except for event handlers)
```

#### Architecture Rules

```csharp
// Follow Clean Architecture - Dependencies point inward
// Domain → Application → Infrastructure → WebApi

// Use CQRS pattern for operations
public class SendChatCommand : IRequest<ChatResponse> { }
public class SendChatCommandHandler : IRequestHandler<SendChatCommand, ChatResponse> { }

// Inject dependencies via constructor
public class ProviderService
{
    private readonly IProviderClient _client;
    private readonly ILogger<ProviderService> _logger;

    public ProviderService(IProviderClient client, ILogger<ProviderService> logger)
    {
        _client = client;
        _logger = logger;
    }
}

// Use Result pattern for operations that can fail
public async Task<Result<ChatResponse, Error>> SendRequestAsync() { }
```

### TypeScript / React Standards

#### Naming Conventions

```typescript
// Components: PascalCase
function ProviderCard() { }

// Hooks: camelCase with 'use' prefix
function useProviderStatus() { }

// Types/Interfaces: PascalCase
type ProviderConfig = { };
interface ProviderProps { }

// Constants: UPPER_SNAKE_CASE
const MAX_RETRY_ATTEMPTS = 3;

// Files: PascalCase for components, camelCase for utilities
// ProviderCard.tsx, useProviderStatus.ts
```

#### React Patterns

```typescript
// Use function declarations for components
function ProviderList({ providers }: ProviderListProps): JSX.Element {
  return <div>{/* ... */}</div>;
}

// Explicit return types on exported functions
export function formatProviderName(name: string): string {
  return name.toLowerCase().trim();
}

// Prefer explicit props interfaces
interface ProviderCardProps {
  provider: Provider;
  onToggle: (id: string) => void;
}

// Use custom hooks to extract logic
function useProviderToggle() {
  const [enabled, setEnabled] = useState(false);
  const toggle = useCallback(() => setEnabled(e => !e), []);
  return { enabled, toggle };
}
```

### File Organization

```
src/
├── InferenceGateway/
│   ├── Domain/              # Entities, value objects, domain events
│   ├── Application/         # Use cases, commands, queries
│   ├── Infrastructure/      # External services, persistence
│   └── WebApi/             # Controllers, middleware, configuration
├── Synaxis.WebApp/
│   ├── ClientApp/
│   │   ├── src/
│   │   │   ├── components/  # Reusable UI components
│   │   │   ├── features/    # Feature-specific code
│   │   │   ├── hooks/       # Custom React hooks
│   │   │   ├── services/    # API clients
│   │   │   └── types/       # TypeScript definitions
│   │   └── tests/
│   └── Server/             # ASP.NET host
└── Tests/
    ├── Unit/               # Unit tests
    ├── Integration/        # Integration tests
    └── Benchmarks/         # Performance tests
```

---

## Testing Requirements

### The Golden Rule

> **No code is merged without tests.** We're too poor for regressions.

### Test Coverage Expectations

| Component | Minimum Coverage |
|-----------|------------------|
| Domain logic | 90% |
| Application services | 85% |
| Infrastructure adapters | 70% |
| React components | 80% |
| Utility functions | 90% |

### Writing Tests

#### C# (xUnit)

```csharp
public class ProviderRoutingServiceTests
{
    [Fact]
    public async Task RouteRequestAsync_WithValidRequest_ReturnsResponse()
    {
        // Arrange
        var mockClient = new Mock<IProviderClient>();
        var service = new ProviderRoutingService(mockClient.Object);
        var request = new ChatRequest { /* ... */ };

        // Act
        var result = await service.RouteRequestAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task RouteRequestAsync_WithInvalidModel_ReturnsError(string model)
    {
        // Arrange
        var request = new ChatRequest { Model = model };

        // Act
        var result = await _service.RouteRequestAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("INVALID_MODEL");
    }
}
```

#### TypeScript (Vitest)

```typescript
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

describe('ProviderCard', () => {
  it('renders provider name', () => {
    render(<ProviderCard provider={{ name: 'Groq', enabled: true }} />);
    expect(screen.getByText('Groq')).toBeInTheDocument();
  });

  it('calls onToggle when switch is clicked', async () => {
    const onToggle = vi.fn();
    render(<ProviderCard provider={{ name: 'Groq', enabled: true }} onToggle={onToggle} />);
    
    await userEvent.click(screen.getByRole('switch'));
    
    expect(onToggle).toHaveBeenCalledWith('groq');
  });
});
```

### Running Tests

```bash
# Backend - all tests
dotnet test src/InferenceGateway/InferenceGateway.sln

# Backend - with coverage
dotnet test src/InferenceGateway/InferenceGateway.sln --collect:"XPlat Code Coverage"

# Frontend
cd src/Synaxis.WebApp/ClientApp
npm test

# Frontend - with coverage
npm run test:coverage

# E2E tests
npm run test:e2e
```

---

## Commit Message Conventions

We follow [Conventional Commits](https://www.conventionalcommits.org/) with a miser twist.

### Format

```
<type>(<scope>): <description>

[optional body]

[optional footer(s)]
```

### Types

| Type | Use When | Example |
|------|----------|---------|
| `feat` | Adding functionality | `feat(routing): add DeepSeek provider support` |
| `fix` | Fixing bugs | `fix(streaming): resolve SSE connection drops` |
| `docs` | Documentation changes | `docs(api): update authentication examples` |
| `test` | Adding/updating tests | `test(providers): add retry logic coverage` |
| `refactor` | Code restructuring | `refactor(domain): extract provider interface` |
| `perf` | Performance improvements | `perf(routing): cache provider health checks` |
| `chore` | Maintenance tasks | `chore(deps): update .NET packages` |

### Scopes

Common scopes: `routing`, `providers`, `streaming`, `auth`, `api`, `ui`, `tests`, `deps`, `config`

### Examples

```
feat(providers): add Together AI integration

- Implement Together AI client
- Add model mapping for Llama 3.1 405B
- Update configuration schema

Closes #123
```

```
fix(streaming): handle provider timeout gracefully

Previously, timeouts would crash the SSE connection. Now we:
1. Catch timeout exceptions
2. Return partial response if available
3. Log for monitoring

Fixes #456
```

### Commit Best Practices

- **Keep commits atomic** — One logical change per commit
- **Write in imperative mood** — "Add feature" not "Added feature"
- **Reference issues** — Include `Fixes #123` or `Closes #456`
- **No WIP commits in PRs** — Squash before submitting

---

## Pull Request Process

### Before Creating a PR

1. **Sync with upstream**
   ```bash
   git fetch upstream
   git rebase upstream/main
   ```

2. **Run verification gate**
   ```bash
   dotnet format Synaxis.sln --verify-no-changes
   dotnet build Synaxis.sln -c Release -warnaserror
   dotnet test Synaxis.sln --no-build -p:Configuration=Release
   ```

3. **Run additional tests**
   ```bash
   dotnet test src/InferenceGateway/InferenceGateway.sln
   cd src/Synaxis.WebApp/ClientApp && npm test
   ```

4. **Check code style**
   ```bash
   # C# formatting
dotnet format src/InferenceGateway/InferenceGateway.sln --verify-no-changes

   # TypeScript linting
   cd src/Synaxis.WebApp/ClientApp
   npm run lint
   ```

5. **Update documentation** if needed

### Creating a PR

1. **Push your branch**
   ```bash
   git push origin feature/your-feature-name
   ```

2. **Fill out the PR template** (if available) or include:
   - **What** — What does this PR do?
   - **Why** — Why is this change needed?
   - **How** — How was it implemented?
   - **Testing** — How was it tested?
   - **Breaking Changes** — Any breaking changes?

3. **Link related issues** — Use `Fixes #123` or `Closes #456`

4. **Request review** — From maintainers or relevant contributors

### PR Requirements Checklist

- [ ] Tests pass locally
- [ ] New tests added for new functionality
- [ ] Code follows style guidelines
- [ ] Documentation updated
- [ ] No compiler warnings
- [ ] No ESLint/TypeScript errors
- [ ] CHANGELOG.md updated (for significant changes)

### Review Process

1. **Automated checks** must pass (CI/CD)
2. **Code review** by at least one maintainer
3. **Address feedback** — Push fixes as new commits (don't force push)
4. **Approval** — PR is merged by maintainer

### After Merge

- Your branch will be deleted
- Changes appear in next release
- You get eternal gratitude (and possibly a free API key if we ever get sponsors)

---

## Issue Reporting

### Before Creating an Issue

1. **Search existing issues** — Your bug might already be reported
2. **Check documentation** — The answer might be in CONFIGURATION.md
3. **Try latest version** — Your issue might already be fixed

### Bug Reports

Use this template:

```markdown
**Description**
Clear description of the bug.

**Reproduction Steps**
1. Start with configuration '...'
2. Send request to '...'
3. Observe error '...'

**Expected Behavior**
What should have happened.

**Actual Behavior**
What actually happened.

**Environment**
- OS: [e.g., Ubuntu 22.04]
- .NET Version: [e.g., 10.0.100]
- Synaxis Version: [e.g., 1.2.3]
- Provider: [e.g., Groq]

**Logs**
```
Paste relevant logs here
```

**Configuration**
```json
Redacted configuration (remove API keys!)
```
```

### Feature Requests

```markdown
**Feature Description**
What do you want to add?

**Motivation**
Why is this needed? What problem does it solve?

**Proposed Solution**
How should this work?

**Alternatives Considered**
What else did you consider?

**Additional Context**
Any other relevant information.
```

### Provider Requests

Want to add a new free-tier provider? Include:

- Provider name and URL
- Free tier limits (requests/month, tokens/day)
- API documentation link
- Whether they support streaming
- Your willingness to implement it (we love PRs!)

---

## Code Review Guidelines

### For Authors

- **Respond to all comments** — Even if it's just "Done" or "Acknowledged"
- **Don't take it personally** — We're reviewing code, not you
- **Ask questions** — If feedback is unclear, ask for clarification
- **Push fixes as commits** — Don't force push until final approval

### For Reviewers

- **Be constructive** — Suggest improvements, don't just criticize
- **Explain the "why"** — Help authors learn from feedback
- **Distinguish preferences from requirements** — "Must fix" vs "Consider"
- **Approve when ready** — Don't leave PRs in limbo

### Review Checklist

- [ ] Code follows project standards
- [ ] Tests cover new functionality
- [ ] No obvious bugs or edge cases missed
- [ ] Performance implications considered
- [ ] Security implications considered
- [ ] Documentation is adequate
- [ ] Commit messages are clear

---

## Questions?

- **Discord/Slack** — [Link to community chat if available]
- **GitHub Discussions** — For general questions
- **Issues** — For bugs and feature requests
- **Email** — For security issues: security@synaxis.local

---

## ULTRA MISER MODE™ Contributor Pledge

> *I pledge to write efficient code, test thoroughly, and never break someone else's free-tier workflow. I will optimize for readability, respect the architecture, and remember that somewhere out there, someone is counting on this software to route their last free prompt of the day.*

**Welcome to the team, fellow connoisseur of free compute.**

---

<p align="center">
  <em>Built with spite, clean architecture pride, and the tears of expired API keys.</em>
</p>
