# Synaxis Enterprise Stabilization - Issues

## [2026-01-30] Session Start

### Known Issues (from Plan Context)
- **Flaky Smoke Tests**: Tests hit real providers, causing non-deterministic failures
- **Missing WebApp Features**: Streaming support, admin UI, responses endpoint
- **Test Coverage**: Unknown baseline (need to measure in Phase 1)
- **Compiler Warnings**: Unknown count (need to verify in Phase 10)

### Blockers
- None yet (starting fresh)

### Gotchas
- User emphasized "flaky" smoke tests needing skepticism
- User demands NO shortcuts, no #pragma, no NOWARN
- User expects enterprise-grade quality with modern .NET 10 features


### Verification run - 2026-01-30 23:06:00 UTC

- dotnet build Synaxis.sln: FAILED
  - Errors:
    - TS errors during frontend build invoked by MSBuild:
      - src/features/admin/HealthDashboard.tsx(10,3): error TS6133: 'Server' is declared but its value is never read.
      - src/features/chat/ChatWindow.tsx(6,67): error TS6133: 'ChatStreamChunk' is declared but its value is never read.
    - MSBuild reported npm run build exited with code 2 for Synaxis.WebApp project.
    - C# compile errors in tests:
      - tests/InferenceGateway/Application.Tests/ChatClients/SmartRoutingChatClientTests.cs: CS0162 unreachable code detected
      - CS8602 possible null dereference
      - CS1061 missing member 'Name' on ChatClientMetadata

- npm run build (ClientApp) invoked directly: FAILED
  - Errors:
    - TypeScript errors:
      - src/features/admin/HealthDashboard.tsx: 'Server' declared but never used
      - src/features/chat/ChatWindow.tsx: 'ChatStreamChunk' declared but never used

- docker compose build: SUCCEEDED with warnings
  - Warnings about unset environment variables: OPENAI_API_KEY, ANTIGRAVITY_* etc (expected in local dev)
  - Build proceeded but logs truncated; full log saved to tool output file for later inspection.

Notes:
- Per instructions: no fixes applied. Recorded errors and warnings for next phases.

### Smoke test flakiness baseline run - 2026-01-30T22:23:36Z
- Total runs: 10
- Passes: 10
- Fails: 0
- Failure rate: 0.0% (0/10)
- Per-run results saved in .sisyphus/smoke-test-results.txt
- Baseline summary: .sisyphus/baseline-flakiness.txt
- Flaky tests list: .sisyphus/flaky-tests.txt
