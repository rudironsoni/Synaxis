# Frontend Implementation Plan: Synaxis WebApp (Local-First)

**Date:** 2026-01-29
**Status:** Approved
**Goal:** Create a "Local-First" React WebApp for Synaxis that manages chat history and costs client-side, respecting the stateless "Ultra Miser Mode" backend philosophy.

## 1. Architecture: Local-First
*   **Backend Constraint:** `InferenceGateway` is stateless. No session/message storage. No telemetry API.
*   **Frontend Solution:**
    *   **Database:** IndexedDB (via Dexie.js) stores `sessions`, `messages`, `usage_logs`.
    *   **Telemetry:** Frontend captures token usage from every `/v1/chat/completions` response and aggregates "Money Saved" locally.
    *   **Auth:** "Personal Mode" (default) allows setting a custom Gateway URL.

## 2. Tech Stack
*   **Core:** React 19, Vite, TypeScript.
*   **Styling:** Tailwind CSS v4, Framer Motion (animations), Lucide React (icons).
*   **Data:** Dexie.js (Local DB), TanStack Query (API State), Zustand (UI State).
*   **Testing:** Vitest (Unit), React Testing Library (Component), Playwright (E2E).

## 3. Implementation Phases

### Phase 0: Foundation
1.  **Scaffold:** Initialize `synaxis-ui` (Vite + TS).
2.  **Config:** Setup Tailwind CSS v4, ESLint, Prettier, Path Aliases (`@/*`).
3.  **Dependencies:** Install `dexie`, `clsx`, `tailwind-merge`, `lucide-react`, `axios`, `zod`.

### Phase 1: Core Logic (Local DB & API)
1.  **Database Layer (`src/db`):**
    *   Define schema: `sessions`, `messages`, `settings`, `usage`.
    *   Create typed Dexie wrapper.
2.  **Settings Engine:**
    *   Store Gateway URL (default `http://localhost:5000`).
    *   Store Cost Rate (default $0.00/1k tokens).
3.  **API Client:**
    *   `GatewayClient` wrapper for OpenAI-compatible endpoints.
    *   Handling streaming responses (SSE).

### Phase 2: UI Implementation
1.  **Components:**
    *   `AppShell`: Sidebar + Main Area + Settings Modal.
    *   `ChatInterface`: Message list, Input composer, Auto-scroll.
    *   `MiserBadge`: Real-time counter of saved money.
2.  **Features:**
    *   Create/Delete Sessions.
    *   Send/Receive Messages.
    *   Calculate and display costs.

## 4. Testing Strategy
*   **Unit Tests:** >80% coverage for Utils, DB Logic, Hooks.
*   **Integration:** Verify Dexie persistence and API client mocking.
*   **E2E:** Playwright tests for full user flows (New Chat -> Send Message -> Verify History).

## 5. Directory Structure
```
synaxis-ui/
├── src/
│   ├── api/          # API Clients
│   ├── components/   # UI Components
│   ├── db/           # Dexie Database
│   ├── features/     # Feature-based modules (chat, settings)
│   ├── hooks/        # React Hooks
│   ├── lib/          # Utilities (cn, formatters)
│   ├── stores/       # Zustand Stores
│   └── types/        # TypeScript Definitions
```
