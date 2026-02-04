# Documentation Archive and Recreation

## TL;DR

> **Complete rebuild of all project documentation with new structure**
> 
> **Deliverables**: 17 new documents (8 core + 3 reference + 3 ADR + 3 operational), archived legacy docs
> - Archive: Move all current docs to `docs/archive/2026-02-02-docs-rebuild/`
> - Recreate: Core docs → Reference docs → ADRs → Operational docs
> 
> **Estimated Effort**: Large (4 waves, 17 files, ~1500 lines total)
> **Parallel Execution**: YES - 4 sequential waves with parallel tasks within waves
> **Critical Path**: Wave 1 (README → ARCHITECTURE → API) → Wave 2 → Wave 3 → Wave 4

---

## Context

### Original Request
Archive all documentation at @docs/ and recreate from scratch with complete new structure, keeping ULTRA MISER MODE™ personality.

### Interview Summary
**Key Decisions**:
- **Intent**: Complete rebuild with new structure (not just reorganization)
- **Scope**: All project documentation (docs/, root-level, .sisyphus/)
- **Standards**: Keep current ULTRA MISER MODE™ personality and humor

**Research Findings**:
- **Current docs**: 5 active files (1,490 lines total), 62 already archived
- **Architecture**: .NET 10 backend (198 files), React 19 frontend (46 files), Clean Architecture
- **Features**: 10+ AI providers, tiered routing, streaming SSE, OAuth, local-first frontend
- **Missing**: SECURITY.md, DEPLOYMENT.md, CONTRIBUTING.md, troubleshooting, monitoring docs

### Metis-Inspired Gap Analysis (Applied)
**Guardrails Set**:
- Archive strategy: Preserve all existing content in dated directory with README index
- Recreation order: Core → Reference → ADR → Operational (dependency-based)
- Personality enforcement: All docs must include at least one ULTRA MISER MODE™ reference
- Cross-link strategy: Relative links only, validate after all docs created
- Commit strategy: One commit per wave (not per file) to reduce noise

---

## Work Objectives

### Core Objective
Archive all current project documentation and recreate a complete, well-structured documentation suite from scratch while maintaining the project's ULTRA MISER MODE™ personality.

### Concrete Deliverables
**Tier 1 - Core Documentation (docs/ root)**:
1. `README.md` (root) - Project overview, quick start, ULTRA MISER MODE™ intro
2. `ARCHITECTURE.md` - Clean Architecture deep dive with diagrams
3. `API.md` - OpenAI-compatible API reference (reorganized, ~600 lines)
4. `CONFIGURATION.md` - Provider setup and configuration
5. `DEPLOYMENT.md` - Docker, infrastructure, environment setup (NEW)
6. `SECURITY.md` - Authentication, authorization, security practices (NEW)
7. `TESTING.md` - Test strategy, running tests, contributing tests (NEW)
8. `CONTRIBUTING.md` - Development setup, PR process, guidelines (NEW)

**Tier 2 - Reference Documentation (docs/reference/)**:
9. `reference/providers.md` - Provider-specific details and capabilities
10. `reference/models.md` - Supported models and feature matrix
11. `reference/errors.md` - Error codes, troubleshooting, debugging

**Tier 3 - Architecture Decisions (docs/adr/)**:
12. `adr/001-stream-native-cqrs.md` - Keep existing (already good)
13. `adr/002-tiered-routing-strategy.md` - Routing algorithm deep dive (NEW)
14. `adr/003-authentication-architecture.md` - OAuth and JWT flow (NEW)

**Tier 4 - Operational (docs/ops/)**:
15. `ops/troubleshooting.md` - Common issues and solutions (NEW)
16. `ops/monitoring.md` - Health checks, metrics, observability (NEW)
17. `ops/performance.md` - Benchmarks, optimization tips (NEW)

**Archive**:
- `docs/archive/2026-02-02-docs-rebuild/` - Complete archive of current docs with README index

### Definition of Done
- [x] All current docs archived to dated directory with index
- [x] All 17 new documents created with ULTRA MISER MODE™ personality
- [x] All internal cross-links validated and working
- [x] Root README.md updated with new doc structure links
- [x] One commit per wave (4 commits total)

### Must Have
- ULTRA MISER MODE™ references in every document (minimum one)
- All 5 existing docs archived (not deleted)
- Cross-link validation automated via grep/AST-grep
- New directory structure created before writing docs
- Existing ADR 001 preserved (good quality)

### Must NOT Have (Guardrails)
- Direct editing of archived files (read-only after archive)
- Breaking external links without redirects
- Loss of technical accuracy during recreation
- Multiple commits for single wave (batch commits)
- Removal of existing archive (docs/archive/2026-02-02-pre-refactor/)

---

## Verification Strategy

### Test Decision
- **Infrastructure exists**: NO (documentation has no test framework)
- **User wants tests**: NO
- **QA approach**: Manual verification with agent-executable commands

### Manual Verification Procedures

**For each document created**:
```bash
# Verify file exists and has content
cat docs/FILENAME.md | head -20
# Assert: File exists, has frontmatter, has ULTRA MISER MODE™ reference

# Verify relative links work
grep -o '\[.*\](docs/.*)' docs/FILENAME.md | head -5
# For each link found: verify target exists

# Verify ULTRA MISER MODE™ reference exists
grep -i "ultra.*miser\|miser.*mode\|ULTRA MISER MODE" docs/FILENAME.md
# Assert: At least one match found
```

**For archive verification**:
```bash
# Verify all original docs archived
ls -la docs/archive/2026-02-02-docs-rebuild/
# Assert: API.md, ARCHITECTURE.md, CONFIGURATION.md, TESTING_SUMMARY.md present

# Verify archive README exists
cat docs/archive/2026-02-02-docs-rebuild/README.md
# Assert: Contains inventory of archived files with descriptions
```

**Cross-link validation (after all docs created)**:
```bash
# Extract all markdown links
grep -r -o '\[.*\](.*\.md)' docs/ | grep -v archive > /tmp/all_links.txt

# Verify each link target exists
while read line; do
  link=$(echo $line | grep -o '(.*\.md)' | tr -d '()')
  if [ ! -f "$link" ]; then
    echo "BROKEN LINK: $line"
  fi
done < /tmp/all_links.txt
# Assert: No broken links reported
```

---

## Execution Strategy

### Parallel Execution Waves

```
Wave 1: Archive + Structure Setup (Foundation)
├── Task 1: Create archive directory structure
├── Task 2: Move all current docs to archive
├── Task 3: Create new directory structure
└── Task 4: Create archive README index

Wave 2: Core Documentation (Critical Path)
├── Task 5: README.md (root)
├── Task 6: ARCHITECTURE.md (can parallel with Task 5)
├── Task 7: API.md (depends: Task 6 for architecture context)
└── Task 8: CONFIGURATION.md (can parallel with Task 7)

Wave 3: Operational Essentials
├── Task 9: DEPLOYMENT.md
├── Task 10: SECURITY.md (can parallel with Task 9)
├── Task 11: TESTING.md (can parallel with Task 9)
└── Task 12: CONTRIBUTING.md (can parallel with Task 9)

Wave 4: Reference + ADR + Ops (Final)
├── Task 13: Create reference/ docs (3 files)
├── Task 14: Create adr/ docs (2 new ADRs)
├── Task 15: Create ops/ docs (3 files)
└── Task 16: Cross-link validation and final polish
```

### Dependency Matrix

| Task | Depends On | Blocks | Can Parallelize With |
|------|------------|--------|---------------------|
| 1-4 | None | 5-8 | All Wave 1 tasks parallel |
| 5 | None | None | 6 |
| 6 | None | 7 | 5 |
| 7 | 6 | 8 | None (needs arch context) |
| 8 | None | None | 7 |
| 9-12 | 5-8 | 13-15 | All Wave 3 tasks parallel |
| 13-15 | 9-12 | 16 | All Wave 4 tasks parallel |
| 16 | 13-15 | None | None (final validation) |

### Agent Dispatch Summary

| Wave | Tasks | Recommended Agents |
|------|-------|-------------------|
| 1 | 1-4 | `delegate_task(category='quick', load_skills=['git-master'], run_in_background=true)` - 4 parallel agents |
| 2 | 5-8 | `delegate_task(category='writing', load_skills=['simplify'], run_in_background=true)` - 4 parallel agents |
| 3 | 9-12 | `delegate_task(category='writing', load_skills=['simplify'], run_in_background=true)` - 4 parallel agents |
| 4 | 13-16 | `delegate_task(category='writing', load_skills=['simplify'], run_in_background=true)` - 4 parallel agents |

---

## TODOs

### Wave 1: Archive and Structure Setup

- [x] **1. Create Archive Directory Structure**
  
  **What to do**:
  - Create `docs/archive/2026-02-02-docs-rebuild/` directory
  - Preserve subdirectory structure (adr/ stays in archive)
  
  **Must NOT do**:
  - Delete or modify original files yet
  - Remove existing archive at `docs/archive/2026-02-02-pre-refactor/`
  
  **Recommended Agent Profile**:
  - **Category**: `quick`
  - **Skills**: `['git-master']`
  - Reason: Simple directory operations, needs git context
  
  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1
  - **Blocks**: Task 2
  
  **Acceptance Criteria**:
  - [x] `docs/archive/2026-02-02-docs-rebuild/` exists
  - [x] `docs/archive/2026-02-02-docs-rebuild/adr/` exists
  - [x] `ls -la docs/archive/2026-02-02-docs-rebuild/` shows empty directories

- [x] **2. Archive Current Documentation**
  
  **What to do**:
  - Move `docs/API.md` to archive
  - Move `docs/ARCHITECTURE.md` to archive
  - Move `docs/CONFIGURATION.md` to archive
  - Move `docs/TESTING_SUMMARY.md` to archive
  - Move `docs/adr/001-stream-native-cqrs.md` to archive
  
  **Must NOT do**:
  - Delete files (must use git mv to preserve history)
  - Copy instead of move (creates duplicates)
  
  **Recommended Agent Profile**:
  - **Category**: `quick`
  - **Skills**: `['git-master']`
  - Reason: Git operations, file moves
  
  **Parallelization**:
  - **Can Run In Parallel**: YES (with Task 1 completed)
  - **Parallel Group**: Wave 1
  - **Blocks**: Task 4
  
  **Acceptance Criteria**:
  - [x] All 5 files moved to `docs/archive/2026-02-02-docs-rebuild/`
  - [x] Original locations no longer have these files
  - [x] Git history preserved (git log --follow shows move)

- [x] **3. Create New Directory Structure**
  
  **What to do**:
  - Create `docs/reference/` directory
  - Create `docs/adr/` directory (empty, for new ADRs)
  - Create `docs/ops/` directory
  
  **Must NOT do**:
  - Create files yet (just directories)
  - Remove archive directory
  
  **Recommended Agent Profile**:
  - **Category**: `quick`
  - **Skills**: `['git-master']`
  - Reason: Directory creation
  
  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1
  - **Blocks**: Wave 2 tasks
  
  **Acceptance Criteria**:
  - [x] `docs/reference/` exists
  - [x] `docs/adr/` exists
  - [x] `docs/ops/` exists

- [x] **4. Create Archive README Index**
  
  **What to do**:
  - Create `docs/archive/2026-02-02-docs-rebuild/README.md`
  - List all archived files with descriptions
  - Explain why archived (documentation rebuild)
  - Include links to new documentation locations
  
  **Must NOT do**:
  - Include file content (just inventory)
  - Delete this README later
  
  **Recommended Agent Profile**:
  - **Category**: `writing`
  - **Skills**: `['simplify']`
  - Reason: Documentation writing
  
  **Parallelization**:
  - **Can Run In Parallel**: NO (depends on Task 2)
  - **Blocks**: Wave 2
  
  **Acceptance Criteria**:
  - [x] README.md created in archive directory
  - [x] Lists: API.md, ARCHITECTURE.md, CONFIGURATION.md, TESTING_SUMMARY.md, adr/001-stream-native-cqrs.md
  - [x] Contains description of each archived file
  - [x] References new documentation structure

**Wave 1 Commit**:
- Message: `docs: archive legacy documentation to 2026-02-02-docs-rebuild`
- Files: `docs/archive/2026-02-02-docs-rebuild/*`

---

### Wave 2: Core Documentation

- [x] **5. Create README.md (Root)**
  
  **What to do**:
  - Update root README.md with new structure
  - Include: Project overview, ULTRA MISER MODE™ intro, quick start, features
  - Add links to all new documentation
  - Keep existing project description but reorganize
  
  **Must NOT do**:
  - Remove existing technical content
  - Break existing external links
  - Remove ULTRA MISER MODE™ references
  
  **Recommended Agent Profile**:
  - **Category**: `writing`
  - **Skills**: `['simplify']`
  - Reason: Documentation writing with personality
  
  **Parallelization**:
  - **Can Run In Parallel**: YES (with Task 6)
  - **Parallel Group**: Wave 2
  
  **Acceptance Criteria**:
  - [x] README.md updated with new doc links
  - [x] Contains "ULTRA MISER MODE™" reference
  - [x] Quick start section present
  - [x] All Tier 1 docs linked

- [x] **6. Create ARCHITECTURE.md**
  
  **What to do**:
  - Deep dive into Clean Architecture
  - Include: WebApi layer, Application layer, Infrastructure layer
  - Add mermaid diagrams for architecture
  - Document CQRS pipeline, RoutingAgent, SmartRoutingChatClient
  - Include ULTRA MISER MODE™ context (cost optimization)
  
  **Must NOT do**:
  - Copy old ARCHITECTURE.md content verbatim
  - Skip mermaid diagrams
  - Omit tiered routing explanation
  
  **Recommended Agent Profile**:
  - **Category**: `writing`
  - **Skills**: `['simplify']`
  - Reason: Technical architecture documentation
  
  **Parallelization**:
  - **Can Run In Parallel**: YES (with Task 5)
  - **Parallel Group**: Wave 2
  - **Blocks**: Task 7 (API needs architecture context)
  
  **Acceptance Criteria**:
  - [x] ARCHITECTURE.md created in docs/
  - [x] Contains mermaid diagram
  - [x] Explains 3-layer architecture
  - [x] Documents CQRS and routing
  - [x] Contains "ULTRA MISER MODE™" reference

- [x] **7. Create API.md**
  
  **What to do**:
  - Reorganize API documentation (~600 lines, down from 1147)
  - Structure: Overview → Authentication → Endpoints → Schemas → SDKs
  - Endpoints: chat completions, responses, models, admin, health, identity, OAuth
  - Include streaming SSE details
  - Add error codes and handling
  
  **Must NOT do**:
  - Copy old API.md verbatim (selectively extract good content)
  - Remove any endpoint documentation
  - Skip SDK examples
  
  **Recommended Agent Profile**:
  - **Category**: `writing`
  - **Skills**: `['simplify']`
  - Reason: API reference documentation
  
  **Parallelization**:
  - **Can Run In Parallel**: NO (depends on Task 6 for context)
  - **Parallel Group**: Wave 2
  
  **Acceptance Criteria**:
  - [x] API.md created in docs/
  - [x] All endpoints documented
  - [x] Authentication section present
  - [x] Error handling documented
  - [x] Contains "ULTRA MISER MODE™" reference
  - [x] Links to reference/errors.md for detailed error codes

- [x] **8. Create CONFIGURATION.md**
  
  **What to do**:
  - Provider configuration guide
  - All 10+ providers: OpenAI, Groq, Cohere, Cloudflare, Gemini, OpenRouter, Nvidia, HuggingFace, Pollinations, Antigravity
  - JSON structure and examples
  - Environment variable configuration
  - Docker Compose configuration
  
  **Must NOT do**:
  - Remove any provider documentation
  - Skip Docker Compose details
  
  **Recommended Agent Profile**:
  - **Category**: `writing`
  - **Skills**: `['simplify']`
  - Reason: Configuration documentation
  
  **Parallelization**:
  - **Can Run In Parallel**: YES (with Task 7)
  - **Parallel Group**: Wave 2
  
  **Acceptance Criteria**:
  - [x] CONFIGURATION.md created in docs/
  - [x] All providers documented
  - [x] JSON examples present
  - [x] Environment variables documented
  - [x] Contains "ULTRA MISER MODE™" reference

**Wave 2 Commit**:
- Message: `docs: create core documentation (README, ARCHITECTURE, API, CONFIGURATION)`
- Files: `README.md`, `docs/ARCHITECTURE.md`, `docs/API.md`, `docs/CONFIGURATION.md`

---

### Wave 3: Operational Essentials

- [x] **9. Create DEPLOYMENT.md**
  
  **What to do**:
  - Docker deployment guide
  - Docker Compose setup
  - Environment configuration
  - Production deployment considerations
  - Reverse proxy setup (nginx/traefik)
  
  **Must NOT do**:
  - Skip production security notes
  - Omit scaling considerations
  
  **Recommended Agent Profile**:
  - **Category**: `writing`
  - **Skills**: `['simplify']`
  - Reason: DevOps/deployment documentation
  
  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 3
  
  **Acceptance Criteria**:
  - [x] DEPLOYMENT.md created in docs/
  - [x] Docker instructions present
  - [x] Docker Compose example included
  - [x] Production notes included
  - [x] Contains "ULTRA MISER MODE™" reference

- [x] **10. Create SECURITY.md**
  
  **What to do**:
  - Authentication methods (JWT, OAuth)
  - Authorization and access control
  - Rate limiting configuration
  - Security headers
  - Best practices
  
  **Must NOT do**:
  - Skip OAuth flow explanation
  - Omit security best practices
  
  **Recommended Agent Profile**:
  - **Category**: `writing`
  - **Skills**: `['simplify']`
  - Reason: Security documentation
  
  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 3
  
  **Acceptance Criteria**:
  - [x] SECURITY.md created in docs/
  - [x] JWT authentication explained
  - [x] OAuth flows documented
  - [x] Rate limiting covered
  - [x] Contains "ULTRA MISER MODE™" reference

- [x] **11. Create TESTING.md**
  
  **What to do**:
  - Test infrastructure overview
  - Running tests (dotnet test)
  - Test categories: Integration, Unit, Performance
  - Writing new tests
  - CI/CD integration
  
  **Must NOT do**:
  - Skip performance testing details
  - Omit test writing guidelines
  
  **Recommended Agent Profile**:
  - **Category**: `writing`
  - **Skills**: `['simplify']`
  - Reason: Testing documentation
  
  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 3
  
  **Acceptance Criteria**:
  - [x] TESTING.md created in docs/
  - [x] Test commands documented
  - [x] Integration tests explained
  - [x] Performance tests covered
  - [x] Contains "ULTRA MISER MODE™" reference

- [x] **12. Create CONTRIBUTING.md**
  
  **What to do**:
  - Development setup
  - Code standards
  - PR process
  - Commit message conventions
  - Issue reporting
  
  **Must NOT do**:
  - Skip development environment setup
  - Omit code review process
  
  **Recommended Agent Profile**:
  - **Category**: `writing`
  - **Skills**: `['simplify']`
  - Reason: Contributor documentation
  
  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 3
  
  **Acceptance Criteria**:
  - [x] CONTRIBUTING.md created in docs/
  - [x] Setup instructions present
  - [x] PR process documented
  - [x] Commit conventions included
  - [x] Contains "ULTRA MISER MODE™" reference

**Wave 3 Commit**:
- Message: `docs: create operational documentation (DEPLOYMENT, SECURITY, TESTING, CONTRIBUTING)`
- Files: `docs/DEPLOYMENT.md`, `docs/SECURITY.md`, `docs/TESTING.md`, `docs/CONTRIBUTING.md`

---

### Wave 4: Reference, ADR, and Operations

- [x] **13. Create Reference Documentation**
  
  **What to do**:
  - `docs/reference/providers.md` - All provider details, capabilities, limitations
  - `docs/reference/models.md` - Model matrix, features, context windows
  - `docs/reference/errors.md` - Error codes, troubleshooting steps
  
  **Must NOT do**:
  - Skip any provider
  - Omit error code explanations
  
  **Recommended Agent Profile**:
  - **Category**: `writing`
  - **Skills**: `['simplify']`
  - Reason: Reference documentation
  
  **Parallelization**:
  - **Can Run In Parallel**: YES (3 files in parallel)
  - **Parallel Group**: Wave 4
  
  **Acceptance Criteria**:
  - [x] providers.md created with all 10+ providers
  - [x] models.md created with feature matrix
  - [x] errors.md created with error codes
  - [x] All contain "ULTRA MISER MODE™" reference

- [x] **14. Create ADR Documents**
  
  **What to do**:
  - Copy `adr/001-stream-native-cqrs.md` from archive (already good quality)
  - Create `docs/adr/002-tiered-routing-strategy.md` - Routing algorithm ADR
  - Create `docs/adr/003-authentication-architecture.md` - OAuth/JWT ADR
  
  **Must NOT do**:
  - Rewrite ADR 001 (preserve existing)
  - Skip ADR format (context, decision, consequences)
  
  **Recommended Agent Profile**:
  - **Category**: `writing`
  - **Skills**: `['simplify']`
  - Reason: ADR documentation
  
  **Parallelization**:
  - **Can Run In Parallel**: YES (3 files in parallel)
  - **Parallel Group**: Wave 4
  
  **Acceptance Criteria**:
  - [x] 001-stream-native-cqrs.md restored from archive
  - [x] 002-tiered-routing-strategy.md created
  - [x] 003-authentication-architecture.md created
  - [x] All follow ADR format (Context, Decision, Consequences)
  - [x] All contain "ULTRA MISER MODE™" reference

- [x] **15. Create Operational Documentation**
  
  **What to do**:
  - `docs/ops/troubleshooting.md` - Common issues, solutions, debugging
  - `docs/ops/monitoring.md` - Health checks, metrics, observability
  - `docs/ops/performance.md` - Benchmarks, optimization, profiling
  
  **Must NOT do**:
  - Skip common error scenarios
  - Omit monitoring setup
  
  **Recommended Agent Profile**:
  - **Category**: `writing`
  - **Skills**: `['simplify']`
  - Reason: Operations documentation
  
  **Parallelization**:
  - **Can Run In Parallel**: YES (3 files in parallel)
  - **Parallel Group**: Wave 4
  
  **Acceptance Criteria**:
  - [x] troubleshooting.md created
  - [x] monitoring.md created
  - [x] performance.md created
  - [x] All contain "ULTRA MISER MODE™" reference

- [x] **16. Cross-Link Validation and Polish**
  
  **What to do**:
  - Validate all internal markdown links
  - Check ULTRA MISER MODE™ references in all docs
  - Verify consistent formatting
  - Ensure archive README is accurate
  - Update root README with final structure
  
  **Must NOT do**:
  - Skip link validation
  - Leave broken references
  
  **Recommended Agent Profile**:
  - **Category**: `quick`
  - **Skills**: `['cartography']`
  - Reason: Validation and cross-checking
  
  **Parallelization**:
  - **Can Run In Parallel**: NO (final validation)
  - **Parallel Group**: Wave 4
  
  **Acceptance Criteria**:
  - [x] All markdown links validated (no broken links)
  - [x] ULTRA MISER MODE™ count: 17+ (one per doc minimum)
  - [x] Root README updated with final structure
  - [x] Archive README accurate
  - [x] All new docs present in docs/ structure

**Wave 4 Commit**:
- Message: `docs: complete documentation suite (reference, ADR, ops, validation)`
- Files: `docs/reference/*`, `docs/adr/*`, `docs/ops/*`, `README.md`

---

## Commit Strategy

| Wave | Commit Message | Files | Verification |
|------|---------------|-------|--------------|
| 1 | `docs: archive legacy documentation to 2026-02-02-docs-rebuild` | `docs/archive/2026-02-02-docs-rebuild/*` | `ls docs/archive/2026-02-02-docs-rebuild/` shows 5 files |
| 2 | `docs: create core documentation (README, ARCHITECTURE, API, CONFIGURATION)` | `README.md`, `docs/ARCHITECTURE.md`, `docs/API.md`, `docs/CONFIGURATION.md` | `ls docs/*.md` shows 4 files |
| 3 | `docs: create operational documentation (DEPLOYMENT, SECURITY, TESTING, CONTRIBUTING)` | `docs/DEPLOYMENT.md`, `docs/SECURITY.md`, `docs/TESTING.md`, `docs/CONTRIBUTING.md` | `ls docs/*.md` shows 8 files |
| 4 | `docs: complete documentation suite (reference, ADR, ops, validation)` | `docs/reference/*`, `docs/adr/*`, `docs/ops/*`, `README.md` | All 17 files present, links valid |

---

## Success Criteria

### Verification Commands
```bash
# Verify archive exists and contains correct files
ls docs/archive/2026-02-02-docs-rebuild/ | wc -l
# Expected: 5 (API.md, ARCHITECTURE.md, CONFIGURATION.md, TESTING_SUMMARY.md, adr/)

# Verify new structure has 17 documents
count=0
[ -f README.md ] && ((count++))
[ -f docs/ARCHITECTURE.md ] && ((count++))
[ -f docs/API.md ] && ((count++))
[ -f docs/CONFIGURATION.md ] && ((count++))
[ -f docs/DEPLOYMENT.md ] && ((count++))
[ -f docs/SECURITY.md ] && ((count++))
[ -f docs/TESTING.md ] && ((count++))
[ -f docs/CONTRIBUTING.md ] && ((count++))
[ -f docs/reference/providers.md ] && ((count++))
[ -f docs/reference/models.md ] && ((count++))
[ -f docs/reference/errors.md ] && ((count++))
[ -f docs/adr/001-stream-native-cqrs.md ] && ((count++))
[ -f docs/adr/002-tiered-routing-strategy.md ] && ((count++))
[ -f docs/adr/003-authentication-architecture.md ] && ((count++))
[ -f docs/ops/troubleshooting.md ] && ((count++))
[ -f docs/ops/monitoring.md ] && ((count++))
[ -f docs/ops/performance.md ] && ((count++))
echo "Total docs: $count"
# Expected: 17

# Verify ULTRA MISER MODE references
grep -r "ULTRA MISER MODE" docs/ | wc -l
# Expected: 17+ (one per document minimum)

# Validate no broken links
grep -r -o '\[.*\](docs/.*\.md)' docs/ 2>/dev/null | while read line; do
  link=$(echo $line | grep -o '(docs/[^)]*)' | tr -d '()')
  [ -f "$link" ] || echo "BROKEN: $line"
done
# Expected: No output (no broken links)
```

### Final Checklist
- [x] All current docs archived to dated directory
- [x] 17 new documents created
- [x] All docs contain ULTRA MISER MODE™ reference
- [x] All internal links validated
- [x] Root README updated with new structure
- [x] Archive README created with inventory
- [x] 4 commits created (one per wave)
- [x] No broken external links

---

## Post-Completion Handoff

**After all tasks complete**:
1. Verify all 17 files exist
2. Run link validation
3. Verify ULTRA MISER MODE™ references present
4. Final commit with `docs: complete documentation rebuild`
5. Delete draft file: `.sisyphus/drafts/docs-archive-recreate.md`
6. Guide user: Documentation rebuild complete. Run `/start-work` to execute the plan.

**Execution Command**:
```
/start-work
```
