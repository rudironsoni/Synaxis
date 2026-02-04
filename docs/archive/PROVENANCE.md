# Archive Provenance

This document provides complete traceability for all archived documentation, including original locations, archival dates, and git commit history.

## Overview

- **Total Files**: 103
- **Date Range**: 2026-01-24 to 2026-02-03
- **Archive Created**: 2026-02-04
- **Archive Commit**: 40b36173dd6bd054b8a11337f1b9495b9050e713

## Source Root Mapping

### Pre-Refactor Documentation (2026-02-02)

Historical documentation archived before the comprehensive refactor on February 2, 2026.

| Original Path | Archived Path | Canonical Date | File Count |
|---------------|---------------|----------------|------------|
| `docs/archive/2026-02-02-pre-refactor/plan/` | `2026/02/02/docs_archive/2026-02-02-pre-refactor/plan/` | 2026-02-02 | 15 |
| `docs/archive/2026-02-02-pre-refactor/plans/` | `2026/02/02/docs_archive/2026-02-02-pre-refactor/plans/` | 2026-02-02 | 4 |
| `docs/archive/2026-02-02-pre-refactor/opencode-plans/` | `2026/02/02/docs_archive/2026-02-02-pre-refactor/opencode-plans/` | 2026-02-02 | 8 |
| `docs/archive/2026-02-02-pre-refactor/sisyphus/` | `2026/02/02/docs_archive/2026-02-02-pre-refactor/sisyphus/` | 2026-02-02 | 49 |
| `docs/archive/2026-02-02-pre-refactor/notepads/` | `2026/02/02/docs_archive/2026-02-02-pre-refactor/notepads/` | 2026-02-02 | 0 |
| `docs/archive/2026-02-02-pre-refactor/README.md` | `2026/02/02/docs_archive/2026-02-02-pre-refactor/README.md` | 2026-02-02 | 1 |

**Git History:**
- First Commit: `04c9035e04779f05bb5c6ffdfd04ed1c4d8970be` (2026-02-02)
- Last Commit: `8e89e75bf6d8c532054fc97e974decf8b0a6d610` (2026-02-02)
- Commit Message: "feat(webapi): add admin and user controllers with middleware and tests"

### Docs Rebuild Archive (2026-02-02)

Documentation from the docs rebuild initiative on February 2, 2026.

| Original Path | Archived Path | Canonical Date | File Count |
|---------------|---------------|----------------|------------|
| `docs/archive/2026-02-02-docs-rebuild/` | `2026/02/02/docs_archive/2026-02-02-docs-rebuild/` | 2026-02-02 | 6 |
| `docs/archive/2026-02-02-docs-rebuild/adr/` | `2026/02/02/docs_archive/2026-02-02-docs-rebuild/adr/` | 2026-02-02 | 1 (included) |

**Contents:**
- `API.md` - API documentation
- `ARCHITECTURE.md` - System architecture
- `CONFIGURATION.md` - Configuration guide
- `README.md` - Archive overview
- `TESTING_SUMMARY.md` - Testing overview
- `adr/001-stream-native-cqrs.md` - Architecture Decision Record

**Git History:**
- First Commit: `7af10e366761b143afc02eeed6d79d9862019b1c` (2026-02-02)
- Last Commit: `0c1b62f30f0576fb544a0ccc687d339b4035b103` (2026-02-02)
- Commit Message: "docs: archive legacy documentation to 2026-02-02-docs-rebuild"

### Sisyphus Plans (2026-02-03)

Plans and state from the sisyphus directory on February 3, 2026.

| Original Path | Archived Path | Canonical Date | File Count |
|---------------|---------------|----------------|------------|
| `sisyphus/boulder.json` | `2026/02/03/sisyphus/boulder.json` | 2026-02-03 | 1 |
| `sisyphus/plans/` | `2026/02/03/sisyphus/plans/` | 2026-02-03 | 1 |

**Contents:**
- `boulder.json` - Sisyphus state
- `plans/docs-archive-recreation.md` - Documentation archive recreation plan

## Chronological Archive Structure

### January 2026

| Date | Path | Files | Description |
|------|------|-------|-------------|
| 2026-01-24 | `2026/01/24/docs_archive/2026-02-02-pre-refactor/plan/` | 2 | Initial planning documents |
| 2026-01-25 | `2026/01/25/docs_archive/2026-02-02-pre-refactor/plan/` | 1 | OpenAI gateway roadmap |
| 2026-01-26 | `2026/01/26/docs_archive/2026-02-02-pre-refactor/plan/` | 2 | Docker compose infrastructure plans |
| 2026-01-26 | `2026/01/26/docs_archive/2026-02-02-pre-refactor/plans/` | 1 | Stream-native CQRS architecture |
| 2026-01-27 | `2026/01/27/docs_archive/2026-02-02-pre-refactor/plan/` | 1 | Security and quality remediation |
| 2026-01-28 | `2026/01/28/docs_archive/2026-02-02-pre-refactor/plan/` | 4 | Identity refactor, free providers, Microsoft agents, quality plans |
| 2026-01-29 | `2026/01/29/docs_archive/2026-02-02-pre-refactor/plan/` | 7 | Multiple implementation plans including frontend, webapp, kilocode, ultra miser |

### February 2026

| Date | Path | Files | Description |
|------|------|-------|-------------|
| 2026-02-02 | `2026/02/02/docs_archive/2026-02-02-pre-refactor/` | 77 | Complete pre-refactor archive including plan, plans, opencode-plans, sisyphus |
| 2026-02-02 | `2026/02/02/docs_archive/2026-02-02-docs-rebuild/` | 6 | Rebuilt documentation from docs rebuild initiative |
| 2026-02-03 | `2026/02/03/sisyphus/` | 2 | Sisyphus state and plans |

## Summary Statistics

- **Total Markdown Files**: 87
- **Total Script Files**: 3 (shell scripts)
- **Total Configuration Files**: 2 (JSON)
- **Total Text Files**: 11 (coverage, logs, exit codes)
- **Total Directories**: 54
- **Date Span**: 11 days (2026-01-24 to 2026-02-03)

## Archive Integrity

All files have been archived under their canonical date structure at `docs/archive/2026/YYYY/MM/DD/`. The chronological organization preserves the temporal context of documentation evolution while maintaining source root organization within each date folder.

## Related Artifacts

- [INDEX.md](./INDEX.md) - Complete archive index
- [CHANGELOG_BY_DATE.md](./CHANGELOG_BY_DATE.md) - Change timeline
- [TRACEABILITY.md](./TRACEABILITY.md) - Cross-reference mapping
- [OPEN_LOOPS.md](./OPEN_LOOPS.md) - Unresolved action items
- [DRIFT_REPORT.md](./DRIFT_REPORT.md) - Plan vs. reality analysis
- [COMPONENTS.md](./COMPONENTS.md) - System component inventory
