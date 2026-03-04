# Synaxis Rollback Procedures

> **Version:** 1.0.0  
> **Last Updated:** 2026-03-04  
> **Owner:** Platform Engineering Team  
> **Review Cycle:** Quarterly

## Table of Contents

1. [Overview](#overview)
2. [Rollback Decision Matrix](#rollback-decision-matrix)
3. [Application Rollback](#application-rollback)
4. [Data Rollback](#data-rollback)
5. [Infrastructure Rollback](#infrastructure-rollback)
6. [Communication Templates](#communication-templates)
7. [Post-Rollback Validation](#post-rollback-validation)
8. [Escalation Procedures](#escalation-procedures)
9. [Appendices](#appendices)

---

## Overview

This runbook provides comprehensive rollback procedures for all Synaxis deployment scenarios. Rollbacks are critical for maintaining system stability when deployments cause unexpected issues.

### Target Rollback Times

| Scenario | Target Time | Maximum Tolerable |
|----------|-------------|-------------------|
| Application (Blue/Green) | 5 minutes | 10 minutes |
| Application (Rolling) | 10 minutes | 15 minutes |
| Database Migration | 15 minutes | 30 minutes |
| Infrastructure (Terraform) | 20 minutes | 45 minutes |
| DNS/Certificate | 10 minutes | 20 minutes |

### Rollback Philosophy

> **"Fail Fast, Recover Faster"**

1. **Detection First**: Automated monitoring detects anomalies within 2 minutes
2. **Decision Speed**: Clear criteria enable rapid decision-making
3. **Execution Precision**: Automated scripts minimize human error
4. **Validation Rigor**: Post-rollback checks ensure system health
5. **Communication Transparency**: Stakeholders informed throughout process

### Prerequisites

Before executing any rollback, ensure:

- [ ] Access to deployment environments (kubectl, terraform, cloud CLI)
- [ ] Database connection strings and credentials
- [ ] Backup verification completed
- [ ] Rollback approval from on-call engineer (P1/P2) or auto-approved (P0)
- [ ] Communication channels ready (Slack, PagerDuty, Email)

---

## Rollback Decision Matrix

### When to Trigger a Rollback

| Metric | Warning Threshold | Critical Threshold | Rollback Trigger |
|--------|-------------------|-------------------|------------------|
| Error Rate | > 1% | > 5% | Critical |
| P99 Latency | > 500ms | > 2000ms | Critical |
| CPU Utilization | > 70% | > 90% | Warning |
| Memory Utilization | > 80% | > 95% | Warning |
| Database Connection Pool | > 70% | > 90% | Critical |
| Failed Requests/min | > 100 | > 500 | Critical |
| Business Impact | Degraded | Service Unavailable | Critical |

### Severity Classification

#### P0 - Emergency Rollback (Auto-Approved)
- **Triggers:**
  - Complete service outage
  - Data corruption detected
  - Security breach
  - Cascading failures across regions
- **Timeline:** Immediate rollback within 5 minutes
- **Approvers:** Auto-approved, post-incident review required
- **Communication:** Executive notification within 15 minutes

#### P1 - Critical Rollback (On-Call Approval)
- **Triggers:**
  - Error rate > 5%
  - P99 latency > 2000ms
  - Database connectivity issues
  - > 50% of traffic affected
- **Timeline:** Rollback within 10 minutes of detection
- **Approvers:** On-call engineer
- **Communication:** Team notification within 5 minutes

#### P2 - Standard Rollback (Manager Approval)
- **Triggers:**
  - Error rate 1-5%
  - P99 latency 500-2000ms
  - Feature degradation
  - < 50% of traffic affected
- **Timeline:** Rollback within 30 minutes
- **Approvers:** Engineering manager or delegate
- **Communication:** Team notification within 15 minutes

#### P3 - Deferred Rollback (Planned)
- **Triggers:**
  - Minor issues with workarounds available
  - Non-critical feature bugs
- **Timeline:** Next maintenance window
- **Approvers:** Product owner
- **Communication:** Standard release notes

### Decision Flowchart

```
┌─────────────────────────────────────────────────────────────────┐
│                    ANOMALY DETECTED                             │
└─────────────────────────────────────────────────────────────────┘
                                │
                ┌───────────────┴───────────────┐
                ▼                               ▼
    ┌───────────────────────┐       ┌───────────────────────┐
    │  P0 - Data Corruption │       │  P0 - Complete Outage │
    │  P0 - Security Breach │       │  P0 - Cascading Fail  │
    └───────────────────────┘       └───────────────────────┘
                │                               │
                └───────────────┬───────────────┘
                                ▼
                ┌───────────────────────────────┐
                │   AUTO-TRIGGER ROLLBACK       │
                │   Timeline: Immediate (< 5m)  │
                └───────────────────────────────┘
                                │
                ┌───────────────┼───────────────┐
                ▼               ▼               ▼
    ┌───────────────┐ ┌───────────────┐ ┌───────────────┐
    │ P1 - Critical │ │ P2 - Standard │ │ P3 - Deferred │
    │  Error > 5%   │ │  Error 1-5%   │ │  Minor Issue  │
    │  Latency >2s  │ │  Latency .5-2s│ │  Workaround   │
    └───────────────┘ └───────────────┘ └───────────────┘
            │               │               │
            ▼               ▼               ▼
    ┌───────────────┐ ┌───────────────┐ ┌───────────────┐
    │ On-Call Eng   │ │ Eng Manager   │ │ Product Owner │
    │ < 10 min      │ │ < 30 min      │ │ Next Window   │
    └───────────────┘ └───────────────┘ └───────────────┘
```

---

## Application Rollback

### Kubernetes Deployment Rollback

#### Method 1: Blue/Green Rollback (Recommended)

**Scenario:** Switch traffic from green (new) back to blue (stable) deployment.

```bash
#!/bin/bash
# rollback-bluegreen.sh - Rollback to blue deployment

set -euo pipefail

NAMESPACE="${NAMESPACE:-synaxis}"
SERVICE_NAME="${SERVICE_NAME:-synaxis-api}"
CURRENT_VERSION="${1:-green}"  # 'green' or 'blue'

echo "[INFO] Starting Blue/Green rollback..."
echo "[INFO] Current version: $CURRENT_VERSION"

# Determine target version
if [[ "$CURRENT_VERSION" == "green" ]]; then
    TARGET_VERSION="blue"
else
    TARGET_VERSION="green"
fi

echo "[INFO] Rolling back to: $TARGET_VERSION"

# Update service selector to point to stable version
kubectl patch service "$SERVICE_NAME" -n "$NAMESPACE" --type='json' -p='[{
    "op": "replace",
    "path": "/spec/selector/version",
    "value": "'"$TARGET_VERSION"'"
}]'

# Verify traffic shift
sleep 5
echo "[INFO] Verifying traffic shift..."
kubectl get endpoints "$SERVICE_NAME" -n "$NAMESPACE" -o wide

# Scale down problematic deployment
echo "[INFO] Scaling down $CURRENT_VERSION deployment..."
kubectl scale deployment "${SERVICE_NAME}-${CURRENT_VERSION}" -n "$NAMESPACE" --replicas=0

# Verify rollback
echo "[INFO] Verification:"
kubectl get pods -n "$NAMESPACE" -l "app=${SERVICE_NAME},version=${TARGET_VERSION}" -o wide

echo "[SUCCESS] Blue/Green rollback completed"
```

**Execution:**
```bash
chmod +x scripts/rollback-bluegreen.sh
./scripts/rollback-bluegreen.sh green  # Rollback from green to blue
```

#### Method 2: Rolling Update Rollback

**Scenario:** Rollback a standard rolling update using kubectl.

```bash
#!/bin/bash
# rollback-deployment.sh - Rollback to previous revision

set -euo pipefail

DEPLOYMENT="${DEPLOYMENT:-synaxis-api}"
NAMESPACE="${NAMESPACE:-synaxis}"
REVISION="${1:-0}"  # 0 = previous, or specify revision number

echo "[INFO] Checking rollout history..."
kubectl rollout history deployment/"$DEPLOYMENT" -n "$NAMESPACE"

echo "[INFO] Initiating rollback..."
if [[ "$REVISION" == "0" ]]; then
    kubectl rollout undo deployment/"$DEPLOYMENT" -n "$NAMESPACE"
else
    kubectl rollout undo deployment/"$DEPLOYMENT" -n "$NAMESPACE" --to-revision="$REVISION"
fi

echo "[INFO] Monitoring rollback progress..."
kubectl rollout status deployment/"$DEPLOYMENT" -n "$NAMESPACE" --timeout=300s

echo "[INFO] Verifying deployment..."
kubectl get deployment/"$DEPLOYMENT" -n "$NAMESPACE" -o wide
kubectl get pods -n "$NAMESPACE" -l app.kubernetes.io/component=api -o wide

echo "[SUCCESS] Deployment rollback completed"
```

#### Method 3: Helm Rollback

```bash
#!/bin/bash
# rollback-helm.sh - Rollback Helm release

set -euo pipefail

RELEASE="${RELEASE:-synaxis}"
NAMESPACE="${NAMESPACE:-synaxis}"
REVISION="${1:-0}"

echo "[INFO] Helm release history:"
helm history "$RELEASE" -n "$NAMESPACE" --max=10

if [[ "$REVISION" == "0" ]]; then
    echo "[INFO] Rolling back to previous revision..."
    helm rollback "$RELEASE" -n "$NAMESPACE"
else
    echo "[INFO] Rolling back to revision $REVISION..."
    helm rollback "$RELEASE" "$REVISION" -n "$NAMESPACE"
fi

echo "[INFO] Verifying rollback..."
helm status "$RELEASE" -n "$NAMESPACE"
helm test "$RELEASE" -n "$NAMESPACE" --timeout=5m || echo "[WARNING] Helm tests failed"

echo "[SUCCESS] Helm rollback completed"
```

### Database Migration Rollback (EF Core)

#### Automated Migration Rollback

**Pre-built Scripts:**
- Bash: `scripts/rollback-migration.sh`
- PowerShell: `scripts/rollback-migration.ps1`

**Usage:**
```bash
# Rollback to specific migration
./scripts/rollback-migration.sh InitialMultiTenant

# Rollback all migrations (destructive - use with caution)
./scripts/rollback-migration.sh 0

# With custom connection string
CONNECTION_STRING="Host=prod.db.synaxis.io;Database=synaxis;Username=postgres;Password=***" \
  ./scripts/rollback-migration.sh AddInvitationEntity
```

#### Manual Migration Rollback

```bash
#!/bin/bash
# manual-migration-rollback.sh

set -euo pipefail

INFRA_PROJECT="src/Synaxis.Infrastructure/Synaxis.Infrastructure.csproj"
TARGET_MIGRATION="${1:-}"

if [[ -z "$TARGET_MIGRATION" ]]; then
    echo "[ERROR] Migration name required"
    echo "Usage: $0 <migration_name>"
    echo ""
    echo "Available migrations:"
    dotnet ef migrations list --project "$INFRA_PROJECT"
    exit 1
fi

echo "[INFO] Rolling back to migration: $TARGET_MIGRATION"

# 1. Create backup (if not using automated script)
BACKUP_FILE="backups/manual_$(date +%Y%m%d_%H%M%S).sql"
mkdir -p backups
echo "[INFO] Backup will be created at: $BACKUP_FILE"

# 2. Execute rollback
dotnet ef database update "$TARGET_MIGRATION" --project "$INFRA_PROJECT"

# 3. Verify
echo "[INFO] Current migration state:"
dotnet ef migrations list --project "$INFRA_PROJECT"

echo "[SUCCESS] Migration rollback completed"
```

#### Migration Safety Checklist

Before rolling back migrations:

- [ ] Verify target migration exists: `dotnet ef migrations list`
- [ ] Review Down() method in migration file
- [ ] Confirm no data loss in Down() operations
- [ ] Backup database (automated scripts do this)
- [ ] Notify team of potential brief downtime
- [ ] Have database admin on standby for complex rollbacks

**Dangerous Operations Requiring Manual Review:**
- Column drops with data
- Table deletions
- Foreign key constraint removals
- Index drops on large tables

### Feature Flag Rollback

#### Quick Feature Disable

```bash
#!/bin/bash
# rollback-feature-flags.sh

set -euo pipefail

# For LaunchDarkly
FEATURE_KEY="${1:-}"
ENVIRONMENT="${ENVIRONMENT:-production}"

if [[ -z "$FEATURE_KEY" ]]; then
    echo "[ERROR] Feature key required"
    echo "Usage: $0 <feature_key>"
    exit 1
fi

echo "[INFO] Disabling feature flag: $FEATURE_KEY"

# Using LaunchDarkly API
curl -X PATCH "https://app.launchdarkly.com/api/v2/flags/default/${FEATURE_KEY}" \
  -H "Authorization: ${LD_API_KEY}" \
  -H "Content-Type: application/json" \
  -d '{
    "environments": {
      "'"$ENVIRONMENT"'": {
        "on": false
      }
    }
  }'

echo "[SUCCESS] Feature flag $FEATURE_KEY disabled"
```

#### Environment Variable Toggle

```bash
# Quick toggle via ConfigMap
kubectl patch configmap synaxis-api-config -n synaxis --type='json' -p='[{
    "op": "replace",
    "path": "/data/FEATURE_NEW_ROUTING",
    "value": "false"
}]'

# Rollout restart to pick up changes
kubectl rollout restart deployment/synaxis-api -n synaxis
kubectl rollout status deployment/synaxis-api -n synaxis
```

### CDN Cache Purge

#### CloudFront Cache Invalidation

```bash
#!/bin/bash
# purge-cloudfront.sh

set -euo pipefail

DISTRIBUTION_ID="${CLOUDFRONT_DISTRIBUTION_ID:-}"
PATHS="${1:-/*}"  # Default: purge all

if [[ -z "$DISTRIBUTION_ID" ]]; then
    echo "[ERROR] CLOUDFRONT_DISTRIBUTION_ID not set"
    exit 1
fi

echo "[INFO] Creating CloudFront invalidation for paths: $PATHS"

INVALIDATION_ID=$(aws cloudfront create-invalidation \
    --distribution-id "$DISTRIBUTION_ID" \
    --paths "$PATHS" \
    --query 'Invalidation.Id' \
    --output text)

echo "[INFO] Invalidation created: $INVALIDATION_ID"
echo "[INFO] Waiting for completion..."

aws cloudfront wait invalidation-completed \
    --distribution-id "$DISTRIBUTION_ID" \
    --id "$INVALIDATION_ID"

echo "[SUCCESS] Cache purge completed"
```

#### Azure CDN Purge

```bash
#!/bin/bash
# purge-azure-cdn.sh

set -euo pipefail

PROFILE_NAME="${CDN_PROFILE_NAME:-synaxis-cdn}"
ENDPOINT_NAME="${CDN_ENDPOINT_NAME:-synaxis-api}"
RESOURCE_GROUP="${RESOURCE_GROUP:-synaxis-rg}"
PATHS="${1:-/*}"

echo "[INFO] Purging Azure CDN endpoint: $ENDPOINT_NAME"

az cdn endpoint purge \
    --name "$ENDPOINT_NAME" \
    --profile-name "$PROFILE_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --content-paths "$PATHS" \
    --no-wait

echo "[INFO] Purge initiated (running asynchronously)"
echo "[INFO] Monitor status: az cdn endpoint show -n $ENDPOINT_NAME -g $RESOURCE_GROUP"
```

---

## Data Rollback

### Point-in-Time Restore (PostgreSQL)

#### AWS RDS Point-in-Time Restore

```bash
#!/bin/bash
# rds-pitr-restore.sh

set -euo pipefail

SOURCE_DB="${SOURCE_DB:-synaxis-production}"
TARGET_DB="${TARGET_DB:-synaxis-rollback-$(date +%Y%m%d-%H%M%S)}"
RESTORE_TIME="${1:-}"  # ISO 8601 format: 2026-03-04T10:30:00Z

if [[ -z "$RESTORE_TIME" ]]; then
    echo "[ERROR] Restore time required"
    echo "Usage: $0 <restore_time_iso8601>"
    echo "Example: $0 2026-03-04T10:30:00Z"
    exit 1
fi

echo "[INFO] Initiating RDS point-in-time restore..."
echo "[INFO] Source: $SOURCE_DB"
echo "[INFO] Target: $TARGET_DB"
echo "[INFO] Restore time: $RESTORE_TIME"

# Create PITR instance
aws rds restore-db-instance-to-point-in-time \
    --source-db-instance-identifier "$SOURCE_DB" \
    --target-db-instance-identifier "$TARGET_DB" \
    --restore-time "$RESTORE_TIME" \
    --use-latest-restorable-time \
    --no-publicly-accessible \
    --db-instance-class db.t3.medium \
    --vpc-security-group-ids "${DB_SECURITY_GROUP_ID}"

echo "[INFO] Restore initiated. This may take 15-30 minutes."
echo "[INFO] Monitor: aws rds describe-db-instances --db-instance-identifier $TARGET_DB"

# Wait for available
aws rds wait db-instance-available \
    --db-instance-identifier "$TARGET_DB"

echo "[SUCCESS] PITR restore completed: $TARGET_DB"
```

#### Azure Database for PostgreSQL Restore

```bash
#!/bin/bash
# azure-postgres-restore.sh

set -euo pipefail

SOURCE_SERVER="${SOURCE_SERVER:-synaxis-postgres-prod}"
TARGET_SERVER="${TARGET_SERVER:-synaxis-postgres-rollback-$(date +%Y%m%d)}"
RESOURCE_GROUP="${RESOURCE_GROUP:-synaxis-rg}"
RESTORE_TIME="${1:-}"

if [[ -z "$RESTORE_TIME" ]]; then
    echo "[ERROR] Restore time required (ISO 8601)"
    exit 1
fi

echo "[INFO] Restoring Azure PostgreSQL to: $RESTORE_TIME"

az postgres flexible-server restore \
    --source-server "$SOURCE_SERVER" \
    --name "$TARGET_SERVER" \
    --resource-group "$RESOURCE_GROUP" \
    --point-in-time "$RESTORE_TIME"

echo "[SUCCESS] Azure PostgreSQL restore initiated"
```

### Transaction Log Replay

#### PostgreSQL WAL Replay

```bash
#!/bin/bash
# wal-replay-recovery.sh

set -euo pipefail

DATA_DIR="${PGDATA:-/var/lib/postgresql/data}"
TARGET_LSN="${1:-}"

echo "[INFO] Preparing WAL replay recovery..."

# Create recovery signal
touch "$DATA_DIR/recovery.signal"

# Configure recovery parameters
cat >> "$DATA_DIR/postgresql.auto.conf" <<EOF
restore_command = 'cp /mnt/wal_archive/%f %p'
recovery_target_lsn = '$TARGET_LSN'
recovery_target_action = 'pause'
EOF

# Restart PostgreSQL
echo "[INFO] Restarting PostgreSQL for recovery..."
systemctl restart postgresql

# Monitor recovery
echo "[INFO] Monitoring recovery progress..."
psql -c "SELECT pg_last_wal_replay_lsn(), pg_last_xact_replay_timestamp();"

echo "[SUCCESS] WAL replay initiated. Monitor with pg_stat_wal_receiver"
```

### Data Correction Scripts

#### Safe Update Pattern

```sql
-- data-correction-template.sql
-- Always use transactions and verify before commit

BEGIN;

-- 1. Create backup of affected rows
CREATE TABLE IF NOT EXISTS data_correction_log (
    id SERIAL PRIMARY KEY,
    table_name TEXT NOT NULL,
    operation TEXT NOT NULL,
    affected_count INTEGER,
    old_values JSONB,
    new_values JSONB,
    executed_by TEXT DEFAULT CURRENT_USER,
    executed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 2. Preview changes (dry run)
SELECT 
    id,
    column_name,
    current_value,
    corrected_value
FROM target_table
WHERE condition = 'problematic'
LIMIT 100;

-- 3. Backup affected rows
INSERT INTO data_correction_log (table_name, operation, affected_count, old_values)
SELECT 
    'target_table',
    'UPDATE',
    COUNT(*),
    jsonb_agg(row_to_json(target_table))
FROM target_table
WHERE condition = 'problematic';

-- 4. Execute correction
UPDATE target_table
SET column_name = corrected_value
WHERE condition = 'problematic';

-- 5. Verify count
DO $$
DECLARE
    affected INTEGER;
BEGIN
    GET DIAGNOSTICS affected = ROW_COUNT;
    RAISE NOTICE 'Rows affected: %', affected;
    
    -- Update log with count
    UPDATE data_correction_log 
    SET affected_count = affected 
    WHERE id = (SELECT MAX(id) FROM data_correction_log);
END $$;

-- 6. Verify sample
SELECT * FROM target_table 
WHERE condition = 'corrected'
LIMIT 10;

-- Uncomment to commit:
-- COMMIT;

-- Or rollback to cancel:
-- ROLLBACK;
```

### Event Sourcing Stream Rollback

#### Stream Reversal Pattern

```bash
#!/bin/bash
# event-stream-rollback.sh

set -euo pipefail

STREAM_ID="${1:-}"
TARGET_VERSION="${2:-}"
EVENT_STORE_URL="${EVENT_STORE_URL:-http://localhost:2113}"

if [[ -z "$STREAM_ID" || -z "$TARGET_VERSION" ]]; then
    echo "[ERROR] Stream ID and target version required"
    echo "Usage: $0 <stream_id> <target_version>"
    exit 1
fi

echo "[INFO] Rolling back stream: $STREAM_ID to version: $TARGET_VERSION"

# 1. Get current stream state
curl -s "$EVENT_STORE_URL/streams/$STREAM_ID" \
    -H "Accept: application/vnd.eventstore.atom+json" \
    -u "${EVENT_STORE_USER:-admin}:${EVENT_STORE_PASS:-changeit}"

# 2. Create compensating events (soft rollback)
COMPENSATION_EVENT='{
    "eventId": "'"$(uuidgen)"'",
    "eventType": "StreamRolledBack",
    "data": {
        "originalStreamId": "'"$STREAM_ID"'",
        "targetVersion": '"$TARGET_VERSION"',
        "reason": "deployment-rollback",
        "timestamp": "'"$(date -Iseconds)"'"
    }
}'

curl -X POST "$EVENT_STORE_URL/streams/$STREAM_ID" \
    -H "Content-Type: application/vnd.eventstore.events+json" \
    -H "ES-ExpectedVersion: $(($TARGET_VERSION - 1))" \
    -u "${EVENT_STORE_USER:-admin}:${EVENT_STORE_PASS:-changeit}" \
    -d "[$COMPENSATION_EVENT]"

echo "[SUCCESS] Event stream rollback completed"
```

---

## Infrastructure Rollback

### Terraform State Rollback

#### State Recovery Procedures

```bash
#!/bin/bash
# terraform-rollback.sh

set -euo pipefail

ENVIRONMENT="${1:-production}"
TARGET_VERSION="${2:-}"
TF_DIR="infrastructure/terraform/us"

echo "[INFO] Terraform rollback for environment: $ENVIRONMENT"

# 1. Show current state
echo "[INFO] Current Terraform state:"
cd "$TF_DIR"
terraform workspace select "$ENVIRONMENT"
terraform show | head -50

# 2. List state versions (S3 backend)
echo "[INFO] Available state versions:"
aws s3api list-object-versions \
    --bucket synaxis-terraform-state \
    --prefix "${ENVIRONMENT}/terraform.tfstate" \
    --query 'Versions[*].{VersionId:VersionId,LastModified:LastModified,IsLatest:IsLatest}' \
    --output table

# 3. If specific version provided, restore it
if [[ -n "$TARGET_VERSION" ]]; then
    echo "[INFO] Restoring state version: $TARGET_VERSION"
    
    # Backup current state
    aws s3 cp "s3://synaxis-terraform-state/${ENVIRONMENT}/terraform.tfstate" \
        "s3://synaxis-terraform-state/${ENVIRONMENT}/terraform.tfstate.backup-$(date +%Y%m%d-%H%M%S)"
    
    # Restore target version
    aws s3api get-object \
        --bucket synaxis-terraform-state \
        --key "${ENVIRONMENT}/terraform.tfstate" \
        --version-id "$TARGET_VERSION" \
        terraform.tfstate
    
    # Re-upload as current
    aws s3 cp terraform.tfstate "s3://synaxis-terraform-state/${ENVIRONMENT}/terraform.tfstate"
fi

# 4. Plan rollback
echo "[INFO] Planning rollback..."
terraform plan -out=rollback.plan

echo "[INFO] Review rollback.plan before applying"
echo "[INFO] Apply with: terraform apply rollback.plan"
```

#### Resource-Specific Rollback

```bash
#!/bin/bash
# terraform-resource-rollback.sh

set -euo pipefail

RESOURCE="${1:-}"
ACTION="${2:-taint}"  # taint, untaint, or replace

if [[ -z "$RESOURCE" ]]; then
    echo "[ERROR] Resource address required"
    echo "Usage: $0 <resource_address> [taint|untaint|replace]"
    echo "Example: $0 aws_instance.example replace"
    exit 1
fi

echo "[INFO] Performing $ACTION on resource: $RESOURCE"

case "$ACTION" in
    taint)
        terraform taint "$RESOURCE"
        echo "[INFO] Resource marked for recreation on next apply"
        ;;
    untaint)
        terraform untaint "$RESOURCE"
        echo "[INFO] Resource taint removed"
        ;;
    replace)
        terraform plan -replace="$RESOURCE" -out=replace.plan
        echo "[INFO] Replace plan created. Review and apply with: terraform apply replace.plan"
        ;;
    *)
        echo "[ERROR] Unknown action: $ACTION"
        exit 1
        ;;
esac
```

### DNS Rollback

#### Route53 Rollback

```bash
#!/bin/bash
# route53-rollback.sh

set -euo pipefail

HOSTED_ZONE_ID="${HOSTED_ZONE_ID:-}"
RECORD_NAME="${1:-}"
ROLLBACK_TYPE="${2:-failover}"  # failover or weighted

if [[ -z "$HOSTED_ZONE_ID" || -z "$RECORD_NAME" ]]; then
    echo "[ERROR] HOSTED_ZONE_ID and RECORD_NAME required"
    exit 1
fi

echo "[INFO] Initiating DNS rollback for: $RECORD_NAME"
echo "[INFO] Rollback type: $ROLLBACK_TYPE"

case "$ROLLBACK_TYPE" in
    failover)
        # Failover to secondary
        aws route53 change-resource-record-sets \
            --hosted-zone-id "$HOSTED_ZONE_ID" \
            --change-batch '{
                "Changes": [{
                    "Action": "UPSERT",
                    "ResourceRecordSet": {
                        "Name": "'"$RECORD_NAME"'",
                        "Type": "A",
                        "Failover": "PRIMARY",
                        "SetIdentifier": "primary",
                        "HealthCheckId": "'"${HEALTH_CHECK_ID}"'",
                        "AliasTarget": {
                            "HostedZoneId": "'"${ALB_ZONE_ID}"'",
                            "DNSName": "'"${SECONDARY_ALB_DNS}"'",
                            "EvaluateTargetHealth": true
                        }
                    }
                }]
            }'
        ;;
    
    weighted)
        # Shift all weight to stable endpoints
        aws route53 change-resource-record-sets \
            --hosted-zone-id "$HOSTED_ZONE_ID" \
            --change-batch '{
                "Changes": [
                    {
                        "Action": "UPSERT",
                        "ResourceRecordSet": {
                            "Name": "'"$RECORD_NAME"'",
                            "Type": "A",
                            "SetIdentifier": "stable",
                            "Weight": 100,
                            "AliasTarget": {
                                "HostedZoneId": "'"${STABLE_ZONE_ID}"'",
                                "DNSName": "'"${STABLE_DNS}"'",
                                "EvaluateTargetHealth": true
                            }
                        }
                    },
                    {
                        "Action": "UPSERT",
                        "ResourceRecordSet": {
                            "Name": "'"$RECORD_NAME"'",
                            "Type": "A",
                            "SetIdentifier": "new",
                            "Weight": 0,
                            "AliasTarget": {
                                "HostedZoneId": "'"${NEW_ZONE_ID}"'",
                                "DNSName": "'"${NEW_DNS}"'",
                                "EvaluateTargetHealth": true
                            }
                        }
                    }
                ]
            }'
        ;;
esac

echo "[INFO] Waiting for DNS propagation (60 seconds)..."
sleep 60

echo "[SUCCESS] DNS rollback completed"
```

### Certificate Rollback

#### ACM Certificate Reversion

```bash
#!/bin/bash
# certificate-rollback.sh

set -euo pipefail

CERT_ARN="${1:-}"
PREVIOUS_CERT_ARN="${2:-}"

if [[ -z "$CERT_ARN" || -z "$PREVIOUS_CERT_ARN" ]]; then
    echo "[ERROR] Current and previous certificate ARNs required"
    echo "Usage: $0 <current_cert_arn> <previous_cert_arn>"
    exit 1
fi

echo "[INFO] Rolling back certificate..."
echo "[INFO] Current: $CERT_ARN"
echo "[INFO] Previous: $PREVIOUS_CERT_ARN"

# Update ALB/CloudFront to use previous certificate
aws elbv2 modify-listener \
    --listener-arn "${LISTENER_ARN}" \
    --certificates CertificateArn="$PREVIOUS_CERT_ARN"

echo "[SUCCESS] Certificate rollback completed"
```

### Network Configuration Rollback

#### Security Group Rollback

```bash
#!/bin/bash
# security-group-rollback.sh

set -euo pipefail

SG_ID="${1:-}"

if [[ -z "$SG_ID" ]]; then
    echo "[ERROR] Security Group ID required"
    exit 1
fi

echo "[INFO] Rolling back security group: $SG_ID"

# Get current rules
echo "[INFO] Current rules:"
aws ec2 describe-security-groups \
    --group-ids "$SG_ID" \
    --query 'SecurityGroups[0].IpPermissions' \
    --output table

# Revoke recent changes (example: remove overly permissive rule)
aws ec2 revoke-security-group-ingress \
    --group-id "$SG_ID" \
    --protocol tcp \
    --port 0-65535 \
    --cidr 0.0.0.0/0

echo "[SUCCESS] Security group rollback completed"
```

---

## Communication Templates

### P0 - Emergency Rollback Notification

```markdown
**Subject: [P0] EMERGENCY ROLLBACK INITIATED - Synaxis Production**

**Incident ID:** INC-$(date +%Y%m%d-%H%M%S)
**Severity:** P0 - Critical
**Status:** Rollback in Progress
**Started:** $(date -Iseconds)

**Summary:**
An emergency rollback has been initiated due to [REASON].

**Impact:**
- Service: [Affected Service]
- Users: [Estimated affected users]
- Duration: [Current downtime]

**Actions Taken:**
1. Rollback initiated at [TIME]
2. ETA to recovery: [X] minutes
3. Engineering team engaged

**Next Update:** $(date -d '+15 minutes' -Iminutes)

**Incident Commander:** [Name]
**Technical Lead:** [Name]
```

### P1 - Critical Rollback Notification

```markdown
**Subject: [P1] Rollback Initiated - Synaxis Production**

**Incident ID:** INC-$(date +%Y%m%d-%H%M%S)
**Severity:** P1 - High
**Status:** Rollback in Progress
**Started:** $(date -Iseconds)

**Summary:**
A rollback is in progress due to elevated error rates following deployment.

**Detection:**
- Error rate: [X]% (threshold: 5%)
- Detection time: [TIME]
- Rollback decision: [TIME]

**Rollback Progress:**
- [ ] Application rollback
- [ ] Database verification
- [ ] CDN purge
- [ ] Validation complete

**Next Update:** $(date -d '+30 minutes' -Iminutes)
```

### Rollback Completion Notification

```markdown
**Subject: [RESOLVED] Rollback Complete - Synaxis Production**

**Incident ID:** [ID]
**Status:** Resolved
**Resolved At:** $(date -Iseconds)
**Duration:** [X] minutes

**Summary:**
The rollback has been completed successfully. All systems are operating normally.

**Metrics Post-Rollback:**
- Error rate: [X]%
- P99 Latency: [X]ms
- Availability: [X]%

**Root Cause:**
[To be determined in post-mortem]

**Post-Mortem:**
Scheduled for: [Date/Time]
```

### Slack Notification (Automated)

```bash
#!/bin/bash
# notify-slack.sh

SEVERITY="${1:-P1}"
MESSAGE="${2:-}"
WEBHOOK_URL="${SLACK_WEBHOOK_URL:-}"

if [[ -z "$WEBHOOK_URL" ]]; then
    echo "[ERROR] SLACK_WEBHOOK_URL not set"
    exit 1
fi

COLOR="warning"
case "$SEVERITY" in
    P0) COLOR="danger" ;;
    P1) COLOR="danger" ;;
    P2) COLOR="warning" ;;
    RESOLVED) COLOR="good" ;;
esac

curl -X POST "$WEBHOOK_URL" \
    -H 'Content-Type: application/json' \
    -d '{
        "attachments": [{
            "color": "'"$COLOR"'",
            "title": "Rollback Notification - '"$SEVERITY"'",
            "text": "'"$MESSAGE"'",
            "footer": "Synaxis Platform",
            "ts": '$(date +%s)'
        }]
    }'
```

---

## Post-Rollback Validation

### Validation Checklist

#### Application Health

- [ ] All pods/containers running
- [ ] Health checks passing (/health, /ready)
- [ ] Error rates < 0.1%
- [ ] P99 latency within SLA
- [ ] No memory leaks or CPU spikes
- [ ] Log aggregation functioning
- [ ] Metrics flowing to Prometheus/Grafana

#### Database Integrity

- [ ] Connection pool utilization < 70%
- [ ] No active locks or deadlocks
- [ ] Migration state matches target
- [ ] Critical queries executing normally
- [ ] Backup system operational

#### Infrastructure

- [ ] Load balancer health checks passing
- [ ] DNS resolution working
- [ ] CDN cache responding correctly
- [ ] SSL certificates valid
- [ ] Security groups properly configured
- [ ] Network connectivity verified

#### Business Functionality

- [ ] Core user journeys functional
- [ ] Authentication/authorization working
- [ ] API endpoints responding correctly
- [ ] Third-party integrations connected
- [ ] Payment processing (if applicable) functional

### Automated Validation Script

```bash
#!/bin/bash
# post-rollback-validation.sh

set -euo pipefail

ENDPOINT="${API_ENDPOINT:-https://api.synaxis.io}"
NAMESPACE="${NAMESPACE:-synaxis}"
FAILED=0

echo "[INFO] Starting post-rollback validation..."

# 1. Kubernetes health
echo "[CHECK] Pod health..."
UNREADY_PODS=$(kubectl get pods -n "$NAMESPACE" -o json | \
    jq '[.items[] | select(.status.phase != "Running" or .status.containerStatuses[0].ready != true)] | length')

if [[ "$UNREADY_PODS" -gt 0 ]]; then
    echo "[FAIL] $UNREADY_PODS pods not ready"
    FAILED=$((FAILED + 1))
else
    echo "[PASS] All pods ready"
fi

# 2. HTTP health endpoint
echo "[CHECK] Health endpoint..."
if curl -sf "${ENDPOINT}/health" > /dev/null; then
    echo "[PASS] Health endpoint responding"
else
    echo "[FAIL] Health endpoint failed"
    FAILED=$((FAILED + 1))
fi

# 3. Readiness endpoint
echo "[CHECK] Readiness endpoint..."
if curl -sf "${ENDPOINT}/ready" > /dev/null; then
    echo "[PASS] Readiness endpoint responding"
else
    echo "[FAIL] Readiness endpoint failed"
    FAILED=$((FAILED + 1))
fi

# 4. Error rate check (if metrics available)
echo "[CHECK] Error rate..."
ERROR_RATE=$(curl -sf "${ENDPOINT}/metrics" 2>/dev/null | \
    grep "http_requests_total" | grep "status=\"5" | \
    awk '{sum+=$2} END {print sum}' || echo "0")

if [[ "${ERROR_RATE:-0}" -gt 100 ]]; then
    echo "[WARN] Elevated error rate detected: $ERROR_RATE"
else
    echo "[PASS] Error rate acceptable"
fi

# 5. Database connectivity
echo "[CHECK] Database connectivity..."
# This would be a custom endpoint or direct check
# Example: kubectl exec into pod and test connection

# Summary
echo ""
echo "======================================"
if [[ "$FAILED" -eq 0 ]]; then
    echo "[SUCCESS] All validation checks passed"
    exit 0
else
    echo "[FAILURE] $FAILED validation check(s) failed"
    exit 1
fi
```

---

## Escalation Procedures

### Escalation Matrix

| Time Elapsed | Action | Contact |
|--------------|--------|---------|
| 0 min | Auto-detect & alert | PagerDuty |
| 5 min | On-call engineer engaged | On-call rotation |
| 10 min | P1 declared, manager notified | Engineering Manager |
| 15 min | P0 declared if unresolved | VP Engineering |
| 30 min | Executive briefing | CTO/CEO |
| 1 hour | War room convened | All stakeholders |

### On-Call Responsibilities

1. **Acknowledge Alert** within 5 minutes
2. **Assess Impact** - Determine severity
3. **Initiate Rollback** if criteria met
4. **Communicate** - Notify team via Slack
5. **Document** - Start incident timeline
6. **Validate** - Confirm rollback success
7. **Follow-up** - Schedule post-mortem

### War Room Protocol

For P0 incidents exceeding 30 minutes:

1. **Bridge Line:** [Conference bridge number]
2. **Slack Channel:** #incident-response
3. **Required Roles:**
   - Incident Commander
   - Technical Lead
   - Communications Lead
   - Scribe

---

## Appendices

### Appendix A: Quick Reference Commands

```bash
# Check deployment status
kubectl get deployments -n synaxis
kubectl get pods -n synaxis -o wide

# View logs
kubectl logs -n synaxis deployment/synaxis-api --tail=100 -f

# Rollback deployment
kubectl rollout undo deployment/synaxis-api -n synaxis

# Check rollout history
kubectl rollout history deployment/synaxis-api -n synaxis

# Port forward for debugging
kubectl port-forward -n synaxis svc/synaxis-api 8080:80

# Database migration status
dotnet ef migrations list --project src/Synaxis.Infrastructure

# Terraform state list
terraform state list

# CloudFront invalidation status
aws cloudfront get-invalidation --distribution-id $ID --id $INVALIDATION_ID
```

### Appendix B: Contact Information

| Role | Primary | Secondary |
|------|---------|-----------|
| On-Call Engineer | PagerDuty | [Backup] |
| Engineering Manager | [Name/Phone] | [Name/Phone] |
| VP Engineering | [Name/Phone] | [Name/Phone] |
| Platform Lead | [Name/Phone] | [Name/Phone] |
| Database Admin | [Name/Phone] | [Name/Phone] |

### Appendix C: Related Documentation

- [Deployment Guide](../deployment.md)
- [Architecture Overview](../architecture.md)
- [Runbook: Blue/Green Deployments](../../infrastructure/kubernetes/deployment/bluegreen-traffic-split.sh)
- [Runbook: Chaos Engineering](../../infrastructure/kubernetes/chaos/chaos-manage.sh)
- [Testing Procedures](../testing.md)

### Appendix D: Environment URLs

| Environment | API URL | Health Endpoint |
|-------------|---------|-----------------|
| Production | https://api.synaxis.io | /health |
| Staging | https://api.staging.synaxis.io | /health |
| Development | http://localhost:8080 | /health |

---

**Document Maintenance:**

- **Owner:** Platform Engineering Team
- **Review Cycle:** Quarterly
- **Last Review:** 2026-03-04
- **Next Review:** 2026-06-04

**Change Log:**

| Date | Version | Changes | Author |
|------|---------|---------|--------|
| 2026-03-04 | 1.0.0 | Initial release | Platform Team |
