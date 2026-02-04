# ADR 008: Frontend Local-First Architecture

**Status:** Accepted  
**Date:** 2026-01-29

> **ULTRA MISER MODE™ Engineering**: Why pay for backend storage when browsers come with IndexedDB for free? A stateless backend + client-side persistence = zero storage costs and instant offline capability.

---

## Context

Synaxis's `InferenceGateway` adheres to a strict **stateless** philosophy—no session storage, no message history, no telemetry API. This design minimizes infrastructure costs but creates a challenge: how do users track chat history, monitor token usage, and calculate "money saved" without a backend database?

The traditional solution would be to add a database layer, storage APIs, and authentication—incurring hosting costs and operational complexity. This directly contradicts the **ULTRA MISER MODE™** principle of zero recurring infrastructure costs.

---

## Decision

We have adopted a **Local-First Architecture** for the Synaxis WebApp, where all state (chat sessions, messages, usage logs) is stored client-side in **IndexedDB** using **Dexie.js**. The backend remains purely computational—it processes inference requests and returns responses, but stores nothing.

### Core Architectural Principles

#### 1. Stateless Backend, Stateful Frontend

```typescript
// Backend: Pure computation, zero persistence
POST /v1/chat/completions
→ Response: { id, choices, usage: { prompt_tokens, completion_tokens } }

// Frontend: Captures and persists everything
const { data } = await gatewayClient.chat(request);
await db.messages.add({
  sessionId,
  content: data.choices[0].message.content,
  tokens: data.usage.total_tokens,
  timestamp: Date.now()
});
```

**Benefit:** Backend scales horizontally without state synchronization. Frontend works offline.

#### 2. IndexedDB as the Single Source of Truth

Using **Dexie.js** for a typed, reactive database layer:

```typescript
// src/db/schema.ts
export class SynaxisDB extends Dexie {
  sessions!: Table<ChatSession>;
  messages!: Table<ChatMessage>;
  settings!: Table<AppSettings>;
  usage!: Table<UsageLog>;

  constructor() {
    super('SynaxisDB');
    this.version(1).stores({
      sessions: '++id, createdAt, updatedAt',
      messages: '++id, sessionId, timestamp',
      settings: 'key',
      usage: '++id, timestamp, model'
    });
  }
}
```

**Data Schema:**

| Table | Purpose | Retention |
|-------|---------|-----------|
| `sessions` | Chat threads | User-controlled |
| `messages` | Individual messages | Linked to session |
| `settings` | Gateway URL, cost rates | Persistent |
| `usage` | Token telemetry | 30-day rolling window |

#### 3. Client-Side Telemetry Engine

The frontend captures token usage from every API response and calculates "Money Saved":

```typescript
// src/lib/telemetry.ts
export async function recordUsage(response: ChatResponse) {
  const { usage, model } = response;
  const costRate = await db.settings.get('costRate'); // User-configured
  
  const moneySaved = calculateSavings({
    promptTokens: usage.prompt_tokens,
    completionTokens: usage.completion_tokens,
    costRate: costRate || 0, // Default: free
    benchmarkRate: 0.03 // GPT-4 Turbo baseline
  });

  await db.usage.add({
    model,
    tokens: usage.total_tokens,
    moneySaved,
    timestamp: Date.now()
  });
}
```

**Dashboard Integration:**
- Real-time badge: "💰 Saved: $47.23 this month"
- Charts: Token usage over time, cost comparison by model

#### 4. Personal Mode (No Authentication)

The default mode allows users to self-host without identity management:

```typescript
// src/features/settings/store.ts
export const settingsStore = create<SettingsState>((set) => ({
  gatewayUrl: 'http://localhost:5000', // User-configurable
  isPersonalMode: true, // Default
  
  updateGatewayUrl: async (url) => {
    await db.settings.put({ key: 'gatewayUrl', value: url });
    set({ gatewayUrl: url });
  }
}));
```

**User Flow:**
1. User opens app → sees default localhost gateway
2. User clicks "Settings" → enters custom gateway URL
3. App saves to IndexedDB → all future requests use custom URL

---

## Tech Stack

### Core Framework
- **React 19:** Latest concurrent features (Suspense, transitions)
- **Vite:** Fast dev server, optimized production builds
- **TypeScript:** Strict mode, full type safety

### Data & State Management
- **Dexie.js:** Typed IndexedDB wrapper with reactive queries
- **TanStack Query:** API state management, caching, optimistic updates
- **Zustand:** Lightweight UI state (modals, themes)

### UI & Styling
- **Tailwind CSS v4:** Utility-first styling with CSS variables
- **Framer Motion:** Declarative animations
- **Lucide React:** Icon library (tree-shakeable)

### Testing
- **Vitest:** Unit tests for utilities, hooks, stores
- **React Testing Library:** Component integration tests
- **Playwright:** End-to-end user flow tests

---

## Implementation Phases

### Phase 0: Foundation
1. Scaffold Vite project with TypeScript preset
2. Configure Tailwind CSS v4, ESLint, Prettier
3. Setup path aliases (`@/components`, `@/lib`, etc.)

### Phase 1: Core Logic (Local DB & API Client)
1. **Database Layer:**
   - Define Dexie schema (sessions, messages, settings, usage)
   - Create typed wrappers with CRUD operations
   - Implement reactive hooks (`useLiveQuery`)

2. **API Client:**
   - Create `GatewayClient` class with OpenAI-compatible methods
   - Handle SSE streaming for `/v1/chat/completions`
   - Parse and validate responses with Zod schemas

3. **Settings Engine:**
   - Store gateway URL (default: `http://localhost:5000`)
   - Store cost rates (user-configurable for telemetry)
   - Persist theme, language preferences

### Phase 2: UI Implementation
1. **Components:**
   - `AppShell`: Sidebar (session list) + Main area (chat) + Settings modal
   - `ChatInterface`: Message list with auto-scroll, input composer with streaming support
   - `MiserBadge`: Real-time counter displaying cumulative savings

2. **Features:**
   - Create/delete chat sessions
   - Send messages with streaming responses
   - Display token usage per message
   - Show cumulative "money saved" calculation

### Phase 3: Polish & Testing
1. Animations with Framer Motion (message fly-in, session transitions)
2. Responsive design (mobile, tablet, desktop)
3. Keyboard shortcuts (Ctrl+K for new chat, Esc to close modals)
4. Unit tests (>80% coverage)
5. E2E tests (new chat → send message → verify persistence)

---

## Directory Structure

```
synaxis-ui/
├── src/
│   ├── api/          # API clients (GatewayClient)
│   ├── components/   # Reusable UI components
│   ├── db/           # Dexie database schema & operations
│   ├── features/     # Feature modules (chat, settings, telemetry)
│   ├── hooks/        # Custom React hooks (useStreamingChat, useSessions)
│   ├── lib/          # Utilities (cn, formatters, validators)
│   ├── stores/       # Zustand stores (UI state)
│   └── types/        # Shared TypeScript types
├── tests/
│   ├── unit/         # Vitest unit tests
│   ├── integration/  # Component integration tests
│   └── e2e/          # Playwright end-to-end tests
├── public/           # Static assets
└── package.json      # Dependencies & scripts
```

---

## Consequences

### Positive

- **Zero Backend Storage Costs:** No database, no S3, no Redis—just stateless compute
- **Instant Offline Mode:** IndexedDB works without network connectivity
- **Privacy by Design:** User data never leaves the browser (no GDPR compliance needed)
- **Horizontal Scaling:** Backend can scale without state synchronization complexity
- **Fast Development:** No backend API design needed for CRUD operations

### Negative

- **No Cross-Device Sync:** Data is locked to a single browser (mitigated by export/import)
- **Storage Limits:** IndexedDB quota varies by browser (~50MB–unlimited)
- **No Server-Side Analytics:** Cannot track aggregate usage across users

### Mitigations

- **Export/Import:** Users can export chat history as JSON and import on another device
- **Storage Quotas:** Implement rolling retention (e.g., auto-delete messages older than 30 days)
- **Optional Cloud Sync:** Future ADR could add opt-in cloud backup via WebDAV or S3-compatible APIs

---

## Related Decisions

- [ADR-001: Stream-Native CQRS](./001-stream-native-cqrs.md) — Backend architecture that frontend consumes
- [ADR-011: Ultra-Miser Mode](./011-ultra-miser-mode.md) — Cost optimization philosophy enabling stateless design

---

## Evidence

- **Archived Plan:** `docs/archive/2026/01/29/docs_archive/2026-02-02-pre-refactor/plan/plan1-20260129-frontend-local-first.md`
- **Related Commits:** Initial React app scaffold with Dexie integration
- **Implementation:** `synaxis-ui/` directory (planned)

---

> *"The best database is the one you don't pay for. Browsers have been giving us free storage since 2010—why aren't we using it?"* — ULTRA MISER MODE™ Principle #19
