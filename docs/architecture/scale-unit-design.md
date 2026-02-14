# Ephemeral Scale Unit Architecture

**Epic:** E2 - Ephemeral Scale Unit Architecture  
**Pattern:** Hansjoerg Scherer - Mission Critical Infrastructure  
**Status:** In Progress  
**Last Updated:** 2026-02-14

## Overview

Synaxis implements an **ephemeral scale unit architecture** where each "stamp" (scale unit) is an independently deployable, ephemeral unit of infrastructure that can be created, destroyed, and replaced without affecting the overall system availability.

This architecture enables:
- **Horizontal scaling** by adding stamps during high load
- **Blue-green deployments** using stamp rotation
- **Disaster recovery** through stamp failover
- **Cost optimization** by decommissioning idle stamps
- **Regional expansion** by deploying stamps to new regions

## Scale Unit Definition

A **scale unit (stamp)** is a complete, self-contained deployment of the Synaxis platform including all tiers:

```
Stamp: synaxis-{region}-{stamp-id}
  ├── Resource Group: rg-synaxis-{region}-{stamp-id}
  ├── Compute Tier (AKS)
  ├── Data Tier (Cosmos DB, Redis, PostgreSQL)
  └── Integration Tier (Service Bus, Event Hubs)
```

### Stamp Naming Convention

- **Format:** `synaxis-{region}-{stamp-id}`
- **Examples:**
  - `synaxis-weu-001` - West Europe, Stamp 001
  - `synaxis-sea-002` - Southeast Asia, Stamp 002
  - `synaxis-eus-001` - East US, Stamp 001

## Three-Tier Architecture

Each stamp consists of three independent tiers:

### 1. Compute Tier

**Components:**
- Azure Kubernetes Service (AKS) - Private cluster
- Azure Container Registry (ACR) - Shared across stamps
- Application Gateway (optional per stamp)

**Configuration:**
- **System Node Pool:** 3-5 nodes (Standard_D4s_v3)
  - Purpose: K8s system services, ingress controllers
  - Taints: `CriticalAddonsOnly=true:NoSchedule`
- **General Node Pool:** 3-20 nodes (Standard_D8s_v3)
  - Purpose: Application workloads, inference engines
  - Auto-scaling: Enabled (HPA + Cluster Autoscaler)
- **GPU Node Pool:** 0-10 nodes (Standard_NC24s_v3)
  - Purpose: AI/ML workloads, model inference
  - Auto-scaling: Enabled, scale-to-zero supported

**Specifications:**
- Kubernetes: 1.29+
- CNI: Azure CNI with Calico
- Network Policy: Calico
- Pod CIDR: 10.0.64.0/18
- Service CIDR: 10.0.32.0/20

### 2. Data Tier

**Components:**
- **Cosmos DB** - Tenant data, conversation history
- **Redis Enterprise** - Session cache, rate limiting
- **PostgreSQL** - Relational data, audit logs
- **Blob Storage** - Model artifacts, exports

**Cosmos DB Configuration:**
- API: SQL (Core) API
- Consistency: Session (default)
- Multi-region writes: Enabled
- Throughput: Autoscale 1000-10000 RU/s
- Partition Key: `/tenantId`
- TTL: Enabled for conversation history (90 days)
- Backup: Continuous with 30-day retention

**Redis Enterprise Configuration:**
- SKU: Enterprise E10 or E20
- Clustering: 6+ shards
- Persistence: AOF every 1 second
- Databases:
  - `session-cache`: sessions, volatile
  - `rate-limiter`: counters, persistent
  - `response-cache`: cached responses, volatile
  - `routing-state`: consistent hashing, persistent

**PostgreSQL Configuration:**
- SKU: Flexible Server GP_Standard_D4s_v3
- Storage: 256GB SSD, auto-grow
- Backup: 35-day retention, geo-redundant
- High Availability: Zone redundant

### 3. Integration Tier

**Components:**
- **Service Bus Premium** - Async messaging, cross-stamp communication
- **Event Hubs** - Telemetry, audit streams
- **Front Door** - Global load balancing (shared)

**Service Bus Configuration:**
- Tier: Premium (for geo-DR and private endpoints)
- Messaging Units: 1-16 (elastic scaling)
- Topics:
  - `inference-requests` - Model inference jobs
  - `batch-processing` - Bulk operations
  - `audit-events` - Compliance logging
- Queues:
  - `webhooks-delivery` - Outbound webhooks
  - `email-notifications` - Email queue
- Geo-DR: Paired namespace in secondary region
- Retention: 14 days (queues), 31 days (topics)

## Stamp Lifecycle

```
┌──────────┐    Deploy     ┌──────────┐
│          │ ─────────────▶│          │
│ Creating │               │ Active   │
│          │ ◀─────────────│          │
└──────────┘    Active     └────┬─────┘
                                │
                       Quiesce  │  Resume
                                ▼
                          ┌──────────┐
                          │          │
                          │ Draining │
                          │          │
                          └────┬─────┘
                               │
                        Drain  │  Complete
                               ▼
                          ┌──────────┐
                          │          │
                          │ Ready    │
                          │ (Standby)│
                          └────┬─────┘
                               │
                        Retire │  Delete
                               ▼
                          ┌──────────┐
                          │          │
                          │ Deleted  │
                          │          │
                          └──────────┘
```

### Lifecycle States

1. **Creating** - Infrastructure provisioning in progress
2. **Active** - Accepting traffic and processing requests
3. **Draining** - No new requests, draining existing connections
4. **Ready (Standby)** - Warm, ready to become Active
5. **Deleted** - Infrastructure destroyed

### State Transitions

| From | To | Trigger | Action |
|------|-----|---------|--------|
| Creating | Active | All health checks pass | Add to Front Door backend |
| Active | Draining | Scale-in signal | Stop accepting new connections |
| Draining | Ready | All connections closed | Keep warm, ready for activation |
| Ready | Active | Scale-out signal | Add to Front Door backend |
| Ready | Deleted | Retirement signal | Destroy infrastructure |

## Inter-Stamp Communication

Stamps communicate through:

1. **Shared Cosmos DB** - Multi-region writes enable stamp-to-stamp data sharing
2. **Service Bus Topics** - Cross-stamp event propagation
3. **Redis Routing Table** - Consistent hashing for request routing
4. **Shared Key Vault** - Centralized secret management

### Communication Patterns

**Pattern 1: Tenant Affinity**
```
Request ──▶ Front Door ──▶ Consistent Hash (tenantId) ──▶ Target Stamp
```

**Pattern 2: Fan-Out Broadcast**
```
Event ──▶ Service Bus Topic ──▶ All Active Stamps
```

**Pattern 3: Async Processing**
```
Request ──▶ Local Stamp ──▶ Service Bus Queue ──▶ Any Available Stamp
```

## Deployment Orchestration

### Stamp Deployment Sequence

```
Phase 1: Network
  ├── Create Resource Group
  ├── Deploy VNet (if not exists)
  └── Configure NSGs and Route Tables

Phase 2: Security
  ├── Deploy Key Vault references
  ├── Create Managed Identities
  └── Configure RBAC

Phase 3: Data
  ├── Deploy Cosmos DB containers (if not exists)
  ├── Deploy Redis Enterprise
  ├── Deploy PostgreSQL
  └── Configure backups

Phase 4: Compute
  ├── Deploy AKS Cluster
  ├── Configure Node Pools
  ├── Install Ingress Controller
  └── Deploy Application Components

Phase 5: Integration
  ├── Configure Service Bus
  ├── Setup Event Hubs
  └── Add to Front Door Backend

Phase 6: Validation
  ├── Run Health Checks
  ├── Smoke Tests
  └── Mark as Active
```

### Parallel Deployment

Multiple stamps can be deployed in parallel, but within a stamp:
- Data tier first (dependencies for compute)
- Compute tier next (dependencies for integration)
- Integration tier last

## Scaling Triggers

### Scale-Out (Add Stamp)

| Metric | Threshold | Duration |
|--------|-----------|----------|
| CPU Utilization | > 70% | 5 min |
| Memory Utilization | > 80% | 5 min |
| Request Latency (p99) | > 500ms | 3 min |
| Queue Depth | > 1000 | 2 min |
| Active Inference Jobs | > 80% capacity | 5 min |

**Action:** Deploy new stamp, wait for health checks, add to rotation

### Scale-In (Remove Stamp)

| Metric | Threshold | Duration |
|--------|-----------|----------|
| CPU Utilization | < 30% | 15 min |
| Active Connections | < 10 | 10 min |
| Queue Depth | 0 | 10 min |

**Action:** Mark as Draining, wait for empty, remove from rotation, delete

## Health Checks

### Per-Stamp Health

**Compute Tier:**
- Kubernetes API reachable
- All nodes Ready
- Critical pods running
- Ingress controller responsive

**Data Tier:**
- Cosmos DB latency < 10ms
- Redis response < 5ms
- PostgreSQL connection successful

**Integration Tier:**
- Service Bus namespace accessible
- Event Hub partitions balanced

### Global Health

- At least 2 stamps Active in different regions
- Cross-region latency < 100ms
- Data replication lag < 5 seconds

## Monitoring and Observability

### Metrics per Stamp

- **Compute:** Pod count, node utilization, request rate, error rate
- **Data:** RU consumption, cache hit rate, query latency
- **Integration:** Message throughput, DLQ depth, connection count

### Alerting

| Alert | Severity | Condition |
|-------|----------|-----------|
| Stamp Unhealthy | Critical | Any health check fails for 2 min |
| High Latency | Warning | p99 latency > 1s for 5 min |
| Data Replication Lag | Warning | Lag > 30s for 5 min |
| Scale-Out Failed | Critical | New stamp fails health check |

## Security Considerations

### Per-Stamp Isolation

- Each stamp has dedicated VNet subnet
- Network policies prevent cross-stamp traffic
- Cosmos DB uses tenant-level RBAC

### Shared Resources

- Key Vault: Separate secrets per stamp
- ACR: Shared across all stamps
- Front Door: Routes to all active stamps

## Cost Optimization

### Auto-Scaling Strategy

1. **Peak Hours:** 3+ stamps per region
2. **Normal Hours:** 2 stamps per region
3. **Off-Hours:** 1 stamp per region (degraded mode)
4. **Standby Stamps:** Minimum node pools, scale up on demand

### Reserved Capacity

- Cosmos DB: Reserved RU/s for predictable workloads
- Redis Enterprise: Reserved nodes for cache tier
- AKS: Spot instances for non-critical workloads

## Regional Strategy

### Primary Regions

| Region | Purpose | Stamps |
|--------|---------|--------|
| West Europe | Primary EU | 2-3 |
| East US | Primary US | 2-3 |
| Southeast Asia | Primary APAC | 1-2 |

### Disaster Recovery

- Each region has at least 2 stamps
- Cross-region replication for Cosmos DB
- Geo-DR for Service Bus
- Automatic failover via Front Door

## References

- [Hansjoerg Scherer Patterns](https://docs.microsoft.com/azure/architecture/framework/mission-critical/mission-critical-deployment-and-testing)
- [Azure Mission-Critical](https://docs.microsoft.com/azure/architecture/reference-architectures/containers/aks-mission-critical/mission-critical-intro)
- [Ephemeral Infrastructure](https://martinfowler.com/bliki/BlueGreenDeployment.html)
