# Synaxis-iosz Runbooks

> **Location**: `/docs/runbooks/`  
> **Purpose**: Operational procedures for deployment, disaster recovery, and rollback

---

## Available Runbooks

### Disaster Recovery

| Document | Purpose | When to Use |
|----------|---------|-------------|
| [disaster-recovery.md](./disaster-recovery.md) | Complete DR procedures and runbook | All DR scenarios |
| [dr-validation-checklist.md](./dr-validation-checklist.md) | DR test validation checklist | After DR testing |
| [dr-architecture-diagrams.md](./dr-architecture-diagrams.md) | DR architecture with Mermaid diagrams | Planning, review |
| [logs/dr-test-execution-logs.md](./logs/dr-test-execution-logs.md) | Test execution logs | Reference, audit |
| [metrics/rto-rpo-validation.md](./metrics/rto-rpo-validation.md) | RTO/RPO measurements | Compliance, metrics |
| [scripts/dr-test-suite.sh](./scripts/dr-test-suite.sh) | Automated DR test script | Weekly/Monthly testing |

### Rollback Procedures

| Document | Purpose | When to Use |
|----------|---------|-------------|
| [rollback-procedures.md](./rollback-procedures.md) | Detailed rollback procedures | Rollback execution |
| [rollback-flowcharts.md](./rollback-flowcharts.md) | Visual rollback decision trees | Quick reference |
| [rollback-quick-reference.md](./rollback-quick-reference.md) | Command cheat sheet | Emergency rollback |

### Migration

| Document | Purpose | When to Use |
|----------|---------|-------------|
| [production-migration.md](./production-migration.md) | Production migration procedures | Migration planning |

---

## DR Testing Schedule

```
┌─────────────────────────────────────────────────────────────────┐
│                    DR TESTING CALENDAR                          │
├─────────────────────────────────────────────────────────────────┤
│  Weekly (Automated)    │  Monthly (Semi-Automated)              │
│  ├── PostgreSQL        │  ├── Regional Failover                 │
│  ├── Redis             │  └── Backup Verification               │
│  ├── Cosmos DB         │                                        │
│  ├── Kubernetes        │  Quarterly (Manual)                    │
│  ├── Pod Eviction      │  └── Full DR Drill                     │
│  └── Circuit Breaker   │                                        │
└─────────────────────────────────────────────────────────────────┘
```

### Running Automated Tests

```bash
# Weekly component tests (non-disruptive)
./scripts/dr-test-suite.sh database --notify
./scripts/dr-test-suite.sh service --notify

# Monthly full test (disruptive - requires approval)
./scripts/dr-test-suite.sh regional --force --notify

# Dry run (no actual failures)
./scripts/dr-test-suite.sh all --dry-run
```

---

## DR Objectives

| Metric | Target | Status |
|--------|--------|--------|
| RTO (Recovery Time Objective) | < 60 minutes | ✅ Validated |
| RPO (Recovery Point Objective) | < 15 minutes | ✅ Validated |
| Test Success Rate | 100% | ✅ Achieved |
| Last Full Test | 2026-03-04 | ✅ Current |

---

## Quick Reference

### Emergency Contacts

| Role | Contact | Escalation |
|------|---------|------------|
| On-Call Engineer | sre-oncall@synaxis.io | +1-xxx-xxx-xxxx |
| Platform Lead | platform-lead@synaxis.io | +1-xxx-xxx-xxxx |
| Engineering Manager | eng-mgr@synaxis.io | +1-xxx-xxx-xxxx |

### Key Metrics Dashboards

- **Replication Status**: https://grafana.synaxis.io/d/dr-replication
- **RTO/RPO Metrics**: https://grafana.synaxis.io/d/dr-metrics
- **Backup Health**: https://grafana.synaxis.io/d/backup-status

### Alert Channels

- **Critical**: #platform-critical (PagerDuty)
- **Warning**: #platform-alerts (Slack)
- **Info**: #platform-info (Slack)

---

## DR Scenarios Covered

### Database Failover
- ✅ PostgreSQL primary failure
- ✅ Cosmos DB regional failover
- ✅ Redis cache failure
- ✅ Point-in-time recovery

### Service Failover
- ✅ Kubernetes node failure
- ✅ Pod eviction and rescheduling
- ✅ Service mesh circuit breaker
- ✅ Load balancer failover

### Regional Failover
- ✅ Complete region failure simulation
- ✅ DNS failover
- ✅ Data replication verification
- ✅ RTO/RPO validation

### Backup & Restore
- ✅ Database backup verification
- ✅ File storage recovery
- ✅ Configuration backup restore
- ✅ Event store recovery

---

## Document Maintenance

| Document | Owner | Review Cycle | Last Updated |
|----------|-------|--------------|--------------|
| disaster-recovery.md | Platform Team | Quarterly | 2026-03-04 |
| dr-validation-checklist.md | QA Team | Per test | 2026-03-04 |
| dr-architecture-diagrams.md | Architecture | Quarterly | 2026-03-04 |
| dr-test-execution-logs.md | SRE Team | Per test | 2026-03-04 |
| rto-rpo-validation.md | Platform Team | Monthly | 2026-03-04 |
| dr-test-suite.sh | Platform Team | Monthly | 2026-03-04 |

---

## Contributing

When updating DR procedures:

1. Update the relevant runbook
2. Update validation checklist if procedures change
3. Update architecture diagrams if infrastructure changes
4. Test procedures in staging before production
5. Update this README with new sections

---

**For questions or issues with these runbooks, contact the Platform Engineering Team.**
