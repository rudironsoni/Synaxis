# GitHub Workflows Documentation

This directory contains GitHub Actions workflows for the Synaxis multi-region architecture.

## Workflows

### 1. docker-publish.yml
**Purpose**: Build and publish Docker images for all services to GitHub Container Registry (ghcr.io)

**Triggers**:
- Push to `main` branch
- Push tags matching `v*.*.*` pattern
- Pull requests to `main` branch
- Release published events

**Services Built**:
- `synaxis-api` - Main API service (Synaxis.Core, Synaxis.Infrastructure, Synaxis.Api)
- `synaxis-inference-gateway` - Inference Gateway service

**Features**:
- Multi-architecture builds: `linux/amd64`, `linux/arm64`
- Matrix strategy for parallel builds
- GitHub Actions cache for faster builds
- Automated tagging:
  - `latest` for main branch
  - `sha-{sha}` for commit SHA
  - `v{version}` for version tags
  - Semantic versioning support (`{major}.{minor}`)
- Images pushed to: `ghcr.io/{owner}/{service-name}:{tag}`

**Usage**:
- Automatically runs on push/PR
- Images only pushed on non-PR events
- Pull images: `docker pull ghcr.io/{owner}/synaxis-api:latest`

### 2. ci.yml
**Purpose**: Continuous Integration - Run tests and quality checks on every PR and push

**Triggers**:
- Push to `main` or `develop` branches
- Pull requests to `main` or `develop` branches

**Jobs**:

#### test
- Sets up PostgreSQL 16 test database
- Runs all .NET tests with code coverage
- Uploads coverage to Codecov (requires `CODECOV_TOKEN` secret)

#### lint
- Checks code formatting with `dotnet format`
- Runs static analyzers
- Treats warnings as errors

#### docker-build (PR only)
- Validates Dockerfiles build successfully
- Tests both service images
- Uses GitHub Actions cache for speed

#### security
- Scans for vulnerable NuGet packages
- Fails if vulnerabilities found
- Checks both direct and transitive dependencies

**Environment Variables**:
- `DOTNET_VERSION`: .NET SDK version (10.0.x)
- `ConnectionStrings__DefaultConnection`: Test database connection

### 3. tag-release.yml
**Purpose**: Automatically create semantic version tags and GitHub releases

**Triggers**:
- Push to `main` branch

**Features**:
- Uses conventional commits for versioning
- Creates git tags (format: `v{major}.{minor}.{patch}`)
- Generates changelog from commits
- Creates GitHub releases automatically
- Supports version bumps:
  - `feat:` → minor version bump
  - `fix:` → patch version bump
  - `BREAKING CHANGE:` → major version bump

**Usage**:
- Commit messages determine version bump
- Tag format: `v1.2.3`
- Only tags when conventional commits detected

### 4. deploy.yml
**Purpose**: Deploy services to multi-region environments

**Trigger**: Manual workflow dispatch

**Inputs**:
- **environment**: `development`, `staging`, or `production`
- **regions**: Comma-separated list or `all` (eu-west-1, us-east-1, sa-east-1)
- **service**: `all`, `synaxis-api`, or `synaxis-inference-gateway`
- **version**: Docker image tag to deploy (default: `latest`)

**Jobs**:

#### prepare
- Parses input parameters
- Sets up deployment matrix
- Outputs configuration for deploy job

#### deploy
- Matrix deployment across regions and services
- Configures AWS credentials per region
- Pulls specified Docker image
- Executes deployment (TODO: Add deployment commands)
- Verifies deployment health

#### post-deploy
- Creates deployment summary
- Reports overall status

**Required Secrets**:
- `AWS_ACCESS_KEY_ID_{REGION}` (per region) or `AWS_ACCESS_KEY_ID` (global)
- `AWS_SECRET_ACCESS_KEY_{REGION}` (per region) or `AWS_SECRET_ACCESS_KEY` (global)
- `GITHUB_TOKEN` (automatically provided)

**TODO**:
- Add specific deployment commands (ECS, EKS, EC2, etc.)
- Add health check verification
- Add rollback mechanism
- Add notification integration

## Secrets Configuration

### Required Secrets
Add these in GitHub repository settings → Secrets and variables → Actions:

```
CODECOV_TOKEN                    # For code coverage (optional)
AWS_ACCESS_KEY_ID               # AWS credentials (global or per-region)
AWS_SECRET_ACCESS_KEY           # AWS credentials (global or per-region)
AWS_ACCESS_KEY_ID_EU_WEST_1     # Region-specific AWS credentials (optional)
AWS_SECRET_ACCESS_KEY_EU_WEST_1 # Region-specific AWS credentials (optional)
AWS_ACCESS_KEY_ID_US_EAST_1     # Region-specific AWS credentials (optional)
AWS_SECRET_ACCESS_KEY_US_EAST_1 # Region-specific AWS credentials (optional)
AWS_ACCESS_KEY_ID_SA_EAST_1     # Region-specific AWS credentials (optional)
AWS_SECRET_ACCESS_KEY_SA_EAST_1 # Region-specific AWS credentials (optional)
```

## Environment Configuration

### GitHub Environments
Configure these environments in GitHub repository settings → Environments:

- `development-eu-west-1`
- `development-us-east-1`
- `development-sa-east-1`
- `staging-eu-west-1`
- `staging-us-east-1`
- `staging-sa-east-1`
- `production-eu-west-1` (with protection rules)
- `production-us-east-1` (with protection rules)
- `production-sa-east-1` (with protection rules)

### Protection Rules (recommended for production)
- Required reviewers: 1+
- Wait timer: 5 minutes
- Deployment branches: `main` only

## Docker Images

### Structure
```
ghcr.io/{owner}/synaxis-api:latest
ghcr.io/{owner}/synaxis-api:v1.2.3
ghcr.io/{owner}/synaxis-api:sha-abc1234
ghcr.io/{owner}/synaxis-inference-gateway:latest
ghcr.io/{owner}/synaxis-inference-gateway:v1.2.3
ghcr.io/{owner}/synaxis-inference-gateway:sha-abc1234
```

### Image Architecture
- **synaxis-api**: Includes Synaxis.Core, Synaxis.Infrastructure, Synaxis.Api
- **synaxis-inference-gateway**: Includes InferenceGateway.Application, Infrastructure, WebApi

### Multi-Architecture Support
Both images support:
- `linux/amd64` (Intel/AMD)
- `linux/arm64` (ARM, Apple Silicon)

## Workflow Diagram

```
┌─────────────┐
│   Push/PR   │
└──────┬──────┘
       │
       ├─────────────┐
       │             │
       ▼             ▼
┌─────────┐   ┌──────────┐
│   CI    │   │  Docker  │
│  Tests  │   │  Build   │
└─────────┘   └──────┬───┘
                     │
              (on main branch)
                     │
              ┌──────▼───────┐
              │ Tag Release  │
              └──────┬───────┘
                     │
              ┌──────▼────────┐
              │ Docker Publish│
              └──────┬────────┘
                     │
              ┌──────▼────────┐
              │    Deploy     │
              │   (manual)    │
              └───────────────┘
```

## Local Testing

### Test Docker Builds Locally
```bash
# Test Synaxis.Api build
docker build -f src/Synaxis.Api/Dockerfile -t synaxis-api:test .

# Test InferenceGateway build
docker build -f src/InferenceGateway/WebApi/Dockerfile -t synaxis-inference-gateway:test .

# Test multi-arch build
docker buildx build --platform linux/amd64,linux/arm64 -f src/Synaxis.Api/Dockerfile .
```

### Run Tests Locally
```bash
# Run all tests
dotnet test --configuration Release

# Run with coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage

# Check formatting
dotnet format --verify-no-changes

# Check for vulnerabilities
dotnet list package --vulnerable --include-transitive
```

## Troubleshooting

### Docker Build Fails
- Check Dockerfile paths are correct
- Verify all referenced projects exist
- Ensure Directory.Packages.props and Directory.Build.props are present
- Check .dockerignore is not excluding required files

### Tests Fail
- Ensure PostgreSQL service is running (CI workflow handles this)
- Check connection string configuration
- Verify all test dependencies are restored

### Deployment Fails
- Verify AWS credentials are configured
- Check region names are correct
- Ensure image exists in registry
- Verify environment configuration

### Image Not Found
- Check if workflow completed successfully
- Verify image name and tag
- Ensure you're logged into ghcr.io: `docker login ghcr.io -u USERNAME`
- Make sure packages are public or you have access

## Best Practices

1. **Conventional Commits**: Use conventional commit format for automatic versioning
   - `feat: add new feature` → minor version bump
   - `fix: resolve bug` → patch version bump
   - `feat!: breaking change` → major version bump

2. **PR Workflow**:
   - Always create PRs to main
   - CI will run automatically
   - Docker builds are tested but not pushed
   - Address all CI failures before merging

3. **Deployment**:
   - Test in `development` first
   - Promote to `staging` for validation
   - Deploy to `production` with approval
   - Use specific version tags in production, not `latest`

4. **Security**:
   - Keep secrets in GitHub Secrets, never commit
   - Use environment-specific credentials
   - Enable vulnerability scanning
   - Review security scan results regularly

5. **Monitoring**:
   - Check workflow runs regularly
   - Monitor coverage trends
   - Review deployment success rates
   - Set up notifications for failures

## Future Enhancements

- [ ] Add integration tests with Testcontainers
- [ ] Implement blue-green deployment
- [ ] Add canary deployment support
- [ ] Integrate with monitoring (Datadog, New Relic)
- [ ] Add Slack/Discord notifications
- [ ] Implement automated rollback
- [ ] Add performance testing stage
- [ ] Integrate security scanning (Snyk, Trivy)
- [ ] Add infrastructure as code validation
- [ ] Implement feature flag management
