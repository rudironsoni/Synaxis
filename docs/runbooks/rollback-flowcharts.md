# Synaxis Rollback Decision Flowchart

This document provides visual flowcharts for rollback decision-making.

## Main Rollback Decision Tree

```mermaid
flowchart TD
    A[Anomaly Detected] --> B{Severity Assessment}
    
    B -->|P0 Critical| C[Auto-Approve<br/>Immediate Rollback]
    B -->|P1 High| D[On-Call Engineer<br/>Decision < 10min]
    B -->|P2 Standard| E[Manager Approval<br/>Decision < 30min]
    B -->|P3 Low| F[Defer to Next<br/>Maintenance Window]
    
    C --> G[Execute P0<br/>Rollback Procedure]
    D -->|Approve| H[Execute P1<br/>Rollback Procedure]
    D -->|Monitor| I[Continue Monitoring]
    E -->|Approve| J[Execute P2<br/>Rollback Procedure]
    E -->|Defer| F
    
    G --> K{Validation}
    H --> K
    J --> K
    
    K -->|Pass| L[Complete Rollback<br/>Post-Mortem Scheduled]
    K -->|Fail| M[Escalate to<br/>War Room]
    
    I --> N{Continued Issues?}
    N -->|Yes| D
    N -->|No| O[Resolve Alert]
    
    M --> P[Executive Notification<br/>All Hands Response]
    P --> Q[Manual Recovery<br/>Required]
    
    F --> R[Document Issue<br/>Plan Fix]
    
    style C fill:#ff4444,color:#fff
    style G fill:#ff4444,color:#fff
    style M fill:#ff8800,color:#fff
    style L fill:#44ff44,color:#000
```

## Application Rollback Decision Flow

```mermaid
flowchart TD
    A[Deployment Issue Detected] --> B{Can Blue/Green<br/>Rollback?}
    
    B -->|Yes| C[Switch Traffic to<br/>Stable Version]
    B -->|No| D{Helm Deployed?}
    
    C --> E[Scale Down<br/>Problematic Version]
    D -->|Yes| F[Helm Rollback<br/>to Previous]
    D -->|No| G[kubectl Rollout<br/>Undo]
    
    E --> H{Validation<br/>Pass?}
    F --> H
    G --> H
    
    H -->|Yes| I[Complete<br/>Rollback]
    H -->|No| J[Check Pod<br/>Health]
    
    J --> K{Issue<br/>Identified?}
    K -->|Yes| L[Fix & Retry]
    K -->|No| M[Escalate to<br/>Engineering]
    
    L --> H
    M --> N[War Room<br/>Activated]
    
    style C fill:#44aaff,color:#fff
    style I fill:#44ff44,color:#000
    style M fill:#ff8800,color:#fff
```

## Database Rollback Decision Flow

```mermaid
flowchart TD
    A[Database Issue Detected] --> B{Migration Related?}
    
    B -->|Yes| C{Migration<br/>Reversible?}
    B -->|No| D{Data<br/>Corruption?}
    
    C -->|Yes| E[Execute EF Core<br/>Down Migration]
    C -->|No| F[Point-in-Time<br/>Restore Required]
    
    D -->|Yes| G[Execute Data<br/>Correction Scripts]
    D -->|No| H[Check Connection<br/>Pool/Performance]
    
    E --> I{Rollback<br/>Success?}
    F --> J[PITR to<br/>Pre-Deployment]
    G --> K{Correction<br/>Applied?}
    H --> L[Scale Resources<br/>or Restart]
    
    I -->|Yes| M[Verify Migration<br/>State]
    I -->|No| N[Manual DBA<br/>Intervention]
    
    J --> O[Update Connection<br/>Strings]
    K -->|Yes| P[Validate Data<br/>Integrity]
    K -->|No| Q[Restore from<br/>Backup]
    
    M --> R[Complete<br/>Rollback]
    N --> S[Emergency DBA<br/>Response]
    O --> R
    P --> R
    Q --> R
    L --> R
    
    style E fill:#44aaff,color:#fff
    style F fill:#ff8800,color:#fff
    style R fill:#44ff44,color:#000
    style S fill:#ff4444,color:#fff
```

## Infrastructure Rollback Decision Flow

```mermaid
flowchart TD
    A[Infrastructure Issue] --> B{Terraform<br/>Related?}
    
    B -->|Yes| C{State<br/>Corrupted?}
    B -->|No| D{Network<br/>Issue?}
    
    C -->|Yes| E[Restore State<br/>from Backup]
    C -->|No| F[Terraform Plan<br/>& Rollback]
    
    D -->|Yes| G{Security Group<br/>Change?}
    D -->|No| H{DNS/Cert<br/>Issue?}
    
    G -->|Yes| I[Revert SG<br/>Rules]
    G -->|No| J[Check VPC/<br/>Routing]
    
    H -->|Yes| K[Rollback DNS<br/>or Certificate]
    H -->|No| L[Check Load<br/>Balancer]
    
    E --> M[terraform apply<br/>restored state]
    F --> M
    I --> N{Connectivity<br/>Restored?}
    J --> N
    K --> O{Resolution<br/>Propagated?}
    L --> P[Health Check<br/>Fix]
    
    M --> Q{Apply<br/>Success?}
    N -->|Yes| R[Complete<br/>Rollback]
    N -->|No| S[Network Admin<br/>Required]
    
    O -->|Yes| R
    O -->|No| T[Adjust TTL &<br/>Wait]
    P --> R
    
    Q -->|Yes| R
    Q -->|No| U[Manual Resource<br/>Cleanup]
    
    T --> R
    U --> R
    
    style E fill:#ff8800,color:#fff
    style R fill:#44ff44,color:#000
    style S fill:#ff4444,color:#fff
```

## P0 Emergency Response Flow

```mermaid
sequenceDiagram
    participant Mon as Monitoring
    participant PD as PagerDuty
    participant OC as On-Call Engineer
    participant Auto as Automation
    participant Exec as Executive
    
    Mon->>Mon: Detect P0 Trigger
    Mon->>PD: Alert (Severity: P0)
    PD->>OC: Page Engineer
    
    par Auto-Rollback
        PD->>Auto: Trigger Auto-Rollback
        Auto->>Auto: Execute Blue/Green Switch
        Auto->>Auto: Validate Health
        Auto->>PD: Auto-Rollback Status
    and Human Response
        OC->>OC: Acknowledge (5min)
        OC->>Auto: Monitor Rollback
    end
    
    alt Rollback Success
        Auto->>PD: Mark Resolved
        PD->>OC: Confirm Resolution
        OC->>PD: Schedule Post-Mortem
    else Rollback Fail
        Auto->>PD: Escalate
        PD->>Exec: Executive Alert
        PD->>OC: War Room Convened
        OC->>Exec: Bridge Line
    end
    
    Note over Mon,Exec: P0 Target: <5min rollback<br/>Maximum: 10min
```

## Rollback Validation Flow

```mermaid
flowchart LR
    A[Rollback Complete] --> B[Health Check]
    
    B --> C{HTTP 200?}
    C -->|Yes| D[Pod Status]
    C -->|No| E[Check Logs]
    
    D --> F{All Ready?}
    F -->|Yes| G[Error Rate]
    F -->|No| H[Wait/Cleanup]
    
    G --> I{< 0.1%?}
    I -->|Yes| J[Latency Check]
    I -->|No| K[Investigate]
    
    J --> L{P99 < SLA?}
    L -->|Yes| M[Functional Test]
    L -->|No| N[Scale Resources]
    
    M --> O{Core Flows<br/>Working?}
    O -->|Yes| P[Notify Success]
    O -->|No| Q[Escalate]
    
    E --> R{Fixable?}
    R -->|Yes| S[Apply Fix]
    R -->|No| Q
    
    H --> G
    K --> T[Partial Success<br/>Monitoring]
    N --> L
    S --> B
    
    P --> U[Complete]
    Q --> V[War Room]
    T --> U
    
    style P fill:#44ff44,color:#000
    style U fill:#44ff44,color:#000
    style V fill:#ff4444,color:#fff
    style Q fill:#ff8800,color:#fff
```

## Time-Based Escalation Matrix

```mermaid
gantt
    title Rollback Response Timeline
    dateFormat X
    axisFormat %s
    
    section Detection
    Auto-Detection        :done, 0, 1
    Alert Fired           :done, 1, 2
    
    section Response
    P0 Auto-Rollback      :crit, 2, 5
    On-Call Response      :active, 2, 7
    Manager Notification  :7, 10
    
    section Decision
    P0 Complete           :milestone, 5, 5
    P1 Decision           :10, 15
    War Room Convened     :crit, 15, 30
    
    section Resolution
    Target Resolution     :milestone, crit, 30, 30
    Max Tolerable         :milestone, crit, 60, 60
```

## Communication Flow

```mermaid
flowchart TD
    A[Rollback Triggered] --> B{Severity?}
    
    B -->|P0| C[Immediate Executive<br/>Notification]
    B -->|P1| D[Team Slack Alert]
    B -->|P2| E[Standard Channel]
    
    C --> F[Status Page<br/>Incident]
    D --> F
    E --> G[Team Notification]
    
    F --> H[Customer Comms<br/>if Affecting]
    G --> I{Progress<br/>Update}
    
    H --> J[15-min Updates<br/>Required]
    I -->|Every 30min| K[Status Update]
    
    J --> L{Resolved?}
    K --> L
    
    L -->|Yes| M[All-Clear<br/>Notification]
    L -->|No| N[Continue Updates]
    
    M --> O[Post-Mortem<br/>Scheduled]
    N --> I
    
    O --> P[Document<br/>Lessons Learned]
    
    style C fill:#ff4444,color:#fff
    style M fill:#44ff44,color:#000
    style P fill:#44aaff,color:#fff
```
