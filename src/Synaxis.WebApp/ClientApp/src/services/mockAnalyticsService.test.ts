import { describe, it, expect, beforeEach, vi } from 'vitest';
import { mockAnalyticsService, realAnalyticsService, AnalyticsEvent } from './mockAnalyticsService';

describe('mockAnalyticsService', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('getEvents', () => {
    it('should return array of analytics events', async () => {
      const events = await mockAnalyticsService.getEvents(10);

      expect(Array.isArray(events)).toBe(true);
      expect(events.length).toBeLessThanOrEqual(10);
    });

    it('should return events with required fields', async () => {
      const events = await mockAnalyticsService.getEvents(5);

      events.forEach((event) => {
        expect(event).toHaveProperty('id');
        expect(event).toHaveProperty('timestamp');
        expect(event).toHaveProperty('type');
        expect(event).toHaveProperty('provider');
        expect(event).toHaveProperty('model');
      });
    });

    it('should return events with valid type values', async () => {
      const events = await mockAnalyticsService.getEvents(20);

      events.forEach((event) => {
        expect(['request', 'error', 'stream_start', 'stream_end']).toContain(event.type);
      });
    });

    it('should filter by provider if specified', async () => {
      const events = await mockAnalyticsService.getEvents(50, 'groq');

      events.forEach((event) => {
        expect(event.provider).toBe('groq');
      });
    });

    it('should filter by model if specified', async () => {
      const events = await mockAnalyticsService.getEvents(50, undefined, 'llama-3.1-70b-versatile');

      events.forEach((event) => {
        expect(event.model).toBe('llama-3.1-70b-versatile');
      });
    });

    it('should respect limit parameter', async () => {
      const events = await mockAnalyticsService.getEvents(25);

      expect(events.length).toBeLessThanOrEqual(25);
    });

    it('should include token usage for successful requests', async () => {
      const events = await mockAnalyticsService.getEvents(100);
      const successEvents = events.filter((e) => !e.errorMessage);

      successEvents.forEach((event) => {
        if (event.type === 'request') {
          expect(event.tokens).toBeDefined();
          expect(event.tokens?.total_tokens).toBeGreaterThan(0);
        }
      });
    });

    it('should include latency for successful requests', async () => {
      const events = await mockAnalyticsService.getEvents(100);
      const successEvents = events.filter((e) => e.statusCode === 200);

      successEvents.forEach((event) => {
        expect(event.latency).toBeDefined();
        expect(event.latency).toBeGreaterThan(0);
      });
    });
  });

  describe('getSummary', () => {
    it('should return analytics summary', async () => {
      const summary = await mockAnalyticsService.getSummary();

      expect(summary).toHaveProperty('totalRequests');
      expect(summary).toHaveProperty('totalTokens');
      expect(summary).toHaveProperty('totalErrors');
      expect(summary).toHaveProperty('averageLatency');
      expect(summary).toHaveProperty('errorRate');
      expect(summary).toHaveProperty('topProviders');
      expect(summary).toHaveProperty('topModels');
    });

    it('should have correct data types', async () => {
      const summary = await mockAnalyticsService.getSummary();

      expect(typeof summary.totalRequests).toBe('number');
      expect(typeof summary.totalTokens).toBe('number');
      expect(typeof summary.totalErrors).toBe('number');
      expect(typeof summary.averageLatency).toBe('number');
      expect(typeof summary.errorRate).toBe('number');
      expect(Array.isArray(summary.topProviders)).toBe(true);
      expect(Array.isArray(summary.topModels)).toBe(true);
    });

    it('should have valid error rate (0-100)', async () => {
      const summary = await mockAnalyticsService.getSummary();

      expect(summary.errorRate).toBeGreaterThanOrEqual(0);
      expect(summary.errorRate).toBeLessThanOrEqual(100);
    });

    it('should list top providers with request counts', async () => {
      const summary = await mockAnalyticsService.getSummary();

      summary.topProviders.forEach((provider) => {
        expect(provider).toHaveProperty('provider');
        expect(provider).toHaveProperty('requests');
        expect(provider).toHaveProperty('tokens');
        expect(typeof provider.requests).toBe('number');
        expect(provider.requests).toBeGreaterThan(0);
      });
    });

    it('should list top models with request counts', async () => {
      const summary = await mockAnalyticsService.getSummary();

      summary.topModels.forEach((model) => {
        expect(model).toHaveProperty('model');
        expect(model).toHaveProperty('requests');
        expect(model).toHaveProperty('tokens');
        expect(typeof model.requests).toBe('number');
        expect(model.requests).toBeGreaterThan(0);
      });
    });

    it('should filter by provider if specified', async () => {
      const summary = await mockAnalyticsService.getSummary(undefined, undefined, 'groq');

      // All top providers in the summary should be groq
      expect(summary.topProviders.every((p) => p.provider === 'groq')).toBe(true);
    });
  });

  describe('getTimeSeries', () => {
    it('should return time series data', async () => {
      const data = await mockAnalyticsService.getTimeSeries('hour');

      expect(Array.isArray(data)).toBe(true);
      expect(data.length).toBeGreaterThan(0);
    });

    it('should include required time series fields', async () => {
      const data = await mockAnalyticsService.getTimeSeries('day');

      data.forEach((point) => {
        expect(point).toHaveProperty('timestamp');
        expect(point).toHaveProperty('requests');
        expect(point).toHaveProperty('tokens');
        expect(point).toHaveProperty('errors');
        expect(point).toHaveProperty('averageLatency');
      });
    });

    it('should return chronologically sorted data', async () => {
      const data = await mockAnalyticsService.getTimeSeries('hour');

      for (let i = 1; i < data.length; i++) {
        const prev = new Date(data[i - 1].timestamp).getTime();
        const curr = new Date(data[i].timestamp).getTime();
        expect(curr).toBeGreaterThanOrEqual(prev);
      }
    });

    it('should support different intervals', async () => {
      const hourly = await mockAnalyticsService.getTimeSeries('hour');
      const daily = await mockAnalyticsService.getTimeSeries('day');
      const weekly = await mockAnalyticsService.getTimeSeries('week');

      expect(hourly.length).toBeGreaterThan(0);
      expect(daily.length).toBeGreaterThan(0);
      expect(weekly.length).toBeGreaterThan(0);
    });
  });

  describe('getProviderComparison', () => {
    it('should return provider comparison data', async () => {
      const comparison = await mockAnalyticsService.getProviderComparison();

      expect(Array.isArray(comparison)).toBe(true);
      expect(comparison.length).toBeGreaterThan(0);
    });

    it('should include required comparison fields', async () => {
      const comparison = await mockAnalyticsService.getProviderComparison();

      comparison.forEach((provider) => {
        expect(provider).toHaveProperty('provider');
        expect(provider).toHaveProperty('requests');
        expect(provider).toHaveProperty('avgLatency');
        expect(provider).toHaveProperty('errorRate');
        expect(provider).toHaveProperty('tokensPerMinute');
      });
    });

    it('should have valid metric values', async () => {
      const comparison = await mockAnalyticsService.getProviderComparison();

      comparison.forEach((provider) => {
        expect(provider.requests).toBeGreaterThanOrEqual(0);
        expect(provider.avgLatency).toBeGreaterThan(0);
        expect(provider.errorRate).toBeGreaterThanOrEqual(0);
        expect(provider.tokensPerMinute).toBeGreaterThanOrEqual(0);
      });
    });
  });

  describe('trackEvent', () => {
    it('should accept and track analytics events', async () => {
      const event = {
        type: 'request' as const,
        provider: 'groq',
        model: 'llama-3.1-70b-versatile',
        tokens: {
          prompt_tokens: 100,
          completion_tokens: 200,
          total_tokens: 300,
        },
        latency: 500,
        statusCode: 200,
      };

      // Should not throw
      await expect(mockAnalyticsService.trackEvent(event)).resolves.toBeUndefined();
    });
  });

  describe('getErrorBreakdown', () => {
    it('should return error breakdown', async () => {
      const errors = await mockAnalyticsService.getErrorBreakdown();

      expect(Array.isArray(errors)).toBe(true);
      expect(errors.length).toBeGreaterThan(0);
    });

    it('should include error details', async () => {
      const errors = await mockAnalyticsService.getErrorBreakdown();

      errors.forEach((error) => {
        expect(error).toHaveProperty('error');
        expect(error).toHaveProperty('count');
        expect(error).toHaveProperty('providers');
        expect(typeof error.count).toBe('number');
        expect(Array.isArray(error.providers)).toBe(true);
      });
    });

    it('should have positive error counts', async () => {
      const errors = await mockAnalyticsService.getErrorBreakdown();

      errors.forEach((error) => {
        expect(error.count).toBeGreaterThan(0);
      });
    });
  });

  describe('Service Interface Compatibility', () => {
    it('mock and real services should have same interface', () => {
      const mockMethods = Object.keys(mockAnalyticsService).sort();
      const realMethods = Object.keys(realAnalyticsService).sort();

      expect(mockMethods).toEqual(realMethods);
    });
  });

  describe('Data Structure Matching', () => {
    it('should return data matching analytics API structure', async () => {
      const events = await mockAnalyticsService.getEvents(5);
      const event = events[0];

      expect(event).toHaveProperty('id');
      expect(event).toHaveProperty('timestamp');
      expect(event).toHaveProperty('type');
      expect(event).toHaveProperty('provider');
      expect(event).toHaveProperty('model');

      if (event.tokens) {
        expect(event.tokens).toHaveProperty('prompt_tokens');
        expect(event.tokens).toHaveProperty('completion_tokens');
        expect(event.tokens).toHaveProperty('total_tokens');
      }
    });
  });
});
