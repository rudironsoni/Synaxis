# Region Routing Permutations Test Suite

## Overview
Comprehensive test suite covering all permutations of cross-border data transfer scenarios for the Synaxis SaaS platform.

## Test Coverage: 72 Tests

### Core Permutations (27 tests)
Tests covering basic region routing scenarios:

**Same-Region (No Transfer) - 3 tests:**
- Test 01: EU User → EU Region
- Test 10: US User → US Region  
- Test 19: Brazil User → Brazil Region

**EU User Cross-Border - 8 tests:**
- Tests 02-05: EU → US (SCC, Consent, Adequacy, Contract)
- Tests 06-09: EU → Brazil (SCC, Consent, Adequacy, Contract)

**US User Cross-Border - 8 tests:**
- Tests 11-14: US → EU (SCC, Consent, Adequacy, Contract)
- Tests 15-18: US → Brazil (SCC, Consent, Adequacy, Contract)

**Brazil User Cross-Border - 8 tests:**
- Tests 20-23: Brazil → US (SCC, Consent, Adequacy, Contract)
- Tests 24-27: Brazil → EU (SCC, Consent, Adequacy, Contract)

### Validation & Error Handling (10 tests)
Tests covering error scenarios and validation:

- Test 28: Invalid legal basis for same-region
- Test 29: Missing legal basis for cross-border
- Test 30: Cross-border flag mismatch (different regions, flag=false)
- Test 31: Cross-border flag mismatch (same regions, flag=true)
- Test 32: Invalid legal basis value
- Test 33: Invalid user region
- Test 34: Invalid processed region
- Test 35: Missing user region header
- Test 36: Missing processed region header
- Test 37: Missing cross-border header

### Advanced Compliance Scenarios (35 tests)
Tests covering specific compliance features and edge cases:

**Provider & Technical Compliance:**
- Test 38: Model provider restrictions
- Test 39: Consent ID tracking
- Test 40: Dual GDPR/LGPD compliance
- Test 41: Data Privacy Framework certification
- Test 42: ANPD approval (Brazil)
- Test 43: Contract necessity validation
- Test 44: Streaming response compliance
- Test 45: Large payload tracking
- Test 53: Encryption verification
- Test 54: Pseudonymization

**Sensitive Data Categories:**
- Test 46: Biometric data transfer
- Test 47: Health data (GDPR special category)

**Operational Controls:**
- Test 48: Rate limiting with compliance
- Test 49: Audit logging
- Test 50: CCPA disclosure
- Test 51: Data retention policy
- Test 52: Minor user protection
- Test 55: DPO notification
- Test 56: Data breach protocol
- Test 57: Sub-processor agreements

**Data Subject Rights:**
- Test 58: Consent withdrawal rights
- Test 60: Subject Access Request (SAR)
- Test 61: Right to erasure (RTBF)
- Test 62: Data portability
- Test 63: Rectification request
- Test 64: Processing restrictions

**Automated Processing:**
- Test 65: Automated decision-making
- Test 66: Profiling transparency

**Purpose-Based Processing:**
- Test 67: Marketing purpose
- Test 68: Analytics purpose
- Test 69: Research purpose
- Test 70: Emergency/vital interest
- Test 71: Legal obligation
- Test 72: Public interest tasks

## Legal Bases Covered

### Standard Contractual Clauses (SCC)
Primary mechanism for GDPR-compliant international transfers. Tests verify:
- Transfer acknowledgment
- Compliance framework application
- Audit trail creation

### Consent
Explicit user consent for cross-border transfers. Tests verify:
- Consent ID tracking
- Withdrawal mechanisms
- GDPR Article 49(1)(a) compliance

### Adequacy Decision
Transfers under adequacy frameworks. Tests verify:
- EU-US Data Privacy Framework
- EU-Brazil partial adequacy
- Framework certification status

### Contract Necessity
Transfers required for contract performance. Tests verify:
- Contract ID tracking
- Necessity documentation
- GDPR Article 49(1)(b) compliance

## Regions Covered

- **eu-west-1**: European Union (GDPR jurisdiction)
- **us-east-1**: United States (CCPA, state laws)
- **sa-east-1**: Brazil (LGPD jurisdiction)

## Test File Naming Convention

Format: `##-[User Region] - [Processed Region] - [Transfer Status] - [Legal Basis/Feature].bru`

Examples:
- `01-EU User - EU Region - No Transfer - No Basis.bru`
- `02-EU User - US Region - Cross Border - SCC.bru`
- `38-EU User - US Region - SCC - With Model Restriction.bru`

## Headers Used

### Required Headers
- `X-User-Region`: User's originating region
- `X-Processed-Region`: Region where data is processed
- `X-Cross-Border`: Boolean flag for international transfer
- `X-Organization-Id`: Organization identifier

### Conditional Headers
- `X-Legal-Basis`: Required when X-Cross-Border=true (SCC/consent/adequacy/contract)
- `X-Consent-ID`: Required for consent-based transfers
- `X-Contract-ID`: Required for contract-based transfers

### Optional Feature Headers
- `X-Data-Category`: Sensitive data categories (biometric, health, etc.)
- `X-Audit-Required`: Enable audit logging
- `X-Retention-Days`: Data retention policy
- `X-User-Age-Category`: Age verification (minor/adult)
- `X-Processing-Purpose`: Purpose limitation (marketing, analytics, research)
- `X-Encryption-Required`: Enforce encryption standards
- `X-Pseudonymization`: Enable pseudonymization
- `X-DPO-Notification`: Trigger DPO notification

## Response Headers Verified

### Compliance Headers
- `x-cross-border-transfer`: Confirms transfer status
- `x-legal-basis`: Applied legal mechanism
- `x-processed-region`: Actual processing region
- `x-compliance-frameworks`: Applicable regulations (GDPR, LGPD, CCPA)

### Audit & Tracking
- `x-audit-id`: Audit trail identifier
- `x-audit-timestamp`: Processing timestamp
- `x-transfer-timestamp`: Transfer timestamp
- `x-consent-id`: Consent record reference
- `x-contract-id`: Contract reference

### Technical Safeguards
- `x-encryption-at-rest`: Encryption standard (AES-256)
- `x-encryption-in-transit`: Transport security (TLS-1.3)
- `x-pseudonymization-applied`: Privacy enhancement status

### Data Subject Rights
- `x-sar-status`: Subject access request status
- `x-rtbf-status`: Right to be forgotten status
- `x-consent-withdrawal-url`: Consent management URL
- `x-portability-format`: Data export format

## Running the Tests

### Prerequisites
```bash
# Install Bruno CLI
npm install -g @usebruno/cli

# Set environment variables
export baseUrl="https://api.synaxis.dev"
export apiKey="your-api-key"
export orgId="your-org-id"
```

### Run All Tests
```bash
bru run collections/Synaxis.SaaS/11-RegionRoutingPermutations/
```

### Run Specific Test Categories

**Core routing tests (1-27):**
```bash
bru run collections/Synaxis.SaaS/11-RegionRoutingPermutations/ --filter "0[1-2][0-9]|10|19"
```

**Validation tests (28-37):**
```bash
bru run collections/Synaxis.SaaS/11-RegionRoutingPermutations/ --filter "2[8-9]|3[0-7]"
```

**Advanced compliance tests (38-72):**
```bash
bru run collections/Synaxis.SaaS/11-RegionRoutingPermutations/ --filter "[4-7][0-9]"
```

### Run by Region

**EU origin tests:**
```bash
bru run collections/Synaxis.SaaS/11-RegionRoutingPermutations/ --filter "EU User"
```

**Cross-border only:**
```bash
bru run collections/Synaxis.SaaS/11-RegionRoutingPermutations/ --filter "Cross Border"
```

**Same-region only:**
```bash
bru run collections/Synaxis.SaaS/11-RegionRoutingPermutations/ --filter "No Transfer"
```

### Run by Legal Basis

```bash
# SCC-based transfers
bru run collections/Synaxis.SaaS/11-RegionRoutingPermutations/ --filter "SCC"

# Consent-based transfers
bru run collections/Synaxis.SaaS/11-RegionRoutingPermutations/ --filter "Consent"

# Adequacy-based transfers
bru run collections/Synaxis.SaaS/11-RegionRoutingPermutations/ --filter "Adequacy"

# Contract-based transfers
bru run collections/Synaxis.SaaS/11-RegionRoutingPermutations/ --filter "Contract"
```

## Compliance Frameworks

### GDPR (EU)
- Chapter V: International transfers (Articles 44-50)
- Article 6: Lawful basis for processing
- Article 9: Special category data
- Article 15-22: Data subject rights
- Article 28: Processor requirements
- Article 33-34: Breach notification
- Article 35: Data Protection Impact Assessment

### LGPD (Brazil)
- Article 33: International transfers
- Article 7: Legal bases for processing
- Article 11: Sensitive personal data
- Article 18: Data subject rights
- Article 48: Data Protection Officer
- Article 52: Controller/processor duties

### CCPA (California)
- Right to know
- Right to delete
- Right to opt-out
- Non-discrimination
- Service provider requirements

### Data Privacy Framework (US-EU)
- EU-US DPF certification
- Privacy Shield successor
- Adequacy decision mechanism

## Test Maintenance

### Adding New Regions
1. Add region to supported list
2. Create same-region test (no transfer)
3. Create cross-border tests to existing regions (4 legal bases each)
4. Create validation tests for new region codes

### Adding New Legal Bases
1. Document legal basis in compliance guide
2. Add tests for each region pair
3. Verify response headers
4. Update documentation

### Adding New Features
1. Create feature-specific tests
2. Combine with existing permutations where relevant
3. Document new headers and assertions
4. Update this README

## Related Documentation

- `/collections/Synaxis.SaaS/08-Compliance/` - Core compliance tests
- `/collections/Synaxis.SaaS/README.md` - Collection overview
- `/docs/compliance/data-transfers.md` - Transfer mechanism details
- `/docs/api/headers.md` - Header reference

## Compliance Verification Checklist

- [ ] All 72 tests pass
- [ ] Same-region tests show no transfer flags
- [ ] Cross-border tests require legal basis
- [ ] Invalid scenarios return 400 errors
- [ ] Audit trails created for transfers
- [ ] Encryption verified for sensitive data
- [ ] Data subject rights honored
- [ ] Consent mechanisms validated
- [ ] Retention policies enforced
- [ ] DPO notifications triggered

## Last Updated
February 5, 2026
