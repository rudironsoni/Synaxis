## WebApp Features & Gaps

This document inventories the WebApp (src/Synaxis.WebApp/ClientApp/src) UI components/views, the WebAPI endpoints they call, the current implementation status, and missing features needed for parity with the WebAPI.

### Summary
- Files scanned: src/Synaxis.WebApp/ClientApp/src/**
- Key API integration entry: src/Synaxis.WebApp/ClientApp/src/api/client.ts

---

### Implemented Components / Views

- App (App.tsx)
  - Purpose: Main application shell; selects active session and renders ChatWindow
  - WebAPI usage: None directly
  - State: implemented

- AppShell (components/layout/AppShell.tsx)
  - Purpose: Layout with sidebar (SessionList), header (settings/cost badge), main content area
  - WebAPI usage: None directly (uses local stores)
  - State: implemented

- SessionList (features/sessions/SessionList.tsx)
  - Purpose: CRUD for chat sessions (list, create, delete)
  - WebAPI usage: None — uses local IndexedDB store (src/.../db/db.ts)
  - State: implemented

- ChatWindow (features/chat/ChatWindow.tsx)
  - Purpose: Chat conversation UI; shows messages and input
  - WebAPI usage:
    - Uses defaultClient.sendMessage -> POST /v1/chat/completions (non-stream)
    - Uses defaultClient.sendMessageStream -> POST /v1/chat/completions with stream=true and SSE-style parsing
  - State: implemented (supports streaming and non-streaming flows controlled by settings.streamingEnabled)

- ChatInput (features/chat/ChatInput.tsx)
  - Purpose: Text input control used by ChatWindow
  - WebAPI usage: none directly
  - State: implemented

- MessageBubble (features/chat/MessageBubble.tsx)
  - Purpose: Render individual messages with optional token usage and streaming indicator
  - WebAPI usage: none
  - State: implemented

- SettingsDialog (features/settings/SettingsDialog.tsx)
  - Purpose: Configure Gateway URL and cost rate; toggles persisted via zustand
  - WebAPI usage: none (writes to local settings store)
  - State: implemented

- ProviderConfig (features/admin/ProviderConfig.tsx)
  - Purpose: Admin UI to list providers, edit keys, enable/disable providers, edit tiers and models
  - WebAPI usage:
    - GET /admin/providers (fetchProviders)
    - PUT /admin/providers/{providerId} (updateProvider)
    - Authorization header uses jwtToken from settings store
  - State: implemented (calls endpoints; has local fallback data on error)

- HealthDashboard (features/admin/HealthDashboard.tsx)
  - Purpose: Monitor services and providers
  - WebAPI usage:
    - GET /admin/health (fetchHealth)
    - Authorization header uses jwtToken
  - State: implemented (polling auto-refresh; local fallback data on error)

- AdminShell / AdminLogin / AdminSettings
  - Purpose: Admin routing and minimal auth flow
  - WebAPI usage: AdminLogin likely posts credentials (not present in scanned files) — AdminShell protects admin routes using jwtToken
  - State: implemented (routing + protection in main.tsx)

---

### API Client Patterns (src/api/client.ts)

- GatewayClient (axios wrapper)
  - baseURL defaults to '/v1' and can be updated with updateConfig(baseURL, token)
  - sendMessage(messages, model) -> POST /chat/completions with stream: false
  - sendMessageStream(messages, model) -> performs fetch to /chat/completions with stream: true and parses streaming SSE-style `data: {json}` lines
  - Streaming implementation exists client-side and yields parsed ChatStreamChunk objects
  - defaultClient is instantiated and used by ChatWindow

### WebAPI endpoints observed (used by WebApp)

- POST /v1/chat/completions
  - Used for both non-stream and stream responses. Client sends { model, messages, stream }.

- GET /admin/providers
  - Admin provider configuration listing

- PUT /admin/providers/{providerId}
  - Update provider config (enable/disable, set key, endpoint, tier)

- GET /admin/health
  - Health dashboard data (services, providers, overall status)

Other endpoints likely supported by backend but not directly used in the WebApp code scanned:
- /auth/* (login) — AdminLogin exists but HTTP calls not found in scanned files; might be implemented elsewhere or simulated
- /v1/responses or /v1/usage endpoints — not referenced in client code

---

### Current Feature State and Gaps (Parity Checklist)

- Streaming support in chat completions
  - Present: Partial. Client implements sendMessageStream and ChatWindow wires streamingEnabled setting to use it. The client performs fetch SSE parsing and renders streaming content.
  - Gaps: Needs backend streaming support to be fully functional. client.ts expects SSE-style 'data: ' lines and [DONE] sentinel.
  - Status: implemented client-side; requires backend streaming parity.

- Admin UI for provider configuration
  - Present: Yes. ProviderConfig.tsx implements listing, editing, key entry, toggling enabled state, and model listing. Uses /admin/providers endpoints and JWT header.
  - Gaps: AdminLogin exists but integration of obtaining/storing jwtToken is via settings store; actual login POST endpoint not found in scanned files.
  - Status: implemented (UI present). Needs backend admin endpoints to be available and authentication flow wired.

- Health monitoring dashboard
  - Present: Yes. HealthDashboard polls /admin/health and renders providers/services with auto-refresh.
  - Gaps: None in UI; needs backend /admin/health endpoint to provide expected JSON shape.
  - Status: implemented (UI present; local fallback provided on errors)

- Model selection UI (all providers/models)
  - Present: Partial. ProviderConfig lists models for providers and shows model status per-provider.
  - Gaps: No global model-selection control in Chat UI (user cannot pick model per chat). ChatWindow/sendMessage(sendMessageStream) accept an optional model argument, but ChatInput/ChatWindow do not expose model choice.
  - Status: provider-level model data shown in admin UI; missing user-facing model selection in chat flow.

- JWT token management
  - Present: Partial. settings store has jwtToken and AdminRoute checks it for admin area. ProviderConfig/HealthDashboard send Authorization: Bearer <jwtToken>.
  - Gaps: AdminLogin likely exists but its network calls were not scanned; no explicit login POST usage found. No token refresh flow implemented.
  - Status: basic token storage + usage present; missing login network flow and refresh handling.

- Health checks and monitoring
  - Present in UI (HealthDashboard) and README documents /health endpoints.
  - Status: UI implemented; depends on backend readiness checks.

- Usage / cost tracking
  - Present: stores/usage exists and ChatWindow records usage from ChatResponse. AppShell displays cost badge (costRate * usage).
  - Gaps: No central billing UI; usage stored locally and displayed only as a simple badge.
  - Status: partial implemented

---

### Missing / Recommended Features (for parity with WebAPI)

- Backend streaming support for /v1/chat/completions (SSE) — client expects it
- In-chat model selection UI (per-session or per-message model override)
- Admin login network integration and token lifecycle (login endpoint, refresh, logout server-side)
- Provider-management features: reorder providers, tier editing UI, model enable/disable persisting
- Responses endpoint usage (e.g., /v1/responses) — not used; evaluate whether to support Responses API
- JWT token refresh and role-based UI (admin vs. read-only)
- Centralized usage analytics page (beyond small badge)

---

### Files scanned (selected)
- src/Synaxis.WebApp/ClientApp/src/api/client.ts
- src/Synaxis.WebApp/ClientApp/src/features/chat/ChatWindow.tsx
- src/Synaxis.WebApp/ClientApp/src/features/chat/ChatInput.tsx
- src/Synaxis.WebApp/ClientApp/src/features/chat/MessageBubble.tsx
- src/Synaxis.WebApp/ClientApp/src/features/sessions/SessionList.tsx
- src/Synaxis.WebApp/ClientApp/src/features/settings/SettingsDialog.tsx
- src/Synaxis.WebApp/ClientApp/src/features/admin/ProviderConfig.tsx
- src/Synaxis.WebApp/ClientApp/src/features/admin/HealthDashboard.tsx
- src/Synaxis.WebApp/ClientApp/src/features/admin/AdminShell.tsx
- src/Synaxis.WebApp/ClientApp/src/features/admin/AdminLogin.tsx
- src/Synaxis.WebApp/ClientApp/src/stores/settings.ts
- src/Synaxis.WebApp/ClientApp/src/stores/sessions.ts
- src/Synaxis.WebApp/ClientApp/src/App.tsx
- src/Synaxis.WebApp/ClientApp/src/main.tsx

---

### Verification

- Document added at .sisyphus/webapp-features.md
- Findings appended to notepad learnings file (see below)

---

Generated: 2026-01-30T
