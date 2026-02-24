# Behavior-Driven Development Tests

This directory contains behavior specifications written in Gherkin syntax for the Synaxis platform. These specifications define the business requirements and expected system behavior in a human-readable format.

## Overview

The feature files describe key aspects of Synaxis:
- **Multi-Tenancy**: Organization isolation and data security
- **Data Residency**: Geographic compliance (GDPR, LGPD, CCPA)
- **Quota Enforcement**: Usage limits and rate limiting
- **GDPR Compliance**: EU data protection rights
- **Regional Failover**: High availability and disaster recovery
- **Multi-Currency Billing**: Global payment processing

## Structure

```
Behaviors/
├── MultiTenancy.feature      # 15 scenarios - tenant isolation
├── DataResidency.feature      # 16 scenarios - geographic compliance
├── QuotaEnforcement.feature   # 16 scenarios - usage limits
├── GDPRCompliance.feature     # 17 scenarios - privacy rights
├── Failover.feature           # 15 scenarios - high availability
├── Billing.feature            # 17 scenarios - payment processing
└── README.md                  # This file
```

**Total: 96 behavior scenarios covering critical business requirements**

## Gherkin Syntax

Each scenario follows the Given-When-Then pattern:

```gherkin
Scenario: Organization A cannot see Organization B's data
  Given Organization "Acme Corp" exists with ID "org-acme-001"
  And Organization "Globex Industries" exists with ID "org-globex-002"
  And User "alice@acme.com" belongs to "org-acme-001"
  When User "alice@acme.com" tries to access Organization "org-globex-002" data
  Then Access should be denied with 403 Forbidden
  And Audit log should record the unauthorized access attempt
```

### Keywords

- **Feature**: High-level description of functionality
- **Background**: Setup that applies to all scenarios
- **Scenario**: Specific test case
- **Given**: Preconditions and setup
- **When**: Action or event
- **Then**: Expected outcome
- **And/But**: Additional conditions or outcomes

## Implementation with SpecFlow

To implement these specifications as executable tests, use SpecFlow (or similar BDD framework):

### 1. Install SpecFlow

Add to `Synaxis.Tests.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="SpecFlow" Version="3.9.*" />
  <PackageReference Include="SpecFlow.xUnit" Version="3.9.*" />
  <PackageReference Include="SpecFlow.Tools.MsBuild.Generation" Version="3.9.*" />
</ItemGroup>
```

### 2. Create Step Definitions

Step definitions bind Gherkin steps to actual C# code:

```csharp
// tests/Synaxis.Tests/Behaviors/StepDefinitions/MultiTenancySteps.cs
[Binding]
public class MultiTenancySteps
{
    private readonly ScenarioContext _scenarioContext;
    private readonly TestFixture _fixture;

    [Given(@"Organization ""(.*)"" exists with ID ""(.*)""")]
    public void GivenOrganizationExists(string orgName, string orgId)
    {
        var organization = _fixture.CreateOrganization(orgId, orgName);
        _scenarioContext["Organization_" + orgName] = organization;
    }

    [When(@"User ""(.*)"" tries to access Organization ""(.*)"" data")]
    public async Task WhenUserTriesToAccess(string userEmail, string orgId)
    {
        var user = _scenarioContext["User_" + userEmail] as User;
        try
        {
            var response = await _fixture.ApiClient.GetOrganizationData(orgId, user.Token);
            _scenarioContext["Response"] = response;
        }
        catch (HttpRequestException ex)
        {
            _scenarioContext["Exception"] = ex;
        }
    }

    [Then(@"Access should be denied with 403 Forbidden")]
    public void ThenAccessShouldBeDenied()
    {
        var exception = _scenarioContext["Exception"] as HttpRequestException;
        exception.Should().NotBeNull();
        exception.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
```

### 3. Run Tests

```bash
dotnet test --filter "Category=BDD"
```

## Linking to Implementation Tests

Each feature file includes references to actual test classes:

- `MultiTenancy.feature` → `tests/Synaxis.Tests/Unit/TenantServiceTests.cs`
- `DataResidency.feature` → `tests/Synaxis.Tests/Integration/CrossRegionRoutingTests.cs`
- `QuotaEnforcement.feature` → `tests/Synaxis.Tests/Integration/QuotaEnforcementTests.cs`
- `GDPRCompliance.feature` → `tests/Synaxis.Tests/Compliance/GdprComplianceProviderTests.cs`
- `Failover.feature` → `tests/Synaxis.Tests/Unit/FailoverServiceTests.cs`
- `Billing.feature` → `tests/Synaxis.Tests/Services/BillingServiceTests.cs`

These references ensure behavior specs align with actual test implementation.

## Business Rules Documentation

Each feature documents key business rules:

### Multi-Tenancy Rules
1. All queries automatically filtered by tenant_id
2. API keys scoped to single organization
3. Cache keys use tenant-specific namespaces
4. Super admin can bypass tenant isolation (logged)

### Data Residency Rules
1. User region assigned by IP geolocation on signup
2. Cross-border transfer requires explicit consent (except emergency)
3. EU users default to GDPR compliance mode
4. Provider selection prioritizes data residency over latency

### Quota Rules
1. Quotas enforced at organization level (not per API key)
2. Sliding window rate limiting for burst tolerance
3. Grace period allows 5% overage for good customers
4. Downgrade mid-cycle immediately applies new limits

### GDPR Rules
1. Data export must complete within 30 days
2. Data deletion within 30 days (except legal holds)
3. Breach notification within 72 hours to supervisory authority
4. Consent must be granular and freely given

### Failover Rules
1. Region unhealthy when health score < 70%
2. Failover respects data residency requirements
3. Return to primary after 10 minutes of stable health
4. Circuit breaker opens after 5 consecutive failures

### Billing Rules
1. Exchange rates locked at start of billing period
2. Volume discounts applied progressively across tiers
3. VAT reverse charge for B2B within EU
4. Credits applied before calculating total

## Testing Strategy

### Unit Tests
Test individual components (services, validators):
- `TenantServiceTests.cs`
- `QuotaServiceTests.cs`
- `GeoIPServiceTests.cs`

### Integration Tests
Test interactions between components:
- `CrossRegionRoutingTests.cs`
- `QuotaEnforcementTests.cs`
- `BillingCalculationTests.cs`

### End-to-End Tests
Test complete user workflows:
- `EndToEndWorkflowTests.cs`
- `FullRequestLifecycleTests.cs`

### BDD Tests (These Specifications)
Test business requirements from user perspective:
- Executable documentation
- Shared understanding between business and technical teams
- Acceptance criteria for features

## Maintenance

### Adding New Scenarios

1. Identify business requirement
2. Write scenario in Gherkin
3. Create step definitions
4. Link to implementation tests
5. Run and verify

### Updating Scenarios

When requirements change:
1. Update feature file first (source of truth)
2. Update step definitions if needed
3. Update implementation tests
4. Verify all tests pass

### Review Checklist

- [ ] Scenario follows Given-When-Then pattern
- [ ] Steps are clear and unambiguous
- [ ] Business logic is correctly captured
- [ ] Edge cases are covered
- [ ] Linked to implementation tests
- [ ] All stakeholders can understand scenario

## Tools

### Recommended BDD Tools for .NET

1. **SpecFlow** (recommended)
   - Most popular .NET BDD framework
   - Excellent Visual Studio integration
   - Supports xUnit, NUnit, MSTest

2. **LightBDD**
   - Lightweight alternative
   - Fluent syntax option
   - Good for teams already using xUnit

3. **BDDfy**
   - Convention-based approach
   - No separate feature files needed
   - Lightweight and fast

### Living Documentation

SpecFlow can generate HTML reports:

```bash
# Generate living documentation
specflow livingdoc generate -f json -o LivingDoc.html
```

This creates interactive documentation from your feature files.

## CI/CD Integration

Add BDD tests to your pipeline:

```yaml
# .github/workflows/bdd-tests.yml
name: BDD Tests
on: [push, pull_request]

jobs:
  bdd:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'
      - run: dotnet test --filter "Category=BDD" --logger "trx;LogFileName=bdd-results.trx"
      - uses: actions/upload-artifact@v3
        if: always()
        with:
          name: bdd-test-results
          path: '**/bdd-results.trx'
```

## References

- [SpecFlow Documentation](https://docs.specflow.org/)
- [Gherkin Syntax Reference](https://cucumber.io/docs/gherkin/reference/)
- [BDD Best Practices](https://cucumber.io/docs/bdd/)
- [Given-When-Then](https://martinfowler.com/bliki/GivenWhenThen.html)

## Contact

For questions about these specifications:
- **Product Team**: Features and business logic
- **QA Team**: Test coverage and scenarios
- **Engineering**: Implementation and step definitions
