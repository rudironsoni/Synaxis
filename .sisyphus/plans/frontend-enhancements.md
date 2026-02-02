# Frontend Dashboard Enhancement Plan

## TL;DR

> **Quick Summary**: Replace current chat-focused interface with comprehensive dashboard similar to 9router, featuring provider management, usage analytics, model configuration, and API key management.
>
> **Deliverables**:
> - Dashboard with provider status cards and analytics
> - Provider management interface
> - Usage tracking and monitoring
> - API key management
> - Model configuration interface
>
> **Estimated Effort**: Large
> **Parallel Execution**: YES - 3 waves (foundation → features → integration)
> **Critical Path**: Dashboard foundation → Provider management → Analytics → Integration

---

## Context

### Original Request
Enhance the Synaxis frontend with a 9router-style dashboard interface (inspired by https://github.com/decolua/9router), replacing the current chat-focused interface.

**9router Reference Implementation**:
- **Repository**: https://github.com/decolua/9router
- **Dashboard Structure**: Sidebar navigation with sections (Endpoint, Providers, Combos, Usage, CLI Tools, Settings)
- **Provider Management**: Card grid layout with status indicators and quick actions
- **Analytics Display**: Mini bar graphs, tabbed interfaces, sortable tables
- **Configuration Management**: Modal dialogs, API key tables, cloud proxy toggle
- **UI Patterns**: Responsive design, mobile-first approach, card-based layouts

### Interview Summary
**Key Discussions**:
- User wants ALL aspects of 9router interface (provider management, analytics, model config, API keys)
- Decision to REPLACE chat interface with dashboard as main interface
- Current frontend has solid React 19 + Zustand + Tailwind foundation
- Backend has comprehensive API endpoints available

**Research Findings**:
**Backend API**: Comprehensive endpoints available but missing dashboard-specific APIs (provider management, analytics, configuration)
**Frontend Architecture**: Well-structured with clear extension patterns
**9router Patterns**: Card-based provider mgmt, analytics charts, modal configuration

**Backend API Gap Strategy**: BACKEND-FIRST APPROACH - Plan includes backend API development tasks for dashboard-specific endpoints before frontend integration

**CURRENT BACKEND STATE**:
- **Existing APIs**: `/openai/v1/chat/completions`, `/openai/v1/models`, `/auth/dev-login`, `/projects/{projectId}/keys`
- **Missing Dashboard APIs**: `/api/providers`, `/api/analytics`, `/api/config` - MUST BE IMPLEMENTED FIRST
- **Authentication**: JWT system exists via AuthController.cs - can be extended for dashboard access
- **API Pattern**: Follow existing OpenAI endpoints pattern using minimal APIs

### Metis Review
**Identified Gaps** (addressed):
- **Backend API gap**: BACKEND-FIRST APPROACH - Develop backend APIs before frontend integration
- **Authentication complexity**: ENHANCED AUTH SYSTEM - Plan proper authentication alongside dashboard
- **Chat integration**: INTEGRATE AS DASHBOARD FEATURE - Chat becomes dashboard tab/navigation item
- **Data persistence**: Extend existing IndexedDB usage
- **Scope creep**: Define clear MVP boundaries

### Authentication Integration Strategy
- **Current Auth System**: JWT-based authentication via `AuthController.cs` exists
- **Dashboard Access**: Use existing JWT tokens for dashboard authentication
- **Fallback Strategy**: If enhanced auth not ready, use current dev login with dashboard access
- **API Key Management**: Keys are user-scoped and authenticated
- **Error Handling**: Graceful handling of authentication failures with redirect to login

### Backend API Implementation Strategy
- **Use Minimal APIs**: Follow `OpenAIEndpointsExtensions.cs` pattern (not Controller pattern)
- **Create Dashboard Endpoints Group**: `/api/providers`, `/api/analytics`, `/api/config`
- **Leverage Existing Infrastructure**: Use current JWT auth, Redis, PostgreSQL patterns
- **Progressive Enhancement**: Frontend can use mock data while backend APIs are being developed

### AppShell Integration Strategy
**Current App Structure**:
- `App.tsx` renders `AppShell` with `ChatWindow` as main content
- `AppShell` has sidebar with `SessionList` and header with cost rate badge
- Sidebar collapses on mobile breakpoints (`max-width: 640px`)

**Dashboard Integration Approach**:
- **Create `DashboardShell` component**: Extends `AppShell` structure but replaces sidebar content
- **Sidebar Replacement**: Replace `SessionList` with dashboard navigation (Providers, Analytics, Keys, Models, Chat tabs)
- **Main Content Area**: Use React Router `Outlet` to render dashboard sections
- **Mobile Responsiveness**: Keep existing sidebar collapse behavior, dashboard sections adapt to mobile
- **Route Structure**:
  - `/dashboard` → Main dashboard with overview
  - `/dashboard/providers` → Provider management
  - `/dashboard/analytics` → Usage analytics
  - `/dashboard/keys` → API key management
  - `/dashboard/models` → Model configuration
  - `/dashboard/chat` → Chat interface (preserves current functionality)

### State Management Strategy
**Dashboard Store Structure**:
```typescript
interface DashboardState {
  providers: Provider[]
  analytics: AnalyticsData
  config: ConfigData
  loading: boolean
  error: string | null
}
```

**Integration with Existing Stores**:
- **Separate Store**: Create `useDashboardStore` separate from existing stores
- **Authentication**: Extend `settings` store to include JWT token management
- **Data Flow**: Dashboard components read from dashboard store, backend APIs update store

### Authentication Flow
**Current Authentication**:
- JWT tokens generated via `AuthController.cs` `dev-login` endpoint
- Tokens stored in `settings` store (need to extend)

**Dashboard Authentication**:
- **Route Guards**: Wrap dashboard routes with authentication check using JWT token
- **Login Redirect**: If no valid token, redirect to enhanced login page
- **Token Management**: Extend `settings` store to handle JWT tokens
- **Fallback**: If auth not ready, use mock authentication for development

### Mock Data Strategy
**Frontend Development Approach**:
- **Mock Services**: Create `mockProviderService.ts`, `mockAnalyticsService.ts`, `mockConfigService.ts`
- **Interface Consistency**: Mock services implement same interfaces as real services
- **Easy Replacement**: Switch from mock to real services by changing imports
- **Development Flow**: Frontend development proceeds with mock data, backend development proceeds in parallel

**Mock Data Structure**:
- Follows backend API response structure exactly
- Realistic data that matches expected production data
- Easy to verify mock-to-real transition

### Backend API Contracts

**Provider Management API**:
```json
GET /api/providers
Response: {
  "providers": [
    {
      "id": "groq",
      "name": "Groq",
      "status": "healthy",
      "tier": 0,
      "models": ["llama-3.1-70b-versatile"],
      "usage": {"totalTokens": 15000, "requests": 45}
    }
  ]
}

GET /api/providers/{id}/status
Response: {"status": "healthy", "lastChecked": "2026-01-30T10:30:00Z"}

PUT /api/providers/{id}/config
Request: {"enabled": true, "tier": 1}
Response: {"success": true}
```

**Analytics API**:
```json
GET /api/analytics/usage
Response: {
  "totalTokens": 45000,
  "totalRequests": 120,
  "providers": [
    {"id": "groq", "tokens": 15000, "requests": 45},
    {"id": "deepseek", "tokens": 30000, "requests": 75}
  ],
  "timeRange": {"start": "2026-01-01", "end": "2026-01-30"}
}

GET /api/analytics/providers
Response: {
  "providers": [
    {
      "id": "groq",
      "performance": {"avgResponseTime": 450, "successRate": 0.98},
      "usage": {"dailyTokens": 1500, "dailyRequests": 15}
    }
  ]
}
```

**Configuration API**:
```json
GET /api/config/models
Response: {
  "models": [
    {
      "id": "deepseek-chat",
      "provider": "DeepSeek",
      "capabilities": {"streaming": true, "tools": true, "vision": false}
    }
  ]
}

PUT /api/config/models/{id}
Request: {"enabled": true, "priority": 1}
Response: {"success": true}
```

### Integration Strategy

**Routing Architecture**:
- **Client-Side Routing**: Install and configure React Router for dashboard navigation
- **Router Setup**: Add `react-router-dom` dependency and configure BrowserRouter
- **Dashboard Route**: `/dashboard` - Main dashboard interface (replaces current `/` route)
- **Nested Routes**: `/dashboard/providers`, `/dashboard/analytics`, `/dashboard/keys`, `/dashboard/models`, `/dashboard/chat`
- **Route Structure**: DashboardLayout wraps all dashboard sections with shared sidebar navigation
- **Navigation Flow**: Sidebar includes sections (Providers, Analytics, Keys, Models, Chat) with active state tracking
- **State Preservation**: Chat sessions persist during dashboard navigation via Zustand store with session context
- **Fallback Routing**: If backend APIs not ready, dashboard shows mock data with loading states

**AppShell Integration Strategy**:
- **DashboardLayout Extension**: DashboardLayout extends AppShell by replacing sidebar content with dashboard navigation
- **Sidebar Integration**: Current SessionList sidebar replaced with dashboard navigation (Providers, Analytics, Keys, Models, Chat tabs)
- **Header Preservation**: AppShell header with cost rate badge and settings button preserved
- **Main Content Area**: Dashboard sections render in main content area previously used by ChatWindow
- **State Management**: Dashboard state extends existing Zustand patterns with dashboard-specific data

**Routing Transition Strategy**:
- **Phase 1**: Add React Router with `/dashboard` route alongside existing `/` route
- **Phase 2**: Dashboard accessible via `/dashboard` while chat remains at `/`
- **Phase 3**: Redirect `/` to `/dashboard` after dashboard validation
- **Phase 4**: `/dashboard/chat` becomes the chat interface with preserved functionality

**Authentication Integration**:
- **Route Guards**: Dashboard routes wrapped with authentication check using current JWT token
- **Login Flow**: Unauthenticated users redirected to enhanced login page
- **Session Management**: Extend current dev login to support dashboard user sessions
- **API Integration**: Dashboard API calls include authentication headers from current auth system

**State Management Architecture**:
- **Dashboard Store**: Extends existing Zustand patterns for dashboard-specific state
- **Provider State**: `{providers: [], loading: false, error: null}`
- **Analytics State**: `{usage: {}, loading: false}`
- **Configuration State**: `{models: [], settings: {}}`
- **Error Handling**: Graceful error states with retry mechanisms

**Component Architecture**:
- **DashboardLayout**: Extends AppShell with dashboard-specific navigation
- **DashboardRouter**: Handles routing between dashboard sections
- **Section Components**: ProviderManagement, AnalyticsDisplay, KeyManagement, ModelConfiguration, ChatIntegration
- **Error Boundaries**: Wraps each section for isolated error handling

**Authentication Flow**:
- **Route Guards**: Dashboard routes wrapped with authentication check component
- **Login Redirect**: Unauthenticated users redirected to `/login` with return URL
- **Session Validation**: JWT tokens validated on route navigation with automatic refresh
- **API Integration**: All dashboard API calls include `Authorization: Bearer {token}` headers
- **Error Handling**: Authentication failures trigger logout and redirect to login
- **Fallback Auth**: If enhanced auth not ready, use current dev login with dashboard access

**Data Loading Strategy**:
- **Lazy Loading**: Dashboard sections load data on demand
- **Caching**: Provider status and analytics cached with TTL
- **Background Sync**: Critical data refreshed in background
- **Offline Support**: Dashboard works with cached data when offline

**UI Component Pattern Specifications**:
- **Button Usage**: Use `variant="primary"` for main actions, `variant="ghost"` for secondary, `variant="danger"` for destructive
- **Badge Patterns**: Use status badges with color coding (green=healthy, red=error, yellow=warning)
- **Modal Implementation**: Follow existing Modal pattern with header, content, and footer sections
- **Input Components**: Use existing Input component with proper validation and error states
- **Card Layouts**: Create dashboard cards following 9router card patterns with status indicators

**State Management Integration**:
- **Dashboard Store**: Create `useDashboardStore` extending existing Zustand patterns
- **Store Structure**: `{providers: [], analytics: {}, config: {}, loading: false, error: null}`
- **Persistence**: Follow settings store pattern with localStorage persistence for user preferences
- **Integration**: Dashboard store integrates with existing stores (sessions, settings, usage) via shared actions

**Mobile Responsiveness Strategy**:
- **Sidebar Behavior**: Preserve existing mobile collapse (`max-width: 640px`)
- **Dashboard Sections**: Adapt to mobile with responsive layouts
- **Navigation**: Mobile-friendly sidebar with touch interactions
- **Breakpoints**: Use existing CSS custom properties and responsive patterns

**Error Handling Strategy**:
- **Backend Unavailable**: Dashboard shows mock data with "backend not ready" indicator
- **Authentication Failure**: Redirect to login page with return URL
- **API Errors**: Show error states with retry functionality
- **Loading States**: Show loading indicators for async operations

**Fallback Strategy for Backend Dependencies**:
- **Mock Data First**: Frontend development proceeds with mock services regardless of backend API readiness
- **Progressive Enhancement**: Dashboard works with mock data, enhances with real APIs when available
- **Feature Flags**: Dashboard features can be enabled/disabled based on backend API availability
- **Graceful Degradation**: If backend APIs fail, dashboard shows cached data with error states
- **Independent Development**: Frontend and backend development can proceed in parallel

---

## Work Objectives

### Core Objective
Transform Synaxis from chat-focused interface to comprehensive dashboard management interface similar to 9router, providing visibility and control over AI providers, usage analytics, and configuration.

### Concrete Deliverables
- Dashboard layout with sidebar navigation
- Provider management cards with status indicators
- Usage analytics charts and monitoring
- Model configuration interface
- API key management interface
- Responsive design maintaining mobile compatibility

### Definition of Done
- [x] Dashboard loads with provider status cards
- [x] Analytics charts display usage data
- [x] Provider management interface functional
- [x] API key creation/revocation works
- [x] Model configuration persists
- [x] All tests pass
- [x] Existing chat functionality preserved or integrated

### Must Have
- Provider status visualization
- Usage analytics display
- API key management
- Model configuration
- Responsive design
- Integration with existing backend APIs

### Must NOT Have (Guardrails)
- Advanced provider analytics beyond basic usage
- Real-time provider monitoring
- Complex provider configuration workflows
- Multi-tenant user management
- Advanced authentication features
- AI slop patterns (over-abstraction, documentation bloat)

---

## Verification Strategy

### Test Decision
- **Infrastructure exists**: YES
- **User wants tests**: YES (TDD)
- **Framework**: npx vitest run (existing setup)

### TDD Enabled
Each TODO follows RED-GREEN-REFACTOR:

**Task Structure:**
1. **RED**: Write failing test first
   - Test file: `[path].test.tsx`
   - Test command: `npx vitest run [file]`
   - Expected: FAIL (test exists, implementation doesn't)
2. **GREEN**: Implement minimum code to pass
   - Command: `npx vitest run [file]`
   - Expected: PASS
3. **REFACTOR**: Clean up while keeping green
   - Command: `npx vitest run [file]`
   - Expected: PASS (still)

### Automated Verification (Agent-Executable)

**For Frontend/UI changes** (using vitest + @testing-library/react):
```bash
# Agent runs component tests:
npm run test -- src/features/dashboard/DashboardLayout.test.tsx
# Assert: Tests pass (0 failures)

# Agent runs integration tests:
npm run test -- src/features/dashboard/integration.test.tsx
# Assert: Integration tests pass (0 failures)
```

**For API Integration changes** (using Bash curl):
```bash
# Agent runs:
curl -s http://localhost:8080/api/providers | jq '.providers | length'
# Assert: Output is greater than 0
# Assert: HTTP status 200
```

**Evidence Requirements**:
- Terminal output from verification commands captured
- Screenshots saved to .sisyphus/evidence/ for visual verification
- JSON response fields validated with specific assertions
- Exit codes checked (0 = success)

---

## Execution Strategy

### Parallel Execution Waves

```
Wave 0 (Backend APIs - PARALLEL DEVELOPMENT):
├── Task B1: Provider management API endpoints
├── Task B2: Analytics and usage API endpoints
├── Task B3: Configuration management API endpoints
└── Task B4: Enhanced authentication system

Wave 1 (Frontend Foundation):
├── Task 1: Install React Router Dependencies
├── Task 2: Dashboard layout foundation
└── Task 3: Mock data services

Wave 2 (Core Features):
├── Task 4: Provider management interface
├── Task 5: Usage analytics display
├── Task 6: API key management
└── Task 7: Model configuration

Wave 3 (Integration):
├── Task 8: Integrate with backend APIs
├── Task 9: Responsive design polish
└── Task 10: Testing and cleanup
└── Task 11: Extend Zustand stores

Critical Path: Task 1 → Task 2 → Task 4 → Task 8 → Task 9 (Frontend can proceed independently)
```

### Dependency Matrix

| Task | Depends On | Blocks | Can Parallelize With |
|------|------------|--------|---------------------|
| B1 | None | 8 | B2, B3, B4 |
| B2 | None | 8 | B1, B3, B4 |
| B3 | None | 8 | B1, B2, B4 |
| B4 | None | 8 | B1, B2, B3 |
| 1 | None | 2, 3 | None |
| 2 | 1 | 4, 5, 6, 7 | 3 |
| 3 | 1 | 4, 5, 6, 7 | 2 |
| 4 | 2, 3, B1, B2, B3, B4 | 8 | 5, 6, 7 |
| 5 | 2, 3, B1, B2, B3, B4 | 8 | 4, 6, 7 |
| 6 | 2, 3, B1, B2, B3, B4 | 8 | 4, 5, 7 |
| 7 | 2, 3, B1, B2, B3, B4 | 8 | 4, 5, 6 |
| 8 | 4, 5, 6, 7 | 9, 10 | None |
| 9 | 8 | 10, 11 | None |
| 10 | 8, 9 | 11 | None |
| 11 | 10 | None | None |

### Agent Dispatch Summary

| Wave | Tasks | Recommended Agents |
|------|-------|-------------------|
| 0 | B1, B2, B3, B4 | delegate_task(category="unspecified-high", load_skills=[], run_in_background=true) |
| 1 | 1, 2, 3 | delegate_task(category="quick", load_skills=["frontend-ui-ux"], run_in_background=true) |
| 2 | 4, 5, 6, 7 | delegate_task(category="visual-engineering", load_skills=["frontend-ui-ux"], run_in_background=true) |
| 3 | 8, 9, 10 | delegate_task(category="quick", load_skills=["frontend-ui-ux"], run_in_background=true) |

---

## TODOs

### Wave 0: Backend APIs (PREREQUISITE)

- [x] B1. Provider Management API Endpoints

  **What to do**:
  - Create provider management controller with CRUD operations
  - Add endpoints for listing providers, status checks, configuration
  - Implement provider health monitoring endpoints
  - Ensure API follows existing controller patterns

  **Must NOT do**:
  - Break existing API endpoints
  - Change current authentication patterns
  - Create complex provider orchestration

  **Recommended Agent Profile**:
  - **Category**: `unspecified-high`
    - Reason: Backend API development requires careful architecture
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 0 (with Tasks B2, B3, B4, 1, 2, 3)
  - **Blocks**: Task 8 (frontend integration)
  - **Blocked By**: None (can start immediately)

**References**:
- `src/InferenceGateway/WebApi/Endpoints/OpenAI/OpenAIEndpointsExtensions.cs:1-161` - Minimal API endpoint patterns (use this pattern)
- `src/InferenceGateway/WebApi/Endpoints/OpenAI/ModelsEndpoint.cs:1-50` - Simple GET endpoint patterns
- `src/InferenceGateway/WebApi/Controllers/AuthController.cs:1-56` - JWT authentication patterns

**Acceptance Criteria**:
- [x] Provider listing endpoint returns provider data
- [x] Provider status endpoint returns health status
- [x] Provider configuration endpoint works

**Automated Verification**:
```bash
# Agent runs API tests:
curl -s http://localhost:5000/api/providers | jq '.providers | length > 0'
# Assert: Output is "true"

curl -s http://localhost:5000/api/providers/status | jq '.status'
# Assert: Output is "healthy" or "unhealthy"

# Agent runs backend unit tests:
dotnet test src/InferenceGateway/WebApi.Tests/ProvidersEndpointTests.cs
# Assert: Tests pass (0 failures)
```

  **Evidence to Capture**:
  - [ ] Terminal output from API tests
  - [ ] HTTP response codes and content

**Commit**: YES
- Message: `feat(api): add provider management endpoints`
- Files: `src/InferenceGateway/WebApi/Endpoints/Dashboard/ProvidersEndpoint.cs`
- Pre-commit: `dotnet test`

- [x] B2. Analytics and Usage API Endpoints

  **What to do**:
  - Create analytics controller with usage tracking endpoints
  - Add endpoints for token usage, request statistics, provider performance
  - Implement time-based analytics aggregation
  - Ensure data follows existing usage patterns

  **Must NOT do**:
  - Create real-time streaming analytics
  - Add complex data aggregation logic
  - Break existing usage tracking

  **Recommended Agent Profile**:
  - **Category**: `unspecified-high`
    - Reason: Analytics API requires data architecture
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 0 (with Tasks B1, B3, B4)
  - **Blocks**: Task 8 (frontend integration)
  - **Blocked By**: None (can start immediately)

**References**:
- `src/Synaxis.WebApp/ClientApp/src/stores/usage.ts:1-25` - Usage tracking patterns
- `src/InferenceGateway/WebApi/Endpoints/OpenAI/OpenAIEndpointsExtensions.cs:1-161` - Minimal API patterns

  **Acceptance Criteria**:
  - [ ] Usage statistics endpoint returns data
  - [ ] Analytics endpoints provide time-based data
  - [ ] Provider performance metrics available

  **Automated Verification**:
  ```bash
  # Agent runs API tests:
  curl -s http://localhost:5000/api/analytics/usage | jq '.totalTokens > 0'
  # Assert: Output is "true"

  curl -s http://localhost:5000/api/analytics/providers | jq '.providers | length > 0'
  # Assert: Output is "true"
  ```

  **Evidence to Capture**:
  - [ ] Terminal output from API tests
  - [ ] Analytics data structure validation

**Commit**: YES
- Message: `feat(api): add analytics and usage endpoints`
- Files: `src/InferenceGateway/WebApi/Endpoints/Dashboard/AnalyticsEndpoint.cs`
- Pre-commit: `dotnet test`

- [x] B3. Configuration Management API Endpoints

  **What to do**:
  - Create configuration controller for model and system settings
  - Add endpoints for model configuration, system settings, user preferences
  - Implement configuration validation and persistence
  - Ensure configuration follows existing patterns

  **Must NOT do**:
  - Create complex configuration workflows
  - Add advanced configuration validation
  - Break existing configuration systems

  **Recommended Agent Profile**:
  - **Category**: `unspecified-high`
    - Reason: Configuration API requires careful design
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 0 (with Tasks B1, B2, B4)
  - **Blocks**: Task 8 (frontend integration)
  - **Blocked By**: None (can start immediately)

  **References**:
  - `src/InferenceGateway/WebApi/Endpoints/OpenAI/ModelsEndpoint.cs:1-50` - Model configuration patterns
  - `src/Synaxis.WebApp/ClientApp/src/stores/settings.ts:1-24` - Settings patterns

  **Acceptance Criteria**:
  - [ ] Model configuration endpoint works
  - [ ] System settings endpoint functional
  - [ ] Configuration validation implemented

  **Automated Verification**:
  ```bash
  # Agent runs API tests:
  curl -s http://localhost:5000/api/config/models | jq '.models | length > 0'
  # Assert: Output is "true"

  curl -s -X POST http://localhost:5000/api/config/models -H "Content-Type: application/json" -d '{"model":"test"}' | jq '.success'
  # Assert: Output is "true"
  ```

  **Evidence to Capture**:
  - [ ] Terminal output from API tests
  - [ ] Configuration validation results

**Commit**: YES
- Message: `feat(api): add configuration management endpoints`
- Files: `src/InferenceGateway/WebApi/Endpoints/Dashboard/ConfigurationEndpoint.cs`
- Pre-commit: `dotnet test`

- [x] B4. Enhanced Authentication System

  **What to do**:
  - Extend current dev authentication to proper user management
  - Add user registration, login, session management
  - Implement API key authentication with user scoping
  - Ensure backward compatibility with existing dev auth

  **Must NOT do**:
  - Break existing dev authentication
  - Create complex multi-tenant systems
  - Remove current authentication patterns

  **Recommended Agent Profile**:
  - **Category**: `unspecified-high`
    - Reason: Authentication requires security expertise
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 0 (with Tasks B1, B2, B3)
  - **Blocks**: Task 8 (frontend integration)
  - **Blocked By**: None (can start immediately)

**References**:
- `src/InferenceGateway/WebApi/Controllers/AuthController.cs:1-56` - Existing JWT auth patterns
- `src/InferenceGateway/WebApi/Endpoints/OpenAI/OpenAIEndpointsExtensions.cs:1-161` - Minimal API patterns

  **Acceptance Criteria**:
  - [ ] User registration and login work
  - [ ] API keys are user-scoped
  - [ ] Backward compatibility maintained

  **Automated Verification**:
  ```bash
  # Agent runs auth tests:
  curl -s -X POST http://localhost:5000/auth/register -H "Content-Type: application/json" -d '{"email":"test@test.com","password":"test"}' | jq '.success'
  # Assert: Output is "true"

  curl -s -X POST http://localhost:5000/auth/login -H "Content-Type: application/json" -d '{"email":"test@test.com","password":"test"}' | jq '.token'
  # Assert: Output contains JWT token
  ```

  **Evidence to Capture**:
  - [ ] Terminal output from auth tests
  - [ ] Authentication token validation

**Commit**: YES
- Message: `feat(auth): extend authentication for dashboard`
- Files: `src/InferenceGateway/WebApi/Controllers/AuthController.cs`
- Pre-commit: `dotnet test`

### Wave 1: Foundation

- [x] 1. Install React Router Dependencies

  **What to do**:
  - Add `react-router-dom` dependency to package.json
  - Configure BrowserRouter in main.tsx
  - Set up basic routing structure
  - Ensure existing App component integrates with routing

  **Must NOT do**:
  - Break existing functionality
  - Remove current App structure
  - Create complex routing prematurely

  **Recommended Agent Profile**:
  - **Category**: `quick`
    - Reason: Dependency installation is straightforward
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1 (with Tasks 2, 3, B1, B2, B3, B4)
  - **Blocks**: Task 2, Task 3
  - **Blocked By**: None (can start immediately)

  **References**:
  - `src/Synaxis.WebApp/ClientApp/package.json:1-49` - Current dependencies structure
  - `src/Synaxis.WebApp/ClientApp/src/main.tsx:1-15` - Current app entry point

  **Acceptance Criteria**:
  - [ ] react-router-dom dependency added
  - [ ] BrowserRouter configured in main.tsx
  - [ ] Basic routing structure working

  **Automated Verification**:
  ```bash
  # Agent checks dependency installation:
  grep "react-router-dom" package.json
  # Assert: Dependency found in package.json

  # Agent checks routing setup:
  grep "BrowserRouter" src/main.tsx
  # Assert: BrowserRouter found in main.tsx
  ```

  **Evidence to Capture**:
  - [ ] Terminal output from verification commands

  **Commit**: YES
  - Message: `feat(dashboard): add React Router dependencies`
  - Files: `package.json`, `src/main.tsx`
  - Pre-commit: `npx vitest run`

- [x] 2. Dashboard Layout Foundation

  **What to do**:
  - Create DashboardLayout component extending AppShell structure
  - Replace AppShell sidebar SessionList with dashboard navigation (Providers, Analytics, Keys, Models, Chat tabs)
  - Implement main content area for dashboard sections using React Router Outlet
  - Preserve AppShell header with cost rate badge and settings button
  - Ensure responsive design maintains mobile compatibility with collapsible sidebar
  - Integrate with existing Zustand stores for state preservation

  **Must NOT do**:
  - Break existing chat functionality
  - Create new state management system
  - Remove existing session management

  **Recommended Agent Profile**:
  - **Category**: `visual-engineering`
    - Reason: Layout design requires visual/UI expertise
  - **Skills**: [`frontend-ui-ux`]
    - `frontend-ui-ux`: Needed for responsive design and layout patterns

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1 (with Tasks 2, 3, B1, B2, B3, B4)
  - **Blocks**: Task 4, Task 5, Task 6, Task 7
  - **Blocked By**: None (can start immediately)

**References**:
- `src/Synaxis.WebApp/ClientApp/src/components/layout/AppShell.tsx:1-48` - Current AppShell implementation pattern (sidebar, header, main content)
- `src/Synaxis.WebApp/ClientApp/src/App.tsx:1-23` - Current app structure (AppShell wrapping ChatWindow)
- `src/Synaxis.WebApp/ClientApp/src/index.css:1-50` - CSS variables and styling patterns (custom properties for Miser theme)
- `src/Synaxis.WebApp/ClientApp/src/components/ui/Button.tsx:1-30` - UI component patterns (variants: primary, ghost, danger)
- `src/Synaxis.WebApp/ClientApp/src/stores/sessions.ts:1-37` - Zustand store pattern with async actions
- `src/Synaxis.WebApp/ClientApp/src/stores/settings.ts:1-24` - Zustand store with persistence

**Acceptance Criteria**:
- [x] Dashboard component created: `src/features/dashboard/DashboardLayout.tsx`
- [x] Sidebar navigation displays dashboard sections (Providers, Analytics, Keys, Models, Chat)
- [x] Main content area loads dashboard sections via React Router `Outlet`
- [x] Responsive design works on mobile breakpoints (sidebar collapses, dashboard adapts)
- [x] Existing chat functionality preserved at `/dashboard/chat` route
- [x] AppShell header preserved with cost rate badge and settings button

  **Automated Verification**:
  ```bash
  # Agent runs component tests:
  npm run dev & sleep 5 && npx vitest run src/features/dashboard/DashboardLayout.test.tsx
  # Assert: Tests pass (0 failures)

  # Agent runs routing integration tests:
  npx vitest run src/features/dashboard/routing.test.tsx
  # Assert: Routing tests pass (0 failures)
  ```

  **Evidence to Capture**:
  - [ ] Terminal output from test commands
  - [ ] Test result logs

  **Commit**: YES
  - Message: `feat(dashboard): add dashboard layout foundation`
  - Files: `src/features/dashboard/DashboardLayout.tsx`
  - Pre-commit: `npm run test -- src/features/dashboard/DashboardLayout.test.tsx`

- [x] 2. Extend Zustand Stores for Dashboard State

  **What to do**:
  - Create dashboard store extending existing Zustand patterns
  - Add state for providers, analytics, configuration
  - Implement actions for managing dashboard data
  - Ensure persistence follows existing settings store pattern

  **Must NOT do**:
  - Create new state management library
  - Break existing stores
  - Change current state persistence patterns

  **Recommended Agent Profile**:
  - **Category**: `quick`
    - Reason: State management is straightforward extension
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1 (with Tasks 1, 3, B1, B2, B3, B4)
  - **Blocks**: Task 4, Task 5, Task 6, Task 7
  - **Blocked By**: None (can start immediately)

**References**:
- `src/Synaxis.WebApp/ClientApp/src/stores/settings.ts:1-24` - Zustand store pattern with persistence (gatewayUrl, costRate)
- `src/Synaxis.WebApp/ClientApp/src/stores/sessions.ts:1-37` - Store with async actions pattern (loadSessions, createSession, deleteSession)
- `src/Synaxis.WebApp/ClientApp/src/stores/usage.ts:1-25` - Usage tracking store pattern (token counting, database initialization)

**Dashboard Store Implementation Pattern**:
```typescript
// src/stores/dashboard.ts
export const useDashboardStore = create<DashboardState>()(
  devtools(
    persist(
      (set, get) => ({
        providers: [],
        analytics: {},
        config: {},
        loading: false,
        error: null,
        // Async actions
        loadProviders: async () => {
          set({ loading: true, error: null })
          try {
            const providers = await providerService.getProviders()
            set({ providers, loading: false })
          } catch (error) {
            set({ error: error.message, loading: false })
          }
        }
      }),
      { name: 'synaxis-dashboard' }
    )
  )
)
```

**Acceptance Criteria**:
- [x] Dashboard store created: `src/stores/dashboard.ts`
- [x] Store manages provider state (providers array, loading, error)
- [x] Store manages analytics data (usage statistics, performance metrics)
- [x] Store manages configuration state (model settings, user preferences)
- [x] Store integrates with existing patterns (devtools, persistence)
- [x] Store provides async actions for data loading
- [x] Store handles error states gracefully

  **Automated Verification**:
  ```bash
  # Agent runs via Node.js test:
  bun -e "import { useDashboardStore } from './src/stores/dashboard'; console.log('Store loaded successfully')"
  # Assert: Output is "Store loaded successfully"

  npx vitest run src/stores/dashboard.test.ts
  # Assert: All tests pass (0 failures)
  ```

  **Evidence to Capture**:
  - [ ] Terminal output from test commands

  **Commit**: YES
  - Message: `feat(dashboard): add dashboard state management`
  - Files: `src/stores/dashboard.ts`
  - Pre-commit: `npx vitest run src/stores/dashboard.test.ts`

- [x] 3. Mock Data Services for Dashboard Features

  **What to do**:
  - Create mock data services for providers, analytics, configuration
  - Implement mock API clients that return realistic data
  - Create data structures matching backend API responses
  - Ensure mock data can be easily replaced with real APIs

  **Must NOT do**:
  - Hardcode data that can't be replaced
  - Create complex mock logic
  - Mock data that doesn't match real API structure

  **Recommended Agent Profile**:
  - **Category**: `quick`
    - Reason: Mock data creation is straightforward
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1 (with Tasks 1, 2)
  - **Blocks**: Task 4, Task 5, Task 6, Task 7
  - **Blocked By**: None (can start immediately)

**References**:
- `src/Synaxis.WebApp/ClientApp/src/api/client.ts:1-52` - API client pattern (GatewayClient with configurable baseURL)
- `src/Synaxis.WebApp/ClientApp/src/db/db.ts:1-36` - Data structure patterns (Dexie schema for sessions, messages)
- `src/InferenceGateway/WebApi/Endpoints/OpenAI/ModelsEndpoint.cs:1-50` - Backend API response structure (OpenAI-compatible models endpoint)

**Mock Service Implementation Pattern**:
```typescript
// src/services/mockProviderService.ts
export const mockProviderService = {
  getProviders: async (): Promise<Provider[]> => [
    {
      id: 'groq',
      name: 'Groq',
      status: 'healthy',
      tier: 0,
      models: ['llama-3.1-70b-versatile'],
      usage: { totalTokens: 15000, requests: 45 }
    }
  ],
  getProviderStatus: async (id: string): Promise<ProviderStatus> => ({
    status: 'healthy',
    lastChecked: new Date().toISOString()
  })
}

// Real service interface matches mock exactly
export const realProviderService = {
  getProviders: async (): Promise<Provider[]> => {
    const response = await fetch('/api/providers')
    return response.json()
  }
}
```

**Acceptance Criteria**:
- [x] Mock provider service created with realistic data
- [x] Mock analytics service created with usage statistics
- [x] Mock configuration service created with model settings
- [x] Mock data matches backend API structure exactly
- [x] Mock services can be easily replaced with real services
- [x] Frontend development proceeds independently of backend availability

  **Automated Verification**:
  ```bash
  # Agent runs via Node.js test:
  bun -e "import { mockProviderService } from './src/services/mockProviderService'; console.log(mockProviderService.getProviders().length)"
  # Assert: Output is greater than 0

  npx vitest run src/services/mockProviderService.test.ts
  # Assert: All tests pass (0 failures)
  ```

  **Evidence to Capture**:
  - [ ] Terminal output from test commands

  **Commit**: YES
  - Message: `feat(dashboard): add mock data services`
  - Files: `src/services/mockProviderService.ts`, `src/services/mockAnalyticsService.ts`, `src/services/mockConfigService.ts`
  - Pre-commit: `npx vitest run src/services/*.test.ts`

### Wave 2: Core Features

- [x] 4. Provider Management Interface

  **What to do**:
  - Create provider cards component with status indicators
  - Implement provider list view with grid layout
  - Add provider status (online/offline/error) with visual indicators
  - Create provider configuration modal
  - Follow 9router card patterns with enhanced status visualization

  **Must NOT do**:
  - Over-engineer provider cards
  - Create complex provider configuration workflows
  - Add real-time monitoring beyond basic status

  **Recommended Agent Profile**:
  - **Category**: `visual-engineering`
    - Reason: UI design and component creation
  - **Skills**: [`frontend-ui-ux`]
    - `frontend-ui-ux`: Needed for card design and visual patterns

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 2 (with Tasks 5, 6, 7)
  - **Blocks**: Task 8
  - **Blocked By**: Task 1, Task 2, Task 3

  **References**:
  - `src/Synaxis.WebApp/ClientApp/src/components/ui/Badge.tsx:1-30` - Badge component for status indicators
  - `src/Synaxis.WebApp/ClientApp/src/components/ui/Modal.tsx:1-50` - Modal pattern for configuration
  - `src/Synaxis.WebApp/ClientApp/src/features/sessions/SessionList.tsx:1-34` - List view patterns

  **Acceptance Criteria**:
  - [ ] Provider cards display status with visual indicators
  - [ ] Provider list shows in responsive grid
  - [ ] Configuration modal opens and closes properly
  - [ ] Status indicators update based on mock data

  **Automated Verification**:
  ```bash
  # Agent runs provider component tests:
  npm run test -- src/features/dashboard/providers/ProviderCards.test.tsx
  # Assert: Tests pass (0 failures)

  # Agent runs modal integration tests:
  npm run test -- src/features/dashboard/providers/ProviderModal.test.tsx
  # Assert: Modal tests pass (0 failures)
  ```

  **Evidence to Capture**:
  - [ ] Terminal output from test commands
  - [ ] Test result logs

  **Commit**: YES
  - Message: `feat(dashboard): add provider management interface`
  - Files: `src/features/dashboard/providers/ProviderCards.tsx`, `src/features/dashboard/providers/ProviderModal.tsx`
  - Pre-commit: `npx vitest run src/features/dashboard/providers/*.test.tsx`

- [x] 5. Usage Analytics Display

  **What to do**:
  - Create analytics charts component
  - Implement usage statistics display
  - Add time-based usage visualization
  - Create provider performance comparison
  - Follow 9router analytics patterns with enhanced charts

  **Must NOT do**:
  - Over-engineered charting with complex interactions
  - Real-time streaming analytics
  - Advanced filtering beyond basic time ranges

  **Recommended Agent Profile**:
  - **Category**: `visual-engineering`
    - Reason: Data visualization requires UI expertise
  - **Skills**: [`frontend-ui-ux`]
    - `frontend-ui-ux`: Needed for chart design and data presentation

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 2 (with Tasks 4, 6, 7)
  - **Blocks**: Task 8
  - **Blocked By**: Task 1, Task 2, Task 3

  **References**:
  - `src/Synaxis.WebApp/ClientApp/src/stores/usage.ts:1-25` - Usage tracking patterns
  - `src/Synaxis.WebApp/ClientApp/src/components/ui/Badge.tsx:1-30` - Display patterns for metrics

  **Acceptance Criteria**:
- [x] Analytics charts display usage data
  - [ ] Usage statistics show token counts
  - [ ] Time-based visualization works
  - [ ] Provider comparison displays correctly

  **Automated Verification**:
  ```bash
  # Agent runs analytics component tests:
  npm run test -- src/features/dashboard/analytics/AnalyticsCharts.test.tsx
  # Assert: Chart tests pass (0 failures)

  # Agent runs usage stats tests:
  npm run test -- src/features/dashboard/analytics/UsageStats.test.tsx
  # Assert: Usage tests pass (0 failures)
  ```

  **Evidence to Capture**:
  - [ ] Terminal output from test commands
  - [ ] Test result logs

  **Commit**: YES
  - Message: `feat(dashboard): add usage analytics display`
  - Files: `src/features/dashboard/analytics/AnalyticsCharts.tsx`, `src/features/dashboard/analytics/UsageStats.tsx`
  - Pre-commit: `npx vitest run src/features/dashboard/analytics/*.test.tsx`

- [x] 6. API Key Management Interface

  **What to do**:
  - Create API key list component
  - Implement key creation and revocation
  - Add key copy functionality
  - Create key management modal
  - Follow security best practices for key handling

  **Must NOT do**:
  - Store keys insecurely
  - Expose keys in UI without proper masking
  - Create complex key permission systems

  **Recommended Agent Profile**:
  - **Category**: `quick`
    - Reason: Form-based interface is straightforward
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 2 (with Tasks 4, 5, 7)
  - **Blocks**: Task 8
  - **Blocked By**: Task 1, Task 2, Task 3

  **References**:
  - `src/Synaxis.WebApp/ClientApp/src/components/ui/Modal.tsx:1-50` - Modal pattern
  - `src/Synaxis.WebApp/ClientApp/src/components/ui/Input.tsx:1-30` - Input component patterns
  - `src/InferenceGateway/WebApi/Controllers/ApiKeysController.cs:1-83` - Backend API structure

  **Acceptance Criteria**:
  - [ ] API key list displays existing keys
  - [ ] Key creation modal works
  - [ ] Key revocation functions properly
  - [ ] Key copying works securely

  **Automated Verification**:
  ```bash
  # Agent runs API key component tests:
  npm run test -- src/features/dashboard/keys/ApiKeyList.test.tsx
  # Assert: Key list tests pass (0 failures)

  # Agent runs key creation modal tests:
  npm run test -- src/features/dashboard/keys/KeyCreationModal.test.tsx
  # Assert: Modal tests pass (0 failures)
  ```

  **Evidence to Capture**:
  - [ ] Terminal output from test commands
  - [ ] Test result logs

  **Commit**: YES
  - Message: `feat(dashboard): add API key management`
  - Files: `src/features/dashboard/keys/ApiKeyList.tsx`, `src/features/dashboard/keys/KeyCreationModal.tsx`
  - Pre-commit: `npx vitest run src/features/dashboard/keys/*.test.tsx`

- [x] 7. Model Configuration Interface

  **What to do**:
  - Create model list component
  - Implement model configuration forms
  - Add model capability display
  - Create model selection interface
  - Follow backend model configuration patterns

  **Must NOT do**:
  - Over-complicate model configuration
  - Create advanced model tuning interfaces
  - Add model training capabilities

  **Recommended Agent Profile**:
  - **Category**: `quick`
    - Reason: Configuration interface is straightforward
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 2 (with Tasks 4, 5, 6)
  - **Blocks**: Task 8
  - **Blocked By**: Task 1, Task 2, Task 3

  **References**:
  - `src/InferenceGateway/WebApi/Endpoints/OpenAI/ModelsEndpoint.cs:1-50` - Backend model API structure
  - `src/Synaxis.WebApp/ClientApp/src/components/ui/Badge.tsx:1-30` - Capability badge patterns
  - `src/Synaxis.WebApp/ClientApp/src/components/ui/Input.tsx:1-30` - Form input patterns

  **Acceptance Criteria**:
  - [ ] Model list displays available models
  - [ ] Model capabilities show as badges
  - [ ] Configuration forms work properly
  - [ ] Model selection updates store

  **Automated Verification**:
  ```bash
  # Agent runs model list component tests:
  npm run test -- src/features/dashboard/models/ModelList.test.tsx
  # Assert: Model list tests pass (0 failures)

  # Agent runs model configuration tests:
  npm run test -- src/features/dashboard/models/ModelConfiguration.test.tsx
  # Assert: Configuration tests pass (0 failures)
  ```

  **Evidence to Capture**:
  - [ ] Terminal output from test commands
  - [ ] Test result logs

  **Commit**: YES
  - Message: `feat(dashboard): add model configuration interface`
  - Files: `src/features/dashboard/models/ModelList.tsx`, `src/features/dashboard/models/ModelConfiguration.tsx`
  - Pre-commit: `npx vitest run src/features/dashboard/models/*.test.tsx`

### Wave 3: Integration

- [x] 8. Integrate with Backend APIs

  **What to do**:
  - Replace mock services with real API clients
  - Integrate provider management with backend endpoints
  - Connect analytics to real usage data
  - Implement API key management with backend
  - Ensure error handling and loading states

  **Must NOT do**:
  - Break existing chat functionality
  - Remove fallback to mock data
  - Create complex error recovery systems

  **Recommended Agent Profile**:
  - **Category**: `quick`
    - Reason: API integration is straightforward replacement
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: NO
  - **Parallel Group**: Wave 3 (sequential)
  - **Blocks**: Task 9, Task 10
  - **Blocked By**: Task 4, Task 5, Task 6, Task 7

  **References**:
  - `src/Synaxis.WebApp/ClientApp/src/api/client.ts:1-52` - Existing API client pattern
  - `src/InferenceGateway/WebApi/Controllers/ApiKeysController.cs:1-83` - Backend API endpoints
  - `src/InferenceGateway/WebApi/Endpoints/OpenAI/ModelsEndpoint.cs:1-50` - Model API endpoints

  **Acceptance Criteria**:
  - [ ] Real API clients replace mock services
  - [ ] Provider status loads from backend
  - [ ] Usage analytics shows real data
  - [ ] API key management works with backend

  **Automated Verification**:
  ```bash
  # Agent runs API integration tests:
  curl -s http://localhost:8080/api/providers | jq '.providers | length'
  # Assert: Output is greater than 0

  npx vitest run src/features/dashboard/integration.test.ts
  # Assert: All integration tests pass (0 failures)
  ```

  **Evidence to Capture**:
  - [ ] Terminal output from API tests
  - [ ] Integration test results

  **Commit**: YES
  - Message: `feat(dashboard): integrate with backend APIs`
  - Files: `src/services/realProviderService.ts`, `src/services/realAnalyticsService.ts`, `src/services/realConfigService.ts`
  - Pre-commit: `npx vitest run src/services/*.test.ts`

- [x] 9. Responsive Design Polish

  **What to do**:
  - Ensure all dashboard components work on mobile
  - Polish responsive breakpoints
  - Optimize touch interactions
  - Test across different screen sizes

  **Must NOT do**:
  - Create complex responsive logic
  - Add unnecessary animations
  - Over-optimize for edge cases

  **Recommended Agent Profile**:
  - **Category**: `visual-engineering`
    - Reason: Responsive design requires UI expertise
  - **Skills**: [`frontend-ui-ux`]
    - `frontend-ui-ux`: Needed for responsive design patterns

  **Parallelization**:
  - **Can Run In Parallel**: NO
  - **Parallel Group**: Wave 3 (sequential)
  - **Blocks**: Task 10
  - **Blocked By**: Task 8

  **References**:
  - `src/Synaxis.WebApp/ClientApp/src/index.css:1-100` - Existing responsive patterns
  - `src/Synaxis.WebApp/ClientApp/src/components/layout/AppShell.tsx:1-48` - Mobile sidebar patterns

  **Acceptance Criteria**:
  - [ ] Dashboard works on mobile breakpoints
  - [ ] Touch interactions work properly
  - [ ] All components adapt to screen size

  **Automated Verification**:
  ```bash
  # Agent runs responsive design tests:
  npm run test -- src/features/dashboard/responsive.test.tsx
  # Assert: Responsive tests pass (0 failures)

  # Agent runs mobile layout tests:
  npm run test -- src/features/dashboard/mobile.test.tsx
  # Assert: Mobile layout tests pass (0 failures)
  ```

  **Evidence to Capture**:
  - [ ] Terminal output from test commands
  - [ ] Test result logs

  **Commit**: YES
  - Message: `style(dashboard): polish responsive design`
  - Files: Various dashboard component files
  - Pre-commit: `npx vitest run src/features/dashboard/responsive.test.tsx`

- [x] 10. Testing and Cleanup

  **What to do**:
  - Write comprehensive test suite
  - Clean up any temporary code
  - Ensure all features work together
  - Verify integration with existing chat functionality

  **Must NOT do**:
  - Remove useful temporary code
  - Over-test edge cases
  - Break working functionality

  **Recommended Agent Profile**:
  - **Category**: `quick`
    - Reason: Testing and cleanup is straightforward
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: NO
  - **Parallel Group**: Wave 3 (final task)
  - **Blocks**: None
  - **Blocked By**: Task 8, Task 9

  **References**:
  - `src/Synaxis.WebApp/ClientApp/src/__tests__/Integration.test.tsx:1-50` - Existing integration test patterns
  - `src/Synaxis.WebApp/ClientApp/src/test/setup.ts:1-30` - Test setup patterns

  **Acceptance Criteria**:
- [x] All tests pass
  - [ ] No console errors
  - [ ] Integration with chat works
  - [ ] Dashboard is production-ready

  **Automated Verification**:
  ```bash
  # Agent runs full test suite:
  npx vitest run
  # Assert: All tests pass (0 failures)

  # Check for console errors:
  bun run dev 2>&1 | grep -i error
  # Assert: No error output
  ```

  **Evidence to Capture**:
  - [ ] Terminal output from test commands
  - [ ] Build output logs

  **Commit**: YES
  - Message: `test(dashboard): add comprehensive test suite`
  - Files: Various test files
  - Pre-commit: `npx vitest run`

---

## Commit Strategy

| After Task | Message | Files | Verification |
|------------|---------|-------|--------------|
| B1 | `feat(api): add provider management endpoints` | `src/InferenceGateway/WebApi/Endpoints/Dashboard/ProvidersEndpoint.cs` | `dotnet test` |
| B2 | `feat(api): add analytics and usage endpoints` | `src/InferenceGateway/WebApi/Endpoints/Dashboard/AnalyticsEndpoint.cs` | `dotnet test` |
| B3 | `feat(api): add configuration management endpoints` | `src/InferenceGateway/WebApi/Endpoints/Dashboard/ConfigurationEndpoint.cs` | `dotnet test` |
| B4 | `feat(auth): extend authentication for dashboard` | `src/InferenceGateway/WebApi/Controllers/AuthController.cs` | `dotnet test` |
| 1 | `feat(dashboard): add React Router dependencies` | `package.json`, `src/main.tsx` | `npx vitest run` |
| 2 | `feat(dashboard): add dashboard layout foundation` | `src/features/dashboard/DashboardLayout.tsx` | `npx vitest run src/features/dashboard/DashboardLayout.test.tsx` |
| 3 | `feat(dashboard): add mock data services` | `src/services/mockProviderService.ts` etc | `npx vitest run src/services/*.test.ts` |
| 4 | `feat(dashboard): add provider management interface` | Provider component files | `npx vitest run src/features/dashboard/providers/*.test.tsx` |
| 5 | `feat(dashboard): add usage analytics display` | Analytics component files | `npx vitest run src/features/dashboard/analytics/*.test.tsx` |
| 6 | `feat(dashboard): add API key management` | Key management files | `npx vitest run src/features/dashboard/keys/*.test.tsx` |
| 7 | `feat(dashboard): add model configuration interface` | Model config files | `npx vitest run src/features/dashboard/models/*.test.tsx` |
| 8 | `feat(dashboard): integrate with backend APIs` | Real service files | `npx vitest run src/services/*.test.ts` |
| 9 | `style(dashboard): polish responsive design` | Various component files | `npx vitest run src/features/dashboard/responsive.test.tsx` |
| 10 | `test(dashboard): add comprehensive test suite` | Test files | `npx vitest run` |
| 11 | `feat(dashboard): add dashboard state management` | `src/stores/dashboard.ts` | `npx vitest run src/stores/dashboard.test.ts` |

---

## Success Criteria

### Verification Commands
```bash
# Full test suite
npx vitest run && dotnet test
# Assert: All tests pass (0 failures)

# Dashboard accessibility
curl -s http://localhost:8080/dashboard | grep -q "dashboard"
# Assert: Exit code 0 (dashboard page loads)

# Backend API integration
curl -s http://localhost:5000/api/providers | jq '.providers | length > 0'
# Assert: Output is "true"

# Analytics API integration
curl -s http://localhost:5000/api/analytics/usage | jq '.totalTokens > 0'
# Assert: Output is "true"

# Frontend API integration
curl -s http://localhost:8080/api/providers | jq '.providers | length > 0'
# Assert: Output is "true"
```

### Final Checklist
- [x] All "Must Have" present (provider mgmt, analytics, API keys, model config)
- [x] All "Must NOT Have" absent (no advanced features beyond MVP)
- [x] All tests pass
- [x] Responsive design works on mobile
- [x] Integration with backend APIs functional
- [x] Existing chat functionality preserved or integrated