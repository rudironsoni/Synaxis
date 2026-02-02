import { describe, it, expect, beforeEach, vi } from 'vitest';
import { mockConfigService, realConfigService, CanonicalModel } from './mockConfigService';

describe('mockConfigService', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('getCanonicalModels', () => {
    it('should return array of canonical models', async () => {
      const models = await mockConfigService.getCanonicalModels();

      expect(Array.isArray(models)).toBe(true);
      expect(models.length).toBeGreaterThan(0);
    });

    it('should return models with required fields', async () => {
      const models = await mockConfigService.getCanonicalModels();

      models.forEach((model) => {
        expect(model).toHaveProperty('id');
        expect(model).toHaveProperty('name');
        expect(model).toHaveProperty('provider');
        expect(model).toHaveProperty('modelPath');
        expect(model).toHaveProperty('streaming');
        expect(model).toHaveProperty('tools');
        expect(model).toHaveProperty('vision');
        expect(model).toHaveProperty('structuredOutput');
        expect(model).toHaveProperty('logProbs');
      });
    });

    it('should return models with boolean capability fields', async () => {
      const models = await mockConfigService.getCanonicalModels();

      models.forEach((model) => {
        expect(typeof model.streaming).toBe('boolean');
        expect(typeof model.tools).toBe('boolean');
        expect(typeof model.vision).toBe('boolean');
        expect(typeof model.structuredOutput).toBe('boolean');
        expect(typeof model.logProbs).toBe('boolean');
      });
    });

    it('should include popular models like Llama', async () => {
      const models = await mockConfigService.getCanonicalModels();
      const hasLlama = models.some((m) => m.id.includes('llama'));

      expect(hasLlama).toBe(true);
    });

    it('should have valid context window values', async () => {
      const models = await mockConfigService.getCanonicalModels();

      models.forEach((model) => {
        if (model.contextWindow) {
          expect(model.contextWindow).toBeGreaterThan(0);
        }
      });
    });

    it('should simulate network delay', async () => {
      const start = Date.now();
      await mockConfigService.getCanonicalModels();
      const elapsed = Date.now() - start;

      expect(elapsed).toBeGreaterThanOrEqual(150);
    });
  });

  describe('getAliases', () => {
    it('should return array of alias configurations', async () => {
      const aliases = await mockConfigService.getAliases();

      expect(Array.isArray(aliases)).toBe(true);
      expect(aliases.length).toBeGreaterThan(0);
    });

    it('should return aliases with required fields', async () => {
      const aliases = await mockConfigService.getAliases();

      aliases.forEach((alias) => {
        expect(alias).toHaveProperty('id');
        expect(alias).toHaveProperty('candidates');
        expect(alias).toHaveProperty('priority');
      });
    });

    it('should have candidates as string array', async () => {
      const aliases = await mockConfigService.getAliases();

      aliases.forEach((alias) => {
        expect(Array.isArray(alias.candidates)).toBe(true);
        expect(alias.candidates.length).toBeGreaterThan(0);
        alias.candidates.forEach((candidate) => {
          expect(typeof candidate).toBe('string');
        });
      });
    });

    it('should include default alias', async () => {
      const aliases = await mockConfigService.getAliases();
      const defaultAlias = aliases.find((a) => a.id === 'default');

      expect(defaultAlias).toBeDefined();
      expect(defaultAlias?.candidates.length).toBeGreaterThan(0);
    });
  });

  describe('getGatewayConfig', () => {
    it('should return gateway configuration', async () => {
      const config = await mockConfigService.getGatewayConfig();

      expect(config).toHaveProperty('jwtSecret');
      expect(config).toHaveProperty('defaultModel');
      expect(config).toHaveProperty('defaultProvider');
      expect(config).toHaveProperty('requestTimeout');
      expect(config).toHaveProperty('maxRetries');
      expect(config).toHaveProperty('cacheTTL');
      expect(config).toHaveProperty('rateLimitPerMinute');
    });

    it('should have valid configuration values', async () => {
      const config = await mockConfigService.getGatewayConfig();

      expect(typeof config.jwtSecret).toBe('string');
      expect(typeof config.defaultModel).toBe('string');
      expect(typeof config.defaultProvider).toBe('string');
      expect(config.requestTimeout).toBeGreaterThan(0);
      expect(config.maxRetries).toBeGreaterThanOrEqual(0);
      expect(config.cacheTTL).toBeGreaterThan(0);
      expect(config.rateLimitPerMinute).toBeGreaterThan(0);
    });
  });

  describe('updateCanonicalModel', () => {
    it('should update a model configuration', async () => {
      const updated = await mockConfigService.updateCanonicalModel('llama-3.1-70b-versatile', {
        streaming: false,
      });

      expect(updated).toBeDefined();
      expect(updated?.id).toBe('llama-3.1-70b-versatile');
      expect(updated?.streaming).toBe(false);
    });

    it('should return null for non-existent model', async () => {
      const updated = await mockConfigService.updateCanonicalModel('non-existent', {
        streaming: false,
      });

      expect(updated).toBeNull();
    });

    it('should preserve other fields when updating', async () => {
      const original = await mockConfigService.getCanonicalModels();
      const model = original.find((m) => m.id === 'llama-3.1-70b-versatile')!;

      const updated = await mockConfigService.updateCanonicalModel('llama-3.1-70b-versatile', {
        tools: !model.tools,
      });

      expect(updated?.name).toBe(model.name);
      expect(updated?.provider).toBe(model.provider);
      expect(updated?.tools).toBe(!model.tools);
    });
  });

  describe('updateAlias', () => {
    it('should update alias configuration', async () => {
      const updated = await mockConfigService.updateAlias('default', {
        priority: 10,
      });

      expect(updated).toBeDefined();
      expect(updated?.id).toBe('default');
      expect(updated?.priority).toBe(10);
    });

    it('should return null for non-existent alias', async () => {
      const updated = await mockConfigService.updateAlias('non-existent', {
        priority: 5,
      });

      expect(updated).toBeNull();
    });

    it('should preserve candidates when updating', async () => {
      const original = await mockConfigService.getAliases();
      const alias = original.find((a) => a.id === 'default')!;

      const updated = await mockConfigService.updateAlias('default', {
        priority: 100,
      });

      expect(updated?.candidates).toEqual(alias.candidates);
      expect(updated?.priority).toBe(100);
    });
  });

  describe('updateGatewayConfig', () => {
    it('should update gateway configuration', async () => {
      const updated = await mockConfigService.updateGatewayConfig({
        requestTimeout: 60000,
      });

      expect(updated).toBeDefined();
      expect(updated.requestTimeout).toBe(60000);
    });

    it('should preserve other fields when updating', async () => {
      const original = await mockConfigService.getGatewayConfig();

      const updated = await mockConfigService.updateGatewayConfig({
        maxRetries: 10,
      });

      expect(updated.defaultModel).toBe(original.defaultModel);
      expect(updated.defaultProvider).toBe(original.defaultProvider);
      expect(updated.maxRetries).toBe(10);
    });
  });

  describe('validateModelConfiguration', () => {
    it('should validate model configuration', async () => {
      const result = await mockConfigService.validateModelConfiguration({
        id: 'test-model',
        provider: 'test',
        modelPath: 'test',
        streaming: true,
        tools: true,
        vision: false,
        structuredOutput: false,
        logProbs: false,
      });

      expect(result.valid).toBe(true);
      expect(result.errors).toBeUndefined();
    });

    it('should reject invalid configurations', async () => {
      const result = await mockConfigService.validateModelConfiguration({
        id: '',
        provider: '',
        modelPath: '',
        streaming: true,
        tools: true,
        vision: false,
        structuredOutput: false,
        logProbs: false,
      });

      expect(result.valid).toBe(false);
      expect(result.errors).toBeDefined();
      expect(result.errors?.length).toBeGreaterThan(0);
    });

    it('should identify missing required fields', async () => {
      const result = await mockConfigService.validateModelConfiguration({
        id: 'test',
        provider: '',
        modelPath: 'test',
        streaming: true,
        tools: true,
        vision: false,
        structuredOutput: false,
        logProbs: false,
      });

      expect(result.valid).toBe(false);
      expect(result.errors?.some((e) => e.includes('Provider'))).toBe(true);
    });
  });

  describe('getModelCapabilities', () => {
    it('should return model capabilities', async () => {
      const capabilities = await mockConfigService.getModelCapabilities('llama-3.1-70b-versatile');

      expect(capabilities).toBeDefined();
      expect(capabilities).toHaveProperty('streaming');
      expect(capabilities).toHaveProperty('tools');
      expect(capabilities).toHaveProperty('vision');
      expect(capabilities).toHaveProperty('structuredOutput');
      expect(capabilities).toHaveProperty('logProbs');
    });

    it('should return null for non-existent model', async () => {
      const capabilities = await mockConfigService.getModelCapabilities('non-existent');

      expect(capabilities).toBeNull();
    });

    it('should return boolean values for all capabilities', async () => {
      const capabilities = await mockConfigService.getModelCapabilities('llama-3.1-70b-versatile');

      if (capabilities) {
        expect(typeof capabilities.streaming).toBe('boolean');
        expect(typeof capabilities.tools).toBe('boolean');
        expect(typeof capabilities.vision).toBe('boolean');
        expect(typeof capabilities.structuredOutput).toBe('boolean');
        expect(typeof capabilities.logProbs).toBe('boolean');
      }
    });
  });

  describe('Service Interface Compatibility', () => {
    it('mock and real services should have same interface', () => {
      const mockMethods = Object.keys(mockConfigService).sort();
      const realMethods = Object.keys(realConfigService).sort();

      expect(mockMethods).toEqual(realMethods);
    });
  });

  describe('Data Structure Matching', () => {
    it('should return canonical models matching API structure', async () => {
      const models = await mockConfigService.getCanonicalModels();
      const model = models[0];

      expect(model).toHaveProperty('id');
      expect(model).toHaveProperty('provider');
      expect(model).toHaveProperty('modelPath');
      expect(typeof model.id).toBe('string');
      expect(typeof model.provider).toBe('string');
      expect(typeof model.modelPath).toBe('string');
    });

    it('should return aliases matching API structure', async () => {
      const aliases = await mockConfigService.getAliases();
      const alias = aliases[0];

      expect(alias).toHaveProperty('id');
      expect(alias).toHaveProperty('candidates');
      expect(alias).toHaveProperty('priority');
      expect(typeof alias.id).toBe('string');
      expect(Array.isArray(alias.candidates)).toBe(true);
      expect(typeof alias.priority).toBe('number');
    });
  });

  describe('Data Consistency', () => {
    it('should have consistent model references between models and aliases', async () => {
      const models = await mockConfigService.getCanonicalModels();
      const aliases = await mockConfigService.getAliases();

      const modelIds = new Set(models.map((m) => m.id));

      aliases.forEach((alias) => {
        alias.candidates.forEach((candidate) => {
          expect(modelIds.has(candidate)).toBe(true);
        });
      });
    });
  });
});
