# Task Completion Checklist

## Before Committing
1. [ ] Build passes: `dotnet build Synaxis.sln -warnaserror`
2. [ ] All tests pass: `dotnet test Synaxis.sln --no-build`
3. [ ] Format verified: `dotnet format --verify-no-changes`
4. [ ] Slopwatch clean: `slopwatch analyze --fail-on warning`

## Code Quality Gates
- [ ] No empty catch blocks
- [ ] No Task.Delay in tests (use TaskCompletionSource)
- [ ] No #pragma warning disable without matching restore
- [ ] No NoWarn in project files
- [ ] No commented-out code
- [ ] Proper XML docs for public APIs

## Git Workflow
1. [ ] Check status: `git status`
2. [ ] Stage changes: `git add -A`
3. [ ] Commit: `git commit -m "type: description"`
4. [ ] Sync beads: `bd sync --from-main`

## Beads Tracking
1. [ ] Issue created: `bd create --title="..." --description="..." --type=task --priority=2`
2. [ ] Issue claimed: `bd update <id> --status in_progress`
3. [ ] Issue completed: `bd close <id> --reason "..."`

## Commit Message Format
```
type: Subject line

Body explaining what changed and why.

Refs: Synaxis-xxx
```

Types: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`

## Verification Commands Summary
```bash
# Format verification:
dotnet format --verify-no-changes

# Build (warnings as errors):
dotnet build Synaxis.sln -warnaserror

# Test:
dotnet test Synaxis.sln --no-build

# Slopwatch:
slopwatch analyze --fail-on warning
```

## Critical Rules
- **NEVER** commit with build errors or test failures
- **NEVER** use Task.Delay in tests
- **NEVER** add warning suppressions
- **ALWAYS** run full verification before claiming completion
- **ALWAYS** close beads issue with reason
