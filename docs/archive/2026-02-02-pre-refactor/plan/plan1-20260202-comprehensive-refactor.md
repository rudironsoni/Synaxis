# SYNAXIS COMPREHENSIVE IMPLEMENTATION PLAN

**Date:** 2026-02-02
**Status:** Ready for Implementation
**Priority:** CRITICAL

## 📐 EXECUTIVE SUMMARY

This plan implements enterprise-grade fixes for Synaxis, organized for parallel execution by multiple coding agents. All work is structured to minimize dependencies and maximize parallelism.

## 🎯 CRITICAL ISSUES TO FIX

1. 🔥 **Ultra Miser Mode broken** - `IsFree` flag exists in config but not in code
2. 🔥 **Test isolation broken** - 154/814 tests fail when run together
3. 🔥 **Configuration hot-reload missing** - No SignalR/DB polling, restart required
4. 🔥 **Naive routing** - Free → Cost → Tier (no intelligence, no quality, no quota awareness)
5. 🔥 **Security holes** - JWT fallback, rate limiting placeholder, input validation gaps
6. 🔥 **Usage tracking incomplete** - No request-level accounting

## 📊 PARALLEL EXECUTION MATRIX

```
[CRITICAL PATH]          [PARALLEL TRACK 1]       [PARALLEL TRACK 2]       [PARALLEL TRACK 3]
     │                          │                       │                       │
┌────▼────┐              ┌─────▼─────┐          ┌─────▼─────┐          ┌─────▼─────┐
│ Phase 0 │              │ Phase 4   │          │ Phase 5   │          │ Phase 6   │
│ Archive │              │ Dynamic   │          │ Test      │          │ Security  │
│ Docs    │              │ Providers │          │ Isolation │          │ Hardening│
└────┬────┘              └─────┬─────┘          └─────┬─────┘          └─────┬─────┘
     │                        │                       │                       │
┌────▼────┐              ┌─────▼─────┐          ┌─────▼─────┐          ┌─────▼─────┐
│ Phase 1 │  ──────────>  │ Phase 1.5│  ──────>  │ Phase 1.5 │  ──────>  │ Phase 1.5 │
│ Ultra   │    Parallel   │ DB        │  Parallel │ Config    │  Parallel │ SignalR   │
│ Miser   │    Track 2    │ Migrations│  Track 3 │ Hot-Reload │  Track 4 │ Setup     │
└────┬────┘              └─────┬─────┘          └─────┬─────┘          └─────┬─────┘
     │                        │                       │                       │
┌────▼────┐              ┌─────▼─────┐          ┌─────▼─────┐          ┌─────▼─────┐
│ Phase 2 │  ──────────>  │ Phase 7   │  ──────>  │ Phase 8   │               │ Phase 10 │
│ Intel   │    Parallel   │ Usage     │  Parallel │ Quota     │               │ Testing  │
│ Routing │    Track 5    │ Tracking  │  Track 6 │ Warnings  │               │          │
└────┬────┘              └─────┬─────┘          └─────┬─────┘          ┌─────┬─────┐
     │                                                           │
┌────▼────┐                                            ┌────────▼─────┐
│ Phase 3 │<────────────────────────────────────────── │ Phase 9      │
│ Hot-    │    Parallel Track 7                        │ Integration   │
│ Reload  │                                            │              │
└─────────┘                                            └───────────────┘
```

## 🗂️ PHASE 0: ARCHIVE DOCUMENTATION (PARALLEL INDEPENDENT)

### Task 0.1: Create Archive Structure
- Create `docs/archive/2026-02-02-pre-refactor/` directory
- Move ALL plan files to archive:
  - `docs/plan/*` → `docs/archive/2026-02-02-pre-refactor/plan/`
  - `docs/plans/*` → `docs/archive/2026-02-02-pre-refactor/plans/`
  - `.opencode/plans/*` → `docs/archive/2026-02-02-pre-refactor/opencode-plans/`
  - `.sisyphus/*` → `docs/archive/2026-02-02-pre-refactor/sisyphus/`
- Preserve key documentation in `docs/`:
  - ✅ KEEP: `ARCHITECTURE.md`, `API.md`, `CONFIGURATION.md`, `TESTING_SUMMARY.md`, `adr/001-stream-native-cqrs.md`
- Create `docs/archive/2026-02-02-pre-refactor/README.md` with index of archived plans

**Estimated Work:** 15 minutes
**Dependencies:** None
**Can Start IMMEDIATELY**

---

## 🔥 PHASE 1: FIX ULTRA MISER MODE (CRITICAL PATH - AGENT 1)

**Dependencies:** Phase 0 completion
**Estimated Work:** 2 hours

### Task 1.1: Update `ProviderConfig` (Core Fix)
**File:** `src/InferenceGateway/Application/Configuration/SynaxisConfiguration.cs`

Add properties:
- `IsFree` (bool, default false)
- `CustomHeaders` (Dictionary<string, string>?)
- `QualityScore` (int, default 5)
- `EstimatedQuotaRemaining` (int, default 100)
- `AverageLatencyMs` (int?)

### Task 1.2: Update `EnrichedCandidate` to Read Config.IsFree
**File:** `src/InferenceGateway/Application/Routing/EnrichedCandidate.cs`

Change: `public bool IsFree => Config.IsFree || (Cost?.FreeTier ?? false);`

### Task 1.3: Update Service Registration for CustomHeaders
**File:** `src/InferenceGateway/Infrastructure/Extensions/InfrastructureExtensions.cs`

Pass `config.CustomHeaders` to all OpenAI client registrations

### Task 1.4: Verify Free Providers Have IsFree=True
**File:** `src/InferenceGateway/WebApi/appsettings.json`

Ensure: SiliconFlow, SambaNova, Zai, GitHubModels, Hyperbolic have `IsFree: true`

### Task 1.5: Add XML Documentation
Add `<summary>` and `<param>` tags to all new properties

---

## 🗄️ PHASE 1.5: DATABASE MIGRATIONS (PARALLEL TRACK 2 - AGENT 2)

**Dependencies:** Phase 1 Start
**Estimated Work:** 3 hours

### Task 1.5.1: Create New Entity Classes

**Files to Create:**
- `RoutingScorePolicy.cs` - Global/Tenant/User hierarchy
- `ProviderRequest.cs` - BYOK requests
- `HealthCheckResult.cs` - Provider health history
- `SandboxTestResult.cs` - Sandbox test history
- `ConfigurationChangeLog.cs` - Audit trail

### Task 1.5.2: Update Existing Entities
Add columns to `ProviderModel`: `IsFree`, `CustomHeaders`, `QualityScore`, `EstimatedQuotaRemaining`, `AverageLatencyMs`

### Task 1.5.3: Create DbContext Updates
Add DbSets for all new entities

### Task 1.5.4: Create EF Core Migration
```bash
dotnet ef migrations add AddDynamicConfigurationAndProviders \
  --project src/InferenceGateway/Infrastructure \
  --startup-project src/InferenceGateway/WebApi \
  --context ControlPlaneDbContext
```

### Task 1.5.5: Seed Global Default Policy
Create seed data for default global routing policy

---

## ⚙️ PHASE 1.5: CONFIGURATION HOT-RELOAD INFRASTRUCTURE (PARALLEL TRACK 3 - AGENT 3)

**Dependencies:** Phase 1 Start
**Estimated Work:** 4 hours

### Task 1.5.1: Create ConfigurationReloadManager
**File:** `src/InferenceGateway/Application/Configuration/ConfigurationReloadManager.cs`

Methods:
- `ReloadProviderConfigAsync(string providerKey)`
- `ReloadRoutingScorePoliciesAsync(string tenantId)`
- `ReloadAllRoutingScorePoliciesAsync()`
- `NotifyConfigurationChangeAsync(string changeType, object data)`

### Task 1.5.2: Create ConfigurationReloadService (Background Service)
**File:** `src/InferenceGateway/WebApi/BackgroundServices/ConfigurationReloadService.cs`

Polls database every 5 seconds for configuration changes

### Task 1.5.3: Register Hot-Reload Services
Register in Program.cs with IOptionsMonitor

---

## 🔌 PHASE 1.5: SIGNALR SETUP (PARALLEL TRACK 4 - AGENT 4)

**Dependencies:** Phase 1 Start
**Estimated Work:** 3 hours

### Task 1.5.1: Create ConfigurationHub
**File:** `src/InferenceGateway/WebApi/Hubs/ConfigurationHub.cs`

Methods:
- `SubscribeToGlobalConfigurationChanges()` (Admin only)
- `SubscribeToTenantConfigurationChanges(string tenantId)`
- `SubscribeToUserConfigurationChanges(string userId)`

### Task 1.5.2: Register SignalR with Security Settings
Add to Program.cs with enterprise security:
- Rate limiting
- Connection timeouts
- Message size limits
- Role-based authorization

### Task 1.5.3: SignalR Security Recommendations
- Rate limiting middleware (5 connections per user, 50 per IP)
- Connection authorization
- Message size limits (1MB)
- Hub method authorization

---

## 🧠 PHASE 2: IMPLEMENT INTELLIGENT ROUTING (DEPENDENT ON: PHASE 1 + PHASE 1.5 TRACK 2)

**Dependencies:** Phase 1, Phase 1.5 Track 2
**Estimated Work:** 5 hours

### Task 2.1: Create RoutingScoreCalculator Service
**File:** `src/InferenceGateway/Application/Routing/RoutingScoreCalculator.cs`

Implements 3-level precedence: Global → Tenant → User
Score calculation: (Quality × 0.3) + (Quota × 0.3) + (RateLimit × 0.2) + (Latency × 0.2)

### Task 2.2: Update SmartRouter to Use Score Calculator
Replace GetCandidatesAsync to use intelligent scoring

### Task 2.3: Create FallbackOrchestrator Service
**File:** `src/InferenceGateway/Application/Routing/FallbackOrchestrator.cs`

Multi-tier fallback:
- Tier 0: User's preferred providers
- Tier 1: Free providers (scored)
- Tier 2: Paid providers (scored)
- Tier 3: Emergency fallback (all providers)

### Task 2.4: Update SmartRoutingChatClient to Use FallbackOrchestrator

---

## 🔗 PHASE 3: COMPLETE SIGNALR INTEGRATION (DEPENDENT ON: PHASE 1.5 TRACK 4)

**Dependencies:** Phase 1.5 Track 4
**Estimated Work:** 2 hours

### Task 3.1: Map SignalR Hub in Application
Add to Program.cs: `app.MapHub<ConfigurationHub>("/hubs/configuration");`

### Task 3.2: Add SignalR Client Library
Create `src/InferenceGateway/WebApi/wwwroot/js/configuration-client.js`

### Task 3.3: Add SignalR Security Middleware
**File:** `src/InferenceGateway/WebApi/Middleware/SignalRRateLimitMiddleware.cs`

---

## 👤 PHASE 4: IMPLEMENT DYNAMIC PROVIDER MANAGEMENT (DEPENDENT ON: PHASE 1 + PHASE 1.5 TRACKS 2,4)

**Dependencies:** Phase 1, Phase 1.5 Track 2, Phase 1.5 Track 4
**Estimated Work:** 6 hours

### Task 4.1: Create ProviderHealthCheckService
**File:** `src/InferenceGateway/Application/Providers/ProviderHealthCheckService.cs`

Methods:
- `PerformHealthCheckAsync(ProviderConfig config)`
- `SandboxTestAsync(ProviderConfig config)`

### Task 4.2: Create Admin Controller
**File:** `src/InferenceGateway/WebApi/Controllers/Admin/ProviderManagementController.cs`

Endpoints:
- `GET /api/admin/providers/requests/pending`
- `POST /api/admin/providers/requests/{id}/approve`
- `POST /api/admin/providers/requests/{id}/reject`
- `POST /api/admin/providers/requests/{id}/sandbox-test`
- `DELETE /api/admin/providers/{key}`

### Task 4.3: Create User Controller for BYOK Submission
**File:** `src/InferenceGateway/WebApi/Controllers/Users/ProviderRequestController.cs`

Endpoints:
- `POST /api/users/providers/request`
- `GET /api/users/providers/requests`
- `GET /api/users/providers/requests/{id}`

---

## 🧪 PHASE 5: TEST ISOLATION FIXES (PARALLEL INDEPENDENT TRACK - AGENT 5)

**Dependencies:** None
**Estimated Work:** 4 hours

### Task 5.1: Create Test Container Factory
**File:** `tests/Common/TestContainerFactory.cs`

Unique containers per test using Guid-based naming

### Task 5.2: Create Test Collection Definitions
- `NonParallelCollection` - Disable parallelization
- `IntegrationCollection` - Shared fixture
- `IntegrationTestFixture` - Database/container setup

### Task 5.3: Fix Static Random in MockProviderResponses
Remove static Random, create local instances

### Task 5.4: Add Collection Attributes to Test Classes
Add `[Collection("Non-Parallel")]` or `[Collection("Integration")]`

### Task 5.5: Create Test Configuration File
`tests/Directory.Build.props` with isolation settings

---

## 🔒 PHASE 6: SECURITY HARDENING (PARALLEL INDEPENDENT TRACK - AGENT 6)

**Dependencies:** None
**Estimated Work:** 2 hours

### Task 6.1: Implement Real Rate Limiting
**File:** `src/InferenceGateway/Infrastructure/Routing/RedisQuotaTracker.cs`

Fix CheckQuotaAsync to actually check RPM/TPM limits

### Task 6.2: Add Input Validation Middleware
**File:** `src/InferenceGateway/WebApi/Middleware/InputValidationMiddleware.cs`

Validate Content-Length, message count, token limits

### Task 6.3: Add Security Headers Middleware
**File:** `src/InferenceGateway/WebApi/Middleware/SecurityHeadersMiddleware.cs`

Add: X-Content-Type-Options, X-Frame-Options, X-XSS-Protection, CSP, HSTS

### Task 6.4: Update CORS Policies
Add TenantSpecific policy

---

## 💰 PHASE 7: USAGE TRACKING (DEPENDENT ON: PHASE 1.5 TRACK 2)

**Dependencies:** Phase 1.5 Track 2
**Estimated Work:** 3 hours

### Task 7.1: Create UsageTrackingMiddleware
**File:** `src/InferenceGateway/WebApi/Middleware/UsageTrackingMiddleware.cs`

Request-level logging to database (100% accountability)

### Task 7.2: Create UsageTracker Service
**File:** `src/InferenceGateway/Application/Usage/UsageTracker.cs`

Methods:
- `LogRequestAsync(RequestLogEntry entry)`
- `LogTokenUsageAsync(TokenUsageEntry entry)`
- `GetUsageSummaryAsync(string tenantId, string period)`
- `GetProviderUsageAsync(string providerKey, string period)`

### Task 7.3: Create Usage Endpoints
**File:** `src/InferenceGateway/WebApi/Endpoints/Analytics/UsageEndpoints.cs`

Endpoints:
- `GET /api/analytics/usage/{tenantId}/{period}`
- `GET /api/analytics/usage/total`
- `GET /api/analytics/providers/{providerKey}/usage`

---

## ⚠️ PHASE 8: QUOTA WARNING SYSTEM (DEPENDENT ON: PHASE 7)

**Dependencies:** Phase 7
**Estimated Work:** 2 hours

### Task 8.1: Create QuotaWarningService
**File:** `src/InferenceGateway/Application/Quota/QuotaWarningService.cs`

Methods:
- `CheckQuotaStatusAsync(string providerKey)`
- `WarnNearLimitAsync(string providerKey, double thresholdPercentage)`

### Task 8.2: Integrate Quota Warnings into Routing
Add quota checks to SmartRouter (warn but never block)

---

## 🔗 PHASE 9: FINAL INTEGRATION (DEPENDENT ON: ALL PHASES EXCEPT 10)

**Dependencies:** All implementation phases
**Estimated Work:** 2 hours

### Task 9.1: Register All Services in Program.cs
Register all new services and middleware

### Task 9.2: Update DI to Use OptionsMonitor
Change IOptions to IOptionsMonitor where hot-reload needed

### Task 9.3: Register All Background Services
Add ConfigurationReloadService

### Task 9.4: Map All Endpoints
Map SignalR hub and usage endpoints

---

## ✅ PHASE 10: TESTING & VALIDATION (DEPENDENT ON: ALL PHASES)

**Dependencies:** All phases
**Estimated Work:** 3 hours

### Task 10.1: Run Full Test Suite
```bash
dotnet test --no-build --verbosity normal
```
Expected: All 814 tests PASS

### Task 10.2: Create Integration Tests
Test coverage for:
- Ultra Miser Mode
- Intelligent Routing
- Hot Reload
- Provider Management
- SignalR
- Usage Tracking
- Security

### Task 10.3: Manual Testing Checklist
- BYOK workflow
- Intelligent routing
- Quota warnings
- Hot reload
- All tests pass in parallel

### Task 10.4: Performance Testing
- No regression from hot-reload polling
- SignalR scaling
- Database query performance

---

## 📊 EXECUTION SUMMARY

### Agent Assignment (3-4 Agents Parallel):

| Agent | Phases | Estimated Time | Dependencies |
|-------|--------|----------------|--------------|
| **Agent 1** | Phase 0, 1, 2, 9 | 11 hours | Starts after Phase 0 |
| **Agent 2** | Phase 1.5 (Track 2), 7, 8 | 8 hours | Can start in parallel with Phase 1 |
| **Agent 3** | Phase 1.5 (Track 3), 3, 4 | 12 hours | Can start in parallel with Phase 1 |
| **Agent 4** | Phase 1.5 (Track 4), 6, 5 | 9 hours | Can start immediately |

### Critical Path:
Phase 0 → Phase 1 → Phase 2 → Phase 9 → Phase 10 (~19 hours)

### Parallel Work:
- Phase 1.5 Track 2/3/4 can run concurrently with Phase 1
- Phase 5/6 are completely independent
- Phases 3, 4, 7, 8 have minimal dependencies

### Total Wall-Clock Time: ~24-30 hours (with 3-4 parallel agents)

---

## 🎯 SUCCESS CRITERIA

- [ ] All 814 tests pass in parallel execution
- [ ] Ultra Miser Mode prioritizes free providers correctly
- [ ] Intelligent routing uses configurable weights
- [ ] Configuration hot-reloads without restart
- [ ] BYOK providers can be added and approved
- [ ] SignalR provides real-time notifications
- [ ] Usage tracking provides 100% accountability
- [ ] Security hardening complete (rate limiting, input validation, headers)
- [ ] Quota warnings work (warn but never block)
- [ ] Code coverage ≥ 80%

---

## 📝 NOTES

- **SignalR Security:** Using same JWT as main API + rate limiting middleware
- **Database Migrations:** Entity Framework Core Migrations
- **Admin Role:** "Administrator"
- **Test Execution:** Run full suite after all fixes
- **Documentation:** XML comments on all public APIs
