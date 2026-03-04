# Migration Rehearsals

This directory contains the migration rehearsal framework for Synaxis, designed to validate database migration procedures before production deployment.

## Overview

Migration rehearsals execute comprehensive test scenarios in staging environments to ensure:

- **Happy Path**: Complete migration runbook execution
- **Failure Scenarios**: Rollback procedures and recovery
- **Partial Failures**: Graceful degradation handling
- **Performance Baselines**: No performance regression

## Quick Start

```bash
# Run all rehearsal scenarios
./scripts/run-migration-rehearsal.sh

# Run specific scenario
./scripts/run-migration-rehearsal.sh -s happy-path

# Run with verbose output
./scripts/run-migration-rehearsal.sh -v

# Run against specific environment
./scripts/run-migration-rehearsal.sh -e staging -c "Host=staging-db;..."
```

## Components

### 1. Orchestrator (`MigrationRehearsalOrchestrator.cs`)

The main coordinator that executes all rehearsal scenarios:

- `ExecuteFullRehearsalAsync()`: Runs the complete rehearsal suite
- `ExecuteHappyPathRehearsalAsync()`: Happy path validation
- `ExecuteFailureScenarioRehearsalAsync()`: Failure and rollback testing
- `ExecutePartialFailureRehearsalAsync()`: Degradation testing
- `ExecutePerformanceBaselineRehearsalAsync()`: Performance validation

### 2. Test Suite (`MigrationRehearsalTests.cs`)

Integration tests using Testcontainers for isolated database testing:

- Tests all orchestrator scenarios
- Validates timing and metrics capture
- Verifies Go/No-Go decision logic

### 3. Automation Scripts

- `run-migration-rehearsal.sh`: Bash script for Unix/Linux
- `run-migration-rehearsal.ps1`: PowerShell script for Windows
- `rollback-migration.sh`: Database rollback utility

### 4. Runbook (`docs/migration-rehearsal-runbook.md`)

Comprehensive documentation including:

- Step-by-step rehearsal procedures
- Go/No-Go decision criteria
- Stakeholder sign-off requirements
- Troubleshooting guide

### 5. CI/CD Integration

GitHub Actions workflow (`.github/workflows/migration-rehearsal.yml`):

- Scheduled weekly runs
- PR-triggered rehearsals for migration changes
- Manual workflow dispatch
- Slack notifications
- Artifact collection

## Rehearsal Scenarios

### Happy Path Rehearsal

Validates the complete migration runbook:

1. Pre-migration validation
2. Database backup
3. Execute migrations
4. Post-migration validation
5. Service health checks
6. Data integrity verification

**Success Criteria:** All steps complete without errors

### Failure Scenario Rehearsal

Tests failure handling and recovery:

1. Migration failure simulation
2. Rollback procedure test
3. Data consistency after rollback
4. Recovery time documentation

**Success Criteria:** Rollback succeeds in < 5 minutes, data consistent

### Partial Failure Rehearsal

Validates graceful degradation:

1. Service failure during rollout
2. Database connection issues
3. Network partition scenarios
4. Graceful degradation verification

**Success Criteria:** Service continues in degraded mode, no cascading failures

### Performance Baseline Rehearsal

Ensures no performance regression:

1. Load test before migration
2. Load test after migration
3. Response time comparison
4. Regression verification

**Success Criteria:** < 10% response time regression

## Go/No-Go Decision Criteria

### GO Criteria (All Must Pass)

| Criterion | Threshold |
|-----------|-----------|
| Happy Path | 100% pass |
| Rollback Test | 100% pass |
| Data Consistency | 100% pass |
| Recovery Time | < 5 min |
| Partial Failure | 100% pass |
| Performance | < 10% regression |

### NO-GO Triggers

- Rollback fails
- Data loss detected
- Recovery time > 10 min
- Performance regression > 10%
- Cascading failures
- Happy path fails

## Usage Examples

### Run All Scenarios

```bash
./scripts/run-migration-rehearsal.sh
```

### Run Specific Scenario

```bash
./scripts/run-migration-rehearsal.sh -s happy-path
./scripts/run-migration-rehearsal.sh -s failure
./scripts/run-migration-rehearsal.sh -s partial
./scripts/run-migration-rehearsal.sh -s performance
```

### Run with Custom Settings

```bash
./scripts/run-migration-rehearsal.sh \
  -e staging \
  -s all \
  -o /custom/output/path \
  -c "Host=mydb;Database=synaxis;Username=postgres;Password=secret" \
  -v
```

### PowerShell (Windows)

```powershell
.\scripts\run-migration-rehearsal.ps1 -Scenario all -VerboseOutput
```

## Output

Rehearsal results are saved to the output directory (default: `./rehearsal-results`):

```
rehearsal-results/
├── happy-path-result.json
├── happy-path-output.log
├── failure-scenario-result.json
├── failure-scenario-output.log
├── partial-failure-result.json
├── partial-failure-output.log
├── performance-baseline-result.json
├── performance-baseline-output.log
├── rehearsal-summary-YYYYMMDD-HHMMSS.md
└── rehearsal-result-{id}-{timestamp}.json
```

## Integration with CI/CD

The rehearsal workflow runs:

1. **Scheduled**: Weekly on Sundays at 2 AM UTC
2. **On PR**: When migration files are modified
3. **Manual**: Via GitHub Actions workflow dispatch

### Required Secrets

- `SLACK_REHEARSAL_WEBHOOK`: Slack webhook for notifications

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `REHEARSAL_ENVIRONMENT` | Target environment | staging |
| `REHEARSAL_CONNECTION_STRING` | Database connection | (required) |
| `REHEARSAL_OUTPUT_DIR` | Results directory | ./rehearsal-results |
| `REHEARSAL_VERBOSE` | Verbose output | false |
| `REHEARSAL_SCENARIO` | Scenario to run | all |

## Running Tests Locally

```bash
# Run all migration rehearsal tests
dotnet test tests/Synaxis.Infrastructure.UnitTests \
  --filter "FullyQualifiedName~MigrationRehearsalTests"

# Run specific test
dotnet test tests/Synaxis.Infrastructure.UnitTests \
  --filter "HappyPathRehearsal_ExecutesAllSteps"
```

## Troubleshooting

### Migration Times Out

```bash
# Increase timeout
export DOTNET_CLI_COMMAND_TIMEOUT=600
./scripts/run-migration-rehearsal.sh
```

### PostgreSQL Connection Issues

```bash
# Verify connection
dotnet ef database info \
  --project src/Synaxis.Infrastructure/Synaxis.Infrastructure.csproj \
  --connection "Host=localhost;Database=synaxis;Username=postgres;Password=postgres"
```

### Missing pg_dump

The rehearsal will continue without pg_dump, but backups may be limited:

```bash
# Ubuntu/Debian
sudo apt-get install postgresql-client

# macOS
brew install libpq
```

## Contributing

When modifying the rehearsal framework:

1. Update tests in `MigrationRehearsalTests.cs`
2. Update orchestrator in `MigrationRehearsalOrchestrator.cs`
3. Update runbook in `docs/migration-rehearsal-runbook.md`
4. Run rehearsals to validate changes
5. Update this README with changes

## References

- [Migration Runbook](./docs/migration-rehearsal-runbook.md)
- [Rollback Scripts](./scripts/rollback-migration.sh)
- [CI Workflow](./.github/workflows/migration-rehearsal.yml)
- [Database Migrations](../src/Synaxis.Infrastructure/Data/Migrations/)
