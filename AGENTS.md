# AGENT POLICY (RFC 2119)

## 1. Normative Language

The key words "MUST", "MUST NOT", "REQUIRED", "SHALL", "SHALL NOT", "SHOULD", "SHOULD NOT", "RECOMMENDED", "NOT RECOMMENDED", "MAY", and "OPTIONAL" are to be interpreted as described in RFC 2119.

## 2. Policy Hierarchy

1. Repository safety, correctness, and security policies in this file are highest priority and MUST be enforced.
2. Explicit user instructions MUST be followed unless they conflict with this file.
3. Existing repository conventions SHOULD be followed when not in conflict with (1) or (2).
4. In case of conflict, the agent MUST choose the stricter interpretation and MUST document the conflict in the final output.

## 3. Issue Tracking Policy (beads)

1. All non-trivial work MUST be tracked with `bd`.
2. Agents MUST use `bd ... --json` for machine-readable operations.
3. Agents MUST NOT start non-trivial code changes before an issue is set to `in_progress`.
4. Agents MUST create linked follow-up issues for discovered work before completion (`discovered-from`).
5. Agents MUST NOT close tracked work until verification evidence is present.
6. Agents MUST NOT use markdown TODO files or alternate trackers for active work.

## 4. Zero-Tolerance Completion Policy

1. Agents MUST NOT claim completion when any REQUIRED verification command has not run.
2. Agents MUST NOT claim completion when any build warning exists.
3. Agents MUST NOT claim completion when any build error exists.
4. Agents MUST NOT claim completion when any test fails.
5. Agents MUST NOT claim completion when REQUIRED tests are unimplemented for behavioral changes.
6. Agents MUST NOT claim completion when REQUIRED tests were not executed.
7. Agents MAY report `NOT VERIFIED` only when execution is technically blocked and the blocker is explicitly documented.

## 5. Stop Conditions

On any condition below, the agent MUST stop forward progress, fix root cause, then rerun all REQUIRED verification commands:

1. Any compiler warning.
2. Any compiler error.
3. Any test failure.
4. Any flaky or intermittent test behavior.
5. Any analyzer or formatting violation required by policy.

The agent MUST NOT proceed to new tasks while stop conditions remain unresolved for the current change.

## 6. Verification Matrix

### 6.1 Required Commands

Run from repository root:

```bash
dotnet format --verify-no-changes
dotnet build <Solution.sln> -warnaserror
dotnet test <Solution.sln> --no-build
```

If artifacts are missing:

```bash
dotnet test <Solution.sln>
```

`<Solution.sln>` MUST be replaced with the CI-primary solution or impacted solution when multiple solutions exist.

### 6.2 Change-Type Requirements

1. Code change: format + build + test REQUIRED.
2. Test-only change: format + build + test REQUIRED.
3. Behavioral code change: test implementation/update REQUIRED and execution REQUIRED.
4. Docs-only change: verification SHOULD run; if not run, final output MUST be `NOT VERIFIED (docs-only)`.

### 6.3 Acceptance Criteria

Verification is valid only when:

1. `dotnet format --verify-no-changes` succeeds.
2. `dotnet build ... -warnaserror` succeeds with zero warnings and zero errors.
3. `dotnet test ...` succeeds with zero failing tests.

## 7. Mandatory Test Implementation and Execution

1. For behavioral changes, tests MUST be added or updated in the same change set.
2. If tests cannot be added due to hard constraints, the agent MUST:
   - document the constraint,
   - create a linked follow-up issue,
   - mark output `NOT VERIFIED` unless waived by maintainer.
3. Relevant test suites MUST be executed before completion claims.
4. Any failing test is release-blocking for that change.

## 8. Test Runner and Forbidden Test Flags

1. `dotnet test` is the only compliant test runner.
2. Agents MUST NOT use alternate runners to satisfy required verification.
3. The following flags are strictly forbidden in test commands, scripts, and CI snippets:
   - `--maxcpucount:1`
   - `-maxcpucount:1`
   - `-nodeReuse:false`
   - `-nodereuse:false`
   - `/nr:false`
4. Agents MUST NOT serialize or alter runtime semantics to mask race conditions or flaky tests.
5. Agents MAY add diagnostics without altering execution semantics (for example, `--logger` or `--blame-hang`).

## 9. Risk Tiers and Change Control

### 9.1 Risk Tiers

1. Low: isolated/internal/non-breaking.
2. Medium: cross-module, non-critical API, moderate blast radius.
3. High: auth, security, billing, data model, public API contracts, migrations, or production reliability paths.

### 9.2 Required Controls

1. Low risk MUST include verification evidence.
2. Medium risk MUST include impact statement and rollback plan.
3. High risk MUST include impact statement, rollback plan, data/backward-compatibility plan, and observability checks.

## 10. Exception Policy

1. Policy exceptions MUST be explicit, time-bound, and approved by repository maintainers.
2. Each exception MUST include scope, rationale, owner, expiry, and mitigation.
3. Each exception MUST have a linked tracking issue.
4. Expired exceptions MUST be treated as violations.

## 11. Security and Supply Chain Policy

1. Secrets MUST NOT be committed.
2. Sensitive values MUST NOT be logged.
3. Package sources MUST be explicit and trusted.
4. Dependency versions SHOULD be centrally managed and deterministic.
5. Lockfile/restore determinism policies used by CI MUST be preserved.
6. Agents MUST NOT bypass restore or integrity checks to force green builds.

## 12. Data Governance Policy

1. Data handling MUST follow least-privilege and minimal-exposure principles.
2. Production-like sensitive data MUST NOT be used in tests.
3. Logs and test artifacts MUST redact secrets, tokens, credentials, and personal data.
4. New data fields SHOULD include classification and retention impact when applicable.

## 13. Multi-Tenancy Policy

1. All components MUST support multi-tenant architecture.
2. Tenant isolation is REQUIRED at database, cache, and API levels.
3. Tenant context MUST be propagated through all service layers.
4. Resources MUST be scoped to tenant (no global shared state).
5. Tenant resolution MUST happen at API gateway/middleware level.
6. Cross-tenant data access MUST be explicitly authorized.
7. Database queries MUST include tenant filtering.
8. Cache keys MUST include tenant identifier.
9. Logs and metrics MUST include tenant attribution.
10. All new features MUST be designed with multi-tenancy from inception.

## 14. Migration Policy

1. Schema/data migrations MUST be forward-safe.
2. Destructive migrations MUST include phased rollout/backward compatibility plan.
3. Migration changes MUST include validation and rollback strategy.
4. Agents MUST NOT claim completion for migration work without migration verification evidence.

## 15. Observability Policy

1. Changed execution paths SHOULD preserve or improve structured logging and diagnostics.
2. High-risk changes MUST include explicit observability checks in verification output.
3. Error paths MUST produce actionable signals without leaking sensitive data.

## 16. Performance Policy

1. Agents MUST NOT introduce known regressions in hot paths.
2. High-risk performance-impacting changes SHOULD include baseline vs. new evidence.
3. Agents MUST document intentional performance tradeoffs.

## 17. Mandatory Output Schema

Final outputs for non-trivial changes MUST contain:

```text
Risk Tier: LOW|MEDIUM|HIGH
Change Scope: <one-line scope>

Verification Evidence
- Command: <exact command>
  Result: PASS|FAIL
  Notes: <optional>

Test Implementation
- Required: YES|NO
- Implemented/Updated: YES|NO
- Executed: YES|NO

Stop Conditions
- Present: YES|NO
- Resolution: <summary or N/A>

Security and Data
- Secrets Exposed: NO|YES
- Sensitive Data Handling Reviewed: YES|NO

Migrations
- Applicable: YES|NO
- Verified: YES|NO

Completion Status: COMPLETE|NOT VERIFIED
```

## 18. Canonical Definition of Done

A change is "COMPLETE" if and only if all conditions below are true:

1. Required issue tracking state transitions are satisfied.
2. Required tests were implemented/updated for behavioral changes.
3. Required verification commands were executed.
4. Build has zero warnings and zero errors.
5. Tests have zero failures.
6. No active stop conditions remain.
7. Mandatory output schema is fully populated.

If any condition is false, status MUST be `NOT VERIFIED` and completion MUST NOT be claimed.

## 19. .NET 10 Baseline

1. Active development paths MUST target .NET 10 toolchain and project settings.
2. `global.json` SHOULD be pinned to an approved .NET 10 SDK band.
3. CI-equivalent behavior MUST be preserved locally (no policy weakening for local success).

## 20. Maintainer-Owned Repository Conventions

Architecture and repository-specific conventions MAY be maintained in dedicated maintainer sections or separate docs and SHOULD remain consistent with this policy.

## 21. Code Generation Standards

### 21.1 Solution-Wide Configuration

The solution SHALL use centralized configuration in `Directory.Build.props`:

| Setting | Value | Location |
|---------|-------|----------|
| Nullable | enable | Directory.Build.props |
| TreatWarningsAsErrors | true | Directory.Build.props |
| ImplicitUsings | enable | Directory.Build.props |
| GenerateDocumentationFile | true | Directory.Build.props |

Agents MUST NOT:
- Add `#nullable enable` directives to individual files
- Add `<Nullable>` properties to individual .csproj files
- Override solution-wide settings in project files

### 21.2 Namespace Declaration

Agents SHALL use file-scoped namespaces (C# 10+):

```csharp
// CORRECT - File-scoped
using System;
using System.Collections.Generic;

namespace Synaxis.Features.Authentication;

public class AuthService
{
    // Implementation
}
```

Agents MUST NOT use block-scoped namespaces:

```csharp
// INCORRECT - Block-scoped
namespace Synaxis.Features.Authentication
{
    using System;
    
    public class AuthService
    {
        // Implementation
    }
}
```

**Rationale:** File-scoped namespaces reduce indentation and are the modern C# standard.

### 21.3 File Structure Template

All generated C# files MUST follow this structure:

```csharp
// <copyright file="[TypeName].cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using System;
// Additional usings at top

namespace [Namespace];  // File-scoped

/// <summary>
/// [Type description]
/// </summary>
public class [TypeName]
{
    // Implementation
}
```

### 21.4 Type Definition Constraints

Agents MUST ensure:

1. **Unique Type Definitions** - Each type SHALL exist in exactly one file
2. **File Naming** - File name MUST match primary type name (e.g., `AuthService.cs` for `class AuthService`)
3. **No Nested Types in Public APIs** - Public types SHOULD NOT be nested; use separate files
4. **Collection Interfaces** - Public APIs MUST return interface types:
   - `IList<T>` instead of `List<T>`
   - `IDictionary<K,V>` instead of `Dictionary<K,V>`
   - `ISet<T>` instead of `HashSet<T>`

### 21.5 Pre-Generation Verification

Before generating code, Agents SHALL verify:

- [ ] Target file does not already exist
- [ ] Type name is not already defined in solution
- [ ] File name follows TypeName.cs convention
- [ ] Namespace is appropriate for location

Command to check for duplicates:
```bash
find src -name "*.cs" -exec grep -l "class $TYPENAME\|interface $TYPENAME" {} \;
```

## 22. Progressive Validation Protocol

### 22.1 Batch Size Limits

Agents SHALL generate code in batches not exceeding:

| Metric | Maximum |
|--------|---------|
| Files per batch | 10 |
| Lines per batch | 500 |
| Types per batch | 5 |

Between batches, Agents MUST run:

```bash
dotnet build <project> 2>&1 | grep -E "error (CS|SA|MA)" | wc -l
```

### 22.2 Error Thresholds

Agents SHALL treat these error counts as stop conditions:

| Error Count | Required Action |
|-------------|-----------------|
| 1-10 | Fix immediately; document root cause |
| 11-20 | Stop; reassess generation approach |
| 21-50 | Emergency stop; notify maintainer |
| 51+ | HALT; do not proceed without approval |

### 22.3 Quality Gates

For each batch, Agents MUST verify:

**Gate 1: Compilation**
```bash
dotnet build <project> 2>&1 | grep -c "error CS"
# MUST equal 0
```

**Gate 2: Style**
```bash
dotnet format style <project> --verify-no-changes
# MUST succeed
```

**Gate 3: No Duplicates**
```bash
# Check no types were duplicated
dotnet build <Solution.sln> 2>&1 | grep -c "error CS0101"
# MUST equal 0
```

### 22.4 Checkpoint Documentation

When stopping at a checkpoint, Agents SHALL report:

```
Checkpoint: [Number]
Files Generated: [Count]
Errors: [Count] (Breakdown by rule)
Status: [Proceed / Stop / Escalate]
Action: [Fix / Regenerate / Escalate]
```

## 23. NoWarn Absolute Prohibition

### 23.1 Prohibition Statement

Agents are **ABSOLUTELY FORBIDDEN** from adding, modifying, or using `<NoWarn>` elements in any form.

This prohibition includes:
- `<NoWarn>` properties in .csproj files
- `<WarningsNotAsErrors>` properties  
- `<TreatWarningsAsErrors>false</TreatWarningsAsErrors>`
- `#pragma warning disable` directives (except as noted in 23.2)
- Any mechanism that suppresses analyzer warnings

### 23.2 Exception for Pragma Directives

The ONLY permitted suppression mechanism is `#pragma` directives in source code, and ONLY when:

1. A beads issue documents the specific warning, justification, and expiry date
2. The pragma is localized to the specific line requiring suppression
3. The pragma includes a comment explaining why suppression is necessary

**Permitted Format:**
```csharp
#pragma warning disable SA1101 // Instance call doesn't need 'this.' for fluent API consistency
_logger.LogInformation("Message");
#pragma warning restore SA1101
```

### 23.3 Enforcement

Any Agent adding NoWarn SHALL:
1. Immediately revert the change
2. Fix the underlying analyzer violation
3. Create incident report documenting the violation
4. Not claim completion until violation is remediated

## 24. Subagent Quality Contracts

### 24.1 Delegation Requirements

When delegating code generation to subagents, Agents SHALL provide:

1. **Explicit Template** with file-scoped namespace structure
2. **Quality Threshold** - Maximum 0 errors per file
3. **Verification Command** - Exact build command to run
4. **Rejection Criteria** - Specific conditions for rejecting output

### 24.2 Subagent Template

```csharp
// TEMPLATE - Subagent MUST use this structure:
// <copyright file="{{TYPE_NAME}}.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using System;
{{ADDITIONAL_USINGS}}

namespace {{NAMESPACE}};  // File-scoped - REQUIRED

/// <summary>
/// {{TYPE_DESCRIPTION}}
/// </summary>
public {{TYPE_KIND}} {{TYPE_NAME}}
{
    // Implementation following style rules:
    // - Prefix local calls with this. (SA1101)
    // - Return interface types (MA0016)
    // - Methods under 60 lines (MA0051)
    // - Trailing commas in initializers (SA1413)
}
```

### 24.3 Acceptance Criteria

Subagent output SHALL be rejected if:

- [ ] Contains NoWarn suppressions
- [ ] Has >0 analyzer errors
- [ ] Uses block-scoped namespaces
- [ ] Missing file headers
- [ ] Missing usings
- [ ] Duplicate type definitions
- [ ] Methods >60 lines without explicit justification

### 24.4 Verification Command

Subagents MUST run and report results:

```bash
dotnet build {{PROJECT}} 2>&1 | tee build.log | grep -E "error [A-Z]+[0-9]+:" | wc -l
# MUST report: 0
```

## 25. Code Accumulation Prevention

### 25.1 Error Budget

Agents SHALL track cumulative analyzer errors introduced during a session.

**Budget:**
- Initial error count: [Record at session start]
- Maximum increase: 20 errors per hour of work
- Hard limit: 50 total errors

### 25.2 Error Tracking

Agents SHALL report error delta with each significant change:

```
Session: [ID]
Start Errors: [N]
Current Errors: [N]
Delta: [+/-N]
Files Changed: [N]
Status: [Within budget / Exceeded budget]
```

### 25.3 Exceeding Budget

When error count exceeds 50, Agents SHALL:

1. HALT all code generation
2. Not proceed with new features
3. Create beads issue: "Analyzer Error Remediation Required"
4. Document:
   - Error count by rule
   - Files most affected
   - Root cause (e.g., missing templates, unchecked duplicates)
5. Request maintainer direction

### 25.4 Prevention Checklist

Before each generation task, Agents SHALL verify:

- [ ] Solution has Directory.Build.props with Nullable=enable
- [ ] Template enforces file-scoped namespaces
- [ ] Template includes file header
- [ ] Duplicate detection command ready
- [ ] Error budget tracking active
