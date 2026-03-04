# Migration Rehearsal Runbook

## Overview

This runbook documents the procedures for executing migration rehearsals in staging environments. Rehearsals validate migration procedures before production deployment.

## Prerequisites

- Staging environment provisioned and accessible
- Database backup configured
- Monitoring and alerting in place
- Rollback scripts tested
- Stakeholders notified

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

## Rehearsal Scenarios

### 1. Happy Path Rehearsal

**Purpose:** Execute complete migration runbook under normal conditions.

**Steps:**

1. **Pre-Migration Validation**
   - Verify database connectivity
   - Check pending migrations count
   - Validate current schema version

2. **Database Backup**
   - Create full database backup
   - Verify backup integrity
   - Record backup location

3. **Execute Migrations**
   - Run `dotnet ef database update`
   - Monitor for errors
   - Record execution time

4. **Post-Migration Validation**
   - Verify no pending migrations
   - Confirm database connectivity
   - Check schema version

5. **Service Health Check**
   - Verify all services healthy
   - Check dependent service connectivity
   - Validate API endpoints

6. **Data Integrity Verification**
   - Run consistency checks
   - Verify constraint enforcement
   - Check index status

**Success Criteria:**
- All steps complete without errors
- Migrations applied successfully
- Services return healthy status
- Data integrity verified

**Timing Targets:**
- Pre-migration validation: < 30s
- Database backup: < 5 min
- Migration execution: < 10 min
- Post-migration validation: < 2 min
- Total happy path: < 20 min

### 2. Failure Scenario Rehearsal

**Purpose:** Test rollback procedures and failure recovery.

**Scenarios:**

#### 2.1 Migration Failure Simulation
- Simulate migration script error
- Verify error detection
- Confirm rollback triggers

#### 2.2 Rollback Procedure Test
- Execute rollback to previous migration
- Verify data restoration
- Confirm schema reversion

#### 2.3 Data Consistency After Rollback
- Run data integrity checks
- Verify no orphaned records
- Confirm referential integrity

#### 2.4 Recovery Time Documentation
- Measure time to detect failure
- Record rollback execution time
- Document total recovery time

**Success Criteria:**
- Rollback completes successfully
- Data remains consistent
- Recovery time < 5 minutes
- No data loss

### 3. Partial Failure Rehearsal

**Purpose:** Test graceful degradation during migration.

**Scenarios:**

#### 3.1 Service Failure During Rollout
- Simulate service pod failure
- Verify traffic rerouting
- Check circuit breaker activation

#### 3.2 Database Connection Issues
- Simulate connection pool exhaustion
- Verify connection retry logic
- Check fallback behavior

#### 3.3 Network Partition Scenarios
- Simulate network isolation
- Verify timeout handling
- Check retry policies

#### 3.4 Graceful Degradation Verification
- Test reduced functionality mode
- Verify error messaging
- Check monitoring alerts

**Success Criteria:**
- Service continues operating in degraded mode
- No cascading failures
- User experience acceptable
- Recovery automatic

### 4. Performance Baseline Rehearsal

**Purpose:** Establish performance baselines and detect regressions.

**Steps:**

#### 4.1 Load Test Before Migration
- Run baseline load test
- Record response times:
  - Average
  - P95
  - P99
- Measure throughput (RPS)
- Document resource utilization

#### 4.2 Load Test After Migration
- Run identical load test
- Record same metrics
- Compare environments

#### 4.3 Response Time Comparison
- Calculate deltas:
  - Average response time change
  - P95 response time change
  - P99 response time change
- Flag regressions > 10%

#### 4.4 Regression Verification
- Compare error rates
- Check throughput degradation
- Validate resource usage

**Acceptance Criteria:**
- Response time regression < 10%
- Throughput maintained
- Error rate unchanged
- Resource usage similar

## Go/No-Go Decision Criteria

### Go Criteria (All Must Pass)

| Criterion | Threshold | Measurement |
|-----------|-----------|-------------|
| Happy Path | 100% pass | All steps complete |
| Rollback Test | 100% pass | Rollback succeeds |
| Data Consistency | 100% pass | No integrity issues |
| Recovery Time | < 5 min | From failure to recovery |
| Partial Failure | 100% pass | Graceful degradation |
| Performance | < 10% regression | Response time delta |
| Error Rate | < 0.1% | Post-migration |

### Go with Monitoring

If performance regression is 5-10%, proceed with:
- Enhanced monitoring
- Performance alert thresholds
- Rollback plan ready
- Stakeholder acknowledgment

### No-Go Criteria (Any Triggers)

| Issue | Action |
|-------|--------|
| Rollback fails | **NO-GO** - Fix rollback procedure |
| Data loss detected | **NO-GO** - Investigate and fix |
| Recovery time > 10 min | **NO-GO** - Optimize recovery |
| Performance regression > 10% | **NO-GO** - Performance tuning required |
| Cascading failures | **NO-GO** - Fix failure isolation |
| Happy path fails | **NO-GO** - Fix and re-rehearse |

### Decision Matrix

```
                    Rollback
                    Pass    Fail
                  +-------+-------+
Happy Path Pass   |   GO  | NO-GO |
                  +-------+-------+
Happy Path Fail   | NO-GO | NO-GO |
                  +-------+-------+
```

## Stakeholder Sign-Off

### Required Approvals

| Role | Responsibility | Sign-Off Criteria |
|------|----------------|-------------------|
| Engineering Lead | Technical validation | All tests pass |
| Database Admin | Data integrity | Rollback verified |
| DevOps/SRE | Deployment readiness | Runbook validated |
| Product Owner | Business impact | Performance acceptable |
| Security | Security review | No new vulnerabilities |

### Sign-Off Checklist

- [ ] Happy path rehearsal completed successfully (3 times)
- [ ] All failure scenarios tested
- [ ] Rollback verified working
- [ ] Performance baselines established
- [ ] Recovery time documented
- [ ] Go/No-Go decision documented
- [ ] Stakeholders reviewed results
- [ ] Production deployment authorized

## Issue Log Template

```markdown
## Issue Log - Rehearsal {ID}

### Issue Summary
- **ID:** ISSUE-001
- **Phase:** Happy Path / Failure / Partial / Performance
- **Severity:** Low / Medium / High / Critical
- **Status:** Open / In Progress / Resolved

### Description
Brief description of the issue.

### Steps to Reproduce
1. Step 1
2. Step 2
3. Step 3

### Expected Behavior
What should have happened.

### Actual Behavior
What actually happened.

### Resolution
How the issue was resolved (if resolved).

### Lessons Learned
What we learned from this issue.

### Runbook Updates
Changes made to the runbook based on this issue.
```

## Timing Documentation

Record timing for each rehearsal:

```markdown
## Rehearsal Timing Log

| Rehearsal | Run | Start | End | Duration | Result |
|-----------|-----|-------|-----|----------|--------|
| Happy Path | 1 | 10:00 | 10:15 | 15m | PASS |
| Happy Path | 2 | 11:00 | 11:12 | 12m | PASS |
| Happy Path | 3 | 14:00 | 14:18 | 18m | PASS |
| Failure | 1 | 10:30 | 10:45 | 15m | PASS |
| Partial | 1 | 11:30 | 11:50 | 20m | PASS |
| Performance | 1 | 15:00 | 15:30 | 30m | PASS |
```

## Rollback Procedure

### When to Rollback

- Migration fails
- Data corruption detected
- Performance degradation > 10%
- Service unavailability
- Security issue discovered

### Rollback Steps

1. **Alert Stakeholders**
   - Notify on-call engineer
   - Update status page
   - Begin incident response

2. **Execute Rollback**
   ```bash
   ./scripts/rollback-migration.sh <target_migration>
   ```

3. **Verify Rollback**
   - Check database version
   - Verify data integrity
   - Confirm service health

4. **Document Incident**
   - Create incident ticket
   - Update issue log
   - Schedule post-mortem

## Troubleshooting

### Common Issues

#### Migration Fails with Timeout

**Symptoms:** Migration times out during execution

**Solution:**
1. Increase command timeout
2. Check for blocking queries
3. Consider splitting large migration

#### Rollback Partially Completes

**Symptoms:** Some tables reverted, others not

**Solution:**
1. Check rollback transaction status
2. Restore from backup if needed
3. Manual cleanup required

#### Performance Regression Detected

**Symptoms:** Response times increased after migration

**Solution:**
1. Check for missing indexes
2. Verify query plan changes
3. Consider migration optimization

## Appendix

### Scripts Reference

| Script | Purpose | Usage |
|--------|---------|-------|
| `run-migration-rehearsal.sh` | Main rehearsal script | `./run-migration-rehearsal.sh` |
| `rollback-migration.sh` | Rollback migrations | `./rollback-migration.sh <migration>` |
| `ci-guardrail-check.sh` | CI validation | `./ci-guardrail-check.sh` |

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `REHEARSAL_ENVIRONMENT` | Target environment | staging |
| `REHEARSAL_CONNECTION_STRING` | Database connection | (required) |
| `REHEARSAL_OUTPUT_DIR` | Results directory | ./rehearsal-results |
| `REHEARSAL_VERBOSE` | Verbose output | false |

### Contact Information

| Role | Contact | Escalation |
|------|---------|------------|
| Database Admin | dba@synaxis.io | On-call pager |
| DevOps | devops@synaxis.io | #incidents Slack |
| Engineering Lead | eng-lead@synaxis.io | Mobile |
