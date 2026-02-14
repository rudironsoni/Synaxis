# Ephemeral Scale Units Architecture

## Overview

This document describes the ephemeral scale unit (stamp) architecture for Synaxis, implementing the **Hansjoerg Scherer Pattern** for multi-region, auto-scaling AI inference infrastructure.

## Architecture Decisions

### Implementation: Azure Bicep (not Terraform)

**Decision:** Use Azure Bicep instead of Terraform  
**Rationale:** 
- Native Azure integration with better ARM template support
- First-class Visual Studio Code support
- Simpler syntax for Azure-specific resources
- Better type safety and IntelliSense

**Trade-offs:**
- Azure-only (acceptable for Synaxis architecture)
- Smaller community than Terraform

## Scale Unit Components

Each stamp is a self-contained deployment unit with:

### Compute Layer
- **AKS Cluster**: Kubernetes 1.29 with 3 node pools
  - System pool: 3x Standard_D4s_v3
  - User pool: 2-10x Standard_D8s_v3
  - Inference pool: 1-20x Standard_D8s_v3
- **ACR**: Premium container registry with geo-replication

### Data Layer
- **Cosmos DB**: Multi-region NoSQL for tenant isolation
- **Redis Enterprise**: Distributed caching and rate limiting
- **Service Bus**: Premium messaging for cross-stamp communication

### Network Layer
- **Virtual Network**: Hub-spoke architecture with private endpoints
- **Azure Firewall**: Traffic inspection and filtering
- **Front Door**: Global load balancing with WAF

### Security Layer
- **Key Vault**: HSM-backed secrets and certificates
- **Managed Identities**: Workload identity for AKS
- **Private Endpoints**: All data services accessed privately

## Multi-Region Deployment

### Active Regions (Production)
- West Europe (WEU) - Primary
- East US (EUS) - Secondary
- Southeast Asia (SEA) - Tertiary

### Stamp Distribution
- 10 isolated stamps total
- 3-4 stamps per region
- Automatic traffic routing via Front Door

## Stamp Lifecycle

### Phases

1. **Provision** (15-20 min)
   - Deploy infrastructure via Bicep
   - Install ArgoCD applications
   - Configure network policies

2. **Register** (2-5 min)
   - Add to Front Door backend pool
   - Update DNS routing
   - Enable health probes

3. **Active** (operational)
   - Receive production traffic
   - Auto-scale based on demand
   - Continuous health monitoring

4. **Drain** (configurable, default 30 min)
   - Stop receiving new requests
   - Complete in-flight requests
   - Migrate stateful sessions

5. **Quarantine** (5-10 min)
   - Remove from load balancer
   - Preserve logs and metrics
   - Await final verification

6. **Decommission** (10-15 min)
   - Delete AKS workloads
   - Preserve persistent volumes
   - Remove from service mesh

7. **Archive** (retained 30 days)
   - Store audit logs
   - Archive metrics
   - Maintain compliance records

8. **Purge** (final)
   - Delete all resources
   - Remove from Front Door
   - Release IP addresses

## Infrastructure as Code

### Bicep Modules

```
infra/bicep/
├── main.bicep                    # Main orchestration
├── modules/
│   ├── vnet.bicep               # Virtual network
│   ├── nsg.bicep                # Network security groups
│   ├── route-table.bicep        # Route tables
│   ├── firewall.bicep           # Azure Firewall
│   ├── firewall-policy.bicep    # Firewall policies
│   ├── keyvault.bicep           # Key Vault
│   ├── keyvault-key.bicep       # Key Vault keys
│   ├── postgresql.bicep         # PostgreSQL Flexible Server
│   ├── redis.bicep              # Redis Cache
│   ├── policy-assignments.bicep # Azure Policy
│   ├── aks.bicep                # AKS cluster
│   ├── acr.bicep                # Container Registry
│   ├── cosmos.bicep             # Cosmos DB
│   ├── redis-enterprise.bicep   # Redis Enterprise
│   ├── servicebus.bicep         # Service Bus
│   └── frontdoor.bicep          # Front Door
```

### Deployment

```bash
# Deploy to West Europe
az deployment sub create \
  --location westeurope \
  --template-file infra/bicep/main.bicep \
  --parameters environment=production location=westeurope

# Deploy to East US
az deployment sub create \
  --location eastus \
  --template-file infra/bicep/main.bicep \
  --parameters environment=production location=eastus
```

## Operational Procedures

### Creating a New Stamp

1. Choose target region with capacity
2. Run Bicep deployment
3. Verify AKS cluster health
4. Register with Front Door
5. Enable in global traffic manager

### Retiring a Stamp

1. Initiate drain mode
2. Monitor request completion
3. Quarantine for verification
4. Decommission workloads
5. Archive audit data
6. Purge after retention period

### Emergency Procedures

**Stamp Failure Detection:**
- Health probes fail 3 consecutive checks
- Front Door automatically routes around failed stamp
- Alert triggered for operations team

**Manual Stamp Isolation:**
```bash
# Remove from Front Door
az afd endpoint update \
  --endpoint-name api \
  --profile-name synaxis-fd \
  --resource-group synaxis-network \
  --enabled-state Disabled
```

## Monitoring

### Metrics Dashboard
- Request latency by stamp
- Error rates by region
- Node pool utilization
- Queue depths
- Database RU consumption

### Alerts
- Stamp health degradation
- Capacity thresholds
- Security policy violations
- Certificate expiration

## Security Considerations

### Network Isolation
- All data services use private endpoints
- No public IPs on AKS nodes
- Firewall rules restrict egress

### Identity
- User-assigned managed identities
- Workload identity for pods
- Key Vault for secrets

### Compliance
- Encryption at rest (HSM-backed)
- Encryption in transit (TLS 1.3)
- Audit logging to immutable storage

## References

- [Scale Unit Design](architecture/scale-unit-design.md)
- [Stamp Lifecycle](architecture/stamp-lifecycle.md)
- [Heyko Oelrichs Security Pattern](security-foundation.md)
- [Hansjoerg Scherer Scale Pattern](scale-unit-design.md)

## Changelog

- 2026-02-14: Initial Bicep implementation (Epic E2)
- Migrated from Terraform to Bicep for Azure-native deployment
- Added Front Door, Cosmos DB, Redis Enterprise, Service Bus
- Implemented 3-node pool AKS architecture
