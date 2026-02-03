# ADR-005: Observability and Monitoring Stack

## Status
**Accepted** | 2026-02-03

## Context

The Inference Gateway requires comprehensive observability to:
- Monitor system health across distributed components
- Track token usage and cost allocation
- Debug routing decisions and provider performance
- Alert on anomalies and service degradation
- Support capacity planning and optimization

We needed a unified observability solution that integrates with our existing Docker Compose infrastructure and provides both operational dashboards and business intelligence.

## Decision

Implement a **Prometheus + Grafana observability stack** with the following architecture:

### Metrics Collection (Prometheus)

**Scrape Configuration:**
```yaml
scrape_interval: 15s
evaluation_interval: 15s
```

**Targets:**
- Prometheus itself (localhost:9090)
- Qdrant vector database (qdrant:9091, 10s interval)
- Inference Gateway API (inference-gateway:8080, /metrics endpoint)

### Metrics Categories

1. **Infrastructure Metrics**
   - Container resource usage (CPU, memory, network)
   - Database performance (query latency, connection pool)
   - Cache hit/miss ratios

2. **Application Metrics**
   - Request rate, latency (p50, p95, p99), error rate
   - Token consumption (input/output per provider)
   - Routing decisions and provider selection
   - Circuit breaker state changes

3. **Business Metrics**
   - Cost per request/tenant/user
   - Cache effectiveness
   - Provider utilization

### Visualization (Grafana)

**Dashboard Structure:**
- `qdrant-dashboard.json`: Vector database health and performance
- Inference Gateway overview dashboard (request volume, latency, errors)
- Cost allocation dashboard (token usage, provider costs)
- Token optimization dashboard (cache hits, compression ratios)

**Datasource Configuration:**
- Prometheus as primary metrics source
- Provisioned dashboards (version-controlled)

## Deployment

**Docker Compose Services:**
```yaml
prometheus:
  image: prom/prometheus:latest
  volumes:
    - ./monitoring/prometheus.yml:/etc/prometheus/prometheus.yml

grafana:
  image: grafana/grafana:latest
  volumes:
    - ./monitoring/grafana:/etc/grafana/provisioning
```

## Consequences

### Positive
- **Unified Visibility**: Single pane of glass for all metrics
- **Proactive Alerting**: Configurable alerts on thresholds
- **Cost Transparency**: Track token spend by tenant/user/provider
- **Debugging Power**: Correlate metrics across the request lifecycle
- **Vendor Neutral**: Open-source, no lock-in

### Negative
- **Storage Growth**: Time-series data grows indefinitely
- **Resource Overhead**: Additional containers to run
- **Configuration Drift**: Dashboards must be kept in sync with code changes
- **Alert Fatigue**: Poorly tuned alerts can create noise

## Implementation Notes

- All metrics follow Prometheus naming conventions
- Custom metrics exposed via `/metrics` endpoint
- Grafana dashboards stored as JSON in version control
- Retention policy: 15 days local, long-term in object storage (future)

## Related ADRs
- ADR-004: Token Optimization Architecture (metrics for cache/optimization)
- ADR-007: Qdrant Integration (vector DB monitoring)
