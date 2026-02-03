## Plan: Docker Compose Infrastructure Alignment

### Summary
- Align appsettings.json and appsettings.Development.json to the current Synaxis:InferenceGateway configuration shape.
- Remove appsettings.Example.json to avoid drift.
- Add a complete docker-compose setup for Postgres, Redis (auth), pgAdmin (dev profile), and WebApi.
- Provide .env.example with placeholder variables for providers and infrastructure.
- Build, test (>=80% coverage), and run the API with dotnet run to verify liveness and models endpoints.

### Steps
1) Rewrite appsettings.json/appsettings.Development.json with a single Synaxis root and InferenceGateway nesting.
2) Delete appsettings.Example.json.
3) Update docker-compose.yml with postgres/redis/pgadmin + environment placeholders and data volumes.
4) Add .env.example with all required placeholders.
5) Run build, tests with coverage threshold, and dotnet run verification.
