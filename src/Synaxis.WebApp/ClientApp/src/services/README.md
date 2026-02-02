# Mock Data Services

Comprehensive mock data services for parallel frontend development, enabling teams to work independently of backend API availability.

## Overview

This directory contains three fully-featured mock services that match the backend API structure exactly:

- **mockProviderService** - Provider management, status, models, and usage tracking
- **mockAnalyticsService** - Analytics events, summaries, time series, and error tracking
- **mockConfigService** - Model configurations, aliases, and gateway settings

Each mock service:
- Returns realistic test data
- Simulates network delays for authentic development experience
- Implements the same interface as the real service will
- Can be easily swapped with real service implementations
- Includes comprehensive type definitions

## Services

### Provider Service

Manages provider configuration, status, models, and usage information.

```typescript
import { mockProviderService } from '@/services';

// Get all configured providers
const providers = await mockProviderService.getProviders();

// Get detailed provider information
const detail = await mockProviderService.getProviderDetail('groq');

// Check provider health status
const status = await mockProviderService.getProviderStatus('groq');

// Get usage statistics
const usage = await mockProviderService.getProviderUsage('groq');

// Update provider configuration
const updated = await mockProviderService.updateProvider('groq', {
  enabled: true,
  tier: 0,
  key: 'new-api-key',
});

// Get all models by provider
const models = await mockProviderService.getAllModels();
```

**Data Structure:**

```typescript
interface Provider {
  id: string;
  name: string;
  type: string;
  enabled: boolean;
  tier: number;
  endpoint?: string;
  keyConfigured: boolean;
  models: ProviderModel[];
  status: 'online' | 'offline' | 'unknown';
  latency?: number;
}

interface ProviderDetail extends Provider {
  usage: {
    totalTokens: number;
    requests: number;
    averageLatency?: number;
  };
  costPerMillionTokens?: number;
  requestsPerMinute?: number;
}
```

### Analytics Service

Tracks and aggregates usage analytics, errors, and performance metrics.

```typescript
import { mockAnalyticsService } from '@/services';

// Get recent analytics events
const events = await mockAnalyticsService.getEvents(100, 'groq');

// Get analytics summary
const summary = await mockAnalyticsService.getSummary();

// Get time series data
const timeSeries = await mockAnalyticsService.getTimeSeries('hour', 'groq');

// Compare providers
const comparison = await mockAnalyticsService.getProviderComparison();

// Track custom event
await mockAnalyticsService.trackEvent({
  type: 'request',
  provider: 'groq',
  model: 'llama-3.1-70b-versatile',
  tokens: { prompt_tokens: 100, completion_tokens: 200, total_tokens: 300 },
  latency: 500,
  statusCode: 200,
});

// Get error breakdown
const errors = await mockAnalyticsService.getErrorBreakdown();
```

**Data Structure:**

```typescript
interface AnalyticsEvent {
  id: string;
  timestamp: string;
  type: 'request' | 'error' | 'stream_start' | 'stream_end';
  provider: string;
  model: string;
  tokens?: Usage;
  latency?: number;
  statusCode?: number;
  errorMessage?: string;
}

interface AnalyticsSummary {
  totalRequests: number;
  totalTokens: number;
  totalErrors: number;
  averageLatency: number;
  errorRate: number;
  topProviders: Array<{ provider: string; requests: number; tokens: number }>;
  topModels: Array<{ model: string; requests: number; tokens: number }>;
}
```

### Configuration Service

Manages model configurations, model aliases, and gateway settings.

```typescript
import { mockConfigService } from '@/services';

// Get canonical models
const models = await mockConfigService.getCanonicalModels();

// Get model aliases
const aliases = await mockConfigService.getAliases();

// Get gateway configuration
const config = await mockConfigService.getGatewayConfig();

// Update a model
const updated = await mockConfigService.updateCanonicalModel('llama-3.1-70b-versatile', {
  streaming: true,
  tools: true,
});

// Update an alias
await mockConfigService.updateAlias('default', {
  priority: 0,
  candidates: ['llama-3.1-70b-versatile', 'command-r'],
});

// Update gateway config
await mockConfigService.updateGatewayConfig({
  requestTimeout: 30000,
  maxRetries: 3,
});

// Validate model configuration
const validation = await mockConfigService.validateModelConfiguration({
  id: 'my-model',
  provider: 'groq',
  modelPath: 'my-model',
  streaming: true,
  tools: true,
  vision: false,
  structuredOutput: true,
  logProbs: false,
});

// Get model capabilities
const capabilities = await mockConfigService.getModelCapabilities('llama-3.1-70b-versatile');
```

**Data Structure:**

```typescript
interface CanonicalModel {
  id: string;
  provider: string;
  modelPath: string;
  name: string;
  description?: string;
  streaming: boolean;
  tools: boolean;
  vision: boolean;
  structuredOutput: boolean;
  logProbs: boolean;
  costPerMillionTokens?: number;
  contextWindow?: number;
  maxTokens?: number;
}

interface AliasConfiguration {
  id: string;
  candidates: string[];
  priority: number;
  description?: string;
}

interface GatewayConfiguration {
  jwtSecret: string;
  defaultModel: string;
  defaultProvider: string;
  requestTimeout: number;
  maxRetries: number;
  cacheTTL: number;
  rateLimitPerMinute: number;
}
```

## Using Mock Services

### Direct Import

```typescript
import { mockProviderService, mockAnalyticsService, mockConfigService } from '@/services';
```

### Creating a Hook for Conditional Usage

```typescript
// hooks/useProviderService.ts
import { useMemo } from 'react';
import { mockProviderService, realProviderService } from '@/services';

export function useProviderService() {
  const useMock = process.env.VITE_USE_MOCK_SERVICES === 'true';
  
  return useMemo(
    () => (useMock ? mockProviderService : realProviderService),
    [useMock]
  );
}
```

### Using in React Components

```typescript
import { useEffect, useState } from 'react';
import { mockProviderService } from '@/services';
import type { Provider } from '@/services';

export function ProviderList() {
  const [providers, setProviders] = useState<Provider[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchProviders = async () => {
      try {
        const data = await mockProviderService.getProviders();
        setProviders(data);
      } finally {
        setLoading(false);
      }
    };

    fetchProviders();
  }, []);

  if (loading) return <div>Loading...</div>;

  return (
    <div>
      {providers.map((provider) => (
        <div key={provider.id}>
          <h3>{provider.name}</h3>
          <p>Status: {provider.status}</p>
          <p>Enabled: {provider.enabled ? 'Yes' : 'No'}</p>
        </div>
      ))}
    </div>
  );
}
```

## Network Simulation

All mock services include realistic network delays:
- Provider service: 150-500ms
- Analytics service: 250-1000ms
- Configuration service: 100-500ms

This ensures frontend developers experience realistic timing and can test loading states.

## Testing

Comprehensive test coverage with 81+ tests across all services:

```bash
# Run all service tests
npm test src/services/

# Run individual service tests
npm test src/services/mockProviderService.test.ts
npm test src/services/mockAnalyticsService.test.ts
npm test src/services/mockConfigService.test.ts
```

### Test Coverage

- ✅ Data structure validation
- ✅ API contract verification
- ✅ Error handling (null returns for missing items)
- ✅ Data filtering and pagination
- ✅ State updates and modifications
- ✅ Interface compatibility between mock and real services
- ✅ Type safety and correctness

## Swapping to Real Services

When real backend APIs are ready, swap services with minimal code changes:

**Before (Mock):**
```typescript
import { mockProviderService } from '@/services';
const service = mockProviderService;
```

**After (Real):**
```typescript
import { realProviderService } from '@/services';
const service = realProviderService;
```

Or use environment configuration:
```typescript
const service = process.env.VITE_USE_MOCK_SERVICES === 'false'
  ? realProviderService
  : mockProviderService;
```

## Data Realism

Mock services include realistic data patterns:

- **Providers:** Groq (Tier 0), Cohere, DeepSeek, Together, DeepInfra, Cloudflare
- **Models:** Llama 3.1, Mixtral, Command R, DeepSeek Chat/Coder
- **Capabilities:** Streaming, tools, vision, structured output
- **Metrics:** Random token counts (100k-1M), request counts (500-5k), latencies (45-320ms)
- **Errors:** ~5% error rate with realistic error types

## Design Principles

1. **Exact API Matching** - Mock and real services have identical interfaces
2. **Realistic Data** - Numbers, timing, and structure match production
3. **Easy Swapping** - Single line changes to use real APIs
4. **Type Safety** - Full TypeScript support with complete type definitions
5. **No Hardcoding** - Data is generated dynamically, not hardcoded
6. **Network Simulation** - Includes realistic delays

## File Structure

```
src/services/
├── index.ts                          # Export all services
├── mockProviderService.ts            # Provider service (280 lines)
├── mockProviderService.test.ts       # 24 tests
├── mockAnalyticsService.ts           # Analytics service (250 lines)
├── mockAnalyticsService.test.ts      # 27 tests
├── mockConfigService.ts              # Config service (250 lines)
├── mockConfigService.test.ts         # 30 tests
└── README.md                         # This file
```

## Performance

- Mock services are optimized for frontend use
- Minimal memory footprint
- Async operations for realistic UX
- No external dependencies
- Tree-shakeable exports

## Future Integration

Services are designed for seamless integration with:
- React Query / TanStack Query for caching and synchronization
- Zustand stores for global state management
- Real backend APIs when available
- End-to-end testing and Playwright automation

## Contributing

When adding new mock services:

1. Match backend API structure exactly
2. Include realistic test data
3. Add network delay simulation
4. Write comprehensive tests
5. Document data structures
6. Provide both mock and real service implementations
