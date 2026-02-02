import type { Usage } from '@/api/client';

export interface ProviderComparison {
  provider: string;
  requests: number;
  avgLatency: number;
  errorRate: number;
  tokensPerMinute: number;
}

export interface AnalyticsEvent {
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

export interface AnalyticsSummary {
  totalRequests: number;
  totalTokens: number;
  totalErrors: number;
  averageLatency: number;
  errorRate: number;
  topProviders: Array<{
    provider: string;
    requests: number;
    tokens: number;
  }>;
  topModels: Array<{
    model: string;
    requests: number;
    tokens: number;
  }>;
}

export interface TimeSeriesData {
  timestamp: string;
  requests: number;
  tokens: number;
  errors: number;
  averageLatency: number;
}

export const mockAnalyticsService = {
  getEvents: async (
    limit: number = 100,
    provider?: string,
    model?: string
  ): Promise<AnalyticsEvent[]> => {
    await new Promise((resolve) => setTimeout(resolve, 250));

    const providers = ['groq', 'cohere', 'deepseek', 'deepinfra'];
    const models = [
      'llama-3.1-70b-versatile',
      'command-r',
      'deepseek-chat',
      'meta-llama/Llama-2-70b-chat-hf',
    ];

    const events: AnalyticsEvent[] = [];
    const baseTime = new Date(Date.now() - 24 * 60 * 60 * 1000);

    for (let i = 0; i < limit; i++) {
      const isError = Math.random() < 0.05;
      const selectedProvider = provider || providers[Math.floor(Math.random() * providers.length)];
      const selectedModel = model || models[Math.floor(Math.random() * models.length)];

      events.push({
        id: `event-${i}`,
        timestamp: new Date(baseTime.getTime() + i * 60000).toISOString(),
        type: isError ? 'error' : Math.random() < 0.3 ? 'stream_start' : 'request',
        provider: selectedProvider,
        model: selectedModel,
        tokens: isError
          ? undefined
          : {
              prompt_tokens: Math.floor(Math.random() * 500) + 50,
              completion_tokens: Math.floor(Math.random() * 800) + 100,
              total_tokens: Math.floor(Math.random() * 1300) + 150,
            },
        latency: isError ? undefined : Math.floor(Math.random() * 5000) + 100,
        statusCode: isError ? 500 : 200,
        errorMessage: isError ? 'Rate limit exceeded' : undefined,
      });
    }

    return events;
  },

  getSummary: async (
    _startDate?: string,
    _endDate?: string,
    provider?: string
  ): Promise<AnalyticsSummary> => {
    await new Promise((resolve) => setTimeout(resolve, 300));

    const events = await mockAnalyticsService.getEvents(500, provider);

    const totalRequests = events.filter((e) => e.type === 'request').length;
    const totalTokens = events.reduce((sum, e) => {
      return sum + (e.tokens?.total_tokens || 0);
    }, 0);
    const totalErrors = events.filter((e) => e.statusCode && e.statusCode >= 400).length;
    const latencies = events
      .filter((e) => e.latency !== undefined)
      .map((e) => e.latency || 0);
    const averageLatency = latencies.length > 0 ? latencies.reduce((a, b) => a + b, 0) / latencies.length : 0;
    const errorRate = totalRequests > 0 ? totalErrors / totalRequests : 0;

    const providerMap = new Map<string, { requests: number; tokens: number }>();
    const modelMap = new Map<string, { requests: number; tokens: number }>();

    events.forEach((event) => {
      if (event.type === 'request') {
        const pKey = event.provider;
        const mKey = event.model;

        providerMap.set(pKey, {
          requests: (providerMap.get(pKey)?.requests || 0) + 1,
          tokens: (providerMap.get(pKey)?.tokens || 0) + (event.tokens?.total_tokens || 0),
        });

        modelMap.set(mKey, {
          requests: (modelMap.get(mKey)?.requests || 0) + 1,
          tokens: (modelMap.get(mKey)?.tokens || 0) + (event.tokens?.total_tokens || 0),
        });
      }
    });

    return {
      totalRequests,
      totalTokens,
      totalErrors,
      averageLatency: Math.round(averageLatency),
      errorRate: Number((errorRate * 100).toFixed(2)),
      topProviders: Array.from(providerMap.entries())
        .map(([provider, data]) => ({
          provider,
          ...data,
        }))
        .sort((a, b) => b.requests - a.requests)
        .slice(0, 5),
      topModels: Array.from(modelMap.entries())
        .map(([model, data]) => ({
          model,
          ...data,
        }))
        .sort((a, b) => b.requests - a.requests)
        .slice(0, 5),
    };
  },

  getTimeSeries: async (
    interval: 'hour' | 'day' | 'week' = 'hour',
    provider?: string
  ): Promise<TimeSeriesData[]> => {
    await new Promise((resolve) => setTimeout(resolve, 350));

    const events = await mockAnalyticsService.getEvents(500, provider);
    const dataMap = new Map<string, TimeSeriesData>();

    const getIntervalKey = (date: Date): string => {
      if (interval === 'hour') {
        return new Date(date.getFullYear(), date.getMonth(), date.getDate(), date.getHours()).toISOString();
      } else if (interval === 'day') {
        return new Date(date.getFullYear(), date.getMonth(), date.getDate()).toISOString();
      } else {
        const week = Math.floor(date.getDate() / 7);
        return new Date(date.getFullYear(), date.getMonth(), date.getDate() - week * 7).toISOString();
      }
    };

    events.forEach((event) => {
      const key = getIntervalKey(new Date(event.timestamp));

      if (!dataMap.has(key)) {
        dataMap.set(key, {
          timestamp: key,
          requests: 0,
          tokens: 0,
          errors: 0,
          averageLatency: 0,
        });
      }

      const data = dataMap.get(key)!;
      if (event.type === 'request') {
        data.requests += 1;
        data.tokens += event.tokens?.total_tokens || 0;
      }
      if (event.statusCode && event.statusCode >= 400) {
        data.errors += 1;
      }
      if (event.latency) {
        data.averageLatency = (data.averageLatency + event.latency) / 2;
      }
    });

    return Array.from(dataMap.values()).sort(
      (a, b) => new Date(a.timestamp).getTime() - new Date(b.timestamp).getTime()
    );
  },

  getProviderComparison: async (): Promise<ProviderComparison[]> => {
    await new Promise((resolve) => setTimeout(resolve, 300));

    const summary = await mockAnalyticsService.getSummary();

    return summary.topProviders.map((p) => ({
      provider: p.provider,
      requests: p.requests,
      avgLatency: Math.floor(Math.random() * 300) + 50,
      errorRate: Number((Math.random() * 5).toFixed(2)),
      tokensPerMinute: Math.floor(p.tokens / 60),
    }));
  },

  trackEvent: async (_event: Omit<AnalyticsEvent, 'id' | 'timestamp'>): Promise<void> => {
    await new Promise((resolve) => setTimeout(resolve, 100));
  },

  getErrorBreakdown: async (): Promise<
    Array<{
      error: string;
      count: number;
      providers: string[];
    }>
  > => {
    await new Promise((resolve) => setTimeout(resolve, 250));

    return [
      {
        error: 'Rate limit exceeded',
        count: 42,
        providers: ['groq', 'cohere'],
      },
      {
        error: 'Timeout',
        count: 18,
        providers: ['deepinfra', 'together'],
      },
      {
        error: 'Invalid model',
        count: 5,
        providers: ['cloudflare'],
      },
      {
        error: 'Authentication failed',
        count: 3,
        providers: ['cohere'],
      },
    ];
  },
};

export const realAnalyticsService = {
  getEvents: async (
    limit: number = 100,
    provider?: string,
    model?: string
  ): Promise<AnalyticsEvent[]> => {
    const params = new URLSearchParams({ limit: String(limit) });
    if (provider) params.append('provider', provider);
    if (model) params.append('model', model);

    const response = await fetch(`/admin/analytics/events?${params}`);
    if (!response.ok) {
      throw new Error(`Failed to fetch analytics events: ${response.status}`);
    }
    return response.json();
  },

  getSummary: async (
    startDate?: string,
    endDate?: string,
    provider?: string
  ): Promise<AnalyticsSummary> => {
    const params = new URLSearchParams();
    if (startDate) params.append('startDate', startDate);
    if (endDate) params.append('endDate', endDate);
    if (provider) params.append('provider', provider);

    const response = await fetch(`/admin/analytics/summary?${params}`);
    if (!response.ok) {
      throw new Error(`Failed to fetch analytics summary: ${response.status}`);
    }
    return response.json();
  },

  getTimeSeries: async (
    interval: 'hour' | 'day' | 'week' = 'hour',
    provider?: string
  ): Promise<TimeSeriesData[]> => {
    const params = new URLSearchParams({ interval });
    if (provider) params.append('provider', provider);

    const response = await fetch(`/admin/analytics/timeseries?${params}`);
    if (!response.ok) {
      throw new Error(`Failed to fetch timeseries data: ${response.status}`);
    }
    return response.json();
  },

  getProviderComparison: async (): Promise<
    Array<{
      provider: string;
      requests: number;
      avgLatency: number;
      errorRate: number;
      tokensPerMinute: number;
    }>
  > => {
    const response = await fetch('/admin/analytics/provider-comparison');
    if (!response.ok) {
      throw new Error(`Failed to fetch provider comparison: ${response.status}`);
    }
    return response.json();
  },

  trackEvent: async (event: Omit<AnalyticsEvent, 'id' | 'timestamp'>): Promise<void> => {
    const response = await fetch('/admin/analytics/events', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(event),
    });

    if (!response.ok) {
      throw new Error(`Failed to track event: ${response.status}`);
    }
  },

  getErrorBreakdown: async (): Promise<
    Array<{
      error: string;
      count: number;
      providers: string[];
    }>
  > => {
    const response = await fetch('/admin/analytics/errors');
    if (!response.ok) {
      throw new Error(`Failed to fetch error breakdown: ${response.status}`);
    }
    return response.json();
  },
};

export default mockAnalyticsService;
