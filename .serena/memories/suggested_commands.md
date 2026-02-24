# Suggested Commands for Synaxis Project

## Build & Verification
```bash
# Format verification (EditorConfig-driven)
dotnet format --verify-no-changes

# Build the solution (warnings treated as errors)
dotnet build Synaxis.sln -warnaserror

# Build in Release mode
dotnet build Synaxis.sln -c Release -warnaserror
```

## Testing
```bash
# Run all tests
dotnet test Synaxis.sln --no-build

# Run tests with diagnostics
dotnet test Synaxis.sln --no-build --logger "trx;LogFileName=test-results.trx"

# Run specific test category
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=Integration"
```

## Frontend (pnpm)
```bash
# Install dependencies
pnpm install

# Build UI package
pnpm build:ui

# Build all frontend apps
pnpm build:all

# Development
pnpm dev:desktop
pnpm dev:mobile
```

## Docker
```bash
# Start local infrastructure
docker-compose up -d

# Start with monitoring
docker-compose --profile monitoring up -d
```

## Quality Gates
```bash
# Run slopwatch analysis
slopwatch analyze --fail-on warning

# Check beads status
bd ready
bd list --status=in_progress
```

## Git Workflow
```bash
# Check status
git status

# Stage and commit
git add -A
git commit -m "message"

# Sync beads
bd sync --from-main
```
