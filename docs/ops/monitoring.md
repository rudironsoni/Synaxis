# Monitoring Guide

> **ULTRA MISER MODE™ Observability**: Why pay for Datadog when you can curl? This guide shows you how to monitor your AI gateway using built-in endpoints, free tools, and the sheer power of being too cheap to buy monitoring software.

Synaxis provides comprehensive monitoring capabilities out of the box. No expensive SaaS required—just good old-fashioned HTTP endpoints and some elbow grease.

---

## Table of Contents

- [Health Check Endpoints](#health-check-endpoints)
- [Admin Health Dashboard](#admin-health-dashboard)
- [Metrics Overview](#metrics-overview)
- [Provider Monitoring](#provider-monitoring)
- [System Monitoring](#system-monitoring)
- [Log Monitoring](#log-monitoring)
- [Alerting Strategies](#alerting-strategies)
- [Monitoring Stack Setup](#monitoring-stack-setup)

---

## Health Check Endpoints

Synaxis exposes three health check endpoints for different purposes:

### Liveness Check

**Endpoint:** `GET /health/liveness`

**Purpose:** Kubernetes/Docker liveness probe. Returns 200 if the application is running.

**Response:**
```json
{
  "status": "healthy"
}
```

**Usage:**
```bash
# Basic check
curl http://localhost:8080/health/liveness

# For load balancers (expect 200)
curl -s -o /dev/null -w "%{http_code}" http://localhost:8080/health/liveness
```

**When to Use:**
- Kubernetes liveness probes
- Load balancer health checks
- Docker health checks
- Basic uptime monitoring

---

### Readiness Check

**Endpoint:** `GET /health/readiness`

**Purpose:** Returns 200 only when all dependencies (database, Redis, providers) are ready to accept traffic.

**Response:**
```json
{
  "status": "healthy",
  "database": "healthy",
  "redis": "healthy",
  "providers": "healthy"
}
```

**Status Values:**
- `healthy` - All checks passed
- `degraded` - Some non-critical issues
- `unhealthy` - Critical dependency failing

**Usage:**
```bash
# Check readiness
curl http://localhost:8080/health/readiness

# Parse with jq
curl -s http://localhost:8080/health/readiness | jq '.status'
```

**When to Use:**
- Kubernetes readiness probes
- Deployment verification
- Traffic routing decisions
- Pre-flight checks before updates

---

### Detailed Health Status

**Endpoint:** `GET /admin/health`

**Authentication:** JWT Bearer token required

**Purpose:** Comprehensive health information including all services and AI providers.

**Response:**
```json
{
  "timestamp": "2026-02-02T12:34:56Z",
  "overallStatus": "healthy",
  "services": [
    {
      "name": "PostgreSQL",
      "status": "healthy",
      "latency": 15,
      "lastChecked": "2026-02-02T12:34:50Z"
    },
    {
      "name": "Redis",
      "status": "healthy",
      "latency": 5,
      "lastChecked": "2026-02-02T12:34:50Z"
    },
    {
      "name": "API Gateway",
      "status": "healthy",
      "latency": 25,
      "lastChecked": "2026-02-02T12:34:56Z"
    }
  ],
  "providers": [
    {
      "name": "Groq",
      "status": "online",
      "latency": 120,
      "lastChecked": "2026-02-02T12:34:45Z",
      "models": ["llama-3.3-70b-versatile", "mixtral-8x7b"]
    },
    {
      "name": "Together",
      "status": "online",
      "latency": 450,
      "lastChecked": "2026-02-02T12:34:40Z",
      "models": ["llama-2-70b"]
    },
    {
      "name": "DeepInfra",
      "status": "offline",
      "latency": null,
      "lastChecked": "2026-02-02T12:33:00Z",
      "error": "Connection timeout"
    }
  ]
}
```

**Usage:**
```bash
# Get detailed health (requires auth)
curl http://localhost:8080/admin/health \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Extract provider status
curl -s http://localhost:8080/admin/health \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  | jq '.providers[] | {name: .name, status: .status, latency: .latency}'

# Check for unhealthy providers
curl -s http://localhost:8080/admin/health \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  | jq '.providers[] | select(.status == "offline") | .name'
```

---

## Admin Health Dashboard

The Synaxis Admin Web UI provides a visual health dashboard at `http://localhost:8080/admin/health`.

### Dashboard Features

1. **Overall System Status**
   - Color-coded status indicator (green/yellow/red)
   - Timestamp of last check
   - Quick summary of issues

2. **Service Health Cards**
   - PostgreSQL: Connection status and query latency
   - Redis: Connection status and operation latency
   - API Gateway: Response time and throughput

3. **Provider Health Cards**
   - Real-time status (online/degraded/offline)
   - Response latency in milliseconds
   - Available models per provider
   - Last check timestamp
   - Error messages for failed checks

4. **Auto-Refresh**
   - Dashboard updates every 30 seconds
   - Manual refresh button available
   - Visual indicators for state changes

### Accessing the Dashboard

```bash
# 1. Get JWT token
TOKEN=$(curl -s -X POST http://localhost:8080/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"your-password"}' \
  | jq -r '.token')

# 2. Open dashboard in browser with token
# Navigate to: http://localhost:8080/admin/health
# Token will be stored in browser localStorage
```

---

## Metrics Overview

### Built-in Metrics

Synaxis tracks the following metrics automatically:

| Metric | Type | Description |
|--------|------|-------------|
| `requests_total` | Counter | Total API requests |
| `requests_duration` | Histogram | Request duration in seconds |
| `provider_requests` | Counter | Requests per provider |
| `provider_errors` | Counter | Errors per provider |
| `provider_latency` | Gauge | Current provider latency |
| `rate_limit_hits` | Counter | Rate limit triggers |
| `active_connections` | Gauge | Current SSE streaming connections |

### Accessing Metrics

```bash
# Prometheus-compatible metrics endpoint
curl http://localhost:8080/metrics

# Example output:
# HELP requests_total Total number of requests
# TYPE requests_total counter
requests_total{method="POST",endpoint="/v1/chat/completions",status="200"} 1234

# HELP provider_latency Current provider latency in milliseconds
# TYPE provider_latency gauge
provider_latency{provider="groq"} 120
provider_latency{provider="together"} 450
```

---

## Provider Monitoring

### Individual Provider Status

Check a specific provider's health:

```bash
curl http://localhost:8080/admin/providers/{providerKey}/status \
  -H "Authorization: Bearer YOUR_TOKEN"

# Example:
curl http://localhost:8080/admin/providers/groq/status \
  -H "Authorization: Bearer YOUR_TOKEN"

# Response:
{
  "provider": "groq",
  "status": "healthy",
  "latency": 120,
  "lastCheck": "2026-02-02T12:34:45Z",
  "modelsAvailable": ["llama-3.3-70b-versatile"]
}
```

### Provider Health History

Providers are marked unhealthy automatically based on error responses:

| Error Code | Cooldown | Reason |
|------------|----------|--------|
| 429 Too Many Requests | 60 seconds | Rate limited |
| 401 Unauthorized | 1 hour | Invalid credentials |
| 5xx Server Error | 30 seconds | Provider error |
| Timeout | 30 seconds | Network issue |

Check current penalties in Redis:

```bash
# List all provider penalties
redis-cli keys "health:*:penalty"

# Check specific provider
docker compose exec redis redis-cli ttl "health:groq:penalty"
# Returns: (integer) 45  (seconds remaining)
```

### Provider Latency Tracking

Monitor provider response times:

```bash
# Get latency for all providers
curl -s http://localhost:8080/admin/health \
  -H "Authorization: Bearer YOUR_TOKEN" \
  | jq '.providers | sort_by(.latency) | .[] | "\(.name): \(.latency // "N/A")ms"'

# Output:
# "Groq: 120ms"
# "Together: 450ms"
# "DeepInfra: N/A"
```

---

## System Monitoring

### Database Monitoring

**PostgreSQL Health:**
```bash
# Check connection pool usage
curl -s http://localhost:8080/admin/health \
  -H "Authorization: Bearer YOUR_TOKEN" \
  | jq '.services[] | select(.name == "PostgreSQL")'

# Direct database check
psql "Host=localhost;Database=synaxis;Username=postgres;Password=postgres" \
  -c "SELECT count(*) as active_connections FROM pg_stat_activity;"
```

**Key Metrics:**
- Connection latency (should be < 50ms)
- Active connections (should be < max pool size)
- Query performance (check slow query log)

### Redis Monitoring

**Redis Health:**
```bash
# Check Redis connection
curl -s http://localhost:8080/admin/health \
  -H "Authorization: Bearer YOUR_TOKEN" \
  | jq '.services[] | select(.name == "Redis")'

# Direct Redis check
redis-cli info stats
```

**Key Metrics:**
- Operation latency (should be < 10ms)
- Memory usage (`used_memory_human`)
- Hit rate (`keyspace_hits / (keyspace_hits + keyspace_misses)`)

### Container Monitoring

**Docker Stats:**
```bash
# View container resource usage
docker stats --no-stream

# Check specific container
docker compose ps
```

**Key Metrics:**
- CPU usage (should be < 80% under load)
- Memory usage (should be < container limit)
- Network I/O (monitor for anomalies)

---

## Log Monitoring

### Structured Logging

Synaxis uses structured logging for easy parsing:

```json
{
  "timestamp": "2026-02-02T12:34:56.789Z",
  "level": "Information",
  "message": "Request routed to provider",
  "provider": "groq",
  "model": "llama-3.3-70b-versatile",
  "latency": 245,
  "requestId": "req-abc-123"
}
```

### Log Aggregation

**Docker Compose:**
```bash
# View all logs
docker compose logs -f webapi

# Filter by level
docker compose logs -f webapi | grep "Error"

# Filter by provider
docker compose logs -f webapi | grep "provider.*groq"
```

**Journald (systemd):**
```bash
# View service logs
journalctl -u synaxis -f

# Filter by time
journalctl -u synaxis --since "1 hour ago"
```

### Log Analysis Queries

**Find Slow Requests:**
```bash
docker compose logs webapi | grep "latency.*[0-9]\{4\}"
```

**Find Provider Failures:**
```bash
docker compose logs webapi | grep -E "(unhealthy|failed|error)" | grep provider
```

**Request Volume by Hour:**
```bash
docker compose logs webapi | grep "Request routed" | awk '{print $1}' | cut -dT -f2 | cut -d: -f1 | sort | uniq -c
```

---

## Alerting Strategies

### Simple Shell-Based Monitoring

Create a basic monitoring script:

```bash
#!/bin/bash
# monitor-synaxis.sh

WEBHOOK_URL="https://hooks.slack.com/services/YOUR/WEBHOOK/URL"
API_URL="http://localhost:8080"
TOKEN="YOUR_JWT_TOKEN"

# Check health
HEALTH=$(curl -s "${API_URL}/health/readiness" | jq -r '.status')

if [ "$HEALTH" != "healthy" ]; then
  curl -X POST "$WEBHOOK_URL" \
    -H 'Content-Type: application/json' \
    -d "{\"text\":\"🚨 Synaxis health check failed: ${HEALTH}\"}"
fi

# Check for offline providers
OFFLINE=$(curl -s "${API_URL}/admin/health" \
  -H "Authorization: Bearer ${TOKEN}" \
  | jq -r '.providers[] | select(.status == "offline") | .name' \
  | wc -l)

if [ "$OFFLINE" -gt 0 ]; then
  curl -X POST "$WEBHOOK_URL" \
    -H 'Content-Type: application/json' \
    -d "{\"text\":\"⚠️ ${OFFLINE} provider(s) offline\"}"
fi
```

Run via cron:
```bash
# Check every 5 minutes
*/5 * * * * /path/to/monitor-synaxis.sh
```

### Prometheus + Alertmanager (Free Stack)

**prometheus.yml:**
```yaml
global:
  scrape_interval: 15s

scrape_configs:
  - job_name: 'synaxis'
    static_configs:
      - targets: ['localhost:8080']
    metrics_path: '/metrics'

alerting:
  alertmanagers:
    - static_configs:
        - targets: ['localhost:9093']

rule_files:
  - 'synaxis-alerts.yml'
```

**synaxis-alerts.yml:**
```yaml
groups:
  - name: synaxis
    rules:
      - alert: SynaxisDown
        expr: up{job="synaxis"} == 0
        for: 1m
        annotations:
          summary: "Synaxis is down"
          
      - alert: HighErrorRate
        expr: rate(provider_errors[5m]) > 0.1
        for: 2m
        annotations:
          summary: "High error rate detected"
          
      - alert: ProviderOffline
        expr: provider_status == 0
        for: 1m
        annotations:
          summary: "Provider {{ $labels.provider }} is offline"
```

---

## Monitoring Stack Setup

### Minimal Stack (Free)

```yaml
# docker-compose.monitoring.yml
version: '3.8'

services:
  prometheus:
    image: prom/prometheus:latest
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml
      - prometheus-data:/prometheus
    ports:
      - "9090:9090"
    command:
      - '--config.file=/etc/prometheus/prometheus.yml'
      - '--storage.tsdb.path=/prometheus'

  grafana:
    image: grafana/grafana:latest
    volumes:
      - grafana-data:/var/lib/grafana
    ports:
      - "3000:3000"
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=admin

volumes:
  prometheus-data:
  grafana-data:
```

### Dashboard Queries

**Request Rate:**
```promql
rate(requests_total[5m])
```

**Average Latency:**
```promql
histogram_quantile(0.95, rate(requests_duration_bucket[5m]))
```

**Provider Error Rate:**
```promql
rate(provider_errors[5m]) / rate(provider_requests[5m])
```

**Active Streams:**
```promql
active_connections
```

---

> **Remember**: In ULTRA MISER MODE™, monitoring isn't about fancy dashboards—it's about knowing your system without paying for the privilege. A well-placed curl command and a simple cron job can replace expensive monitoring suites. The best monitoring is the kind that costs nothing but keeps you informed.

---

## Quick Monitoring Commands

```bash
# Health checks
curl http://localhost:8080/health/liveness
curl http://localhost:8080/health/readiness
curl -H "Authorization: Bearer $TOKEN" http://localhost:8080/admin/health

# Metrics
curl http://localhost:8080/metrics

# Provider status
curl -H "Authorization: Bearer $TOKEN" http://localhost:8080/admin/providers/groq/status

# Logs
docker compose logs -f webapi
docker compose logs -f webapi | grep Error

# Redis
docker compose exec redis redis-cli info stats
```

---

*Last updated: 2026-02-02*
