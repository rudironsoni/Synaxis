# Post-Migration Validation Checklist

**Migration ID**: Synaxis-jru2  
**Date**: 2026-03-04  
**Validator**: _________________________

---

## 1. Health Checks

### 1.1 Service Health Endpoints

| # | Service | Endpoint | Status | Checked By | Date |
|---|---------|----------|--------|------------|------|
| 1.1.1 | Synaxis API | `/health/live` | [ ] | | |
| 1.1.2 | Synaxis API | `/health/ready` | [ ] | | |
| 1.1.3 | Synaxis API | `/health/startup` | [ ] | | |
| 1.1.4 | Gateway | `/openai/v1/health` | [ ] | | |
| 1.1.5 | Identity Service | `/health` | [ ] | | |
| 1.1.6 | WebApp | `/health` | [ ] | | |

### 1.2 Kubernetes Pod Status

| # | Region | Check | Status | Checked By | Date |
|---|--------|-------|--------|------------|------|
| 1.2.1 | us-east-1 | All pods Running | [ ] | | |
| 1.2.2 | us-east-1 | No restart loops | [ ] | | |
| 1.2.3 | us-east-1 | No OOMKilled events | [ ] | | |
| 1.2.4 | eu-west-1 | All pods Running | [ ] | | |
| 1.2.5 | eu-west-1 | No restart loops | [ ] | | |
| 1.2.6 | sa-east-1 | All pods Running | [ ] | | |
| 1.2.7 | sa-east-1 | No restart loops | [ ] | | |

### 1.3 Database Connectivity

| # | Database | Region | Status | Latency | Checked By | Date |
|---|----------|--------|--------|---------|------------|------|
| 1.3.1 | PostgreSQL Primary | us-east-1 | [ ] | ___ms | | |
| 1.3.2 | PostgreSQL Replica | us-east-1 | [ ] | ___ms | | |
| 1.3.3 | PostgreSQL Primary | eu-west-1 | [ ] | ___ms | | |
| 1.3.4 | PostgreSQL Primary | sa-east-1 | [ ] | ___ms | | |
| 1.3.5 | Redis Cluster | us-east-1 | [ ] | ___ms | | |
| 1.3.6 | Redis Cluster | eu-west-1 | [ ] | ___ms | | |

### 1.4 Message Bus Connectivity

| # | Service | Status | Latency | Checked By | Date |
|---|---------|--------|---------|------------|------|
| 1.4.1 | RabbitMQ Cluster | [ ] | ___ms | | |
| 1.4.2 | Kafka Connectors | [ ] | ___ms | | |
| 1.4.3 | EventHub (Azure) | [ ] | ___ms | | |

---

## 2. Functional Validation

### 2.1 Core User Journeys

#### Journey 1: User Login
| # | Step | Expected | Actual | Status | Checked By | Date |
|---|------|----------|--------|--------|------------|------|
| 2.1.1 | POST /auth/login | 200 OK | ___ | [ ] | | |
| 2.1.2 | Token received | Valid JWT | ___ | [ ] | | |
| 2.1.3 | Claims valid | Correct claims | ___ | [ ] | | |
| 2.1.4 | Response time | < 500ms | ___ms | [ ] | | |

#### Journey 2: Inference Request
| # | Provider | Model | Status | Response Time | Checked By | Date |
|---|----------|-------|--------|---------------|------------|------|
| 2.1.5 | Groq | llama3-70b | [ ] | ___s | | |
| 2.1.6 | Gemini | gemini-1.5-pro | [ ] | ___s | | |
| 2.1.7 | Cohere | command-r | [ ] | ___s | | |
| 2.1.8 | OpenRouter | gpt-4o | [ ] | ___s | | |
| 2.1.9 | Pollinations | openai | [ ] | ___s | | |
| 2.1.10 | Cloudflare | @cf/meta/llama-2 | [ ] | ___s | | |
| 2.1.11 | NVIDIA | llama-3.1-405b | [ ] | ___s | | |

#### Journey 3: Billing Operations
| # | Step | Expected | Actual | Status | Checked By | Date |
|---|------|----------|--------|--------|------------|------|
| 2.1.12 | Usage dashboard loads | < 1s | ___s | [ ] | | |
| 2.1.13 | Token count accurate | Match expected | ___ | [ ] | | |
| 2.1.14 | Cost calculation correct | Match expected | ___ | [ ] | | |

### 2.2 API Endpoints

#### Identity API
| # | Endpoint | Method | Expected | Actual | Status | Checked By | Date |
|---|----------|--------|----------|--------|--------|------------|------|
| 2.2.1 | /auth/register | POST | 201 | ___ | [ ] | | |
| 2.2.2 | /auth/login | POST | 200 | ___ | [ ] | | |
| 2.2.3 | /auth/logout | POST | 200 | ___ | [ ] | | |
| 2.2.4 | /auth/refresh | POST | 200 | ___ | [ ] | | |
| 2.2.5 | /auth/mfa/setup | GET | 200 | ___ | [ ] | | |
| 2.2.6 | /auth/mfa/enable | POST | 200 | ___ | [ ] | | |
| 2.2.7 | /auth/forgot-password | POST | 200 | ___ | [ ] | | |
| 2.2.8 | /auth/reset-password | POST | 200 | ___ | [ ] | | |

#### Gateway API
| # | Endpoint | Method | Expected | Actual | Status | Checked By | Date |
|---|----------|--------|----------|--------|--------|------------|------|
| 2.2.9 | /openai/v1/chat/completions | POST | 200 | ___ | [ ] | | |
| 2.2.10 | /openai/v1/completions | POST | 200 | ___ | [ ] | | |
| 2.2.11 | /openai/v1/models | GET | 200 | ___ | [ ] | | |
| 2.2.12 | /openai/v1/models/{id} | GET | 200 | ___ | [ ] | | |
| 2.2.13 | /openai/v1/responses | POST | 200 | ___ | [ ] | | |
| 2.2.14 | /admin/providers | GET | 200 | ___ | [ ] | | |
| 2.2.15 | /admin/providers/{id} | PUT | 200 | ___ | [ ] | | |
| 2.2.16 | /admin/health | GET | 200 | ___ | [ ] | | |

### 2.3 Webhook Deliveries

| # | Endpoint | Events | Success Rate | Avg Time | Status | Checked By | Date |
|---|----------|--------|--------------|----------|--------|------------|------|
| 2.3.1 | /webhooks/usage | ___ | ___% | ___ms | [ ] | | |
| 2.3.2 | /webhooks/billing | ___ | ___% | ___ms | [ ] | | |
| 2.3.3 | /webhooks/alerts | ___ | ___% | ___ms | [ ] | | |

### 2.4 Background Jobs

| # | Job Type | Executed | Success | Duration | Status | Checked By | Date |
|---|----------|----------|---------|----------|--------|------------|------|
| 2.4.1 | TokenAggregationJob | ___ | ___ | ___s | [ ] | | |
| 2.4.2 | BillingSyncJob | ___ | ___ | ___s | [ ] | | |
| 2.4.3 | ProviderHealthCheckJob | ___ | ___ | ___s | [ ] | | |
| 2.4.4 | CacheCleanupJob | ___ | ___ | ___s | [ ] | | |
| 2.4.5 | AuditLogArchiveJob | ___ | ___ | ___s | [ ] | | |

---

## 3. Data Integrity Validation

### 3.1 Row Count Verification

| # | Table | Pre-Migration | Post-Migration | Diff | Status | Checked By | Date |
|---|-------|---------------|----------------|------|--------|------------|------|
| 3.1.1 | Users | ______ | ______ | ______ | [ ] | | |
| 3.1.2 | Organizations | ______ | ______ | ______ | [ ] | | |
| 3.1.3 | Teams | ______ | ______ | ______ | [ ] | | |
| 3.1.4 | ApiKeys | ______ | ______ | ______ | [ ] | | |
| 3.1.5 | ProviderConfigs | ______ | ______ | ______ | [ ] | | |
| 3.1.6 | AuditLogs | ______ | ______ | ______ | [ ] | | |
| 3.1.7 | Inferences | ______ | ______ | ______ | [ ] | | |
| 3.1.8 | WebhookSubscriptions | ______ | ______ | ______ | [ ] | | |

### 3.2 Foreign Key Integrity

| # | Check | Expected | Actual | Status | Checked By | Date |
|---|-------|----------|--------|--------|------------|------|
| 3.2.1 | Orphaned apikeys | 0 | ___ | [ ] | | |
| 3.2.2 | Orphaned team_memberships | 0 | ___ | [ ] | | |
| 3.2.3 | Orphaned audit_logs | 0 | ___ | [ ] | | |
| 3.2.4 | Orphaned inferences | 0 | ___ | [ ] | | |

### 3.3 Event Sourcing Streams

| # | Stream | Events Expected | Events Actual | Gap | Status | Checked By | Date |
|---|--------|-----------------|---------------|-----|--------|------------|------|
| 3.3.1 | user-events | ______ | ______ | ______ | [ ] | | |
| 3.3.2 | organization-events | ______ | ______ | ______ | [ ] | | |
| 3.3.3 | inference-events | ______ | ______ | ______ | [ ] | | |
| 3.3.4 | billing-events | ______ | ______ | ______ | [ ] | | |
| 3.3.5 | provider-events | ______ | ______ | ______ | [ ] | | |

---

## 4. Performance Validation

### 4.1 Response Time SLA

| # | Endpoint | SLA (p95) | Actual | Status | Checked By | Date |
|---|----------|-----------|--------|--------|------------|------|
| 4.1.1 | /auth/login | 500ms | ___ms | [ ] | | |
| 4.1.2 | /auth/register | 1000ms | ___ms | [ ] | | |
| 4.1.3 | /openai/v1/models | 200ms | ___ms | [ ] | | |
| 4.1.4 | /openai/v1/chat/completions | 5000ms | ___ms | [ ] | | |
| 4.1.5 | /admin/providers | 300ms | ___ms | [ ] | | |
| 4.1.6 | /health/live | 50ms | ___ms | [ ] | | |

### 4.2 Throughput Requirements

| # | Metric | Requirement | Actual | Status | Checked By | Date |
|---|--------|-------------|--------|--------|------------|------|
| 4.2.1 | Max RPS (API) | 2000 req/sec | ___ | [ ] | | |
| 4.2.2 | Max RPS (Gateway) | 1000 req/sec | ___ | [ ] | | |
| 4.2.3 | Streaming throughput | 50 MB/s | ___ | [ ] | | |
| 4.2.4 | Batch processing | 10k records/min | ___ | [ ] | | |

### 4.3 Memory Leak Detection

| # | Hour | Heap Size (MB) | Status | Checked By | Date |
|---|------|----------------|--------|------------|------|
| 4.3.1 | 0 | ______ | [ ] | | |
| 4.3.2 | 4 | ______ | [ ] | | |
| 4.3.3 | 8 | ______ | [ ] | | |
| 4.3.4 | 12 | ______ | [ ] | | |
| 4.3.5 | 16 | ______ | [ ] | | |
| 4.3.6 | 20 | ______ | [ ] | | |
| 4.3.7 | 24 | ______ | [ ] | | |

**Trend Analysis**: [ ] Stable [ ] Increasing [ ] Decreasing

### 4.4 CPU Utilization

| # | Hour | CPU % (Avg) | Status | Checked By | Date |
|---|------|-------------|--------|------------|------|
| 4.4.1 | 0 | ______% | [ ] | | |
| 4.4.2 | 4 | ______% | [ ] | | |
| 4.4.3 | 8 | ______% | [ ] | | |
| 4.4.4 | 12 | ______% | [ ] | | |
| 4.4.5 | 16 | ______% | [ ] | | |
| 4.4.6 | 20 | ______% | [ ] | | |
| 4.4.7 | 24 | ______% | [ ] | | |

---

## 5. Security Validation

### 5.1 Authentication

| # | Test Case | Expected | Actual | Status | Checked By | Date |
|---|-----------|----------|--------|--------|------------|------|
| 5.1.1 | Valid JWT | Access granted | ___ | [ ] | | |
| 5.1.2 | Expired JWT | 401 | ___ | [ ] | | |
| 5.1.3 | Invalid JWT | 401 | ___ | [ ] | | |
| 5.1.4 | Missing JWT | 401 | ___ | [ ] | | |
| 5.1.5 | Valid API Key | Access granted | ___ | [ ] | | |
| 5.1.6 | Invalid API Key | 401 | ___ | [ ] | | |
| 5.1.7 | Revoked API Key | 401 | ___ | [ ] | | |

### 5.2 Authorization

| # | Role | Resource | Permission | Expected | Actual | Status | Checked By | Date |
|---|------|----------|------------|----------|--------|--------|------------|------|
| 5.2.1 | Anonymous | /admin/* | Deny | 403 | ___ | [ ] | | |
| 5.2.2 | User | /admin/providers | Deny | 403 | ___ | [ ] | | |
| 5.2.3 | Admin | /admin/providers | Allow | 200 | ___ | [ ] | | |
| 5.2.4 | User | /openai/v1/* | Allow | 200 | ___ | [ ] | | |
| 5.2.5 | User (other org) | /api/org/123 | Deny | 403 | ___ | [ ] | | |

### 5.3 Audit Logging

| # | Event Type | Logged | Fields Complete | Status | Checked By | Date |
|---|------------|--------|-----------------|--------|------------|------|
| 5.3.1 | Login Success | [ ] | ___/12 | [ ] | | |
| 5.3.2 | Login Failure | [ ] | ___/12 | [ ] | | |
| 5.3.3 | Logout | [ ] | ___/12 | [ ] | | |
| 5.3.4 | Password Change | [ ] | ___/12 | [ ] | | |
| 5.3.5 | API Key Created | [ ] | ___/12 | [ ] | | |
| 5.3.6 | API Key Revoked | [ ] | ___/12 | [ ] | | |
| 5.3.7 | Inference Request | [ ] | ___/12 | [ ] | | |
| 5.3.8 | Provider Switch | [ ] | ___/12 | [ ] | | |

### 5.4 Unauthorized Access Attempts

| # | Event Type | Count | Blocked | Status | Checked By | Date |
|---|------------|-------|---------|--------|------------|------|
| 5.4.1 | Failed login attempts | ___ | ___ | [ ] | | |
| 5.4.2 | Invalid API key usage | ___ | ___ | [ ] | | |
| 5.4.3 | Rate limit exceeded | ___ | ___ | [ ] | | |
| 5.4.4 | SQL injection attempts | ___ | ___ | [ ] | | |
| 5.4.5 | XSS attempts | ___ | ___ | [ ] | | |

---

## 6. 24-Hour Stability Period

### 6.1 Availability

| # | Service | Uptime % | Downtime | Status | Checked By | Date |
|---|---------|----------|----------|--------|------------|------|
| 6.1.1 | Synaxis API | ___% | ___s | [ ] | | |
| 6.1.2 | Gateway | ___% | ___s | [ ] | | |
| 6.1.3 | Identity Service | ___% | ___s | [ ] | | |
| 6.1.4 | WebApp | ___% | ___s | [ ] | | |
| 6.1.5 | PostgreSQL | ___% | ___s | [ ] | | |
| 6.1.6 | Redis | ___% | ___s | [ ] | | |

### 6.2 Error Rate Trends

| # | Hour | Error Rate | Status | Checked By | Date |
|---|------|------------|--------|------------|------|
| 6.2.1 | 0 | ___% | [ ] | | |
| 6.2.2 | 4 | ___% | [ ] | | |
| 6.2.3 | 8 | ___% | [ ] | | |
| 6.2.4 | 12 | ___% | [ ] | | |
| 6.2.5 | 16 | ___% | [ ] | | |
| 6.2.6 | 20 | ___% | [ ] | | |
| 6.2.7 | 24 | ___% | [ ] | | |

**24-Hour Average**: ______%  
**Target**: < 0.1%

---

## 7. Issue Log

### Critical Issues (P0)
| ID | Description | Status | Assigned To | Resolution Date |
|----|-------------|--------|-------------|-----------------|
| - | None identified | - | - | - |

### High Issues (P1)
| ID | Description | Status | Assigned To | Resolution Date |
|----|-------------|--------|-------------|-----------------|
| - | None identified | - | - | - |

### Medium Issues (P2)
| ID | Description | Status | Assigned To | Resolution Date |
|----|-------------|--------|-------------|-----------------|
| P2-1 | Redis connection spike during peak hours | Workaround | ___ | ___ |
| P2-2 | Webhook delivery delay for large payloads | Workaround | ___ | ___ |

### Low Issues (P3)
| ID | Description | Status | Assigned To | Resolution Date |
|----|-------------|--------|-------------|-----------------|
| P3-1 | Grafana dashboard stale data | Accepted | ___ | ___ |
| P3-2 | Log verbosity in staging | Accepted | ___ | ___ |
| P3-3 | Health check before cache warmup | Accepted | ___ | ___ |

---

## 8. Sign-Off

### Summary

| Category | Total Checks | Passed | Failed | Pass Rate |
|----------|--------------|--------|--------|-----------|
| Health Checks | 18 | ___ | ___ | ___% |
| Functional Validation | 42 | ___ | ___ | ___% |
| Data Integrity | 12 | ___ | ___ | ___% |
| Performance Validation | 16 | ___ | ___ | ___% |
| Security Validation | 28 | ___ | ___ | ___% |
| **TOTAL** | **116** | ___ | ___ | ___% |

### Approvals

| Role | Name | Signature | Date |
|------|------|-----------|------|
| Lead Engineer | | | |
| QA Lead | | | |
| Security Officer | | | |
| Product Owner | | | |
| Infrastructure Lead | | | |

### Final Decision

- [ ] **APPROVED** - Migration successful, approved for production traffic
- [ ] **CONDITIONAL** - Approved with noted issues (requires follow-up)
- [ ] **REJECTED** - Critical issues found, requires remediation

**Decision Date**: _______________  
**Decision By**: _______________

**Notes**: ___________________________________________________________________

____________________________________________________________________________

---

*End of Validation Checklist*
