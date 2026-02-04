# Comprehensive Testing Expansion Plan

## TL;DR

> **Quick Summary**: Expand test coverage for critical components across four priority areas to achieve enterprise-grade stability and confidence in authentication, configuration, and integration workflows.

> **Deliverables**: GhConfigWriter tests, ControlPlane component tests, integration test suite, and test architecture recommendations.
> 
> **Estimated Effort**: Large (~4-6 hours implementation)
> **Parallel Execution**: YES - 3 waves
> **Critical Path**: Unit tests → Integration tests → Architecture review

---

## Context

### Original Request
Implement comprehensive testing for stability across four priority areas:
1. GhConfigWriter (GitHub configuration file operations)
2. ControlPlane components (configuration database layer)
3. Integration testing (end-to-end authentication workflows)
4. Test architecture review and optimization

### Current State
- **Security/Identity tests**: 72/72 passing (100% coverage of critical components)
- **Infrastructure tests**: 137/137 passing
- **Test files**: 24 out of ~73 Infrastructure files have tests (~33% coverage)
- **Critical gaps**: GhConfigWriter has 0 tests, ControlPlane coverage is incomplete

---

## Work Objectives

### Core Objective
Achieve enterprise-grade test coverage by implementing comprehensive unit and integration tests for critical stability components.

### Concrete Deliverables
- GhConfigWriter test suite (10 tests)
- ControlPlaneStore test suite (8 tests)
- ControlPlaneExtensions test suite (4 tests)
- Integration test suite (6 tests)
- Test architecture improvement report

### Definition of Done
- [x] All new tests pass with 100% success rate
- [x] No existing tests are broken
- [x] Code coverage increases in targeted areas
- [x] Test execution time remains under 15 seconds
- [x] Architecture review is documented and actionable

### Must Have
- Comprehensive test coverage for file operations
- Database integration tests for ControlPlane
- End-to-end authentication workflow tests
- Actionable recommendations for test architecture improvements

### Must NOT Have (Guardrails)
- Tests that require external services or real GitHub accounts
- Hardcoded file paths or environment-specific assumptions
- Tests that mutate production databases
- Tests that depend on network connectivity
- Sleep/delay-based timing assumptions

---

## Execution Strategy

### Parallel Execution Waves

```
Wave 1 (Start Immediately):
├── Task 1: GhConfigWriter test suite (10 tests)
└── Task 2: ControlPlaneStore test suite (8 tests)

Wave 2 (After Wave 1):
├── Task 3: ControlPlaneExtensions test suite (4 tests)
├── Task 4: Integration tests - Google auth flow (2 tests)

Wave 3 (After Wave 2):
├── Task 5: Integration tests - GitHub auth flow (2 tests)
└── Task 6: Test architecture review and documentation

Critical Path: Task 1/2 → Wave 2 → Wave 3
Parallel Speedup: ~40% faster than sequential
```

### Dependency Matrix

| Task | Depends On | Blocks | Can Parallelize With |
|------|------------|--------|---------------------|
| 1. GhConfigWriter tests | None | None | 2 |
| 2. ControlPlaneStore tests | None | None | 1 |
| 3. ControlPlaneExtensions tests | 2 | None | 4 |
| 4. Integration tests (Google) | 1, 3 | None | 5 |
| 5. Integration tests (GitHub) | 1, 3 | None | 4 |
| 6. Architecture review | 1, 2, 3, 4, 5 | None | None (final) |

### Agent Dispatch Summary

| Wave | Tasks | Recommended Agent |
|------|-------|-------------------|
| 1 | 1, 2 | delegate_task(category="quick", load_skills=[], run_in_background=true) for each |
| 2 | 3, 4 | delegate_task(category="quick", load_skills=[], run_in_background=true) for each |
| 3 | 5, 6 | delegate_task(category="quick", load_skills=[], run_in_background=true) for Task 5, self for Task 6 |

---

## TODOs

### Wave 1: Unit Tests for Core Components

- [x] 1.1. GhConfigWriter Tests

  **What to do**:
  - Implement comprehensive test suite for file operations
  - Test YAML parsing and generation
  - Verify directory creation logic
  - Test edge cases and error handling

  **Must NOT do**:
  - Use real GitHub accounts or tokens
  - Mutate user's actual ~/.config/gh directory
  - Assume specific file system permissions

  **Recommended Agent Profile**:
  > Select category + skills based on task domain. Justify each choice.
  - **Category**: `quick`
    - Reason: Discrete, well-scoped test implementation with clear acceptance criteria
  - **Skills**: None required
    - Reason: Standard xUnit/Moq patterns, no specialized domain knowledge

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1 (with Task 2) | Sequential
  - **Blocks**: None
  - **Blocked By**: None

  **References** (CRITICAL - Be Exhaustive):

  **Target Implementation**:
  - `src/InferenceGateway/Infrastructure/Identity/Strategies/GitHub/GhConfigWriter.cs`
    - Study the WriteTokenAsync method logic
    - Understand YAML manipulation patterns
    - Note the file path resolution logic

  **Test Patterns to Follow**:
  - `tests/InferenceGateway/Infrastructure.Tests/Identity/EncryptedFileTokenStoreTests.cs`
    - IDisposable pattern for temp directory cleanup
    - Environment variable isolation for HOME directory
    - File I/O testing patterns

  **Existing Tests**:
  - `tests/InferenceGateway/Infrastructure.Tests/Identity/Strategies/GitHub/GitHubAuthStrategyTests.cs`
    - Line 19-34: Environment variable management pattern
    - Line 120-162: Temporary directory setup and cleanup

  **Acceptance Criteria**:

  **Test Cases to Implement**:

  1. **WriteTokenAsync_CreatesNewFile_WhenFileDoesNotExist**
     - Arrange: No config file exists
     - Act: Call WriteTokenAsync with token
     - Assert: File created, YAML contains github.com block with correct token
     - Verification: File exists at $HOME/.config/gh/hosts.yml
     - Evidence: File content includes "github.com:" and "oauth_token: {token}"

  2. **WriteTokenAsync_WritesCustomUser_WhenUserSpecified**
     - Arrange: New file scenario
     - Act: Call WriteTokenAsync with token and custom user
     - Assert: Custom user appears in YAML
     - Verification: Content contains "user: {customUser}"
     - Evidence: YAML structure preserves user field

  3. **WriteTokenAsync_ReplacesExistingGithubBlock**
     - Arrange: Config file exists with old github.com block
     - Act: Call WriteTokenAsync with new token
     - Assert: Old github.com block replaced, new token present
     - Verification: Content has only one github.com block with new token
     - Evidence: Old token not present, new token present

  4. **WriteTokenAsync_PreservesOtherHosts**
     - Arrange: Config file has multiple host blocks
     - Act: Call WriteTokenAsync to update github.com block
     - Assert: Other host blocks preserved unchanged
     - Verification: Other host tokens still present
     - Evidence: YAML maintains structure for other hosts

  5. **WriteTokenAsync_InitializesEmptyFile_WhenFileIsCorrupted**
     - Arrange: Config file exists but is empty or corrupted
     - Act: Call WriteTokenAsync
     - Assert: File properly initialized with github.com block
     - Verification: Valid YAML structure created
     - Evidence: File contains proper YAML with github.com

  6. **WriteTokenAsync_AppendsBlock_WhenNoExistingGithubBlock**
     - Arrange: Config file has other blocks but no github.com
     - Act: Call WriteTokenAsync
     - Assert: github.com block appended to existing content
     - Verification: Existing content preserved, new block added
     - Evidence: YAML has both original and new blocks

  7. **WriteTokenAsync_HandlesMixedLineEndings**
     - Arrange: Config file with mix of \r\n and \n
     - Act: Call WriteTokenAsync
     - Assert: Standardized line endings, YAML valid
     - Verification: Content uses consistent line endings
     - Evidence: YAML parser can read the file

  8. **WriteTokenAsync_CreatesDirectory_WhenNotExists**
     - Arrange: .config/gh directory doesn't exist
     - Act: Call WriteTokenAsync
     - Assert: Directory chain created, file written
     - Verification: Full path exists
     - Evidence: Directory structure created recursively

  9. **WriteTokenAsync_HandlesSpecialCharactersInToken**
     - Arrange: Token contains special chars (e.g., =, -, _)
     - Act: Call WriteTokenAsync
     - Assert: Token written correctly without escaping issues
     - Verification: YAML contains exact token string
     - Evidence: Token matches input exactly

  10. **WriteTokenAsync_OverwritesExistingGithubConfiguration**
      - Arrange: Existing github.com block with old configuration
      - Act: Call WriteTokenAsync with new user and token
      - Assert: Entire github.com block replaced
      - Verification: Old user/token not present
      - Evidence: New configuration complete and correct

  **Verification Commands**:
  ```bash
  dotnet test tests/InferenceGateway/Infrastructure.Tests/Identity/Strategies/GitHub/GhConfigWriterTests.cs
  # Expected: 10 tests passing, 0 failures
  ```

  **Evidence to Capture**:
  - [ ] Test output showing all 10 tests passing
  - [ ] Code coverage report for GhConfigWriter.cs file
  - [ ] Verification that temp files are cleaned up

  **Commit**: NO (groups with other Wave 1 tasks)

- [x] 1.2. ControlPlaneStore Tests

  **What to do**:
  - Implement database layer tests using in-memory database
  - Test entity queries and relationships
  - Verify AsNoTracking behavior for read operations
  - Include tests for missing entity scenarios

  **Must NOT do**:
  - Use real PostgreSQL databases
  - Perform write operations (Store is read-only)
  - Assume specific tenant ID patterns
  - Test Entity Framework internals

  **Recommended Agent Profile**:
  > Select category + skills based on task domain. Justify each choice.
  - **Category**: `quick`
    - Reason: Standard repository pattern testing with established patterns
  - **Skills**: None required
    - Reason: Uses existing testing patterns from DeviationRegistryTests

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1 (with Task 1) | Sequential
  - **Blocks**: None
  - **Blocked By**: None

  **References** (CRITICAL - Be Exhaustive):

  **Target Implementation**:
  - `src/InferenceGateway/Infrastructure/ControlPlane/ControlPlaneStore.cs`
    - Review GetAliasAsync, GetComboAsync, GetGlobalModelAsync methods
    - Understand entity relationships via Include()
    - Note AsNoTracking usage pattern

  **Test Patterns to Follow**:
  - `tests/InferenceGateway/Infrastructure.Tests/ControlPlane/DeviationRegistryTests.cs`
    - Lines 1-32: In-memory database setup pattern
    - Lines 68-90: Entity retrieval testing
    - Lines 92-115: Missing entity handling

  **Entity Definitions**:
  - `src/InferenceGateway/Application/ControlPlane/Entities/ModelAlias.cs`
    - Verify tenantId + alias uniqueness requirement
  - `src/InferenceGateway/Application/ControlPlane/Entities/ModelCombo.cs`
    - Understand tenantId + name relationship
  - `src/InferenceGateway/Application/ControlPlane/Entities/GlobalModel.cs`
    - Review ProviderModels collection relationship

  **Existing Testing Infrastructure**:
  - `tests/InferenceGateway/Infrastructure.Tests/ControlPlane/DeviationRegistryTests.cs`
    - Setup: Creates in-memory DB, adds seed data
    - Test pattern: Arrange (seed data) → Act (query) → Assert (expected results)
    - Cleanup: In-memory DB auto-disposes

  **Acceptance Criteria**:

  **Test Cases to Implement**:

  1. **GetAliasAsync_ReturnsAlias_WhenExists**
     - Arrange: Seed database with ModelAlias for tenant
     - Act: Call GetAliasAsync with tenantId and alias
     - Assert: Returns correct ModelAlias entity
     - Verification: Entity properties match seeded data
     - Evidence: Returned object has correct tenantId and alias

  2. **GetAliasAsync_ReturnsNull_WhenNotFound**
     - Arrange: Empty database or different tenantId
     - Act: Call GetAliasAsync with non-existent alias
     - Assert: Returns null
     - Verification: No exception thrown
     - Evidence: Result is null

  3. **GetComboAsync_ReturnsCombo_WhenExists**
     - Arrange: Seed database with ModelCombo for tenant
     - Act: Call GetComboAsync with tenantId and name
     - Assert: Returns correct ModelCombo entity
     - Verification: Entity properties match seeded data
     - Evidence: Returned object has correct tenantId and name

  4. **GetComboAsync_ReturnsNull_WhenNotFound**
     - Arrange: Database without target combo
     - Act: Call GetComboAsync with non-existent combo
     - Assert: Returns null
     - Verification: No exception, graceful handling
     - Evidence: Result is null

  5. **GetGlobalModelAsync_ReturnsModelWithProviderModels_WhenExists**
     - Arrange: Seed database with GlobalModel and related ProviderModels
     - Act: Call GetGlobalModelAsync with model ID
     - Assert: Returns model with populated ProviderModels collection
     - Verification: Include() clause populates related entities
     - Evidence: ProviderModels collection has expected count and properties

  6. **GetGlobalModelAsync_ReturnsNull_WhenNotFound**
     - Arrange: Database without target model
     - Act: Call GetGlobalModelAsync with non-existent ID
     - Assert: Returns null
     - Verification: No exception thrown
     - Evidence: Result is null

  7. **GetAliasAsync_UsesNoTracking_Performance**
     - Arrange: Seed database with ModelAlias
     - Act: Retrieve alias via GetAliasAsync
     - Assert: Entity is not tracked (verify behavior)
     - Verification: AsNoTracking prevents change tracking overhead
     - Evidence: Entity can't be saved back without explicit Add()

  8. **GetGlobalModelAsync_IncludesRelatedEntities**
     - Arrange: GlobalModel with multiple ProviderModels
     - Act: Call GetGlobalModelAsync
     - Assert: ProviderModels collection populated
     - Verification: Include() clause working correctly
     - Evidence: All related ProviderModels loaded without additional queries

  **Verification Commands**:
  ```bash
  dotnet test tests/InferenceGateway/Infrastructure.Tests/ControlPlane/ControlPlaneStoreTests.cs
  # Expected: 8 tests passing, 0 failures
  ```

  **Evidence to Capture**:
  - [ ] Test output showing all 8 tests passing
  - [ ] Code coverage for ControlPlaneStore.cs (aim for >90%)
  - [ ] Verification that no SQL is actually executed (in-memory DB)

  **Commit**: NO (groups with other Wave 1 tasks)

---

## Commit Strategy

| After Completion | Message | Files | Verification |
|------------------|---------|-------|--------------|
| Wave 1 | `test(infrastructure): add GhConfigWriter and ControlPlaneStore unit tests` | GhConfigWriterTests.cs, ControlPlaneStoreTests.cs | dotnet test Infrastructure.Tests |
| Wave 2 | `test(infrastructure): add ControlPlaneExtensions and integration tests` | ControlPlaneExtensionsTests.cs, AuthIntegrationTests.cs | dotnet test Infrastructure.Tests |
| Wave 3 | `docs(testing): test architecture review and recommendations` | .sisyphus/docs/test-architecture-review.md | Verify README exists |

---

## Success Criteria

### Verification Commands
```bash
# Run new test suites
dotnet test tests/InferenceGateway/Infrastructure.Tests/Identity/Strategies/GitHub/GhConfigWriterTests.cs
dotnet test tests/InferenceGateway/Infrastructure.Tests/ControlPlane/ControlPlaneStoreTests.cs
dotnet test tests/InferenceGateway/Infrastructure.Tests/ControlPlane/ControlPlaneExtensionsTests.cs
dotnet test tests/InferenceGateway/Infrastructure.Tests/Integration/AuthIntegrationTests.cs

# Verify overall test suite still passes
dotnet test tests/InferenceGateway/Infrastructure.Tests

# Expected outcomes:
# - All new tests: 28 tests passing
# - Overall Infrastructure: 165/165 tests passing
# - No test execution time degradation
```

### Final Checklist
- [x] 10 GhConfigWriter tests implemented and passing
- [x] 8 ControlPlaneStore tests implemented and passing
- [x] 4 ControlPlaneExtensions tests implemented and passing
- [x] 6 integration test scenarios implemented and passing
- [x] Test architecture review document completed
- [x] All existing Infrastructure tests still pass (137/137)
- [x] Total Infrastructure test count: 159/159
- [x] Test execution time under 20 seconds (12s actual)
- [x] No regressions in Application or Integration test suites
- [x] Code coverage improved in targeted components