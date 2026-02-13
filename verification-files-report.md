# Verification Command References

## Files Found

### 1. CI Workflow Files
- **`.github/workflows/build-test-publish.yml`**: Contains full verification sequence:
  - `dotnet restore Synaxis.sln`
  - `dotnet format Synaxis.sln --verify-no-changes`
  - `dotnet build Synaxis.sln -c Release -warnaserror --no-restore`
  - `dotnet test Synaxis.sln --no-build -p:Configuration=Release --collect:"XPlat Code Coverage"`
  - **Status**: Already follows canonical order (format → build → test).

- **`.github/workflows/tag-release.yml`**: No verification commands.

### 2. Active Scripts
- **`docs/archive/2026/02/02/docs_archive/2026-02-02-pre-refactor/sisyphus/scripts/verify-all.sh`**: Master verification script with canonical order:
  - `dotnet restore Synaxis.sln`
  - `dotnet format Synaxis.sln --verify-no-changes`
  - `dotnet build Synaxis.sln -c Release -warnaserror --no-restore`
  - `dotnet test Synaxis.sln --no-build -p:Configuration=Release`
  - **Status**: Compliant with AGENTS.md policy.

- **`docs/archive/2026/02/02/docs_archive/2026-02-02-pre-refactor/sisyphus/run_smoke_tests.sh`**: Smoke‑test runner (project‑specific filter). Not part of standard verification sequence.

### 3. Policy Documentation
- **`AGENTS.md`** (root): Defines canonical verification order:
  - `dotnet format --verify-no-changes`
  - `dotnet build <Solution.sln> -c Release -warnaserror`
  - `dotnet test <Solution.sln> --no-build`
  - **Status**: Authoritative policy.

- **`verification-order-report.md`**: Analysis of inconsistencies. **Note**: Reports CI missing format step, but CI now includes it. File needs updating.

- **`failing_tests_report.md`**: Includes verification commands as part of failure analysis.

### 4. Archived Documentation (may contain outdated patterns)
- **`docs/archive/2025-02-08/AGENTS.md`**: Older version with no explicit ordering.
- **`docs/archive/2025-02-08/TESTING.md`**: Contains forbidden flag `--maxcpucount:4`.
- **`docs/archive/2025-02-08/CONTRIBUTING.md`**: Includes verification commands (format, build, test).
- **`docs/archive/2025-02-08/workflows.md`**: Describes lint job not present in current CI.

### 5. Other Scripts
- **`scripts/rollback-migration.ps1`**: Uses `dotnet ef` for migrations, not verification.
- **`scripts/rollback-migration.sh`**: Same as above.

## Summary
The repository’s active verification paths (CI workflow and `verify-all.sh`) already follow the canonical order defined in `AGENTS.md`. The only file that requires updating is `verification-order-report.md` to reflect that the CI workflow now includes the formatting step. Archived documentation contains outdated patterns but is not used in active workflows.