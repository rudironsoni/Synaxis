export interface ModelConfiguration {
  id: string;
  provider: string;
  modelPath: string;
  streaming: boolean;
  tools: boolean;
  vision: boolean;
  structuredOutput: boolean;
  logProbs: boolean;
}

export interface CanonicalModel extends ModelConfiguration {
  name: string;
  description?: string;
  costPerMillionTokens?: number;
  contextWindow?: number;
  maxTokens?: number;
}

export interface AliasConfiguration {
  id: string;
  candidates: string[];
  priority: number;
  description?: string;
}

export interface GatewayConfiguration {
  jwtSecret: string;
  defaultModel: string;
  defaultProvider: string;
  requestTimeout: number;
  maxRetries: number;
  cacheTTL: number;
  rateLimitPerMinute: number;
}

export const mockConfigService = {
  getCanonicalModels: async (): Promise<CanonicalModel[]> => {
    await new Promise((resolve) => setTimeout(resolve, 200));

    return [
      {
        id: 'llama-3.1-70b-versatile',
        name: 'Llama 3.1 70B Versatile',
        provider: 'groq',
        modelPath: 'llama-3.1-70b-versatile',
        description: 'Highly capable open model optimized for dialogue and coding',
        streaming: true,
        tools: true,
        vision: false,
        structuredOutput: true,
        logProbs: false,
        contextWindow: 128000,
        maxTokens: 8000,
      },
      {
        id: 'llama-3.1-8b-instant',
        name: 'Llama 3.1 8B Instant',
        provider: 'groq',
        modelPath: 'llama-3.1-8b-instant',
        description: 'Compact and fast model for quick responses',
        streaming: true,
        tools: true,
        vision: false,
        structuredOutput: true,
        logProbs: false,
        contextWindow: 128000,
        maxTokens: 8000,
      },
      {
        id: 'mixtral-8x7b-32768',
        name: 'Mixtral 8x7B',
        provider: 'groq',
        modelPath: 'mixtral-8x7b-32768',
        description: 'Mixture of experts model with balanced performance',
        streaming: true,
        tools: true,
        vision: false,
        structuredOutput: true,
        logProbs: false,
        contextWindow: 32768,
        maxTokens: 8000,
      },
      {
        id: 'command-r',
        name: 'Command R',
        provider: 'cohere',
        modelPath: 'command-r',
        description: 'Advanced instruction-following model from Cohere',
        streaming: true,
        tools: true,
        vision: false,
        structuredOutput: true,
        logProbs: false,
        contextWindow: 128000,
        maxTokens: 4096,
      },
      {
        id: 'command-r-plus',
        name: 'Command R+',
        provider: 'cohere',
        modelPath: 'command-r-plus',
        description: 'Most capable Cohere model for complex tasks',
        streaming: true,
        tools: true,
        vision: false,
        structuredOutput: true,
        logProbs: false,
        contextWindow: 128000,
        maxTokens: 4096,
      },
      {
        id: 'deepseek-chat',
        name: 'DeepSeek Chat',
        provider: 'DeepSeek',
        modelPath: 'deepseek-chat',
        description: 'Advanced chat model with strong reasoning',
        streaming: true,
        tools: true,
        vision: false,
        structuredOutput: true,
        logProbs: false,
        contextWindow: 64000,
        maxTokens: 4096,
      },
      {
        id: 'deepseek-coder',
        name: 'DeepSeek Coder',
        provider: 'DeepSeek',
        modelPath: 'deepseek-coder',
        description: 'Specialized model for code generation and analysis',
        streaming: true,
        tools: true,
        vision: false,
        structuredOutput: true,
        logProbs: false,
        contextWindow: 4096,
        maxTokens: 2048,
      },
    ];
  },

  getAliases: async (): Promise<AliasConfiguration[]> => {
    await new Promise((resolve) => setTimeout(resolve, 200));

    return [
      {
        id: 'default',
        candidates: ['llama-3.1-70b-versatile', 'command-r', 'deepseek-chat'],
        priority: 0,
        description: 'Default model alias, routes to best available provider',
      },
      {
        id: 'fast',
        candidates: ['llama-3.1-8b-instant', 'command-r'],
        priority: 1,
        description: 'Fast models optimized for low latency',
      },
      {
        id: 'powerful',
        candidates: ['llama-3.1-70b-versatile', 'command-r-plus'],
        priority: 0,
        description: 'Most capable models for complex tasks',
      },
      {
        id: 'coding',
        candidates: ['deepseek-coder', 'llama-3.1-70b-versatile'],
        priority: 0,
        description: 'Models optimized for code generation',
      },
    ];
  },

  getGatewayConfig: async (): Promise<GatewayConfiguration> => {
    await new Promise((resolve) => setTimeout(resolve, 150));

    return {
      jwtSecret: 'dev-secret-key-change-in-production',
      defaultModel: 'default',
      defaultProvider: 'groq',
      requestTimeout: 30000,
      maxRetries: 3,
      cacheTTL: 3600,
      rateLimitPerMinute: 60,
    };
  },

  updateCanonicalModel: async (
    modelId: string,
    updates: Partial<CanonicalModel>
  ): Promise<CanonicalModel | null> => {
    await new Promise((resolve) => setTimeout(resolve, 300));

    const models = await mockConfigService.getCanonicalModels();
    const model = models.find((m) => m.id === modelId);

    if (!model) {
      return null;
    }

    return {
      ...model,
      ...updates,
      id: modelId,
    };
  },

  updateAlias: async (
    aliasId: string,
    updates: Partial<AliasConfiguration>
  ): Promise<AliasConfiguration | null> => {
    await new Promise((resolve) => setTimeout(resolve, 300));

    const aliases = await mockConfigService.getAliases();
    const alias = aliases.find((a) => a.id === aliasId);

    if (!alias) {
      return null;
    }

    return {
      ...alias,
      ...updates,
      id: aliasId,
    };
  },

  updateGatewayConfig: async (
    updates: Partial<GatewayConfiguration>
  ): Promise<GatewayConfiguration> => {
    await new Promise((resolve) => setTimeout(resolve, 300));

    const current = await mockConfigService.getGatewayConfig();
    return {
      ...current,
      ...updates,
    };
  },

  validateModelConfiguration: async (
    model: ModelConfiguration
  ): Promise<{ valid: boolean; errors?: string[] }> => {
    await new Promise((resolve) => setTimeout(resolve, 100));

    const errors: string[] = [];

    if (!model.id) {
      errors.push('Model ID is required');
    }

    if (!model.provider) {
      errors.push('Provider is required');
    }

    if (!model.modelPath) {
      errors.push('Model path is required');
    }

    return {
      valid: errors.length === 0,
      errors: errors.length > 0 ? errors : undefined,
    };
  },

  getModelCapabilities: async (modelId: string) => {
    await new Promise((resolve) => setTimeout(resolve, 150));

    const models = await mockConfigService.getCanonicalModels();
    const model = models.find((m) => m.id === modelId);

    if (!model) {
      return null;
    }

    return {
      streaming: model.streaming,
      tools: model.tools,
      vision: model.vision,
      structuredOutput: model.structuredOutput,
      logProbs: model.logProbs,
    };
  },
};

export const realConfigService = {
  getCanonicalModels: async (): Promise<CanonicalModel[]> => {
    const response = await fetch('/admin/config/models');
    if (!response.ok) {
      throw new Error(`Failed to fetch models: ${response.status}`);
    }
    return response.json();
  },

  getAliases: async (): Promise<AliasConfiguration[]> => {
    const response = await fetch('/admin/config/aliases');
    if (!response.ok) {
      throw new Error(`Failed to fetch aliases: ${response.status}`);
    }
    return response.json();
  },

  getGatewayConfig: async (): Promise<GatewayConfiguration> => {
    const response = await fetch('/admin/config/gateway');
    if (!response.ok) {
      throw new Error(`Failed to fetch gateway config: ${response.status}`);
    }
    return response.json();
  },

  updateCanonicalModel: async (
    modelId: string,
    updates: Partial<CanonicalModel>
  ): Promise<CanonicalModel | null> => {
    const response = await fetch(`/admin/config/models/${modelId}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(updates),
    });

    if (!response.ok) {
      if (response.status === 404) {
        return null;
      }
      throw new Error(`Failed to update model: ${response.status}`);
    }
    return response.json();
  },

  updateAlias: async (
    aliasId: string,
    updates: Partial<AliasConfiguration>
  ): Promise<AliasConfiguration | null> => {
    const response = await fetch(`/admin/config/aliases/${aliasId}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(updates),
    });

    if (!response.ok) {
      if (response.status === 404) {
        return null;
      }
      throw new Error(`Failed to update alias: ${response.status}`);
    }
    return response.json();
  },

  updateGatewayConfig: async (
    updates: Partial<GatewayConfiguration>
  ): Promise<GatewayConfiguration> => {
    const response = await fetch('/admin/config/gateway', {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(updates),
    });

    if (!response.ok) {
      throw new Error(`Failed to update gateway config: ${response.status}`);
    }
    return response.json();
  },

  validateModelConfiguration: async (
    model: ModelConfiguration
  ): Promise<{ valid: boolean; errors?: string[] }> => {
    const response = await fetch('/admin/config/models/validate', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(model),
    });

    if (!response.ok) {
      throw new Error(`Validation failed: ${response.status}`);
    }
    return response.json();
  },

  getModelCapabilities: async (modelId: string) => {
    const response = await fetch(`/admin/config/models/${modelId}/capabilities`);

    if (!response.ok) {
      if (response.status === 404) {
        return null;
      }
      throw new Error(`Failed to fetch capabilities: ${response.status}`);
    }
    return response.json();
  },
};

export default mockConfigService;
