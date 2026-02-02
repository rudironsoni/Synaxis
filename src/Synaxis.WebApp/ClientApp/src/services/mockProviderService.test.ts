import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mockProviderService, Provider, realProviderService } from './mockProviderService';

describe('mockProviderService', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('getProviders', () => {
    it('should return an array of providers', async () => {
      const providers = await mockProviderService.getProviders();

      expect(Array.isArray(providers)).toBe(true);
      expect(providers.length).toBeGreaterThan(0);
    });

    it('should return providers with required fields', async () => {
      const providers = await mockProviderService.getProviders();

      providers.forEach((provider) => {
        expect(provider).toHaveProperty('id');
        expect(provider).toHaveProperty('name');
        expect(provider).toHaveProperty('type');
        expect(provider).toHaveProperty('enabled');
        expect(provider).toHaveProperty('tier');
        expect(provider).toHaveProperty('keyConfigured');
        expect(provider).toHaveProperty('models');
        expect(provider).toHaveProperty('status');
      });
    });

    it('should return providers with valid status values', async () => {
      const providers = await mockProviderService.getProviders();

      providers.forEach((provider) => {
        expect(['online', 'offline', 'unknown']).toContain(provider.status);
      });
    });

    it('should return providers with models array', async () => {
      const providers = await mockProviderService.getProviders();

      providers.forEach((provider) => {
        expect(Array.isArray(provider.models)).toBe(true);
        provider.models.forEach((model) => {
          expect(model).toHaveProperty('id');
          expect(model).toHaveProperty('name');
          expect(model).toHaveProperty('enabled');
        });
      });
    });

    it('should include groq provider', async () => {
      const providers = await mockProviderService.getProviders();
      const groq = providers.find((p) => p.id === 'groq');

      expect(groq).toBeDefined();
      expect(groq?.enabled).toBe(true);
      expect(groq?.tier).toBe(0);
    });

    it('should simulate network delay', async () => {
      const start = Date.now();
      await mockProviderService.getProviders();
      const elapsed = Date.now() - start;

      expect(elapsed).toBeGreaterThanOrEqual(250);
    });
  });

  describe('getProviderDetail', () => {
    it('should return provider detail with usage information', async () => {
      const detail = await mockProviderService.getProviderDetail('groq');

      expect(detail).toBeDefined();
      expect(detail?.id).toBe('groq');
      expect(detail).toHaveProperty('usage');
      expect(detail?.usage).toHaveProperty('totalTokens');
      expect(detail?.usage).toHaveProperty('requests');
    });

    it('should return null for non-existent provider', async () => {
      const detail = await mockProviderService.getProviderDetail('non-existent');

      expect(detail).toBeNull();
    });

    it('should return provider with realistic usage numbers', async () => {
      const detail = await mockProviderService.getProviderDetail('groq');

      expect(detail?.usage.totalTokens).toBeGreaterThan(0);
      expect(detail?.usage.requests).toBeGreaterThan(0);
    });
  });

  describe('getProviderStatus', () => {
    it('should return status information', async () => {
      const status = await mockProviderService.getProviderStatus('groq');

      expect(status).toBeDefined();
      expect(status?.status).toBe('online');
      expect(status).toHaveProperty('lastChecked');
      expect(status).toHaveProperty('latency');
    });

    it('should return null for non-existent provider', async () => {
      const status = await mockProviderService.getProviderStatus('non-existent');

      expect(status).toBeNull();
    });

    it('should have valid status values', async () => {
      const groqStatus = await mockProviderService.getProviderStatus('groq');

      expect(['online', 'offline', 'unknown']).toContain(groqStatus?.status);
    });
  });

  describe('getProviderUsage', () => {
    it('should return usage statistics', async () => {
      const usage = await mockProviderService.getProviderUsage('groq');

      expect(usage).toBeDefined();
      expect(usage?.totalTokens).toBeGreaterThan(0);
      expect(usage?.requests).toBeGreaterThan(0);
    });

    it('should return null for non-existent provider', async () => {
      const usage = await mockProviderService.getProviderUsage('non-existent');

      expect(usage).toBeNull();
    });
  });

  describe('updateProvider', () => {
    it('should update provider configuration', async () => {
      const updated = await mockProviderService.updateProvider('groq', {
        enabled: false,
        tier: 5,
      });

      expect(updated).toBeDefined();
      expect(updated?.enabled).toBe(false);
      expect(updated?.tier).toBe(5);
    });

    it('should return null for non-existent provider', async () => {
      const updated = await mockProviderService.updateProvider('non-existent', {
        enabled: false,
      });

      expect(updated).toBeNull();
    });

    it('should preserve other properties when updating', async () => {
      const original = await mockProviderService.getProviders();
      const groq = original.find((p) => p.id === 'groq')!;

      const updated = await mockProviderService.updateProvider('groq', {
        tier: 5,
      });

      expect(updated?.id).toBe(groq.id);
      expect(updated?.name).toBe(groq.name);
      expect(updated?.tier).toBe(5);
    });

    it('should update keyConfigured when key is provided', async () => {
      const updated = await mockProviderService.updateProvider('groq', {
        key: 'new-api-key',
      });

      expect(updated?.keyConfigured).toBe(true);
    });
  });

  describe('getAllModels', () => {
    it('should return models organized by provider', async () => {
      const models = await mockProviderService.getAllModels();

      expect(typeof models).toBe('object');
      expect(Object.keys(models).length).toBeGreaterThan(0);
    });

    it('should have groq models', async () => {
      const models = await mockProviderService.getAllModels();

      expect(models.groq).toBeDefined();
      expect(Array.isArray(models.groq)).toBe(true);
      expect(models.groq.length).toBeGreaterThan(0);
    });

    it('should have valid model objects', async () => {
      const models = await mockProviderService.getAllModels();

      Object.values(models).forEach((providerModels) => {
        providerModels.forEach((model) => {
          expect(model).toHaveProperty('id');
          expect(model).toHaveProperty('name');
          expect(model).toHaveProperty('enabled');
        });
      });
    });
  });

  describe('Service Interface Compatibility', () => {
    it('mock service and real service should have same interface', () => {
      const mockMethods = Object.keys(mockProviderService).sort();
      const realMethods = Object.keys(realProviderService).sort();

      expect(mockMethods).toEqual(realMethods);
    });

    it('should be easy to swap mock and real services', async () => {
      let service = mockProviderService;
      let providers = await service.getProviders();

      expect(providers.length).toBeGreaterThan(0);

      service = realProviderService as typeof mockProviderService;
      expect(typeof service.getProviders).toBe('function');
    });
  });

  describe('Data Structure Matching', () => {
    it('should return data matching backend API structure', async () => {
      const providers = await mockProviderService.getProviders();
      const provider = providers[0];

      // Verify all required fields match backend structure
      expect(provider).toHaveProperty('id');
      expect(provider).toHaveProperty('name');
      expect(provider).toHaveProperty('type');
      expect(provider).toHaveProperty('enabled');
      expect(provider).toHaveProperty('tier');
      expect(provider).toHaveProperty('keyConfigured');
      expect(provider).toHaveProperty('models');
      expect(provider).toHaveProperty('status');

      // Verify type correctness
      expect(typeof provider.id).toBe('string');
      expect(typeof provider.name).toBe('string');
      expect(typeof provider.enabled).toBe('boolean');
      expect(typeof provider.tier).toBe('number');
      expect(typeof provider.keyConfigured).toBe('boolean');
      expect(Array.isArray(provider.models)).toBe(true);
    });
  });
});
