# Synaxis BDD Test Scenarios - Summary

**Total Scenarios: 96**  
**Coverage Areas: 6 critical business domains**

---

## 1. Multi-Tenancy (15 scenarios)

### Core Isolation
1. Organization A cannot see Organization B's data
2. API keys are scoped to organization
3. Database queries are automatically filtered by tenant context
4. Shared resources are isolated per tenant

### API Key Management
5. Creating API key associates it with user's organization
6. Listing API keys only shows keys from user's organization
7. Revoking API key requires ownership

### Cross-Tenant Attacks
8. SQL injection cannot bypass tenant isolation
9. JWT token tampering is detected

### Resource Quotas and Billing
10. Each organization has independent quota limits
11. Billing is isolated per organization

### Admin Operations
12. Super admin can access all organizations
13. Organization admin can only manage their own organization

### Edge Cases
14. User belongs to multiple organizations
15. Organization deletion cleans up all associated data

---

## 2. Data Residency (16 scenarios)

### Core Data Residency
1. EU user data stays in EU region
2. Brazil user data stays in Brazil
3. US user data can be processed in US

### Automatic Region Assignment
4. User region is auto-assigned based on IP geolocation
5. User can manually override region assignment

### Cross-Border Transfers
6. Cross-border transfer requires explicit consent
7. Standard Contractual Clauses enable cross-border transfer
8. Emergency failover with retroactive notification

### Data Localization
9. Inference request data is stored in correct region
10. Cached data respects regional boundaries
11. Logs are stored in user's region

### Provider Selection
12. EU users only use GDPR-compliant providers
13. China user data must stay in China

### Compliance Validation
14. System validates data residency before processing
15. Audit trail tracks all data transfers

### Edge Cases
16. User travels to different region
17. VPN masking user location (bonus - 17 total)
18. Multi-region organization (bonus - 17 total)

---

## 3. Quota Enforcement (16 scenarios)

### Tier-Based Quotas
1. Free tier daily request limit
2. Pro tier has higher limits
3. Enterprise tier has no hard limits

### Token-Based Quotas
4. Token quota with sliding window
5. Cost-based quota enforcement

### Rate Limiting
6. Per-second rate limiting
7. Burst allowance with token bucket

### Concurrent Request Limits
8. Maximum concurrent requests per organization
9. Per-user concurrent request limits

### Model-Specific Quotas
10. Different limits for premium models
11. Token limits vary by model

### Quota Resets
12. Daily quota resets at midnight UTC
13. Monthly quota resets on billing cycle date

### Grace Periods and Overages
14. Grace period for quota exceeded
15. Hard limit after grace period exhausted

### Quota Management
16. Organization admin can view quota usage
17. Quota can be temporarily increased for special events (bonus - 17 total)

### Edge Cases
18. Quota counter race condition (bonus - 18 total)
19. Quota bypass attempt via API key rotation (bonus - 18 total)
20. Downgrading tier mid-cycle (bonus - 18 total)

---

## 4. GDPR Compliance (17 scenarios)

### Right to Access (Article 15)
1. Data subject requests data export
2. Data portability in machine-readable format
3. Third-party data processors are included in export

### Right to Erasure (Article 17)
4. Data subject requests account deletion
5. Right to erasure with exceptions
6. Cascade deletion of related data

### Right to Rectification (Article 16)
7. Data subject corrects inaccurate data
8. User disputes inaccurate inference data

### Right to Restriction (Article 18)
9. Data subject requests processing restriction

### Right to Object (Article 21)
10. Data subject objects to direct marketing
11. User objects to automated decision-making

### Consent Management (Article 7)
12. Granular consent for different processing purposes
13. Withdrawing consent stops processing
14. Consent must be freely given (no forced bundling)

### Data Breach Notification (Articles 33 & 34)
15. Data breach detected and reported to supervisory authority
16. High-risk breach requires user notification
17. Low-risk breach does not require user notification

### Privacy by Design (Article 25)
18. Data minimization in inference requests (bonus - 18 total)
19. Pseudonymization of stored data (bonus - 19 total)

### DPIA (Article 35)
20. DPIA required for high-risk processing (bonus - 20 total)

---

## 5. Regional Failover (15 scenarios)

### Health Monitoring
1. Region health score calculation
2. Region degradation detection
3. Region becomes unhealthy

### Automatic Failover
4. Automatic failover to healthy region
5. Failover respects data residency requirements
6. All EU regions unhealthy requires consent for US failover
7. Failover to nearest healthy region by latency

### Failover Notifications
8. User notification of temporary failover
9. Operations team receives failover alert

### Return to Primary Region
10. Automatic return when primary region recovers
11. Gradual traffic shift during recovery
12. Rollback if region degrades during recovery

### Circuit Breaker Pattern
13. Circuit breaker opens after repeated failures
14. Circuit breaker half-open state for testing

### Cascading Failure Prevention
15. Prevent cascading failures to backup region
16. Multi-region outage falls back to degraded service (bonus - 16 total)

### Split-Brain Prevention
17. Network partition between regions (bonus - 17 total)

### Historical Failover Tracking
18. Failover history is tracked for SLO reporting (bonus - 18 total)

### Load Balancing During Failover
19. Geographic load balancing across multiple healthy regions (bonus - 19 total)

---

## 6. Multi-Currency Billing (17 scenarios)

### Basic Currency Billing
1. USD billing for US customer
2. EUR billing with exchange rate
3. GBP billing with real-time rate

### Multi-Model Pricing
4. Different models have different pricing

### Tax Handling
5. EU VAT for European customers
6. Reverse charge for B2B in different EU countries
7. No VAT for non-EU customers

### Payment Methods
8. Credit card payment in customer currency
9. ACH payment for US customers
10. SEPA payment for EU customers

### Subscription Tiers
11. Free tier has no invoice
12. Pro tier monthly subscription
13. Enterprise tier with volume discounts

### Credits and Refunds
14. Promotional credits applied to invoice
15. Partial refund for service disruption

### Exchange Rate Management
16. Exchange rates cached to prevent fluctuations during billing
17. Exchange rate update failure fallback

### Historical Billing
18. View historical invoices in original currency (bonus - 18 total)
19. Currency change affects only future invoices (bonus - 19 total)

### Budget Alerts
20. Budget alert in customer's currency (bonus - 20 total)

### Compliance
21. Invoice meets local accounting standards (bonus - 21 total)

---

## Coverage Summary

| Feature Area        | Scenarios | Key Concerns                                   |
|---------------------|-----------|------------------------------------------------|
| Multi-Tenancy       | 15        | Data isolation, security, admin access         |
| Data Residency      | 16        | Geographic compliance, cross-border transfers  |
| Quota Enforcement   | 16        | Rate limiting, tier limits, burst handling     |
| GDPR Compliance     | 17        | Privacy rights, data protection, breach mgmt   |
| Regional Failover   | 15        | High availability, disaster recovery, health   |
| Multi-Currency      | 17        | Global payments, tax handling, exchange rates  |
| **TOTAL**           | **96**    |                                                |

---

## Business Rule Categories

### Security (25 scenarios)
- Tenant isolation
- Authentication and authorization
- Audit logging
- Data breach notification

### Compliance (33 scenarios)
- GDPR (EU)
- LGPD (Brazil)
- CCPA (US)
- Data residency
- Cross-border transfers

### Reliability (15 scenarios)
- Regional failover
- Health monitoring
- Circuit breakers
- Load balancing

### Financial (23 scenarios)
- Multi-currency billing
- Quota enforcement
- Usage tracking
- Tax handling

---

## Test Execution Strategy

### Phase 1: Critical Path (30 scenarios)
- Multi-tenant isolation (5 scenarios)
- Data residency basics (5 scenarios)
- Quota enforcement (5 scenarios)
- GDPR rights (5 scenarios)
- Basic failover (5 scenarios)
- Currency billing (5 scenarios)

### Phase 2: Edge Cases (30 scenarios)
- Attack scenarios
- Error handling
- Race conditions
- Boundary testing

### Phase 3: Full Coverage (36 scenarios)
- Advanced features
- Integration scenarios
- Performance edge cases
- Compliance corner cases

---

## Implementation Checklist

- [ ] Install SpecFlow NuGet packages
- [ ] Create step definition classes for each feature
- [ ] Link scenarios to existing unit/integration tests
- [ ] Configure test runner (xUnit/NUnit)
- [ ] Set up CI/CD pipeline for BDD tests
- [ ] Generate living documentation
- [ ] Schedule regular scenario reviews with product team

---

## Related Documentation

- `/tests/Synaxis.Tests/Behaviors/README.md` - Implementation guide
- `/tests/Synaxis.Tests/Behaviors/*.feature` - Feature specifications
- `/tests/Synaxis.Tests/Behaviors/StepDefinitions/*.cs.template` - Step definition templates
- `/tests/Synaxis.Tests/Unit/` - Unit test implementations
- `/tests/Synaxis.Tests/Integration/` - Integration test implementations
- `/tests/Synaxis.Tests/Compliance/` - Compliance test implementations

---

**Last Updated**: 2026-02-05  
**Maintained By**: QA Team & Product Team
