# Documentation Archive Index

This archive contains historical documentation from the Synaxis project, organized chronologically by canonical date. All documentation predating the February 2026 comprehensive refactor has been preserved with full provenance.

## Quick Links

### Archive Artifacts
- [PROVENANCE.md](./PROVENANCE.md) - Complete source mapping and git history
- [CHANGELOG_BY_DATE.md](./CHANGELOG_BY_DATE.md) - Chronological change timeline
- [TRACEABILITY.md](./TRACEABILITY.md) - Cross-references between documents
- [OPEN_LOOPS.md](./OPEN_LOOPS.md) - Unresolved action items and TODOs
- [DRIFT_REPORT.md](./DRIFT_REPORT.md) - Analysis of plan vs. implementation drift
- [COMPONENTS.md](./COMPONENTS.md) - System component inventory

### Browse Archive
- [By Date](#browse-by-date) - Chronological view (2026-01-24 to 2026-02-03)
- [By Source](#browse-by-source) - Organized by original location

## Archive Overview

- **Total Files**: 103
- **Date Range**: January 24, 2026 - February 3, 2026
- **Archive Created**: February 4, 2026
- **Structure**: `docs/archive/2026/YYYY/MM/DD/`

## Browse by Date

### January 2026

#### [2026/01/24](./2026/01/24/) (2 files)
- Initial planning documents
- Synaplexer rebuild plan
- Full migration plan

#### [2026/01/25](./2026/01/25/) (1 file)
- OpenAI gateway roadmap

#### [2026/01/26](./2026/01/26/) (3 files)
- Docker compose infrastructure plans
- Verification health report
- Stream-native CQRS architecture

#### [2026/01/27](./2026/01/27/) (1 file)
- Security and quality remediation plan

#### [2026/01/28](./2026/01/28/) (4 files)
- Identity refactor plan
- Free providers implementation
- Microsoft agents integration
- Quality and safety plans

#### [2026/01/29](./2026/01/29/) (7 files)
- Frontend local-first architecture
- Webapp containerization
- Antigravity implementation
- Kilocode integration
- Ultra miser mode
- Smoke tests plan
- Dynamic model registry

### February 2026

#### [2026/02/02](./2026/02/02/) (83 files)

**Pre-Refactor Archive** (77 files)
Complete documentation snapshot before comprehensive refactor:
- **plan/** (15 files) - Implementation plans from docs/plan/
- **plans/** (4 files) - Architecture plans from docs/plans/
- **opencode-plans/** (8 files) - Antigravity and test stabilization plans
- **sisyphus/** (49 files) - Test results, benchmarks, coverage reports, scripts
- **README.md** (1 file) - Archive overview

**Docs Rebuild Archive** (6 files)
Documentation from the docs rebuild initiative:
- `API.md` - API documentation
- `ARCHITECTURE.md` - System architecture
- `CONFIGURATION.md` - Configuration guide
- `README.md` - Documentation overview
- `TESTING_SUMMARY.md` - Testing overview
- `adr/001-stream-native-cqrs.md` - ADR for stream-native CQRS

#### [2026/02/03](./2026/02/03/) (2 files)
- Sisyphus state (`boulder.json`)
- Documentation archive recreation plan

## Browse by Source

### Pre-Refactor Documentation (2026-02-02)

Historical documentation archived before the comprehensive refactor.

**Location**: `2026/02/02/docs_archive/2026-02-02-pre-refactor/`

#### Implementation Plans (`plan/` - 15 files)
- Ultra Miser Mode implementation
- Frontend local-first architecture
- Identity refactor
- Antigravity implementation
- Smoke tests
- Webapp containerization
- Kilocode integration
- Free providers
- Microsoft agents integration
- Comprehensive refactor plan
- And more...

#### Architecture Plans (`plans/` - 4 files)
- Stream-native CQRS architecture
- Security and quality remediation
- Dynamic model registry
- Quality and safety framework

#### OpenCode Plans (`opencode-plans/` - 8 files)
- Antigravity implementation (multiple versions)
- Antigravity verification
- Antigravity multi-account support
- Test stabilization summary

#### Sisyphus Testing Artifacts (`sisyphus/` - 49 files)

**Reports and Analysis**
- Final status report
- Final verification report
- Handoff summary
- Cleanup handoff summary
- Validation summary
- Security audit
- Error handling review
- Coverage gaps analysis
- Benchmark results
- Performance baseline
- Test architecture review

**Test Results**
- Smoke test results
- Flaky tests list
- Exit codes (raw, numbered, formatted)
- Baseline coverage (backend and frontend)
- Baseline flakiness metrics

**Configuration**
- Boulder state (`boulder.json`)
- Circuit breaker state
- Smoke debug configuration

**Scripts** (`sisyphus/scripts/` - 3 files)
- `verify-all.sh` - Complete verification suite
- `webapp-curl-tests.sh` - Frontend API tests
- `webapi-curl-tests.sh` - Backend API tests
- `run_smoke_tests.sh` - Smoke test runner

**Notepads** (`sisyphus/notepads/` - 11 files)
Learning notes and decisions from specific tasks:
- Synaxis enterprise stabilization
- Identity endpoints fix
- Responses endpoint fix
- WebAPI curl tests
- Controlplane tests
- Task 10.2
- Plan 9 learnings

**Plans** (`sisyphus/plans/` - 4 files)
- Comprehensive testing expansion
- Frontend enhancements
- Synaxis enterprise stabilization
- Performance test fixes

**Documentation** (`sisyphus/docs/` - 1 file)
- Test architecture review

**API Documentation**
- WebAPI endpoints documentation
- Webapp features documentation
- Webapp features gaps analysis

### Docs Rebuild Archive (2026-02-02)

Rebuilt documentation from February 2, 2026.

**Location**: `2026/02/02/docs_archive/2026-02-02-docs-rebuild/`

#### Core Documentation (5 files)
- `API.md` - Complete API reference
- `ARCHITECTURE.md` - System architecture overview
- `CONFIGURATION.md` - Configuration guide
- `TESTING_SUMMARY.md` - Testing strategy and coverage
- `README.md` - Documentation overview

#### Architecture Decision Records (`adr/` - 1 file)
- `001-stream-native-cqrs.md` - ADR for stream-native CQRS architecture

### Sisyphus Plans (2026-02-03)

Latest sisyphus state and planning.

**Location**: `2026/02/03/sisyphus/`

#### State Files (1 file)
- `boulder.json` - Current sisyphus task state

#### Plans (1 file)
- `plans/docs-archive-recreation.md` - Documentation archive recreation strategy

## File Type Breakdown

| Type | Count | Description |
|------|-------|-------------|
| Markdown (`.md`) | 87 | Plans, reports, documentation |
| Shell Scripts (`.sh`) | 3 | Test and verification scripts |
| JSON (`.json`) | 2 | Configuration and state files |
| Text (`.txt`) | 11 | Logs, coverage reports, exit codes |
| RC files (`.rc`) | 1 | Debug configuration |
| **Total** | **103** | |

## Archive Structure

```
docs/archive/
├── PROVENANCE.md           # This document's companion (source mapping)
├── INDEX.md                # This document (complete index)
├── CHANGELOG_BY_DATE.md    # Chronological change timeline
├── TRACEABILITY.md         # Cross-reference mapping
├── OPEN_LOOPS.md           # Unresolved action items
├── DRIFT_REPORT.md         # Plan vs. reality analysis
├── COMPONENTS.md           # System component inventory
└── 2026/
    ├── 01/
    │   ├── 24/  # 2 files
    │   ├── 25/  # 1 file
    │   ├── 26/  # 3 files
    │   ├── 27/  # 1 file
    │   ├── 28/  # 4 files
    │   └── 29/  # 7 files
    └── 02/
        ├── 02/  # 83 files
        │   ├── docs_archive/2026-02-02-pre-refactor/
        │   │   ├── plan/         # 15 files
        │   │   ├── plans/        # 4 files
        │   │   ├── opencode-plans/  # 8 files
        │   │   ├── sisyphus/     # 49 files
        │   │   └── README.md
        │   └── docs_archive/2026-02-02-docs-rebuild/
        │       ├── adr/          # 1 file
        │       ├── API.md
        │       ├── ARCHITECTURE.md
        │       ├── CONFIGURATION.md
        │       ├── README.md
        │       └── TESTING_SUMMARY.md
        └── 03/  # 2 files
            └── sisyphus/
                ├── boulder.json
                └── plans/        # 1 file
```

## Navigation Tips

1. **By Date**: Start from the date folders to see what was created/modified on specific days
2. **By Topic**: Use the source-based organization to find related documents
3. **By Type**: Reference the file type breakdown to locate specific artifacts
4. **Cross-References**: Use TRACEABILITY.md to find relationships between documents
5. **Action Items**: Check OPEN_LOOPS.md for unresolved TODOs and action items

## Archive Integrity

All files maintain their original content and structure. The archive is read-only and serves as a historical record. For current documentation, see the main `docs/` directory.

## Last Updated

February 4, 2026 - Archive creation
