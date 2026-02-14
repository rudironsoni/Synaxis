# Testing Guide

This guide defines how to run Synaxis tests in a deterministic, resource-aware way.

## Test Categories

- `Category=Unit`: fast, hermetic tests (no containers, no network).
- `Category=Integration`: local integration tests using shared local containers.
- `Category=ExternalE2E`: external provider contract/E2E tests, opt-in only.

## Run Commands

- Default local gate (no public internet calls):

```bash
dotnet test Synaxis.sln --filter "Category!=ExternalE2E"
```

- Unit tests only:

```bash
dotnet test Synaxis.sln --filter "Category=Unit"
```

- Integration tests only (local containers):

```bash
dotnet test Synaxis.sln --filter "Category=Integration"
```

- External E2E tests only (opt-in):

```bash
export RUN_EXTERNAL_E2E=1
export GROQ_API_KEY=your_api_key_here
dotnet test Synaxis.sln --filter "Category=ExternalE2E"
```

## Container Lifecycle Model

- Containers are owned by shared fixtures, not test methods.
- Container builders are allowed only in fixture files under `tests/Common/Fixtures`.
- Integration collections share fixtures to avoid one-container-per-test churn.
- Stateful systems are reset cheaply between tests (isolated dbs, Redis flush, per-test Qdrant collections).

## External E2E Gating

- External tests use `ExternalE2EFactAttribute`.
- If `RUN_EXTERNAL_E2E` is not `1` or a required API key is missing, tests are marked **Skipped** (not passed, not failed).

## Guardrail

Run the guardrail locally before pushing:

```bash
bash scripts/ci-guardrail-check.sh
```

This fails if `new PostgreSqlBuilder`, `new RedisBuilder`, or `new QdrantBuilder` appears outside fixture files.

## Decision Sources

- ASP.NET Core integration testing with `WebApplicationFactory` and `TestServer`:
  https://learn.microsoft.com/aspnet/core/test/integration-tests
  - Applied by centralizing host setup in shared factory fixture and keeping tests in-memory by default.
- .NET testing fundamentals and test-type separation:
  https://learn.microsoft.com/dotnet/core/testing/
  - Applied by explicit Unit/Integration/ExternalE2E categories and separate filter commands.
- xUnit shared context and fixture lifetime:
  https://xunit.net/docs/shared-context
  - Applied by moving expensive setup from test classes to collection fixtures to avoid per-test container startup.
