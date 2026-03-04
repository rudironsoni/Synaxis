# Synaxis Production Migration Runbook

> **Document ID**: SYN-RUN-001  
> **Version**: 1.0  
> **Last Updated**: 2026-03-04  
> **Owner**: Platform Engineering Team  
> **Review Cycle**: Every release

## Overview

This runbook provides step-by-step procedures for executing production migrations for the Synaxis AI inference gateway platform. Following this runbook ensures safe, repeatable, and auditable deployments.

## Prerequisites

- Access to production environment (requires `prod-deployer` role)
- Database backup permissions
- Monitoring dashboard access
- Rollback script permissions
- Emergency contact list

---

## Section 1: Pre-Migration Checklist

### 1.1 Database Backups Verified

#### Automated Backup Verification

```bash
# Verify automated backups are recent (< 24 hours)
./scripts/verify-backup.sh --env=production --max-age=24h

# Check backup integrity
./scripts/verify-backup.sh --verify-integrity --backup-path=/backups/latest
```

#### Manual Backup Creation

```bash
# Create pre-migration backup
./scripts/backup-database.sh \
  --env=production \
  --backup-type=full \
  --tag="pre-migration-$(date +%Y%m%d-%H%M%S)"
```

#### Backup Checklist

| Item | Status | Verified By | Timestamp |
|------|--------|-------------|-----------|
| PostgreSQL backup created | ☐ | | |
| Redis snapshot created | ☐ | | |
| Backup file size verified | ☐ | | |
| Backup restore tested (staging) | ☐ | | |
| Backup retention confirmed | ☐ | | |

**Acceptance Criteria**: All backups verified within 4 hours of migration window.

### 1.2 Feature Flags Configured

#### Pre-Migration Flag States

| Feature Flag | Current State | Target State | Rollback State |
|--------------|---------------|--------------|----------------|
| `new-provider-routing` | `disabled` | `enabled` | `disabled` |
| `enhanced-caching` | `disabled` | `enabled` | `disabled` |
| `stream-optimization` | `disabled` | `enabled` | `disabled` |
| `metrics-v2` | `disabled` | `enabled` | `disabled` |
| `circuit-breaker-v2` | `enabled` | `enabled` | `enabled` |

#### Feature Flag Verification Commands

```bash
# Check current feature flag states
synaxis-admin flags list --env=production

# Verify feature flag service health
curl -s https://flags.synaxis.io/health | jq .
```

### 1.3 Monitoring Alerts Configured

#### Alert Thresholds

| Metric | Warning Threshold | Critical Threshold | Alert Channel |
|--------|-------------------|-------------------|---------------|
| Error Rate | > 1% | > 5% | #alerts-critical |
| Response Time (p99) | > 2s | > 5s | #alerts-critical |
| CPU Usage | > 70% | > 85% | #alerts-warning |
| Memory Usage | > 80% | > 95% | #alerts-critical |
| Database Connections | > 80% | > 95% | #alerts-critical |
| Queue Depth | > 1000 | > 5000 | #alerts-critical |

#### Monitoring Verification

```bash
# Verify all alerts are active
synaxis-monitoring verify-alerts --env=production

# Test alert channels
synaxis-monitoring test-alert --channel=slack-critical
synaxis-monitoring test-alert --channel=pagerduty
```

### 1.4 Rollback Plan Reviewed

#### Rollback Prerequisites

- [ ] Rollback scripts tested in staging
- [ ] Database rollback scripts validated
- [ ] Service rollback procedures documented
- [ ] Rollback decision criteria agreed upon
- [ ] Rollback team identified and notified

#### Rollback Triggers

| Trigger | Threshold | Action |
|---------|-----------|--------|
| Error rate spike | > 10% for 5 minutes | Immediate rollback |
| P99 latency | > 10s for 10 minutes | Consider rollback |
| Database connection failures | > 50 for 2 minutes | Immediate rollback |
| Feature degradation | Customer complaints > 10 | Evaluate rollback |

---

## Section 2: Migration Steps

### 2.1 Pre-Deployment Phase (T-60 minutes)

#### Step 1: Announce Maintenance Window

```bash
# Post maintenance notice
synaxis-admin notify --channel=status-page \
  --message="Scheduled maintenance starting in 60 minutes" \
  --impact="Brief service degradation possible"

# Notify stakeholders
synaxis-admin notify --channel=slack-ops \
  --message="@channel Production migration starting in 60 minutes"
```

#### Step 2: Verify Infrastructure Capacity

```bash
# Check cluster capacity
kubectl top nodes

# Verify autoscaling is enabled
kubectl get hpa -n synaxis

# Check storage capacity
df -h /data
```

#### Step 3: Enable Maintenance Mode (Optional)

```bash
# Enable maintenance mode for non-critical endpoints
synaxis-admin maintenance enable \
  --duration=120m \
  --allow-health-checks \
  --message="System maintenance in progress"
```

### 2.2 Database Migration Phase (T-0)

#### Migration Script Inventory

| Order | Script ID | Description | Execution Time | Downtime Required |
|-------|-----------|-------------|----------------|-------------------|
| 1 | `20260304_001_AddProviderRouting` | Add provider routing table | 30s | No |
| 2 | `20260304_002_CreateMetricsV2Tables` | Create metrics v2 schema | 45s | No |
| 3 | `20260304_003_MigrateCacheConfig` | Migrate cache configuration | 60s | No |
| 4 | `20260304_004_AddStreamOptimization` | Add stream optimization indexes | 90s | No |
| 5 | `20260304_005_UpdateApiKeySchema` | Update API key schema | 120s | **Yes - 60s** |

#### Execute Database Migrations

```bash
#!/bin/bash
# execute-migrations.sh

set -euo pipefail

ENV="production"
MIGRATION_LOG="/var/log/synaxis/migrations-$(date +%Y%m%d-%H%M%S).log"

log_info() { echo "[INFO] $(date -Iseconds) - $1" | tee -a "$MIGRATION_LOG"; }
log_error() { echo "[ERROR] $(date -Iseconds) - $1" | tee -a "$MIGRATION_LOG"; }

log_info "Starting database migrations..."

# Execute migrations in order
for script in scripts/migrations/production/*.sql; do
    log_info "Executing: $script"
    
    if psql "$DATABASE_URL" -f "$script" >> "$MIGRATION_LOG" 2>&1; then
        log_info "Successfully executed: $script"
    else
        log_error "Failed to execute: $script"
        exit 1
    fi
done

log_info "All database migrations completed successfully"
```

### 2.3 Service Deployment Phase

#### Service Startup Sequence

```
Phase 1: Infrastructure Services (0-5 minutes)
├── Update ConfigMap/Secrets
├── Restart Redis (if required)
├── Update PostgreSQL connection pool
└── Verify infrastructure health

Phase 2: Core Services (5-10 minutes)
├── Deploy API Gateway (rolling update)
│   └── Verify health checks pass
├── Deploy Mediator Service (rolling update)
│   └── Verify CQRS handlers ready
├── Deploy Provider Proxies (rolling update)
│   └── Verify provider connections
└── Verify core service mesh

Phase 3: Supporting Services (10-15 minutes)
├── Deploy Metrics Service
├── Update Monitoring Exporters
├── Deploy Cache Invalidation Service
└── Verify observability stack

Phase 4: Traffic Migration (15-20 minutes)
├── Gradual traffic shift (10% → 50% → 100%)
├── Monitor error rates during shift
├── Verify feature flags active
└── Complete traffic migration
```

#### Deployment Commands

```bash
# Phase 1: Infrastructure
kubectl apply -f k8s/production/config/
kubectl rollout status deployment/redis -n synaxis --timeout=300s

# Phase 2: Core Services
kubectl apply -f k8s/production/core/
kubectl rollout status deployment/synaxis-gateway -n synaxis --timeout=600s
kubectl rollout status deployment/synaxis-mediator -n synaxis --timeout=600s

# Phase 3: Supporting Services
kubectl apply -f k8s/production/supporting/
kubectl rollout status deployment/synaxis-metrics -n synaxis --timeout=300s

# Phase 4: Traffic Migration (using canary)
./scripts/traffic-shift.sh --env=production --percentage=10 --wait=5m
./scripts/traffic-shift.sh --env=production --percentage=50 --wait=5m
./scripts/traffic-shift.sh --env=production --percentage=100
```

### 2.4 Health Check Verification

#### Endpoint Health Checks

```bash
#!/bin/bash
# verify-health.sh

ENDPOINTS=(
  "https://api.synaxis.io/health"
  "https://api.synaxis.io/ready"
  "https://api.synaxis.io/metrics"
)

for endpoint in "${ENDPOINTS[@]}"; do
  echo "Checking: $endpoint"
  
  response=$(curl -s -o /dev/null -w "%{http_code}" "$endpoint")
  
  if [[ "$response" == "200" ]]; then
    echo "✅ $endpoint - Healthy"
  else
    echo "❌ $endpoint - Unhealthy (HTTP $response)"
    exit 1
  fi
done
```

#### Service Health Matrix

| Service | Health Endpoint | Expected Status | Timeout |
|---------|-----------------|-----------------|---------|
| API Gateway | `/health` | HTTP 200 | 5s |
| Mediator | `/ready` | HTTP 200 | 10s |
| Provider Proxy | `/health` | HTTP 200 | 5s |
| Metrics Exporter | `/metrics` | HTTP 200 | 5s |
| Feature Flag Service | `/health` | HTTP 200 | 5s |

---

## Section 3: Post-Migration Validation

### 3.1 Smoke Tests

#### Critical Path Tests

```bash
#!/bin/bash
# smoke-tests.sh

set -euo pipefail

API_URL="https://api.synaxis.io"
API_KEY="${SYNAXIS_API_KEY}"

echo "=== Smoke Test Suite ==="

# Test 1: Health endpoint
echo "Test 1: Health check..."
curl -sf "${API_URL}/health" || { echo "❌ Health check failed"; exit 1; }
echo "✅ Health check passed"

# Test 2: Chat completion
echo "Test 2: Chat completion..."
curl -sf "${API_URL}/v1/chat/completions" \
  -H "Authorization: Bearer ${API_KEY}" \
  -H "Content-Type: application/json" \
  -d '{
    "model": "gpt-4",
    "messages": [{"role": "user", "content": "Hello, this is a smoke test"}],
    "max_tokens": 10
  }' || { echo "❌ Chat completion failed"; exit 1; }
echo "✅ Chat completion passed"

# Test 3: Streaming
echo "Test 3: Streaming..."
curl -sf "${API_URL}/v1/chat/completions" \
  -H "Authorization: Bearer ${API_KEY}" \
  -H "Content-Type: application/json" \
  -d '{
    "model": "gpt-4",
    "messages": [{"role": "user", "content": "Test"}],
    "stream": true
  }' > /dev/null || { echo "❌ Streaming failed"; exit 1; }
echo "✅ Streaming passed"

# Test 4: Embeddings
echo "Test 4: Embeddings..."
curl -sf "${API_URL}/v1/embeddings" \
  -H "Authorization: Bearer ${API_KEY}" \
  -H "Content-Type: application/json" \
  -d '{
    "model": "text-embedding-ada-002",
    "input": "Test embedding"
  }' || { echo "❌ Embeddings failed"; exit 1; }
echo "✅ Embeddings passed"

# Test 5: Provider routing
echo "Test 5: Provider routing..."
response=$(curl -s "${API_URL}/v1/providers" \
  -H "Authorization: Bearer ${API_KEY}")
if [[ -z "$response" ]]; then
  echo "❌ Provider routing failed"
  exit 1
fi
echo "✅ Provider routing passed"

echo "=== All smoke tests passed ==="
```

#### Smoke Test Results Template

| Test | Status | Duration | Notes |
|------|--------|----------|-------|
| Health Check | ☐ | | |
| Chat Completion | ☐ | | |
| Streaming | ☐ | | |
| Embeddings | ☐ | | |
| Provider Routing | ☐ | | |
| Rate Limiting | ☐ | | |
| Authentication | ☐ | | |

### 3.2 Integration Test Execution

#### Integration Test Suite

```bash
# Run full integration test suite
dotnet test tests/Synaxis.IntegrationTests/ \
  --filter "Category=Production" \
  --logger "trx;LogFileName=integration-results.trx" \
  --results-directory ./test-results/
```

#### Integration Test Matrix

| Test Suite | Test Count | Pass Criteria | Status |
|------------|------------|---------------|--------|
| Provider Integration | 25 | 100% pass | ☐ |
| Database Operations | 40 | 100% pass | ☐ |
| Cache Integration | 15 | 100% pass | ☐ |
| Streaming | 20 | 100% pass | ☐ |
| Multi-tenant | 10 | 100% pass | ☐ |
| Authentication | 30 | 100% pass | ☐ |

### 3.3 Performance Baseline Comparison

#### Performance Metrics Collection

```bash
# Collect performance metrics
synaxis-metrics collect \
  --duration=300s \
  --output=post-migration-metrics.json

# Compare with baseline
synaxis-metrics compare \
  --baseline=baseline-metrics.json \
  --current=post-migration-metrics.json \
  --thresholds=performance-thresholds.yaml
```

#### Performance Thresholds

| Metric | Baseline | Threshold | Post-Migration | Status |
|--------|----------|-----------|----------------|--------|
| P50 Latency | 150ms | < +20% | | ☐ |
| P99 Latency | 800ms | < +20% | | ☐ |
| Requests/sec | 5000 | > -10% | | ☐ |
| Error Rate | 0.1% | < 1% | | ☐ |
| CPU Usage | 45% | < 70% | | ☐ |
| Memory Usage | 60% | < 80% | | ☐ |

#### Performance Comparison Chart

```
Latency Comparison (ms)

Baseline    ████████████████████ 150ms (p50)
Post-Mig    ████████████████████████ 175ms (p50)
            [Within threshold ✓]

Baseline    ████████████████████████████████ 800ms (p99)
Post-Mig    ████████████████████████████████████ 850ms (p99)
            [Within threshold ✓]
```

### 3.4 Error Rate Monitoring

#### Error Rate Tracking

```bash
# Monitor error rates for 30 minutes post-migration
synaxis-monitoring watch-errors \
  --duration=30m \
  --threshold=1% \
  --alert-on-threshold-exceeded
```

#### Error Rate Dashboard

| Time Window | Error Rate | Status | Action |
|-------------|------------|--------|--------|
| 0-5 min | | | Monitor |
| 5-10 min | | | Monitor |
| 10-15 min | | | Monitor |
| 15-20 min | | | Evaluate |
| 20-30 min | | | Evaluate |

---

## Section 4: Rollback Procedures

### 4.1 When to Trigger Rollback

#### Automatic Rollback Triggers

| Condition | Threshold | Automatic Rollback |
|-----------|-----------|-------------------|
| Error rate spike | > 10% for 5 min | Yes |
| Database connection pool exhausted | > 95% for 2 min | Yes |
| Memory leak detected | > 90% for 10 min | Yes |
| Critical service down | Any core service | Yes |

#### Manual Rollback Triggers

| Condition | Threshold | Decision Authority |
|-----------|-----------|-------------------|
| Feature degradation | Customer complaints > 10 | Engineering Lead |
| Performance regression | P99 latency > +50% | Engineering Lead |
| Security concern | Any security issue | Security Team |
| Data integrity issue | Any data anomaly | Data Team |

### 4.2 Rollback Steps

#### Step 1: Announce Rollback

```bash
# Immediate stakeholder notification
synaxis-admin notify --channel=slack-critical \
  --message="🚨 INITIATING ROLLBACK - Migration issues detected" \
  --priority=critical

# Update status page
synaxis-admin notify --channel=status-page \
  --message="Experiencing service degradation - investigating" \
  --status=degraded
```

#### Step 2: Database Rollback

```bash
#!/bin/bash
# rollback-database.sh

set -euo pipefail

ENV="production"
BACKUP_TIMESTAMP="${1:-}"

if [[ -z "$BACKUP_TIMESTAMP" ]]; then
  echo "Usage: $0 <backup_timestamp>"
  exit 1
fi

echo "=== Database Rollback Procedure ==="
echo "Environment: $ENV"
echo "Backup: $BACKUP_TIMESTAMP"

# Step 2.1: Stop application writes
echo "Step 1: Enabling read-only mode..."
synaxis-admin maintenance enable --mode=read-only

# Step 2.2: Verify no active transactions
echo "Step 2: Verifying no active transactions..."
active_count=$(psql "$DATABASE_URL" -t -c "SELECT count(*) FROM pg_stat_activity WHERE state = 'active' AND query NOT LIKE '%pg_stat_activity%'")
if [[ "$active_count" -gt 0 ]]; then
  echo "Warning: $active_count active transactions found"
  echo "Waiting 30 seconds for transactions to complete..."
  sleep 30
fi

# Step 2.3: Restore from backup
echo "Step 3: Restoring database from backup..."
./scripts/rollback-migration.sh "$BACKUP_TIMESTAMP" \
  --connection="${DATABASE_URL}" \
  --confirm

# Step 2.4: Verify rollback
echo "Step 4: Verifying rollback..."
./scripts/verify-database.sh --env=production

echo "=== Database rollback completed ==="
```

#### Step 3: Service Rollback

```bash
#!/bin/bash
# rollback-services.sh

set -euo pipefail

PREVIOUS_VERSION="${1:-}"
ENV="production"

echo "=== Service Rollback Procedure ==="
echo "Environment: $ENV"
echo "Previous Version: $PREVIOUS_VERSION"

# Rollback in reverse order
SERVICES=(
  "synaxis-metrics"
  "synaxis-provider-proxy"
  "synaxis-mediator"
  "synaxis-gateway"
)

for service in "${SERVICES[@]}"; do
  echo "Rolling back: $service"
  
  kubectl set image deployment/$service \
    -n synaxis \
    $service="synaxis/$service:$PREVIOUS_VERSION"
  
  kubectl rollout status deployment/$service \
    -n synaxis \
    --timeout=300s
done

echo "=== Service rollback completed ==="
```

#### Step 4: Feature Flag Reset

```bash
# Reset all feature flags to pre-migration state
synaxis-admin flags reset --env=production --tag=pre-migration

# Verify flag states
synaxis-admin flags list --env=production --verify-against=pre-migration-state.json
```

### 4.3 Rollback Decision Tree

```
                    ┌─────────────────┐
                    │  Issue Detected │
                    └────────┬────────┘
                             │
              ┌──────────────┼──────────────┐
              ▼              ▼              ▼
    ┌─────────────┐ ┌─────────────┐ ┌─────────────┐
    │ Error Rate  │ │ Performance │ │  Security   │
    │   > 10%     │ │Regression   │ │   Issue     │
    └──────┬──────┘ └──────┬──────┘ └──────┬──────┘
           │               │               │
     ┌─────┴─────┐    ┌────┴────┐     ┌───┴───┐
     ▼           ▼    ▼         ▼     ▼       ▼
┌────────┐  ┌──────┐┌────────┐┌───┐ ┌──────┐┌────┐
│ Auto   │  │Manual││ Auto   ││Man│ │Auto  ││Manu│
│Rollback│  │Review││Rollback││ual│ │Rollback││al  │
└────────┘  └──────┘└────────┘└───┘ └──────┘└────┘
     │
     ▼
┌───────────────────────────────────────────┐
│           Rollback Execution               │
│  1. Announce rollback                     │
│  2. Enable read-only mode (optional)      │
│  3. Rollback database                     │
│  4. Rollback services (reverse order)     │
│  5. Reset feature flags                   │
│  6. Verify rollback                       │
│  7. Resume normal operations              │
│  8. Post-incident review                  │
└────────────────────────────────────────────┘
```

### 4.4 Communication Plan

#### Communication Templates

**Initial Rollback Announcement**

```
🚨 ROLLBACK IN PROGRESS 🚨

We are rolling back the deployment due to [REASON].

Impact: Brief service interruption expected
ETA: 15-30 minutes
Status: In Progress

Engineering team is actively working on resolution.
Updates will be posted every 5 minutes.
```

**Rollback Complete**

```
✅ ROLLBACK COMPLETE

All services have been rolled back to the previous version.
Service health: Monitoring
Error rates: Normal

We are continuing to monitor the system.
Next update in 30 minutes or upon status change.
```

**Post-Incident Summary**

```
📋 POST-INCIDENT SUMMARY

Incident: Production rollback
Duration: [START] - [END]
Impact: [IMPACT DESCRIPTION]
Root Cause: [ROOT CAUSE]
Resolution: [RESOLUTION]

Follow-up Actions:
- [Action 1]
- [Action 2]

Next Steps:
- [Next Step 1]
- [Next Step 2]
```

#### Communication Channels

| Audience | Channel | Timing |
|----------|---------|--------|
| Engineering Team | #incidents-warroom | Immediate |
| Engineering Leadership | #eng-leadership | Immediate |
| Customer Success | #customer-success | Within 5 min |
| Status Page | status.synaxis.io | Within 5 min |
| Customers | Email (if major) | Within 15 min |

---

## Migration Script Inventory

### Database Migration Scripts

| Script ID | Filename | Description | Rollback Script | Tested |
|-----------|----------|-------------|-----------------|--------|
| M001 | `001_AddProviderRouting.sql` | Add provider routing table | `001_AddProviderRouting_rollback.sql` | ☐ |
| M002 | `002_CreateMetricsV2Tables.sql` | Create metrics v2 schema | `002_CreateMetricsV2Tables_rollback.sql` | ☐ |
| M003 | `003_MigrateCacheConfig.sql` | Migrate cache configuration | `003_MigrateCacheConfig_rollback.sql` | ☐ |
| M004 | `004_AddStreamOptimization.sql` | Add stream optimization indexes | `004_AddStreamOptimization_rollback.sql` | ☐ |
| M005 | `005_UpdateApiKeySchema.sql` | Update API key schema | `005_UpdateApiKeySchema_rollback.sql` | ☐ |

### Application Migration Scripts

| Script ID | Filename | Description | Target Version | Tested |
|-----------|----------|-------------|----------------|--------|
| A001 | `update-configmaps.sh` | Update Kubernetes ConfigMaps | v2.1.0 | ☐ |
| A002 | `migrate-secrets.sh` | Migrate to new secret format | v2.1.0 | ☐ |
| A003 | `update-feature-flags.sh` | Update feature flag definitions | v2.1.0 | ☐ |

---

## Validation Checklist

### Pre-Migration Validation

| # | Check Item | Owner | Status |
|---|------------|-------|--------|
| 1 | Staging deployment successful | | ☐ |
| 2 | Integration tests pass | | ☐ |
| 3 | Performance tests pass | | ☐ |
| 4 | Security scan clear | | ☐ |
| 5 | Database migrations tested | | ☐ |
| 6 | Rollback scripts tested | | ☐ |
| 7 | Monitoring dashboards ready | | ☐ |
| 8 | On-call engineer notified | | ☐ |
| 9 | Maintenance window approved | | ☐ |
| 10 | Communication plan reviewed | | ☐ |

### Post-Migration Validation

| # | Check Item | Owner | Status |
|---|------------|-------|--------|
| 1 | All services healthy | | ☐ |
| 2 | Database connections stable | | ☐ |
| 3 | Smoke tests pass | | ☐ |
| 4 | Integration tests pass | | ☐ |
| 5 | Performance within threshold | | ☐ |
| 6 | Error rates normal | | ☐ |
| 7 | Feature flags working | | ☐ |
| 8 | Monitoring alerts functioning | | ☐ |
| 9 | Logs flowing correctly | | ☐ |
| 10 | Customer notifications sent | | ☐ |

---

## Approval Signatures

| Role | Name | Signature | Date |
|------|------|-----------|------|
| Technical Lead | | | |
| Engineering Manager | | | |
| Product Owner | | | |
| QA Lead | | | |
| DevOps Lead | | | |

---

## Appendix

### A. Quick Reference Commands

```bash
# Check migration status
synaxis-migration status --env=production

# View current migrations
dotnet ef migrations list --project src/Synaxis.Infrastructure/

# Backup database
./scripts/backup-database.sh --env=production

# Execute rollback
./scripts/rollback-migration.sh <migration_name>

# Check service health
kubectl get pods -n synaxis -l app=synaxis-gateway

# View logs
kubectl logs -n synaxis -l app=synaxis-gateway --tail=100

# Scale deployments
kubectl scale deployment synaxis-gateway --replicas=5 -n synaxis
```

### B. Emergency Contacts

| Role | Name | Phone | Slack |
|------|------|-------|-------|
| On-Call Engineer | | | @oncall |
| Platform Lead | | | @platform-lead |
| Engineering Manager | | | @eng-manager |
| CTO | | | @cto |

### C. Related Documentation

- [Architecture Overview](../architecture.md)
- [Deployment Guide](../deployment.md)
- [Getting Started](../getting-started.md)
- [Rollback Script](../../scripts/rollback-migration.sh)

---

**Document Control**  
*This runbook is version controlled. All changes must be peer-reviewed and approved.*
