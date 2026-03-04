# DR-Enabled Architecture Diagrams

> **Document**: Synaxis-iosz Disaster Recovery Architecture  
> **Version**: 1.0  
> **Last Updated**: 2026-03-04

---

## Table of Contents

1. [Overview](#overview)
2. [Multi-Region Architecture](#multi-region-architecture)
3. [Data Replication Flow](#data-replication-flow)
4. [Failover Sequence Diagrams](#failover-sequence-diagrams)
5. [Component High-Availability](#component-high-availability)
6. [Backup Architecture](#backup-architecture)
7. [Monitoring and Alerting](#monitoring-and-alerting)

---

## Overview

This document provides visual representations of the Synaxis-iosz architecture with Disaster Recovery (DR) capabilities highlighted. All diagrams are annotated with:

- **Primary components** in solid lines
- **Secondary/DR components** in dashed lines
- **Replication flows** in dotted lines
- **Failover paths** in red lines

---

## Multi-Region Architecture

```mermaid
flowchart TB
    subgraph Internet["Internet"]
        Users([Users/Clients])
        DNS[Azure Traffic Manager<br/>Health Probes: 10s interval]
    end

    subgraph Primary["Primary Region: East US"]
        subgraph K8sPrimary["AKS Cluster"]
            GW1[Gateway Pods<br/>Replicas: 3]
            API1[API Pods<br/>Replicas: 3]
            EVT1[Event Processor<br/>Replicas: 2]
            LBP[Load Balancer<br/>Azure LB Standard]
        end
        
        subgraph DBPrimary["Data Layer"]
            PG1[(PostgreSQL<br/>Primary<br/>Backup: 1h)]
            RD1[(Redis<br/>Master<br/>AOF: enabled)]
            CS1[(Cosmos DB<br/>Write Region<br/>Multi-master)]
        end
        
        subgraph StoragePrimary["Storage"]
            BLOB1[Blob Storage<br/>GRS Replication]
            EVTSTORE1[Event Store<br/>PITR: 35 days]
        end
    end

    subgraph Secondary["Secondary Region: West US"]
        subgraph K8sSecondary["AKS Cluster (Standby)"]
            GW2[Gateway Pods<br/>Replicas: 1]
            API2[API Pods<br/>Replicas: 1]
            EVT2[Event Processor<br/>Replicas: 0]
            LBS[Load Balancer<br/>Azure LB Standard]
        end
        
        subgraph DBSecondary["Data Layer (Replica)"]
            PG2[(PostgreSQL<br/>Replica<br/>Streaming)]
            RD2[(Redis<br/>Replica<br/>Sentinel)]
            CS2[(Cosmos DB<br/>Read Region<br/>Multi-master)]
        end
        
        subgraph StorageSecondary["Storage (Replica)"]
            BLOB2[Blob Storage<br/>GRS Secondary]
            EVTSTORE2[Event Store<br/>Async Replication]
        end
    end

    Users --> DNS
    DNS -->|Health OK| LBP
    DNS -.->|Failover| LBS
    
    LBP --> GW1
    GW1 --> API1
    API1 --> PG1
    API1 --> RD1
    API1 --> CS1
    
    PG1 -.->|Streaming Replication| PG2
    RD1 -.->|Sentinel Sync| RD2
    CS1 -.->|Multi-region| CS2
    EVT1 -.->|Async| EVTSTORE2
    BLOB1 -.->|GRS| BLOB2
    
    LBS -.->|On Failover| GW2
    GW2 -.->|Scaled Up| API2
    API2 -.->|Reads| PG2
    API2 -.->|Reads| RD2
    API2 -.->|Writes| CS2
```

### Region Specifications

| Component | Primary (East US) | Secondary (West US) | RTO | RPO |
|-----------|-------------------|---------------------|-----|-----|
| AKS | 3 replicas | 1 replica (scalable) | 10 min | 0 |
| PostgreSQL | Primary | Streaming Replica | 8 min | 0 |
| Redis | Master | Sentinel Replica | 45s | 0 |
| Cosmos DB | Write Region | Read Region | 12 min | 0 |
| Blob Storage | Primary | GRS Secondary | 5 min | 0 |
| Event Store | Primary | Async Replica | 30 min | 8s |

---

## Data Replication Flow

```mermaid
sequenceDiagram
    participant App as Application
    participant PG_P as PostgreSQL (Primary)
    participant PG_R as PostgreSQL (Replica)
    participant RD_P as Redis (Master)
    participant RD_R as Redis (Replica)
    participant CS as Cosmos DB
    participant EVT as Event Store

    Note over App,EVT: Synchronous Replication

    App->>PG_P: Write Transaction
    PG_P->>PG_P: Commit WAL
    PG_P->>PG_R: Stream WAL (sync)
    PG_R-->>PG_P: Ack
    PG_P-->>App: Commit OK

    App->>RD_P: SET key value
    RD_P->>RD_R: Replicate (sync)
    RD_R-->>RD_P: Ack
    RD_P-->>App: OK

    App->>CS: Write Document
    CS->>CS: Multi-region commit
    CS-->>App: OK

    Note over App,EVT: Asynchronous Replication

    App->>EVT: Append Event
    EVT-->>App: OK (buffered)
    EVT->>EVT: Async replication
    EVT->>EVT: Secondary region
```

### Replication Characteristics

```
┌─────────────────────────────────────────────────────────────────┐
│                    REPLICATION TOPOLOGY                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│   PostgreSQL Streaming Replication                               │
│   ┌──────────┐         ┌──────────┐                             │
│   │ Primary  │═════════│ Replica  │                             │
│   │ East US  │  sync   │ West US  │                             │
│   └──────────┘         └──────────┘                             │
│         │                    │                                  │
│         │                    │                                  │
│         │         ┌──────────┴──────────┐                     │
│         │         │   WAL Archive       │                     │
│         │         │   (Point-in-Time)   │                     │
│         │         └─────────────────────┘                     │
│                                                                  │
│   Redis Sentinel                                                │
│   ┌─────────┐  ┌─────────┐  ┌─────────┐                      │
│   │ Sentinel│  │  Master │  │ Replica │                      │
│   │  26379  │  │  6379   │  │  6380   │                      │
│   └─────────┘  └────┬────┘  └────┬────┘                      │
│                     │            │                              │
│                     └────────────┘                              │
│                          sync                                   │
│                                                                  │
│   Cosmos DB Multi-Master                                        │
│   ┌──────────┐         ┌──────────┐         ┌──────────┐      │
│   │  East US │◄───────►│  West US │◄───────►│  Central│      │
│   │  Write   │  sync   │  Read    │         │         │      │
│   └──────────┘         └──────────┘         └──────────┘      │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## Failover Sequence Diagrams

### Regional Failover Sequence

```mermaid
sequenceDiagram
    participant TM as Traffic Manager
    participant Health as Health Checks
    participant PG_P as PostgreSQL Primary
    participant PG_R as PostgreSQL Replica
    participant K8s_P as K8s Primary
    participant K8s_S as K8s Secondary
    participant Ops as Operations

    Note over TM,K8s_S: Normal Operation

    Health->>TM: East US Health: OK
    TM->>K8s_P: Route Traffic: East US

    Note over TM,K8s_S: Failure Detected

    Health->>TM: East US Health: FAIL
    TM->>TM: Trigger Failover
    
    TM->>Ops: Alert: Regional Failure
    Ops->>TM: Initiate Failover
    
    TM->>PG_R: Promote to Primary
    PG_R->>PG_R: Become Writable
    PG_R-->>TM: Promotion Complete
    
    TM->>K8s_S: Scale Up
    K8s_S->>K8s_S: Replicas: 1→10
    K8s_S-->>TM: Scaling Complete
    
    TM->>TM: Update DNS
    Note right of TM: TTL: 300s→30s→300s
    
    TM->>Health: Verify West US Health
    Health-->>TM: Health: OK
    
    TM->>K8s_S: Route Traffic: West US
    K8s_S-->>Users: Service Restored
```

### Database Failover Timing

```
Time    Event                                              Duration
─────────────────────────────────────────────────────────────────────────
T+0s    Primary failure detected
T+5s    Health check failure confirmed
T+15s   Automated failover initiated (Patroni)
T+30s   Replica promotion begins
T+45s   Write operations accepted on replica
T+60s   Application connection pool recycled
T+90s   Full service restored
─────────────────────────────────────────────────────────────────────────
Total RTO: 90 seconds (1.5 minutes)
RPO: 0 seconds (no data loss)
```

---

## Component High-Availability

### Kubernetes High Availability

```mermaid
flowchart TB
    subgraph Control["Control Plane"]
        API[API Server<br/>3 replicas]
        ETCD[etcd<br/>3 nodes]
        CM[Controller Manager]
        SCHED[Scheduler]
    end
    
    subgraph Workers["Worker Nodes"]
        subgraph Node1["Node 1"]
            P1A[Pod A<br/>synaxis-gateway-1]
            P1B[Pod B<br/>synaxis-gateway-2]
        end
        
        subgraph Node2["Node 2"]
            P2A[Pod A<br/>synaxis-gateway-3]
            P2B[Pod B<br/>synaxis-gateway-4]
        end
        
        subgraph Node3["Node 3"]
            P3A[Pod A<br/>synaxis-gateway-5]
            P3B[Pod B<br/>synaxis-gateway-6]
        end
    end
    
    subgraph Services["Services"]
        SVC[Service<br/>synaxis-gateway]
        HPA[HPA<br/>min:3 max:20]
        PDB[PDB<br/>minAvailable: 2]
    end
    
    Ingress[Ingress Controller] --> SVC
    SVC --> P1A
    SVC --> P1B
    SVC --> P2A
    SVC --> P2B
    SVC --> P3A
    SVC --> P3B
    
    HPA -.->|Scales| Node1
    HPA -.->|Scales| Node2
    HPA -.->|Scales| Node3
    
    PDB -.->|Protects| SVC
```

### Circuit Breaker Pattern

```mermaid
flowchart LR
    subgraph Client["Client"]
        Req[Request]
    end
    
    subgraph CB["Circuit Breaker"]
        state1["CLOSED<br/>Normal Operation"]
        state2["OPEN<br/>Failure Threshold Met"]
        state3["HALF-OPEN<br/>Testing"]
        
        state1 -->|5xx errors > 5| state2
        state2 -->|Timeout: 30s| state3
        state3 -->|Success| state1
        state3 -->|Failure| state2
    end
    
    subgraph Upstream["Upstream Service"]
        Healthy[Healthy Pod]
        Unhealthy[Unhealthy Pod]
        Healthy2[Healthy Pod 2]
    end
    
    Req --> CB
    CB -->|CLOSED| Healthy
    CB -->|CLOSED| Healthy2
    CB -.->|OPEN: Blocked| Unhealthy
```

---

## Backup Architecture

```mermaid
flowchart TB
    subgraph Sources["Data Sources"]
        PG[(PostgreSQL)]
        CS[(Cosmos DB)]
        Redis[(Redis)]
        Config[K8s ConfigMaps<br/>Secrets]
        Events[Event Store]
    end
    
    subgraph BackupLayer["Backup Layer"]
        PGBACK[pg_dump<br/>Hourly]
        CSBACK[Cosmos Backup<br/>Continuous]
        RDBACK[Redis SAVE<br/>Daily]
        CFGBACK[Velero<br/>Daily]
        EVTBACK[Event Backup<br/>Stream]
    end
    
    subgraph Storage["Backup Storage"]
        Hot[Hot Storage<br/>7 days<br/>RA-GRS]
        Warm[Warm Storage<br/>90 days<br/>GRS]
        Cold[Cold Storage<br/>7 years<br/>Archive]
    end
    
    subgraph DR["DR Validation"]
        Verify[Integrity Checks<br/>Weekly]
        Restore[Test Restores<br/>Monthly]
        PITR[Point-in-Time<br/>On Demand]
    end
    
    PG -->|Streaming| PGBACK
    CS -->|Native| CSBACK
    Redis -->|RDB| RDBACK
    Config -->|Snapshot| CFGBACK
    Events -->|Log shipping| EVTBACK
    
    PGBACK --> Hot
    CSBACK --> Hot
    RDBACK --> Hot
    CFGBACK --> Hot
    EVTBACK --> Hot
    
    Hot --> Warm
    Warm --> Cold
    
    Hot --> Verify
    Warm --> Restore
    Cold --> PITR
```

### Backup Schedule

| Component | Frequency | Retention | Type | Storage |
|-----------|-----------|-----------|------|---------|
| PostgreSQL | Hourly | 7 days (hot), 90 days (warm), 7 years (cold) | Logical | GRS |
| PostgreSQL PITR | Continuous | 35 days | WAL | LRS |
| Cosmos DB | Continuous | 30 days | Native | Geo-redundant |
| Redis | Daily | 7 days | RDB | GRS |
| K8s Config | Daily | 30 days | Velero | GRS |
| Event Store | Real-time | 90 days | Stream | GRS |

---

## Monitoring and Alerting

```mermaid
flowchart TB
    subgraph Sources["Data Sources"]
        Metrics[Prometheus Metrics]
        Logs[Loki Logs]
        Traces[Tempo Traces]
        Health[Health Probes]
    end
    
    subgraph Processing["Processing"]
        Prom[Prometheus]
        Alert[Alert Manager]
        Grafana[Grafana]
    end
    
    subgraph Alerts["Alert Rules"]
        A1[HighReplicationLag<br/>> 5min]
        A2[DatabaseDown<br/>> 1min]
        A3[ServiceUnavailable<br/>> 30s]
        A4[RegionalFailure<br/>> 1min]
        A5[RTOExceeded<br/>> 60min]
        A6[RPOExceeded<br/>> 15min]
    end
    
    subgraph Notifications["Notifications"]
        Pager[PagerDuty<br/>Critical]
        Slack[Slack<br/>Warning]
        Email[Email<br/>Info]
    end
    
    subgraph Dashboards["DR Dashboards"]
        D1[Replication Status]
        D2[RTO/RPO Metrics]
        D3[Backup Health]
        D4[Failover Readiness]
    end
    
    Metrics --> Prom
    Logs --> Prom
    Traces --> Prom
    Health --> Prom
    
    Prom --> Alert
    Alert --> A1
    Alert --> A2
    Alert --> A3
    Alert --> A4
    Alert --> A5
    Alert --> A6
    
    A1 -->|Critical| Pager
    A2 -->|Critical| Pager
    A4 -->|Critical| Pager
    A5 -->|Critical| Pager
    A6 -->|Critical| Pager
    
    A3 -->|Warning| Slack
    
    Prom --> Grafana
    Grafana --> D1
    Grafana --> D2
    Grafana --> D3
    Grafana --> D4
```

### Key Metrics

| Metric | Target | Warning | Critical |
|--------|--------|---------|----------|
| PostgreSQL Replication Lag | < 1s | > 30s | > 300s |
| Redis Replication Lag | < 100ms | > 1s | > 5s |
| Service Availability | 99.99% | < 99.9% | < 99% |
| Backup Age | < 1h | > 2h | > 4h |
| RTO (Test) | < 60m | N/A | > 60m |
| RPO (Live) | < 15m | > 10m | > 15m |

---

## Sign-off

| Role | Name | Date |
|------|------|------|
| Architect | Platform Team | 2026-03-04 |
| DBA | Database Team | 2026-03-04 |
| SRE | SRE Team | 2026-03-04 |
| Operations | Ops Team | 2026-03-04 |
