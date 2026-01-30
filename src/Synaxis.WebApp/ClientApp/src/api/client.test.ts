import { describe, it, expect, vi, beforeEach } from 'vitest';
import { GatewayClient } from './client';
import axios from 'axios';

vi.mock('axios');

describe('GatewayClient', () => {
  let client: GatewayClient;
  let mockAxiosInstance: {
    post: ReturnType<typeof vi.fn>;
    defaults: {
      baseURL: string;
      headers: { common: Record<string, string> };
    };
  };

  beforeEach(() => {
    vi.resetAllMocks();
    mockAxiosInstance = {
      post: vi.fn(),
      defaults: {
        baseURL: '/v1',
        headers: { common: {} },
      },
    };
    // @ts-ignore
    axios.create.mockReturnValue(mockAxiosInstance);
    client = new GatewayClient();
  });

  describe('constructor', () => {
    it('should create client with default config', () => {
      expect(axios.create).toHaveBeenCalledWith({
        baseURL: '/v1',
        headers: { 'Content-Type': 'application/json' },
      });
    });

    it('should create client with custom baseURL', () => {
      const customUrl = 'http://custom-api.com';
      new GatewayClient(customUrl);

      expect(axios.create).toHaveBeenCalledWith({
        baseURL: '/v1',
        headers: { 'Content-Type': 'application/json' },
      });
      expect(mockAxiosInstance.defaults.baseURL).toBe(customUrl);
    });

    it('should create client with custom baseURL and token', () => {
      const customUrl = 'http://custom-api.com';
      const token = 'test-token-123';
      new GatewayClient(customUrl, token);

      expect(mockAxiosInstance.defaults.baseURL).toBe(customUrl);
      expect(mockAxiosInstance.defaults.headers.common['Authorization']).toBe('Bearer test-token-123');
    });
  });

  describe('sendMessage', () => {
    it('should send a message successfully with default model', async () => {
      const mockResponse = {
        data: {
          id: 'chatcmpl-123',
          choices: [{ message: { role: 'assistant', content: 'Hello!' } }],
          usage: { prompt_tokens: 10, completion_tokens: 5, total_tokens: 15 },
        },
      };
      mockAxiosInstance.post.mockResolvedValue(mockResponse);

      const messages = [{ role: 'user' as const, content: 'Hi' }];
      const response = await client.sendMessage(messages);

      expect(mockAxiosInstance.post).toHaveBeenCalledWith('/chat/completions', {
        model: 'default',
        messages,
        stream: false,
      });
      expect(response).toEqual(mockResponse.data);
    });

    it('should send a message with custom model', async () => {
      const mockResponse = {
        data: {
          id: 'chatcmpl-456',
          choices: [{ message: { role: 'assistant', content: 'Custom model response' } }],
          usage: { prompt_tokens: 5, completion_tokens: 3, total_tokens: 8 },
        },
      };
      mockAxiosInstance.post.mockResolvedValue(mockResponse);

      const messages = [{ role: 'user' as const, content: 'Hello' }];
      const customModel = 'gpt-4';
      const response = await client.sendMessage(messages, customModel);

      expect(mockAxiosInstance.post).toHaveBeenCalledWith('/chat/completions', {
        model: customModel,
        messages,
        stream: false,
      });
      expect(response).toEqual(mockResponse.data);
    });

    it('should handle multiple messages', async () => {
      const mockResponse = {
        data: {
          id: 'chatcmpl-789',
          choices: [{ message: { role: 'assistant', content: 'Got it!' } }],
          usage: { prompt_tokens: 20, completion_tokens: 2, total_tokens: 22 },
        },
      };
      mockAxiosInstance.post.mockResolvedValue(mockResponse);

      const messages = [
        { role: 'system' as const, content: 'You are helpful' },
        { role: 'user' as const, content: 'Hello' },
        { role: 'assistant' as const, content: 'Hi there!' },
        { role: 'user' as const, content: 'How are you?' },
      ];
      const response = await client.sendMessage(messages);

      expect(mockAxiosInstance.post).toHaveBeenCalledWith('/chat/completions', {
        model: 'default',
        messages,
        stream: false,
      });
      expect(response.choices[0].message.content).toBe('Got it!');
    });

    it('should throw error on 400 Bad Request', async () => {
      const error = new Error('Bad Request');
      (error as any).response = { status: 400, data: { error: 'Invalid request' } };
      mockAxiosInstance.post.mockRejectedValue(error);

      const messages = [{ role: 'user' as const, content: 'Hi' }];

      await expect(client.sendMessage(messages)).rejects.toThrow('Bad Request');
    });

    it('should throw error on 401 Unauthorized', async () => {
      const error = new Error('Unauthorized');
      (error as any).response = { status: 401, data: { error: 'Invalid token' } };
      mockAxiosInstance.post.mockRejectedValue(error);

      const messages = [{ role: 'user' as const, content: 'Hi' }];

      await expect(client.sendMessage(messages)).rejects.toThrow('Unauthorized');
    });

    it('should throw error on 429 Rate Limit', async () => {
      const error = new Error('Too Many Requests');
      (error as any).response = { status: 429, data: { error: 'Rate limit exceeded' } };
      mockAxiosInstance.post.mockRejectedValue(error);

      const messages = [{ role: 'user' as const, content: 'Hi' }];

      await expect(client.sendMessage(messages)).rejects.toThrow('Too Many Requests');
    });

    it('should throw error on 500 Internal Server Error', async () => {
      const error = new Error('Internal Server Error');
      (error as any).response = { status: 500, data: { error: 'Server error' } };
      mockAxiosInstance.post.mockRejectedValue(error);

      const messages = [{ role: 'user' as const, content: 'Hi' }];

      await expect(client.sendMessage(messages)).rejects.toThrow('Internal Server Error');
    });

    it('should throw error on network failure', async () => {
      const error = new Error('Network Error');
      (error as any).request = {}; // Indicates request was made but no response
      mockAxiosInstance.post.mockRejectedValue(error);

      const messages = [{ role: 'user' as const, content: 'Hi' }];

      await expect(client.sendMessage(messages)).rejects.toThrow('Network Error');
    });

    it('should handle response without usage data', async () => {
      const mockResponse = {
        data: {
          id: 'chatcmpl-minimal',
          choices: [{ message: { role: 'assistant', content: 'Response' } }],
          // No usage field
        },
      };
      mockAxiosInstance.post.mockResolvedValue(mockResponse);

      const messages = [{ role: 'user' as const, content: 'Hi' }];
      const response = await client.sendMessage(messages);

      expect(response.usage).toBeUndefined();
      expect(response.choices[0].message.content).toBe('Response');
    });
  });

  describe('updateConfig', () => {
    it('should update baseURL only', () => {
      const newUrl = 'http://new-api.com/v2';
      client.updateConfig(newUrl);

      expect(mockAxiosInstance.defaults.baseURL).toBe(newUrl);
      expect(mockAxiosInstance.defaults.headers.common['Authorization']).toBeUndefined();
    });

    it('should update baseURL and add token', () => {
      const newUrl = 'http://secure-api.com';
      const token = 'new-secret-token';
      client.updateConfig(newUrl, token);

      expect(mockAxiosInstance.defaults.baseURL).toBe(newUrl);
      expect(mockAxiosInstance.defaults.headers.common['Authorization']).toBe('Bearer new-secret-token');
    });

    it('should update baseURL and replace existing token', () => {
      // First set a token
      client.updateConfig('http://api.com', 'old-token');
      expect(mockAxiosInstance.defaults.headers.common['Authorization']).toBe('Bearer old-token');

      // Now update with new token
      client.updateConfig('http://new-api.com', 'new-token');
      expect(mockAxiosInstance.defaults.headers.common['Authorization']).toBe('Bearer new-token');
    });

    it('should remove token when updating without token parameter', () => {
      // First set a token
      client.updateConfig('http://api.com', 'existing-token');
      expect(mockAxiosInstance.defaults.headers.common['Authorization']).toBe('Bearer existing-token');

      // Now update without token - should remove it
      client.updateConfig('http://new-api.com');
      expect(mockAxiosInstance.defaults.headers.common['Authorization']).toBeUndefined();
    });

    it('should handle empty string token', () => {
      client.updateConfig('http://api.com', '');
      expect(mockAxiosInstance.defaults.headers.common['Authorization']).toBeUndefined();
    });
  });
});
