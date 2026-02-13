# Verification Order Analysis

## Files Defining Command Order

### 1. AGENTS.md (Current Policy)
**Location**: `/home/rrj/src/github/rudironsoni/Synaxis/AGENTS.md:52-53,76-78`
**Snippet**:
```bash
dotnet format --verify-no-changes
dotnet build <Solution.sln> -c Release -warnaserror
dotnet test <Solution.sln> --no-build
```
**Notes**: Defines strict order: format → build → test. Requires formatting verification before build.

### 2. CI Workflow (build-test-publish.yml)
**Location**: `/home/rrj/src/github/rudironsoni/Synaxis/.github/workflows/build-test-publish.yml:57-72`
**Snippet**:
```yaml
- name: Restore dependencies
  run: dotnet restore Synaxis.sln

- name: Build solution
  run: dotnet build Synaxis.sln --configuration Release --no-restore --verbosity diagnostic > build.log 2>&1 || (tail -200 build.log && exit 1)

- name: Run tests with coverage
  run: |
    dotnet test Synaxis.sln \
      --configuration Release \
      --no-build \
      --verbosity normal \
      --collect:"XPlat Code Coverage" \
      --results-directory ./coverage
```
**Notes**: Missing `dotnet format --verify-no-changes`. Order: restore → build → test.

### 3. verify-all.sh (Archive Script)
**Location**: `/home/rrj/src/github/rudironsoni/Synaxis/docs/archive/2026/02/02/docs_archive/2026-02-02-pre-refactor/sisyphus/scripts/verify-all.sh:204-226`
**Snippet**:
```bash
# Restore packages
if run_check "Restore NuGet packages" "dotnet restore Synaxis.sln"; then ...

# Build solution
if run_check "Build solution" "dotnet build Synaxis.sln --no-restore --nologo -v q"; then ...

# Run unit tests
run_check "Unit tests pass" "dotnet test tests/InferenceGateway.UnitTests --no-build --nologo -v q"
```
**Notes**: Missing formatting verification. Runs tests per-project instead of solution-wide.

### 4. Archive AGENTS.md (Old Guidance)
**Location**: `/home/rrj/src/github/rudironsoni/Synaxis/docs/archive/2025-02-08/AGENTS.md:35-62`
**Snippet**:
```bash
### Build
dotnet restore Synaxis.sln
dotnet build Synaxis.sln
dotnet build Synaxis.sln --configuration Release

### Test
dotnet test Synaxis.sln

### Lint/Format
dotnet format Synaxis.sln
dotnet format Synaxis.sln --verify-no-changes
```
**Notes**: No explicit ordering between format, build, test.

### 5. workflows.md (Documentation)
**Location**: `/home/rrj/src/github/rudironsoni/Synaxis/docs/archive/2025-02-08/workflows.md:50-53`
**Snippet**:
```markdown
#### lint
- Checks code formatting with `dotnet format`
- Runs static analyzers
- Treats warnings as errors
```
**Notes**: Describes lint job that doesn't exist in current CI.

## Conflict Analysis

| File | Conflicts with AGENTS.md Policy |
|------|----------------------------------|
| CI Workflow | ❌ Missing `dotnet format --verify-no-changes` step |
| verify-all.sh | ❌ Missing formatting verification; uses per-project tests instead of solution-wide |
| Archive AGENTS.md | ⚠️ No explicit order, but includes separate commands |
| workflows.md | ⚠️ Lint job described but not implemented |

## Recommended Edits

### 1. Add Formatting Verification to CI Workflow
**File**: `.github/workflows/build-test-publish.yml`
**Change**: Insert new step after "Restore dependencies" and before "Build solution":
```yaml
- name: Verify formatting
  run: dotnet format Synaxis.sln --verify-no-changes
```

### 2. Update verify-all.sh (if still used)
**File**: `docs/archive/.../verify-all.sh`
**Changes**:
- Add formatting check before build:
  ```bash
  # Verify formatting
  print_section "Verifying code formatting..."
  run_check "Code formatting" "dotnet format Synaxis.sln --verify-no-changes"
  ```
- Replace per-project test commands with solution-wide test:
  ```bash
  # Run all tests
  print_section "Running all tests..."
  run_check "All tests pass" "dotnet test Synaxis.sln --no-build --nologo -v q"
  ```
- Remove individual test project calls (lines 216-226).

### 3. Standardize Test Command
**Policy**: Use `dotnet test Synaxis.sln --no-build -p:Configuration=Release` (or fallback `dotnet test Synaxis.sln`).
**Rationale**: Ensures consistent configuration across local and CI runs.

### 4. Consider Adding Lint Job to CI
**Optional**: Implement the lint job described in workflows.md as a separate CI job or combine with formatting verification.

## Summary
The repository currently has inconsistent verification ordering. The AGENTS.md policy defines the canonical order, but CI and scripts deviate. Standardizing on `dotnet format Synaxis.sln --verify-no-changes → dotnet build Synaxis.sln -c Release -warnaserror → dotnet test Synaxis.sln --no-build` will align all verification paths.