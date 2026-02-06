# Permutation Matrix Tests

This directory contains exhaustive permutation tests that verify all possible combinations of inputs for critical system functions.

## Test Coverage Summary

### 1. RegionPermutationTests.cs
**Total Permutations: 72 (3 × 3 × 2 × 4)**

Tests all combinations of:
- User Region: [eu-west-1, us-east-1, sa-east-1]
- Processed Region: [eu-west-1, us-east-1, sa-east-1]
- Cross-border: [true, false]
- Legal Basis: [SCC, consent, adequacy, null]

**Key Scenarios:**
- Same-region routing (9 cases)
- Cross-border transfers with valid legal basis (27 cases)
- Cross-border transfers without legal basis (9 cases - should fail)
- Invalid region combinations (edge cases)

### 2. QuotaPermutationTests.cs
**Total Permutations: 480 (2 × 5 × 2 × 4 × 6)**

Tests all combinations of:
- Metric Type: [requests, tokens]
- Time Granularity: [minute, hour, day, week, month]
- Window Type: [fixed, sliding]
- Action: [allow, throttle, block, credit_charge]
- Current Usage %: [0, 50, 90, 99, 100, 101]

**Key Scenarios:**
- Fixed window quota enforcement (120 cases)
- Sliding window quota enforcement (120 cases)
- Threshold boundary testing (60 cases)
- Window calculation for all granularities (20 cases)
- Edge cases (zero/negative/overflow) (40 cases)

### 3. TierPermutationTests.cs
**Total Permutations: 24 (3 × 4 × 2)**

Tests all combinations of:
- Tier: [free, pro, enterprise]
- Feature: [multi_geo, sso, audit_logs, custom_backup]
- Should Have Access: [true, false]

**Feature Access Matrix:**
```
           | multi_geo | sso | audit_logs | custom_backup |
-----------|-----------|-----|------------|---------------|
free       |    ✗      |  ✗  |     ✗      |      ✗        |
pro        |    ✓      |  ✗  |     ✓      |      ✗        |
enterprise |    ✓      |  ✓  |     ✓      |      ✓        |
```

**Key Scenarios:**
- Feature availability per tier (24 cases)
- Tier upgrade unlocking features (15 cases)
- Tier hierarchy validation (10 cases)
- Feature limits per tier (18 cases)

### 4. CurrencyPermutationTests.cs
**Total Permutations: 112 (4 × 7 × 4)**

Tests all combinations of:
- Currency: [USD, EUR, BRL, GBP]
- Base Amount: [0, 0.01, 1, 10, 100, 1000, 10000]
- Exchange Rate variations

**Exchange Rates (USD base):**
- USD: 1.00
- EUR: 0.92
- BRL: 4.95
- GBP: 0.79

**Key Scenarios:**
- Direct USD conversions (28 cases)
- All currency pairs (16 cases)
- Rounding behavior (6 cases)
- Edge cases (zero, sub-cent, overflow) (12 cases)
- Round-trip conversions (4 cases)
- Rate fluctuation handling (9 cases)

### 5. CompliancePermutationTests.cs
**Total Permutations: 72 (3 × 3 × 4 × 2)**

Tests all combinations of:
- Regulation: [GDPR, LGPD, CCPA]
- Data Category: [personal, sensitive, public]
- Processing Purpose: [contract, consent, legitimate_interest, legal_obligation]
- Legal Basis Valid: [true, false]

**Compliance Matrix:**
```
GDPR:
- Personal: All purposes allowed
- Sensitive: Only consent or legal_obligation
- Public: All purposes allowed

LGPD:
- Personal: All purposes allowed (+ credit_protection)
- Sensitive: Only consent or legal_obligation
- Public: All purposes allowed

CCPA:
- Personal: All purposes allowed (opt-out model)
- Sensitive: Only consent or legal_obligation (opt-in)
- Public: All purposes allowed
```

**Key Scenarios:**
- Valid processing combinations (36 cases)
- Invalid sensitive data processing (18 cases)
- Data retention requirements (3 cases)
- Breach notification requirements (9 cases)
- Data subject rights (12 cases)
- Cross-border transfer rules (7 cases)

## Total Test Coverage

- **Total Permutation Test Cases: 760** (72 + 480 + 24 + 112 + 72)
- **Additional Edge Case Tests: ~300**
- **Grand Total: ~1,060 test cases**

## Test Execution

Run all permutation tests:
```bash
dotnet test --filter "FullyQualifiedName~Permutations"
```

Run specific test file:
```bash
dotnet test --filter "FullyQualifiedName~RegionPermutationTests"
dotnet test --filter "FullyQualifiedName~QuotaPermutationTests"
dotnet test --filter "FullyQualifiedName~TierPermutationTests"
dotnet test --filter "FullyQualifiedName~CurrencyPermutationTests"
dotnet test --filter "FullyQualifiedName~CompliancePermutationTests"
```

## Test Design Principles

1. **Exhaustive Coverage**: Every possible combination of inputs is tested
2. **Deterministic**: Tests produce consistent results
3. **Independent**: Each test is self-contained
4. **Edge Cases**: Includes null, empty, boundary values
5. **Documentation**: Each test documents expected behavior
6. **Performance**: Uses TheoryData for efficient data-driven testing

## Maintenance Notes

When adding new values to any dimension:
1. Update the static arrays (e.g., `Regions`, `Currencies`)
2. Update the helper methods to handle new values
3. Update the permutation count in comments
4. Add explicit test cases for critical new combinations
5. Update this README with new totals

Example: Adding a new region "ap-south-1"
- Update `Regions` array: [eu-west-1, us-east-1, sa-east-1, ap-south-1]
- New permutation count: 4 × 4 × 2 × 4 = 128 (was 72)
- Add explicit tests for ap-south-1 cross-border scenarios
