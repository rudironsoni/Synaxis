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

## 13. Migration Policy

1. Schema/data migrations MUST be forward-safe.
2. Destructive migrations MUST include phased rollout/backward compatibility plan.
3. Migration changes MUST include validation and rollback strategy.
4. Agents MUST NOT claim completion for migration work without migration verification evidence.

## 14. Observability Policy

1. Changed execution paths SHOULD preserve or improve structured logging and diagnostics.
2. High-risk changes MUST include explicit observability checks in verification output.
3. Error paths MUST produce actionable signals without leaking sensitive data.

## 15. Performance Policy

1. Agents MUST NOT introduce known regressions in hot paths.
2. High-risk performance-impacting changes SHOULD include baseline vs. new evidence.
3. Agents MUST document intentional performance tradeoffs.

## 16. Mandatory Output Schema

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

## 17. Canonical Definition of Done

A change is "COMPLETE" if and only if all conditions below are true:

1. Required issue tracking state transitions are satisfied.
2. Required tests were implemented/updated for behavioral changes.
3. Required verification commands were executed.
4. Build has zero warnings and zero errors.
5. Tests have zero failures.
6. No active stop conditions remain.
7. Mandatory output schema is fully populated.

If any condition is false, status MUST be `NOT VERIFIED` and completion MUST NOT be claimed.

## 18. .NET 10 Baseline

1. Active development paths MUST target .NET 10 toolchain and project settings.
2. `global.json` SHOULD be pinned to an approved .NET 10 SDK band.
3. CI-equivalent behavior MUST be preserved locally (no policy weakening for local success).

## 19. Maintainer-Owned Repository Conventions

Architecture and repository-specific conventions MAY be maintained in dedicated maintainer sections or separate docs and SHOULD remain consistent with this policy.
