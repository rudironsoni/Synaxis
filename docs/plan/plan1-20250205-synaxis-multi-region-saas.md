# Synaxis Multi-Region SaaS Transformation Plan

**Version**: 1.0  
**Date**: 2025-02-05  
**Status**: Implementation Phase  
**Owner**: Ronsoni Tecnologia LTDA

---

## Executive Summary

Transform Synaxis from a single-tenant .NET 10 inference gateway into a **multi-tenant, multi-region SaaS platform** serving EU (GDPR), US, and Brazil (LGPD) markets from day one. This plan encompasses architecture, infrastructure, compliance, and business model transformation.

---

## Key Architectural Decisions

### Decision 1: Multi-Tenancy as Foundation
**Decision**: Implement full multi-tenant architecture from inception  
**Rationale**: 
- Retrofitting multi-tenancy later requires data migration nightmares
- Affects every database query, API endpoint, and service
- Enables SaaS business model (tiered subscriptions)
- Required for enterprise sales (data isolation)
- **Cost of change**: 10x higher if done later

### Decision 2: Three-Region Deployment from Day One
**Decision**: Deploy EU (Frankfurt), US (Virginia), Brazil (São Paulo) simultaneously  
**Rationale**:
- **Market access**: EU requires GDPR compliance, Brazil requires LGPD
- **Latency**: Users connect to nearest region (< 50ms)
- **Failover**: Regional outages don't cause global downtime
- **Legal**: Data sovereignty requirements (EU data stays in EU)

### Decision 3: User Data Affinity
**Decision**: Each user's data lives in their assigned region permanently  
**Rationale**:
- **GDPR Compliance**: EU user data never leaves EU (unless SCC + consent)
- **Audit simplicity**: Clear data lineage for regulators
- **Performance**: Queries hit local partition
- **Failover clarity**: Temporary routing vs permanent storage distinct

### Decision 4: Failover with Explicit Consent
**Decision**: Route to healthy region during outages, require user consent for cross-border  
**Rationale**:
- **Availability**: 99.9% SLA even during regional outages
- **Compliance**: GDPR Article 49 allows temporary transfers with safeguards
- **Transparency**: User knows their data is crossing borders
- **Reversibility**: Returns to primary region when healthy

### Decision 5: Sophisticated Subscription + Credit System
**Decision**: Subscription tiers with protected overrides + credit top-ups  
**Rationale**:
- **Customer protection**: Global plan changes don't break existing customers
- **Revenue flexibility**: Subscription for base, credits for overages
- **Enterprise friendly**: Custom limits per tenant
- **Competitive**: Matches OpenAI, LiteLLM pricing models

### Decision 6: Dual Rate Limiting (Requests + Tokens)
**Decision**: Independent controls for request count AND token volume  
**Rationale**:
- **Abuse protection**: Request limits stop DDoS
- **Cost protection**: Token limits stop expensive model overruns
- **Granularity**: Different windows per metric
- **Flexibility**: Fixed vs sliding window per limit

### Decision 7: Modular Compliance Architecture
**Decision**: Pluggable compliance providers per regulation  
**Rationale**:
- **Extensibility**: Easy to add CCPA, PIPEDA, etc. later
- **Testability**: Mock providers for testing
- **Clarity**: Each regulation's rules isolated
- **Audit**: Clear lineage of which rules applied

### Decision 8: Eventual Consistency for Cross-Region Cache
**Decision**: Fire-and-forget cache invalidation with 5-second window  
**Rationale**:
- **Performance**: No blocking waits for remote regions
- **Scalability**: Async event bus handles spikes
- **Acceptable staleness**: 5s stale data is tolerable
- **Simplicity**: Easier than distributed transactions

### Decision 9: Configurable Backup Strategies
**Decision**: Per-organization backup configuration (regional-only or cross-region)  
**Rationale**:
- **Compliance**: Some regulations require specific jurisdictions
- **Cost optimization**: Free tier = regional only, Enterprise = cross-region
- **Flexibility**: Customer choice based on risk tolerance
- **Security**: Encryption key per organization

### Decision 10: Multi-Currency from Inception
**Decision**: Support USD, EUR, BRL, GBP with USD as base  
**Rationale**:
- **Global market**: Customers pay in local currency
- **FX risk**: Conversions at billing time (not transaction time)
- **Accounting**: USD base simplifies revenue recognition
- **Customer experience**: No foreign transaction fees

### Decision 11: Embedded Super Admin with Extreme Controls
**Decision**: Super admin panel within main app with strict access controls  
**Rationale**:
- **Security**: Centralized access control, single codebase audit
- **Convenience**: No context switching for admins
- **Controls**: MFA (TOTP), IP allowlist, approval workflows
- **Audit**: All actions logged with justification

### Decision 12: Brazil HQ with Regional Infrastructure
**Decision**: Ronsoni Tecnologia LTDA (Brazil) as primary, infrastructure in all regions  
**Rationale**:
- **Founder location**: Based in Brazil
- **Cost**: Lower operational costs initially
- **Market access**: Can serve global customers via infrastructure
- **Future**: Can establish EU/US entities later

### Decision 13: Self-Service Signup with Trial
**Decision**: Automated signup with 14-day Pro trial, 30-day data retention post-downgrade  
**Rationale**:
- **Scalability**: No manual onboarding bottleneck
- **Conversion**: Trial lets users experience Pro features
- **Retention**: 30-day data retention (GDPR compliant)
- **Recovery**: Easy upgrade path (data preserved)

---

## Technical Architecture

### Multi-Tenant Hierarchy
```
Organization (Tenant Root)
├── Primary Region (chosen at signup)
├── Subscription Plan (Free/Pro/Enterprise)
├── Credit Balance (USD)
├── Billing Currency (USD/EUR/BRL/GBP)
├── Teams
│   ├── Team Budget & Limits
│   ├── Users
│   │   ├── Data Residency Region
│   │   ├── Virtual Keys (API Keys)
│   │   └── Permissions
│   └── Model Access Policies
└── Compliance Settings
```

### Three-Region Infrastructure
```
EU-West-1 (Frankfurt)        US-East-1 (Virginia)        SA-East-1 (São Paulo)
├─ PostgreSQL (Primary)      ├─ PostgreSQL (Primary)     ├─ PostgreSQL (Primary)
├─ Redis Cluster             ├─ Redis Cluster            ├─ Redis Cluster
├─ Qdrant Vector DB          ├─ Qdrant Vector DB         ├─ Qdrant Vector DB
├─ Synaxis API               ├─ Synaxis API              ├─ Synaxis API
└─ Kafka (Regional)          └─ Kafka (Regional)         └─ Kafka (Regional)
       │                             │                            │
       └─────────────────────────────┼────────────────────────────┘
                                     │
                          VPC Peering (Encrypted)
```

### Database Schema (Key Tables)

#### organizations
```sql
CREATE TABLE organizations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    slug VARCHAR(100) UNIQUE NOT NULL,
    name VARCHAR(255) NOT NULL,
    primary_region VARCHAR(50) NOT NULL,
    billing_currency VARCHAR(3) NOT NULL DEFAULT 'USD',
    tier VARCHAR(50) NOT NULL DEFAULT 'free',
    credit_balance DECIMAL(12,4) DEFAULT 0.00,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);
```

#### users
```sql
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id),
    email VARCHAR(255) UNIQUE NOT NULL,
    data_residency_region VARCHAR(50) NOT NULL,
    created_in_region VARCHAR(50) NOT NULL,
    privacy_consent JSONB DEFAULT '{}',
    created_at TIMESTAMPTZ DEFAULT NOW()
);
```

#### virtual_keys
```sql
CREATE TABLE virtual_keys (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id),
    user_id UUID REFERENCES users(id),
    team_id UUID REFERENCES teams(id),
    key_hash VARCHAR(255) UNIQUE NOT NULL,
    name VARCHAR(255),
    max_budget DECIMAL(12,4),
    current_spend DECIMAL(12,4) DEFAULT 0.00,
    rpm_limit INT,
    tpm_limit INT,
    allowed_models TEXT[],
    expires_at TIMESTAMPTZ,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMPTZ DEFAULT NOW()
) PARTITION BY LIST (user_region);
```

#### requests (Partitioned by Region)
```sql
CREATE TABLE requests (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    request_id UUID UNIQUE NOT NULL,
    organization_id UUID NOT NULL,
    user_id UUID REFERENCES users(id),
    user_region VARCHAR(50) NOT NULL,
    processed_region VARCHAR(50) NOT NULL,
    stored_region VARCHAR(50) NOT NULL,
    cross_border_transfer BOOLEAN DEFAULT false,
    transfer_legal_basis VARCHAR(50),
    model VARCHAR(100),
    input_tokens INT,
    output_tokens INT,
    cost DECIMAL(12,6),
    duration_ms INT,
    created_at TIMESTAMPTZ DEFAULT NOW()
) PARTITION BY LIST (stored_region);
```

### Rate Limiting Configuration
```json
{
  "requests": {
    "per_minute": {
      "limit": 100,
      "window": "sliding",
      "action": "throttle"
    },
    "per_hour": {
      "limit": 10000,
      "window": "fixed",
      "action": "throttle"
    },
    "per_day": {
      "limit": 100000,
      "window": "fixed",
      "action": "block"
    }
  },
  "tokens": {
    "per_minute": {
      "limit": 10000,
      "window": "sliding",
      "action": "throttle"
    },
    "per_month": {
      "limit": 10000000,
      "window": "fixed",
      "action": "credit_charge"
    }
  }
}
```

### Compliance Providers
```csharp
public interface IComplianceProvider
{
    string RegulationCode { get; }
    string Region { get; }
    Task<bool> ValidateTransferAsync(TransferContext context);
    Task LogTransferAsync(TransferContext context);
    Task<DataExport> ExportUserDataAsync(Guid userId);
    Task DeleteUserDataAsync(Guid userId);
}

public class GdprComplianceProvider : IComplianceProvider
{
    public string RegulationCode => "GDPR";
    public string Region => "EU";
    // Implementation...
}

public class LgpdComplianceProvider : IComplianceProvider
{
    public string RegulationCode => "LGPD";
    public string Region => "BR";
    // Implementation...
}
```

---

## Business Model

### Pricing Tiers

| Feature | Free | Pro ($49/mo) | Enterprise (Custom) |
|---------|------|--------------|---------------------|
| Regions | Auto-assigned | User choice | Multi-geo teams |
| Teams | 1 | 5 | Unlimited |
| Users/Team | 3 | 20 | Unlimited |
| Keys/User | 2 | 10 | Unlimited |
| Concurrent | 10 | 100 | 1000 |
| Requests/Day | 1,000 | 100,000 | Unlimited |
| Tokens/Month | 100K | 10M | Unlimited |
| Rate Limit Windows | Fixed only | Fixed + Sliding | Custom |
| Currencies | USD | All 4 | All 4 |
| Backups | Regional 7d | Cross-region 30d | Hourly, custom |
| SSO | ❌ | ❌ | ✅ |
| Audit Logs | ❌ | ✅ | ✅ |
| Support | Community | Email | Dedicated |

### Credit System
- **Base Rate**: $0.0001 per token (configurable)
- **Auto Top-up**: Enterprise only
- **Minimum Balance**: $5.00 (configurable)
- **Overage Action**: Credit charge or block (configurable per limit)

---

## Implementation Phases

### Phase 1: Foundation (4 Agents)
- **Agent 1**: Database schema, shared models, contracts
- **Agent 2**: EU Infrastructure (Terraform, K8s)
- **Agent 3**: US Infrastructure (Terraform, K8s)
- **Agent 4**: Brazil Infrastructure (Terraform, K8s)

**Deliverables**:
- Multi-region PostgreSQL schema with partitioning
- Shared domain models
- Kafka topics
- Redis cluster configuration
- 3 regional VPCs with peering

### Phase 2: Core Services (2 Agents)
- **Agent 5**: Tenant Service, User Service, GeoIP Service
- **Agent 6**: Region Router, Compliance Engine

**Deliverables**:
- ITenantService, IUserService
- IRegionRouter with cross-region routing
- IComplianceProvider with GDPR/LGPD implementations

### Phase 3: Resilience & Features (2 Agents)
- **Agent 7**: Failover Service, Health Monitor, Quota Service
- **Agent 8**: Billing Service, Backup Service, Cache Service, Audit Service

**Deliverables**:
- IFailoverService with consent flow
- IQuotaService with dual control
- IBillingService with multi-currency
- IBackupService with configurable strategies

### Phase 4: Integration & Testing (2 Agents)
- **Agent 9**: QA, integration tests, contract validation
- **Agent 10**: Technical writer, documentation, API docs

**Deliverables**:
- Integration test suite (>80% coverage)
- API documentation
- Architecture decision records
- Operations runbooks

---

## Testing Requirements

### Coverage Targets
- **Unit Tests**: 80% minimum coverage
- **Integration Tests**: All service interactions
- **Contract Tests**: All interface implementations
- **Compliance Tests**: GDPR/LGPD rule validation
- **Failover Tests**: Regional outage simulation

### Test Categories
1. **Unit Tests**: Individual service methods
2. **Integration Tests**: Service-to-service communication
3. **Contract Tests**: Interface compliance
4. **Load Tests**: Rate limiting under stress
5. **Failover Tests**: Regional health detection
6. **Compliance Tests**: Data residency validation

---

## Compliance Checklist

### GDPR (EU)
- [ ] Data residency in EU for EU users
- [ ] Standard Contractual Clauses (SCCs) for transfers
- [ ] 72-hour breach notification
- [ ] Right to erasure (30-day deletion)
- [ ] Data portability (JSON export)
- [ ] Privacy by design
- [ ] DPO appointment

### LGPD (Brazil)
- [ ] ANPD notification
- [ ] 10 legal bases for processing
- [ ] 72-hour breach notification
- [ ] Portuguese language support
- [ ] Brazilian data protection

### General
- [ ] Encryption at rest (AES-256)
- [ ] Encryption in transit (TLS 1.3)
- [ ] Audit logging (immutable)
- [ ] Access controls (RBAC)
- [ ] Backup encryption

---

## Open Questions

1. **Trial Data Retention**: After 30-day post-downgrade retention, hard delete or archive?
2. **DPO Strategy**: External service or internal hire?
3. **SSO Priority**: SAML 2.0 in Phase 1 or Enterprise tier only?
4. **Content Moderation**: Custom implementation or OpenAI Moderation/LlamaGuard integration?
5. **Support Hours**: 24/7 for Enterprise or business hours only?

---

## Success Metrics

- **Availability**: 99.9% uptime per region
- **Latency**: <50ms within region, <200ms cross-region
- **Compliance**: Zero data residency violations
- **Performance**: 10,000 concurrent requests per region
- **Adoption**: 1,000 signups Month 1, 100 paying customers Month 6
- **Test Coverage**: >80% code coverage
- **Documentation**: 100% API coverage

---

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| GDPR violation | Strict data affinity, audit logging, SCCs |
| Regional outage | Automated failover with health checks |
| Data corruption | Regional backups, point-in-time recovery |
| Performance degradation | Caching, connection pooling, read replicas |
| Compliance changes | Modular providers, configurable rules |

---

**Document Owner**: Synaxis Architecture Team  
**Last Updated**: 2025-02-05  
**Status**: Approved for Implementation
