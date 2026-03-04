# Synaxis Production Migration Execution Runbook

## Overview

This document describes the production migration execution framework for Synaxis-1mka. The framework provides a structured, auditable process for executing database migrations in production environments.

## Quick Start

```bash
# Execute production migration
./scripts/execute-production-migration.sh \
  --environment production \
  --connection "Host=prod.db;Database=synaxis;Username=admin;Password=secret" \
  --backup-dir ./backups

# Dry run (preview without making changes)
./scripts/execute-production-migration.sh \
  --environment staging \
  --dry-run \
  --connection "Host=staging.db;Database=synaxis;Username=admin;Password=secret"
```

## Architecture

### Components

```
┌─────────────────────────────────────────────────────────────┐
│                    Migration Execution                      │
├─────────────────────────────────────────────────────────────┤
│  execute-production-migration.sh                            │
│  ├─ Pre-flight Checks                                       │
│  ├─ Maintenance Window                                        │
│  ├─ Database Backup                                         │
│  ├─ Database Migration                                      │
│  ├─ Service Deployment                                      │
│  ├─ Post-deployment Validation                              │
│  └─ Go/No-Go Decision                                       │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│              MigrationExecutionService (C#)                 │
│  ├─ MigrationExecutionLog (structured logging)              │
│  ├─ PreflightCheckService (validation)                      │
│  └─ PostDeploymentValidationService (health checks)       │
└─────────────────────────────────────────────────────────────┘
```

### Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | Invalid arguments |
| 2 | Pre-flight checks failed |
| 3 | Migration failed |
| 4 | Deployment failed |
| 5 | Post-deployment validation failed |
| 6 | Rollback triggered |

## Execution Phases

### Phase 1: Pre-Flight Checks

Validates the environment before migration:

1. **Environment Validation** - Ensures valid environment name
2. **Required Tools** - Verifies dotnet CLI is available
3. **Connection String** - Validates format
4. **Database Connectivity** - Tests connection
5. **Migration Infrastructure** - Verifies EF Core project exists
6. **Backup Directory** - Ensures writable
7. **Rollback Plan** - Checks rollback plan exists
8. **Disk Space** - Verifies sufficient space (default: 10GB)

### Phase 2: Maintenance Window

1. Announce downtime start
2. Enable maintenance mode
3. Drain active connections
4. Verify no active transactions

### Phase 3: Database Backup

Creates a PostgreSQL backup before migration:

- Uses `pg_dump` for full backup
- Stores in timestamped directory
- Creates metadata file
- Validates backup integrity

### Phase 4: Database Migration

1. Get current migration
2. Apply pending migrations via `dotnet ef database update`
3. Verify data integrity
4. Log results

### Phase 5: Service Deployment

Deploys services in dependency order:

1. Deploy core services
2. Health check each service
3. Verify inter-service communication

### Phase 6: Post-Deployment Validation

Validates the deployment:

1. **Health Checks** - Query health endpoints
2. **Smoke Tests** - Run basic API tests
3. **Error Rate Monitoring** - Verify nominal error rates
4. **Performance Validation** - Check response times

### Phase 7: Go/No-Go Decision

Records the final decision:

- **GO**: All checks passed
- **NO-GO**: Critical issues detected
- **GO WITH CONDITIONS**: Warnings but acceptable

## C# Services

### MigrationExecutionService

Core service for managing migration execution:

```csharp
// Create execution context
var log = executionService.CreateExecutionContext("production");

// Execute a phase
var result = await executionService.ExecutePhaseAsync(
    log,
    "DatabaseMigration",
    async ct => await ApplyMigrationsAsync(ct),
    cancellationToken);

// Record issues
executionService.RecordIssue(log, IssueSeverity.Warning, "Message", "Component");

// Record decisions
executionService.RecordDecision(log, "GO", "All checks passed", "admin");

// Finalize
executionService.FinalizeExecution(log, success: true);

// Save log
await executionService.SaveExecutionLogAsync(log, "/path/to/log.json");
```

### PreflightCheckService

Runs pre-migration validations:

```csharp
var options = new PreflightCheckOptions
{
    ConnectionString = "...",
    Environment = "production",
    BackupDirectory = "./backups",
    RollbackPlanPath = "./rollback-plan.json",
    RequiredDiskSpaceGb = 10
};

var results = await preflightService.RunChecksAsync(log, options);

if (results.Status == PreflightStatus.Failed)
{
    // Abort migration
}
```

### PostDeploymentValidationService

Validates post-deployment state:

```csharp
var options = new PostDeploymentValidationOptions
{
    HealthCheckEndpoints = new List<HealthCheckEndpoint>
    {
        new() { ServiceName = "api", Url = "https://api/health" }
    },
    SmokeTestEndpoints = new List<SmokeTestEndpoint>
    {
        new() { TestName = "GetUsers", Url = "https://api/users" }
    },
    ErrorRateThreshold = 0.05
};

var results = await validationService.ValidateAsync(log, options);
```

## Dependency Injection

```csharp
services.AddMigrationExecutionServices();
```

Registers:
- `IMigrationExecutionService` → `MigrationExecutionService`
- `IPreflightCheckService` → `PreflightCheckService`
- `IPostDeploymentValidationService` → `PostDeploymentValidationService`

## Output Files

### Migration Log (JSON)

```json
{
  "migrationId": "20260304_120000_production",
  "environment": "production",
  "status": "Completed",
  "dryRun": false,
  "startedAt": "2026-03-04T12:00:00+00:00",
  "endedAt": "2026-03-04T12:05:30+00:00",
  "duration": 330,
  "phases": [
    { "name": "preflight", "status": "Completed", "duration": 15 },
    { "name": "maintenance_window", "status": "Completed", "duration": 30 },
    { "name": "backup", "status": "Completed", "duration": 120 },
    { "name": "database_migration", "status": "Completed", "duration": 60 },
    { "name": "service_deployment", "status": "Completed", "duration": 90 },
    { "name": "post_deployment", "status": "Completed", "duration": 15 }
  ],
  "issues": [],
  "decisions": [
    {
      "decision": "GO",
      "reason": "All checks passed",
      "approver": "admin",
      "timestamp": "2026-03-04T12:05:15+00:00"
    }
  ]
}
```

### Summary Report (Text)

Generated at `{backup_dir}/{migration_id}/logs/summary.txt`

## Testing

### Unit Tests

```bash
dotnet test tests/Synaxis.Infrastructure.UnitTests \
  --filter "FullyQualifiedName~MigrationExecution"
```

### Dry Run

Always run a dry run first:

```bash
./scripts/execute-production-migration.sh \
  --environment staging \
  --dry-run \
  --connection "..."
```

## Rollback

If migration fails, automatic rollback is triggered:

```bash
# Manual rollback (if needed)
./scripts/rollback-migration.sh <target_migration>
```

## Checklist

- [ ] Pre-flight checks passed
- [ ] Team assembled (war room if needed)
- [ ] Monitoring dashboards open
- [ ] Rollback plan ready
- [ ] Communication channels established
- [ ] Database backup completed
- [ ] All services healthy
- [ ] Error rates nominal
- [ ] Performance acceptable
- [ ] No critical issues
- [ ] Monitoring alerts quiet
- [ ] Business sign-off obtained

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `SYNAXIS_ENVIRONMENT` | Target environment | `production` |
| `SYNAXIS_CONNECTION_STRING` | PostgreSQL connection string | - |
| `SYNAXIS_BACKUP_DIR` | Backup directory | `./backups/migrations` |
| `SYNAXIS_MAINTENANCE_MODE` | Enable maintenance mode | `true` |
| `SYNAXIS_ROLLBACK_PLAN` | Path to rollback plan | - |
| `SYNAXIS_NOTIFY_CHANNELS` | Notification channels | `console` |

## License

Copyright (c) Synaxis. All rights reserved.
