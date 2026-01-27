## Plan: Dotnet Verification + Health Checks

### Summary
- Add dotnet-native integration tests covering `/v1/models` and `/v1/chat/completions` (streaming + non-streaming).
- Add production-grade health checks with liveness/readiness endpoints.
- Add provider connectivity checks for enabled providers (DNS/TCP/TLS/HEAD) with 3s total budget.
- Update example config and README for new health and provider enablement settings.
- Run build, tests, and `dotnet run` for end-to-end verification.

### Steps
1) Implement integration tests for all endpoints and validate OpenAI response shape.
2) Add health checks:
   - Liveness: always healthy
   - Readiness: config + EF Core Postgres + Redis + enabled provider connectivity
3) Add provider connectivity check service with strict time budgets.
4) Update `appsettings.Example.json` and README.
5) Build, run tests with coverage, and run the API with `dotnet run` for verification.
