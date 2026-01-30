# Draft: Frontend Enhancements for Synaxis.WebApp

## User Requirements
- **Inspiration**: 9router interface (https://github.com/decolua/9router)
- **Desired Features**: Sections for endpoints, providers (similar to 9router dashboard)
- **Current Frontend**: Basic chat interface with session management
- **Backend**: Reverse proxy to inference gateway with auth and API key management

## Current Frontend Assessment

### Existing Components:
- **AppShell**: Basic layout with sidebar (sessions) and main content area
- **SessionList**: Simple session management (create/delete)
- **ChatWindow**: Chat interface
- **SettingsDialog**: Basic settings (gateway URL, cost rate)

### Missing Features Compared to 9router:
- Provider management dashboard
- Endpoint configuration
- Usage analytics and monitoring
- Provider status and health checks
- Model management
- API key management interface

## 9router Interface Analysis

### Key Sections from 9router:
1. **Providers Section**: Manage AI providers (Groq, OpenAI, etc.)
2. **Endpoints Section**: API endpoint configuration
3. **Usage Tracking**: Real-time monitoring and analytics
4. **Model Management**: Configure available models
5. **API Keys**: Key management interface
6. **Dashboard**: Overview of system health and usage

## Technical Decisions Needed

### Architecture Questions:
1. Should we extend current components or create new dashboard sections?
2. How to integrate with existing backend API structure?
3. What data should be fetched from backend vs stored locally?

### Feature Prioritization:
1. Provider management dashboard
2. Usage analytics and monitoring
3. Model configuration interface
4. API key management
5. System health dashboard

## Research Findings

### Backend Capabilities:
- `/v1/*` routes proxied to inference gateway
- Auth system with dev login
- API key management endpoints
- Control plane database with providers, models, etc.

### Frontend Tech Stack:
- React + TypeScript + Vite
- Zustand for state management
- Tailwind CSS for styling
- Dexie.js for local storage

## Open Questions
1. What specific 9router features do you want prioritized?
2. Should we maintain the chat interface alongside the dashboard?
3. Do you want provider status monitoring similar to 9router?
4. What level of analytics detail do you need?
5. Any specific visual design preferences?

## Agent Research Findings

### Backend API Capabilities (bg_0e6c6d42)
**OpenAI-Compatible Endpoints:**
- `POST /openai/v1/chat/completions` - Main chat endpoint
- `GET /openai/v1/models` - List available models
- `GET /openai/v1/models/{id}` - Get model details

**Authentication & Identity:**
- `POST /auth/dev-login` - Development login
- Provider-specific OAuth endpoints

**API Key Management:**
- `POST /projects/{projectId}/keys` - Create API keys
- `DELETE /projects/{projectId}/keys/{keyId}` - Revoke keys

**System Health:**
- `GET /health/liveness` - Basic health check
- `GET /health/readiness` - Comprehensive health (DB, Redis, providers)

### Frontend Architecture (bg_e2c55f6e)
**Tech Stack:** React 19 + TypeScript + Vite + Zustand + Tailwind CSS
**State Management:** Zustand stores for sessions, settings, usage
**UI Components:** Custom Button, Badge, Modal, Input components
**Data Layer:** Dexie.js for IndexedDB persistence
**API Client:** Axios-based GatewayClient with configurable base URL

### Current Features:**
- Chat interface with session management
- Basic settings (gateway URL, cost rate)
- Local message persistence
- Responsive design with sidebar

## Agent Research Findings (Complete)

### 9router UI Patterns (bg_e4f69c31)
**Navigation Structure:** Collapsible sidebar with 5 sections (Endpoint, Providers, Combos, Usage, CLI Tools, Settings)
**Provider Management:** Card grid layout with status indicators and quick actions
**Data Visualization:** Mini bar graphs, tabbed interfaces, sortable tables
**Configuration Management:** Modal dialogs, API key tables, cloud proxy toggle

### Modern Dashboard Recommendations:
- Enhanced provider cards with visual status indicators
- Advanced analytics with time-series charts
- Progressive disclosure configuration forms
- Breadcrumb navigation and quick action toolbar

## User Preferences Confirmed
- Want ALL aspects of 9router interface (provider management, analytics, model config, API keys)
- Replace chat interface with dashboard as main interface

## Technical Foundation Assessment
**Backend API:** Comprehensive endpoints available for all dashboard needs
**Frontend Architecture:** Solid foundation with React 19 + Zustand + Tailwind CSS
**Integration Points:** Clear paths for extending existing patterns

## Planning Ready**