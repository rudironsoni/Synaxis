# Synaxis OpenAI Gateway Plan

Date: 2026-01-25

Scope: Build a multi-tenant, OpenAI-compatible gateway with OAuth + per-project API keys, regional data locality (EU/US/SA), ULTRA MISER MODE routing (free > cheapest > healthy), per-request token tracking (90-day retention), public dashboard, and distributed control plane (Postgres/Redis).

Guiding constraints:
- OpenAI best-effort compatibility with documented deviations only.
- No paid managed services. BYOK encryption is local-only.
- All data stays in-region; routing must respect tenant region.

Open decisions (blockers):
- Storage backend for Files/Images/Audio: local filesystem (recommended for free MVP) vs self-hosted MinIO.

Milestones:
M0 Foundations [COMPLETED]
- Regional tenancy + RBAC model (Entities created, DbContext wired)
- Postgres schema + migrations (EF Core setup)
- Deviation registry (Implemented & Tested)

M1 Compatibility Layer [COMPLETED]
- Canonical OpenAI schema (CanonicalRequest/Response/Chunk implemented)
- Translator registry + streaming state machine (TranslationPipeline implemented & Tested)
- Tool-call normalization (IToolNormalizer implemented)
- Conformance fixtures + tests (Unit tests passed)

M2 Auth + Security
- GitHub + Google OAuth
- Per-project API keys
- BYOK encryption + rotation
- Audit logs

M3 Routing Intelligence
- Model aliases + combos
- ULTRA MISER MODE cost routing
- Account health + cooldown
- Quota avoidance

M4 Ops + Usage
- Per-request token tracking
- Usage + routing trace APIs
- Provider usage adapters

M5 Public Dashboard
- Multi-tenant UI for usage, keys, routing, logs
- Role-based access controls

M6 SDK + Hot Reload
- Embedded builder + watcher
- Atomic config reload
- Regional worker readiness

Execution sequence:
1) Implement M0/M1 core pieces with tests and coverage
2) Layer in M2 auth + security
3) Add M3 routing intelligence
4) Implement M4 ops + usage tracking
5) Deliver M5 dashboard
6) Finalize M6 SDK + hot reload
