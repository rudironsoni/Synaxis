/**
 * Mock Provider Service
 * 
 * Provides realistic mock data for providers with status, models, and usage information.
 * This service returns data that matches the backend API structure exactly, allowing
 * frontend development to proceed independently of backend availability.
 * 
 * The mock service implements the same interface as the real provider service will,
 * making it easy to swap between mock and real implementations.
 */

export interface ProviderModel {
  id: string;
  name: string;
  enabled: boolean;
}

export interface ProviderStatus {
  status: 'online' | 'offline' | 'unknown';
  lastChecked?: string;
  latency?: number;
}

export interface ProviderUsage {
  totalTokens: number;
  requests: number;
  averageLatency?: number;
}

export interface Provider {
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

export interface ProviderDetail extends Provider {
  usage: ProviderUsage;
  costPerMillionTokens?: number;
  requestsPerMinute?: number;
}

/**
 * Mock provider service for development
 * Returns realistic provider data that matches backend API structure
 */
export const mockProviderService = {
  /**
   * Get all configured providers
   */
  getProviders: async (): Promise<Provider[]> => {
    // Simulate network delay
    await new Promise((resolve) => setTimeout(resolve, 300));

    return [
      {
        id: 'groq',
        name: 'Groq',
        type: 'groq',
        enabled: true,
        tier: 0,
        keyConfigured: true,
        models: [
          { id: 'llama-3.1-70b-versatile', name: 'Llama 3.1 70B', enabled: true },
          { id: 'llama-3.1-8b-instant', name: 'Llama 3.1 8B', enabled: true },
          { id: 'mixtral-8x7b-32768', name: 'Mixtral 8x7B', enabled: true },
        ],
        status: 'online',
        latency: 45,
      },
      {
        id: 'cohere',
        name: 'Cohere',
        type: 'cohere',
        enabled: true,
        tier: 1,
        keyConfigured: false,
        models: [
          { id: 'command-r', name: 'Command R', enabled: true },
          { id: 'command-r-plus', name: 'Command R+', enabled: true },
        ],
        status: 'unknown',
      },
      {
        id: 'deepseek',
        name: 'DeepSeek',
        type: 'openai',
        enabled: true,
        tier: 1,
        endpoint: 'https://api.deepseek.com/v1',
        keyConfigured: true,
        models: [
          { id: 'deepseek-chat', name: 'DeepSeek Chat', enabled: true },
          { id: 'deepseek-coder', name: 'DeepSeek Coder', enabled: true },
        ],
        status: 'online',
        latency: 250,
      },
      {
        id: 'together',
        name: 'Together AI',
        type: 'openai',
        enabled: false,
        tier: 2,
        endpoint: 'https://api.together.xyz/v1',
        keyConfigured: false,
        models: [
          { id: 'togethercomputer/llama-2-70b-chat', name: 'Llama 2 70B Chat', enabled: true },
          { id: 'meta-llama/Llama-2-13b-hf', name: 'Llama 2 13B', enabled: true },
        ],
        status: 'offline',
        latency: undefined,
      },
      {
        id: 'deepinfra',
        name: 'DeepInfra',
        type: 'openai',
        enabled: true,
        tier: 2,
        endpoint: 'https://api.deepinfra.com/v1/openai',
        keyConfigured: true,
        models: [
          { id: 'meta-llama/Llama-2-70b-chat-hf', name: 'Llama 2 70B Chat', enabled: true },
          { id: 'mistralai/Mistral-7B-Instruct-v0.1', name: 'Mistral 7B', enabled: true },
          { id: 'NousResearch/Nous-Hermes-2-Mixtral-8x7B-DPO', name: 'Nous Hermes 2', enabled: true },
        ],
        status: 'online',
        latency: 320,
      },
      {
        id: 'cloudflare',
        name: 'Cloudflare Workers AI',
        type: 'cloudflare',
        enabled: false,
        tier: 2,
        keyConfigured: false,
        models: [
          { id: '@cf/meta/llama-2-7b-chat-int8', name: 'Llama 2 7B', enabled: true },
        ],
        status: 'unknown',
      },
    ];
  },

  /**
   * Get detailed information about a specific provider
   */
  getProviderDetail: async (providerId: string): Promise<ProviderDetail | null> => {
    const providers = await mockProviderService.getProviders();
    const provider = providers.find((p) => p.id === providerId);

    if (!provider) {
      return null;
    }

    // Simulate network delay
    await new Promise((resolve) => setTimeout(resolve, 200));

    return {
      ...provider,
      usage: {
        totalTokens: Math.floor(Math.random() * 1000000) + 100000,
        requests: Math.floor(Math.random() * 5000) + 500,
        averageLatency: provider.latency || 0,
      },
      costPerMillionTokens: Math.random() * 0.1,
      requestsPerMinute: Math.floor(Math.random() * 100) + 10,
    };
  },

  /**
   * Get health status of a provider
   */
  getProviderStatus: async (providerId: string): Promise<ProviderStatus | null> => {
    // Simulate network delay
    await new Promise((resolve) => setTimeout(resolve, 150));

    const statuses: Record<string, ProviderStatus> = {
      groq: {
        status: 'online',
        lastChecked: new Date().toISOString(),
        latency: 45,
      },
      cohere: {
        status: 'unknown',
        lastChecked: new Date().toISOString(),
        latency: undefined,
      },
      deepseek: {
        status: 'online',
        lastChecked: new Date().toISOString(),
        latency: 250,
      },
      together: {
        status: 'offline',
        lastChecked: new Date().toISOString(),
        latency: undefined,
      },
      deepinfra: {
        status: 'online',
        lastChecked: new Date().toISOString(),
        latency: 320,
      },
      cloudflare: {
        status: 'unknown',
        lastChecked: new Date().toISOString(),
        latency: undefined,
      },
    };

    return statuses[providerId] || null;
  },

  /**
   * Get usage statistics for a provider
   */
  getProviderUsage: async (providerId: string): Promise<ProviderUsage | null> => {
    // Simulate network delay
    await new Promise((resolve) => setTimeout(resolve, 200));

    if (!providerId) {
      return null;
    }

    const providers = await mockProviderService.getProviders();
    const exists = providers.some((p) => p.id === providerId);

    if (!exists) {
      return null;
    }

    return {
      totalTokens: Math.floor(Math.random() * 1000000) + 100000,
      requests: Math.floor(Math.random() * 5000) + 500,
      averageLatency: Math.floor(Math.random() * 300) + 50,
    };
  },

  /**
   * Update provider configuration
   * In a real implementation, this would make a PUT request to the backend
   */
  updateProvider: async (
    providerId: string,
    updates: {
      enabled?: boolean;
      key?: string;
      endpoint?: string;
      tier?: number;
    }
  ): Promise<Provider | null> => {
    // Simulate network delay
    await new Promise((resolve) => setTimeout(resolve, 400));

    const providers = await mockProviderService.getProviders();
    const provider = providers.find((p) => p.id === providerId);

    if (!provider) {
      return null;
    }

    // Apply updates
    return {
      ...provider,
      enabled: updates.enabled !== undefined ? updates.enabled : provider.enabled,
      endpoint: updates.endpoint !== undefined ? updates.endpoint : provider.endpoint,
      tier: updates.tier !== undefined ? updates.tier : provider.tier,
      keyConfigured: updates.key !== undefined ? true : provider.keyConfigured,
    };
  },

  /**
   * Get all models across all providers
   */
  getAllModels: async () => {
    const providers = await mockProviderService.getProviders();
    const models: Record<string, ProviderModel[]> = {};

    providers.forEach((provider) => {
      models[provider.id] = provider.models;
    });

    return models;
  },
};

/**
 * Real provider service (future implementation)
 * This interface matches the mock service exactly, enabling easy swapping
 */
export const realProviderService = {
  getProviders: async (): Promise<Provider[]> => {
    const response = await fetch('/admin/providers');
    if (!response.ok) {
      throw new Error(`Failed to fetch providers: ${response.status}`);
    }
    return response.json();
  },

  getProviderDetail: async (providerId: string): Promise<ProviderDetail | null> => {
    const response = await fetch(`/admin/providers/${providerId}`);
    if (!response.ok) {
      if (response.status === 404) {
        return null;
      }
      throw new Error(`Failed to fetch provider: ${response.status}`);
    }
    return response.json();
  },

  getProviderStatus: async (providerId: string): Promise<ProviderStatus | null> => {
    const response = await fetch(`/admin/providers/${providerId}/status`);
    if (!response.ok) {
      if (response.status === 404) {
        return null;
      }
      throw new Error(`Failed to fetch provider status: ${response.status}`);
    }
    return response.json();
  },

  getProviderUsage: async (providerId: string): Promise<ProviderUsage | null> => {
    const response = await fetch(`/admin/providers/${providerId}/usage`);
    if (!response.ok) {
      if (response.status === 404) {
        return null;
      }
      throw new Error(`Failed to fetch provider usage: ${response.status}`);
    }
    return response.json();
  },

  updateProvider: async (
    providerId: string,
    updates: {
      enabled?: boolean;
      key?: string;
      endpoint?: string;
      tier?: number;
    }
  ): Promise<Provider | null> => {
    const response = await fetch(`/admin/providers/${providerId}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(updates),
    });

    if (!response.ok) {
      if (response.status === 404) {
        return null;
      }
      throw new Error(`Failed to update provider: ${response.status}`);
    }
    return response.json();
  },

  getAllModels: async () => {
    const response = await fetch('/v1/models');
    if (!response.ok) {
      throw new Error(`Failed to fetch models: ${response.status}`);
    }
    const data = await response.json();
    return data.data;
  },
};

// Default export uses mock service
export default mockProviderService;
