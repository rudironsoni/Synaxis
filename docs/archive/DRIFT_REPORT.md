# Drift Report

Documentation claims that may be stale or contradicted by later commits or repo state.

## Areas

### APIs

#### Plan describes Microsoft.Agents.AI integration with ported agent code
- **Doc:** `docs/archive/2026/01/28/.../20260128-plan3-microsoft-agents-integration.md`
- **Claim Date:** 2026-01-28
- **Contradiction:** 77db33a (2026-01-28) - Refactored GitHub Copilot to IChatClient pattern
- **Evidence:** Commit removed `GithubCopilotAgent.cs`, `GithubCopilotAgentSession.cs`, and `Microsoft.Agents.AI.Abstractions` package. Replaced with `GitHubCopilotChatClient.cs` using direct SDK integration.
- **Remediation:** Add ADR documenting decision to use IChatClient pattern instead of Microsoft Agent Framework

#### Plan specifies installing gh CLI, nodejs, npm in Dockerfile for Copilot
- **Doc:** `docs/archive/2026/01/28/.../20260128-plan3-microsoft-agents-integration.md`
- **Claim Date:** 2026-01-28
- **Contradiction:** Current implementation (77db33a) - Dockerfile includes these dependencies
- **Evidence:** `src/InferenceGateway/WebApi/Dockerfile` contains gh CLI and npm installation but Microsoft.Agents package was removed
- **Remediation:** Update doc to reflect that gh CLI installation is for GitHub Copilot SDK, not Microsoft Agents

#### Plan describes side-by-side Synaxis.Next.sln with new project structure
- **Doc:** `docs/archive/2026/01/24/.../20260124-synaplexer-rebuild.md`
- **Claim Date:** 2026-01-24
- **Contradiction:** Repository never created separate solution
- **Evidence:** Only `Synaxis.sln` exists; no `Synaxis.Connectors`, `Synaxis.Brain`, or `Synaxis.Gateway` projects. Actual structure uses `InferenceGateway/Application`, `InferenceGateway/Infrastructure`, `InferenceGateway/WebApi`
- **Remediation:** Mark as historical/abandoned approach; add ADR for chosen Clean Architecture structure

### Data

#### Plan describes dynamic model registry with Quartz.NET background jobs
- **Doc:** `docs/archive/2026/01/29/.../20260129-plan7-dynamic-model-registry.md`
- **Claim Date:** 2026-01-29
- **Contradiction:** Partial implementation exists
- **Evidence:** Quartz package added to Infrastructure.csproj, `ModelsDevSyncJob.cs` and `ProviderDiscoveryJob.cs` exist in `src/InferenceGateway/Infrastructure/Jobs/`. Status shows Phase 2 as "In Progress" but doc claims Phase 1 complete.
- **Remediation:** Update doc with actual implementation status; verify if jobs are registered and scheduled

#### Plan describes smoke test system with xUnit data-driven tests
- **Doc:** `docs/archive/2026/01/29/.../20260129-plan2-smoke-tests.md`
- **Claim Date:** 2026-01-29
- **Contradiction:** Partial implementation exists
- **Evidence:** Test files exist: `SmokeTestCase.cs`, `SmokeTestOptions.cs`, `SmokeTestResult.cs`, `SmokeTestExecutor.cs`, `MockSmokeTestHelper.cs` in `tests/InferenceGateway/IntegrationTests/SmokeTests/`. Implementation appears complete despite doc being dated 2026-01-29.
- **Remediation:** Verify test execution status; add completion note if fully implemented

### Infra

#### Plan describes docker-compose with PostgreSQL, Redis, pgAdmin
- **Doc:** `docs/archive/2026/01/26/.../20260126-docker-compose-infrastructure.md`
- **Claim Date:** 2026-01-26
- **Contradiction:** Implementation exists but evolved
- **Evidence:** `docker-compose.yml` and `.env.example` exist at repo root as of 2026-02-03. Later commits (1cc061f on 2026-01-28) moved Postgres/Redis to `docker-compose.infrastructure.yml` for separation of concerns.
- **Remediation:** Update doc to reference actual docker-compose structure with infrastructure separation

#### Plan describes local-first React WebApp with IndexedDB
- **Doc:** `docs/archive/2026/01/29/.../plan1-20260129-frontend-local-first.md`
- **Claim Date:** 2026-01-29
- **Contradiction:** Implementation evolved differently
- **Evidence:** `Synaxis.WebApp` exists as ASP.NET Core YARP proxy (commit 6e8b706 on 2026-01-29), not standalone React app. WebApp containerized and integrated into docker-compose but architecture differs from plan (YARP wrapper instead of standalone Vite app).
- **Remediation:** Add ADR documenting decision to use YARP proxy pattern; note that frontend exists within `src/Synaxis.WebApp/ClientApp`

#### Plan describes containerization with Node.js in .NET SDK image
- **Doc:** `docs/archive/2026/01/29/.../plan2-20260129-webapp-containerization.md`
- **Claim Date:** 2026-01-29
- **Contradiction:** Implementation matches plan
- **Evidence:** Commit 6e8b706 (2026-01-29) implemented exactly as specified. `Synaxis.WebApp/Dockerfile` exists, webapp service in docker-compose.yml, GatewayUrl configuration implemented.
- **Remediation:** Mark as successfully completed; no drift

### Security

#### Plan describes hardcoded secrets removal in AntigravityAuthManager
- **Doc:** `docs/archive/2026/01/27/.../plan5-20260127-security-and-quality-remediation.md`
- **Claim Date:** 2026-01-27
- **Contradiction:** Remediation implemented same day
- **Evidence:** Commit 0dcaba7 (2026-01-28) implemented security fixes: added `AntigravitySettings.cs`, removed hardcoded secrets, fixed error handling in `OpenAIRequestParser.cs`, implemented Cohere streaming, updated `AesGcmTokenVault.cs` and `JwtService.cs` to throw on missing keys.
- **Remediation:** Mark as completed; verify all security measures are in production configuration

#### Plan describes BYOK encryption with AesGcmTokenVault throwing on missing keys
- **Doc:** `docs/archive/2026/01/27/.../plan5-20260127-security-and-quality-remediation.md`
- **Claim Date:** 2026-01-27
- **Contradiction:** Implementation matches plan
- **Evidence:** Same commit 0dcaba7 updated `AesGcmTokenVault.cs` to throw `InvalidOperationException` when encryption keys are missing, removing "SynaxisDefault..." fallback.
- **Remediation:** Mark as completed; ensure development keys are documented

### Runtime

#### Plan describes GitHub + Google OAuth implementation
- **Doc:** `docs/archive/2026/01/25/.../20260125-openai-gateway-roadmap.md`
- **Claim Date:** 2026-01-25 (M2 milestone)
- **Contradiction:** Implementation completed later
- **Evidence:** Commits 5d62c5d (2026-01-27) and later commits show GitHub and Google authentication strategies implemented. Integration tests added (commits 70d5c20, db90321, 2c151ad, 27682177).
- **Remediation:** Update milestone status to completed; document implementation date

#### Plan describes free tier providers: GitHub Copilot, DuckDuckGo, AI Horde
- **Doc:** `docs/archive/2026/01/28/.../20260128-plan1-add-free-providers.md`
- **Claim Date:** 2026-01-28 (marked as "Completed")
- **Contradiction:** Implementation exists and matches plan
- **Evidence:** Commit d1d8886 (2026-01-28) added all three providers: `CopilotSdkClient.cs`, `DuckDuckGoChatClient.cs`, `AiHordeChatClient.cs` in `src/InferenceGateway/Infrastructure/External/` directories.
- **Remediation:** Mark as verified; no drift

#### Plan describes KiloCode integration with custom headers
- **Doc:** `docs/archive/2026/01/29/.../plan1-20260129-kilocode-integration.md`
- **Claim Date:** 2026-01-29
- **Contradiction:** Implementation completed
- **Evidence:** Commits 7bb0754 and f3fa804 (2026-01-29) integrated KiloCode provider with `KiloCodeChatClient.cs`, configuration updates for glm-4.7 and MiniMax-M2.1 models.
- **Remediation:** Mark as completed; verify unit tests exist

### Build and Deploy

#### Plan describes ULTRA MISER MODE routing with free > cheapest > healthy logic
- **Doc:** `docs/archive/2026/01/25/.../20260125-openai-gateway-roadmap.md`
- **Claim Date:** 2026-01-25 (M3 milestone)
- **Contradiction:** Current architecture documentation shows different routing approach
- **Evidence:** `docs/ARCHITECTURE.md` (created 2026-02-02, commit 51b32dd) describes `SmartRoutingChatClient` with fallback orchestration and scoring, but specific "free > cheapest > healthy" algorithm not explicitly documented. Commits 201d4a7 and 35142e4 (2026-02-01) added routing and scoring features.
- **Remediation:** Add ADR documenting actual routing algorithm; verify if free-tier-first logic is implemented

#### Plan describes model aliases and combos
- **Doc:** `docs/archive/2026/01/25/.../20260125-openai-gateway-roadmap.md`
- **Claim Date:** 2026-01-25 (M3 milestone)
- **Contradiction:** Implementation status unclear
- **Evidence:** Control plane entities added (commits e7b3a13, 2e24fb7 on 2026-02-01), but specific "model alias" and "combo" features not found in codebase search. May be part of CanonicalModel system.
- **Remediation:** Needs review - verify if feature exists or is planned

## Needs Review

### Synaxis.Next solution structure claim
- **Doc:** `docs/archive/2026/01/24/.../20260124-synaplexer-rebuild.md`
- **Claim:** "Side-by-side construction using Synaxis.Next.sln with Synaxis.Connectors, Synaxis.Brain, Synaxis.Gateway projects"
- **Missing Evidence:** No Synaxis.Next.sln or mentioned projects exist; actual structure is InferenceGateway/* with Clean Architecture
- **Suggested Action:** Confirm this was an initial proposal that was abandoned; document actual architecture decision in ADR

### WebApp "Local-First" architecture with Dexie.js
- **Doc:** `docs/archive/2026/01/29/.../plan1-20260129-frontend-local-first.md`
- **Claim:** "React 19, Vite, TypeScript with Dexie.js for IndexedDB, standalone at synaxis-ui/"
- **Missing Evidence:** No `synaxis-ui/` directory at repo root; actual implementation is within `src/Synaxis.WebApp/ClientApp`
- **Suggested Action:** Verify if ClientApp uses Dexie.js as planned or different state management; document architectural difference

### M4 (Ops + Usage) and M5 (Public Dashboard) milestone status
- **Doc:** `docs/archive/2026/01/25/.../20260125-openai-gateway-roadmap.md`
- **Claim:** "M4: Per-request token tracking, usage APIs. M5: Multi-tenant UI"
- **Missing Evidence:** Dashboard implementation commits found (a618a5d, 97b470b on 2026-02-01), model configuration UI added (3157e59, 189880c), but usage tracking API status unclear
- **Suggested Action:** Review dashboard implementation to verify M4/M5 completion status; update milestone tracking

### Quartz.NET job registration and scheduling
- **Doc:** `docs/archive/2026/01/29/.../20260129-plan7-dynamic-model-registry.md`
- **Claim:** "Phase 2: Add Quartz dependencies, register services and jobs in Program.cs"
- **Missing Evidence:** Job classes exist but registration in `Program.cs` not verified
- **Suggested Action:** Check `src/InferenceGateway/WebApi/Program.cs` for Quartz registration; verify jobs are scheduled

### Provider health tracking and cooldown mechanism
- **Doc:** `docs/archive/2026/01/25/.../20260125-openai-gateway-roadmap.md`
- **Claim:** "M3: Account health + cooldown, quota avoidance"
- **Missing Evidence:** `HealthStore` mentioned in ARCHITECTURE.md but actual health tracking implementation not verified
- **Suggested Action:** Search for health tracking code in Infrastructure layer; verify Redis-based health store implementation

### Cohere V2 streaming implementation
- **Doc:** `docs/archive/2026/01/27/.../plan5-20260127-security-and-quality-remediation.md`
- **Claim:** "Implement GetStreamingResponseAsync using Cohere's V2 SSE protocol"
- **Missing Evidence:** Commit 0dcaba7 shows changes to `CohereChatClient.cs` (+133 lines) suggesting implementation
- **Suggested Action:** Review CohereChatClient implementation to confirm V2 streaming is complete and tested

### Dockerfile gh CLI installation purpose
- **Doc:** `docs/archive/2026/01/28/.../20260128-plan3-microsoft-agents-integration.md`
- **Claim:** "Install gh CLI for Microsoft Agent Framework integration"
- **Missing Evidence:** Microsoft.Agents removed but gh CLI remains in Dockerfile
- **Suggested Action:** Document that gh CLI is for GitHub Copilot SDK authentication, not Microsoft Agents

### Documentation rebuild supersedes all archived plans
- **Doc:** All archived plans vs. current `docs/ARCHITECTURE.md`, `docs/README.md`, etc.
- **Claim:** Multiple archived plans describe intended architecture
- **Missing Evidence:** Major documentation rebuild on 2026-02-02 (commits 51b32dd, 43d9195, 438e9b3) replaced all documentation
- **Suggested Action:** Add note to archive INDEX.md that all pre-2026-02-02 plans are superseded by current docs/ directory; these are historical records only
