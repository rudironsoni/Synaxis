# Synaxis Production Database Migrations

This directory contains idempotent SQL migration scripts for the Synaxis platform production database.

## Overview

| Migration | Description | Tables Created |
|-----------|-------------|----------------|
| `000_CreateSchemaMigrationsTable.sql` | Migration tracking table | `schema_migrations` |
| `001_InitialInferenceSchema.sql` | Inference domain schema | `model_configs`, `chat_templates`, `user_chat_preferences`, `inference_requests` |
| `002_EventSourcingTables.sql` | Event sourcing infrastructure | `event_streams`, `event_store`, `event_store_snapshots`, `event_projections`, `event_subscriptions`, `event_dead_letter` |
| `003_ApiManagementTables.sql` | API key and rate limiting | `api_keys`, `api_key_usage_logs`, `rate_limit_configs`, `rate_limit_assignments`, `rate_limit_tracking`, `api_endpoints` |
| `004_IdentitySchemaUpdates.sql` | Extended identity tables | `user_sessions`, `user_security_events`, `user_login_history`, `user_recovery_codes`, `user_webauthn_credentials`, `user_consent_records`, `user_profile_preferences` |
| `005_BillingSchema.sql` | Billing and subscription schema | `billing_plans`, `subscriptions`, `subscription_items`, `invoices`, `invoice_line_items`, `payments`, `credit_notes`, `usage_records` |

## Data Migration Scripts

| Script | Purpose |
|--------|---------|
| `migrate_inference_gateway_data.sql` | Migrate legacy Gateway data to new Inference schema |
| `migrate_user_data.sql` | Migrate legacy users to Identity aggregate |
| `seed_event_store.sql` | Seed initial event streams and subscriptions |

## Validation Scripts

| Script | Purpose |
|--------|---------|
| `validate_migration.sql` | Row counts, FK checks, constraint validation |
| `verify_data_integrity.sql` | Checksums, sample validation, temporal checks |

## Rollback Scripts

| Script | Reverts |
|--------|---------|
| `rollback_001.sql` | `001_InitialInferenceSchema` |
| `rollback_002.sql` | `002_EventSourcingTables` |
| `rollback_003.sql` | `003_ApiManagementTables` |
| `rollback_004.sql` | `004_IdentitySchemaUpdates` |
| `rollback_005.sql` | `005_BillingSchema` |

## Usage

### Running Migrations

#### Using PowerShell Script (Recommended for Windows)

```powershell
# Run all pending migrations
.\run_migrations.ps1 -Database synaxis_prod -Username postgres -Password secret123

# Run up to specific version
.\run_migrations.ps1 -TargetVersion 003 -Username postgres -Password secret123

# Dry run (show what would be executed)
.\run_migrations.ps1 -DryRun
```

#### Using psql Command Line

```bash
# Run a single migration
psql -h localhost -U postgres -d synaxis -f 001_InitialInferenceSchema.sql

# Run all migrations in order
for f in 0*.sql; do psql -h localhost -U postgres -d synaxis -f "$f"; done
```

### Running Data Migrations

```bash
# After schema migrations, run data migrations
psql -h localhost -U postgres -d synaxis -f migrate_inference_gateway_data.sql
psql -h localhost -U postgres -d synaxis -f migrate_user_data.sql
psql -h localhost -U postgres -d synaxis -f seed_event_store.sql
```

### Validation

```bash
# Validate migration success
psql -h localhost -U postgres -d synaxis -f validate_migration.sql

# Verify data integrity
psql -h localhost -U postgres -d synaxis -f verify_data_integrity.sql
```

### Rollback (Use with Extreme Caution!)

```bash
# Rollback a specific migration (will DELETE data!)
psql -h localhost -U postgres -d synaxis -f rollback_005.sql
```

## Migration Conventions

1. **Idempotency**: All migrations are idempotent - safe to run multiple times
2. **Transaction Safety**: Each migration runs in a single transaction
3. **Versioning**: Sequential 3-digit version numbers (001, 002, etc.)
4. **Naming**: `{version}_{PascalCaseDescription}.sql`
5. **Logging**: All migrations log to `schema_migrations` table
6. **Timestamps**: All tables use `TIMESTAMP WITH TIME ZONE`
7. **Soft Deletes**: All tables support soft deletion via `deleted_at`
8. **Updated At**: All tables have `updated_at` with auto-update trigger

## Schema Migrations Table

```sql
CREATE TABLE schema_migrations (
    version VARCHAR(100) PRIMARY KEY,
    applied_at TIMESTAMP WITH TIME ZONE NOT NULL,
    description TEXT,
    checksum VARCHAR(64),
    execution_time_ms INTEGER,
    applied_by VARCHAR(256)
);
```

## PostgreSQL Requirements

- PostgreSQL 14+
- Extensions: `uuid-ossp` (created automatically)

## Safety Checks

All rollback scripts include:
- Pre-rollback data count reporting
- Confirmation prompts
- Transaction safety
- Foreign key constraint cleanup

## Troubleshooting

### Migration Fails

1. Check PostgreSQL logs
2. Verify connection parameters
3. Ensure user has CREATE, ALTER, DROP permissions
4. Check for conflicting objects

### Data Migration Issues

1. Verify legacy tables exist before running
2. Check foreign key constraints are satisfied
3. Review migration logs for specific errors

### Validation Failures

1. Run `validate_migration.sql` to identify issues
2. Check `verify_data_integrity.sql` for checksum mismatches
3. Review foreign key orphaned records

## Support

For issues with migrations, contact:
- Database Team: db-team@synaxis.local
- DevOps: devops@synaxis.local
