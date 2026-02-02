# Coverage Gaps Report

**Generated:** 2026-01-30

## Overall Coverage Summary

| Metric | Backend | Frontend | Target | Status |
|--------|---------|----------|--------|--------|
| Line Coverage | 67.6% | 85.89% | 80% | ❌ Backend below target |
| Branch Coverage | 40.7% | 78.26% | 80% | ❌ Both below target |

## Priority 1: Critical Path Files (Target: ≥85%)

These files are on the critical path for every request and need high coverage:

| File | Line % | Branch % | Gap | Priority |
|------|--------|----------|-----|----------|
| `RoutingService.cs` | 0% | 0% | Critical | 🔴 HIGH |
| `OpenAIErrorHandlerMiddleware.cs` | 86.2% | 61.2% | Close | 🟡 MEDIUM |
| `OpenAIRequestParser.cs` | 61.2% | 50% | Significant | 🔴 HIGH |
| `OpenAIRequestMapper.cs` | 97.8% | 94.8% | ✅ Met | 🟢 LOW |
| `SmartRoutingChatClient.cs` | 58.9% | 35.3% | Significant | 🔴 HIGH |

## Priority 2: Infrastructure Files (Target: ≥80%)

| File | Line % | Branch % | Gap | Priority |
|------|--------|----------|-----|----------|
| `CohereChatClient.cs` | 35.2% | 8.5% | Critical | 🔴 HIGH |
| `GenericOpenAiChatClient.cs` | 58.3% | 58.3% | Moderate | 🟡 MEDIUM |
| `IdentityEndpoints.cs` | 33.3% | 0% | Critical | 🔴 HIGH |
| `AntigravityAuthManager.cs` | 22.5% | 17.6% | Critical | 🔴 HIGH |
| `GitHubAuthStrategy.cs` | 6.5% | 12.5% | Critical | 🔴 HIGH |
| `GoogleAuthStrategy.cs` | 2.6% | 4.5% | Critical | 🔴 HIGH |
| `LegacyCompletionsEndpoint.cs` | 58.5% | 18.4% | Moderate | 🟡 MEDIUM |

## Priority 3: Utility & Extension Files (Target: ≥75%)

| File | Line % | Branch % | Gap | Priority |
|------|--------|----------|-----|----------|
| `OpenAIToolNormalizer.cs` | 50% | 25% | Moderate | 🟡 MEDIUM |
| `NoOp*Translator.cs` | 50% | N/A | Moderate | 🟡 MEDIUM |
| `InfrastructureExtensions.cs` | 72.2% | 66.6% | Close | 🟢 LOW |

## Test Infrastructure (Not Production Code)

These are test helper files and don't affect production coverage:

| File | Line % | Notes |
|------|--------|-------|
| `TestDataFactory.cs` | 0% | Test helper - not production code |
| `InMemoryDbContext.cs` | 0% | Test helper - not production code |
| `TestBase.cs` | 28.5% | Test helper - not production code |

## Recommendations

### Immediate Actions (Phase 8.2)

1. **Add tests for RoutingService** (0% coverage) - Critical path
2. **Add tests for OpenAIRequestParser** (61.2% coverage) - Critical path
3. **Add tests for SmartRoutingChatClient** (58.9% coverage) - Core routing logic
4. **Add tests for IdentityEndpoints** (33.3% coverage) - Authentication

### Secondary Actions

5. Add tests for CohereChatClient error paths
6. Add tests for LegacyCompletionsEndpoint edge cases
7. Add tests for OpenAIErrorHandlerMiddleware error scenarios

### Exclusions

The following should be excluded from coverage targets:
- Test infrastructure files (TestDataFactory, InMemoryDbContext, TestBase)
- Auto-generated code (Microsoft.AspNetCore.OpenApi.Generated)
- Migration files (EF Core migrations)
- Program.cs entry point

## Next Steps

Proceed with Phase 8.2: Add Missing Backend Unit Tests focusing on Priority 1 and Priority 2 files.
