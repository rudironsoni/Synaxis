# Synaxis Rollback Quick Reference

> **One-page reference for rollback operations**

## Emergency Contacts

| Role | Contact | Escalation |
|------|---------|------------|
| On-Call Engineer | PagerDuty | Auto-page |
| Engineering Manager | Slack: #incidents | 10 min |
| VP Engineering | Emergency Hotline | 15 min |
| Platform Lead | Slack: @platform-lead | Immediate |

## Quick Commands

### Application Rollback

```bash
# Blue/Green (fastest - 2 min)
./scripts/rollback-bluegreen.sh green

# Rolling deployment (5 min)
./scripts/rollback-deployment.sh

# Helm release (5 min)
./scripts/rollback-helm.sh
```

### Database Rollback

```bash
# Rollback to specific migration
./scripts/rollback-migration.sh InitialMultiTenant

# Rollback with custom connection
CONNECTION_STRING="Host=prod.db;Database=synaxis;..." \
  ./scripts/rollback-migration.sh MigrationName
```

### Infrastructure Rollback

```bash
# Terraform state rollback
./scripts/terraform-rollback.sh production

# With specific state version
./scripts/terraform-rollback.sh production abc123def
```

### CDN Purge

```bash
# CloudFront
CLOUDFRONT_DISTRIBUTION_ID=EDFDVBD6EXAMPLE \
  ./scripts/purge-cloudfront.sh

# Purge specific paths
./scripts/purge-cloudfront.sh /api/* /static/*
```

### Full Orchestrated Rollback

```bash
# Orchestrate complete rollback with validation
./scripts/rollback-orchestrator.sh app-bluegreen --target blue --notify

# Database rollback
./scripts/rollback-orchestrator.sh database --target InitialMultiTenant

# Skip validation (faster)
./scripts/rollback-orchestrator.sh app-rolling --skip-validation
```

## Decision Matrix (30 Seconds)

```
Error Rate > 5%     → P0 → Immediate rollback
Error Rate 1-5%     → P1 → On-call decides (10 min)
Latency > 2000ms    → P1 → On-call decides
Latency 500-2000ms  → P2 → Manager approval
Data Corruption     → P0 → Immediate rollback
Service Unavailable → P0 → Immediate rollback
```

## Validation Checklist

After any rollback, verify:

- [ ] `kubectl get pods` shows all Running
- [ ] `curl $API/health` returns HTTP 200
- [ ] `curl $API/ready` returns HTTP 200
- [ ] Error rate < 0.1% in dashboard
- [ ] P99 latency < 500ms
- [ ] Core user flows functional

```bash
# Automated validation
./scripts/post-rollback-validation.sh
```

## Rollback Time Targets

| Scenario | Target | Maximum |
|----------|--------|---------|
| Blue/Green Switch | 2 min | 5 min |
| Rolling Undo | 5 min | 10 min |
| Database | 10 min | 30 min |
| Terraform | 15 min | 45 min |
| Full Orchestrated | 15 min | 30 min |

## Communication Templates

### Slack (Urgent)
```
🚨 ROLLBACK INITIATED [P0/P1]
Scenario: <app/database/infrastructure>
Started: <time>
ETA: <X> minutes
Incident Commander: <@name>
```

### Status Page
```
[Investigating] We're experiencing issues with 
Synaxis API. We've initiated a rollback and expect 
resolution within 15 minutes.
```

### Customer Email (P0/P1)
```
Subject: Synaxis Service Disruption - Issue Resolved

We experienced a service disruption from [time] to [time].
The issue has been resolved via rollback.
No data was lost. Post-mortem: [link]
```

## Common Issues

### "pods not ready"
```bash
# Check logs
kubectl logs -n synaxis deployment/synaxis-api --tail=100

# Describe pod
kubectl describe pod -n synaxis <pod-name>
```

### "migration failed"
```bash
# Check migration status
dotnet ef migrations list --project src/Synaxis.Infrastructure

# Manual fix required - contact DBA
```

### "terraform lock"
```bash
# Force unlock (use with caution)
terraform force-unlock <lock-id>
```

## Environment URLs

| Environment | Health | Metrics |
|-------------|--------|---------|
| Production | https://api.synaxis.io/health | https://grafana.synaxis.io |
| Staging | https://api.staging.synaxis.io/health | https://grafana.staging.synaxis.io |

## Documentation

- Full Procedures: `docs/runbooks/rollback-procedures.md`
- Flowcharts: `docs/runbooks/rollback-flowcharts.md`
- Migration Guide: `docs/runbooks/production-migration.md`
- Disaster Recovery: `docs/runbooks/disaster-recovery.md`

---

**Version:** 1.0.0 | **Last Updated:** 2026-03-04
