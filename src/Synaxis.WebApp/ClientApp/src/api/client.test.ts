import { describe, it, expect, vi, beforeEach } from 'vitest';
import { GatewayClient } from './client';
import axios from 'axios';

vi.mock('axios');

type AxiosError = Error & { response?: { status: number; data: { error: string } }; request?: Record<string, unknown> };

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
    // @ts-expect-error - mocking private axios creation
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
      (error as AxiosError).response = { status: 400, data: { error: 'Invalid request' } };
      mockAxiosInstance.post.mockRejectedValue(error);

      const messages = [{ role: 'user' as const, content: 'Hi' }];

      await expect(client.sendMessage(messages)).rejects.toThrow('Bad Request');
    });

    it('should throw error on 401 Unauthorized', async () => {
      const error = new Error('Unauthorized');
      (error as AxiosError).response = { status: 401, data: { error: 'Invalid token' } };
      mockAxiosInstance.post.mockRejectedValue(error);

      const messages = [{ role: 'user' as const, content: 'Hi' }];

      await expect(client.sendMessage(messages)).rejects.toThrow('Unauthorized');
    });

    it('should throw error on 429 Rate Limit', async () => {
      const error = new Error('Too Many Requests');
      (error as AxiosError).response = { status: 429, data: { error: 'Rate limit exceeded' } };
      mockAxiosInstance.post.mockRejectedValue(error);

      const messages = [{ role: 'user' as const, content: 'Hi' }];

      await expect(client.sendMessage(messages)).rejects.toThrow('Too Many Requests');
    });

    it('should throw error on 500 Internal Server Error', async () => {
      const error = new Error('Internal Server Error');
      (error as AxiosError).response = { status: 500, data: { error: 'Server error' } };
      mockAxiosInstance.post.mockRejectedValue(error);

      const messages = [{ role: 'user' as const, content: 'Hi' }];

      await expect(client.sendMessage(messages)).rejects.toThrow('Internal Server Error');
    });

    it('should throw error on network failure', async () => {
      const error = new Error('Network Error');
      (error as AxiosError).request = {}; // Indicates request was made but no response
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
      client.updateConfig('http://api.com', 'old-token');
      expect(mockAxiosInstance.defaults.headers.common['Authorization']).toBe('Bearer old-token');

      client.updateConfig('http://new-api.com', 'new-token');
      expect(mockAxiosInstance.defaults.headers.common['Authorization']).toBe('Bearer new-token');
    });

    it('should remove token when updating without token parameter', () => {
      client.updateConfig('http://api.com', 'existing-token');
      expect(mockAxiosInstance.defaults.headers.common['Authorization']).toBe('Bearer existing-token');

      client.updateConfig('http://new-api.com');
      expect(mockAxiosInstance.defaults.headers.common['Authorization']).toBeUndefined();
    });

    it('should handle empty string token', () => {
      client.updateConfig('http://api.com', '');
      expect(mockAxiosInstance.defaults.headers.common['Authorization']).toBeUndefined();
    });
  });

  describe('sendMessageStream', () => {
    let mockFetch: ReturnType<typeof vi.fn>;
    let mockReader: {
      read: ReturnType<typeof vi.fn>;
      releaseLock: ReturnType<typeof vi.fn>;
    };
    let mockBody: {
      getReader: ReturnType<typeof vi.fn>;
    };

    beforeEach(() => {
      mockFetch = vi.fn();
      global.fetch = mockFetch;

      mockReader = {
        read: vi.fn(),
        releaseLock: vi.fn(),
      };

      mockBody = {
        getReader: vi.fn().mockReturnValue(mockReader),
      };
    });

    it('should yield chunks from stream', async () => {
      const chunk1 = {
        id: 'chatcmpl-123',
        object: 'chat.completion.chunk' as const,
        created: 1234567890,
        model: 'default',
        choices: [{ index: 0, delta: { content: 'Hello' }, finish_reason: null }],
      };
      const chunk2 = {
        id: 'chatcmpl-123',
        object: 'chat.completion.chunk' as const,
        created: 1234567890,
        model: 'default',
        choices: [{ index: 0, delta: { content: ' world' }, finish_reason: null }],
      };
      const doneChunk = {
        id: 'chatcmpl-123',
        object: 'chat.completion.chunk' as const,
        created: 1234567890,
        model: 'default',
        choices: [{ index: 0, delta: {}, finish_reason: 'stop' }],
      };

      const encoder = new TextEncoder();
      const streamData = `data: ${JSON.stringify(chunk1)}\n\ndata: ${JSON.stringify(chunk2)}\n\ndata: ${JSON.stringify(doneChunk)}\n\ndata: [DONE]\n\n`;

      mockReader.read
        .mockResolvedValueOnce({ done: false, value: encoder.encode(streamData) })
        .mockResolvedValueOnce({ done: true });

      mockFetch.mockResolvedValue({
        ok: true,
        body: mockBody,
      });

      const messages = [{ role: 'user' as const, content: 'Hi' }];
      const chunks: Array<{ id: string; choices: Array<{ delta: { content?: string } }> }> = [];

      for await (const chunk of client.sendMessageStream(messages)) {
        chunks.push(chunk);
      }

      expect(chunks).toHaveLength(3);
      expect(chunks[0].choices[0].delta.content).toBe('Hello');
      expect(chunks[1].choices[0].delta.content).toBe(' world');
      expect(chunks[2].choices[0].finish_reason).toBe('stop');
      expect(mockReader.releaseLock).toHaveBeenCalled();
    });

    it('should handle chunked data across multiple reads', async () => {
      const chunk = {
        id: 'chatcmpl-456',
        object: 'chat.completion.chunk' as const,
        created: 1234567890,
        model: 'gpt-4',
        choices: [{ index: 0, delta: { content: 'Complete response' }, finish_reason: null }],
      };

      const encoder = new TextEncoder();
      const data = `data: ${JSON.stringify(chunk)}\n\ndata: [DONE]\n\n`;
      const half = Math.floor(data.length / 2);

      mockReader.read
        .mockResolvedValueOnce({ done: false, value: encoder.encode(data.slice(0, half)) })
        .mockResolvedValueOnce({ done: false, value: encoder.encode(data.slice(half)) })
        .mockResolvedValueOnce({ done: true });

      mockFetch.mockResolvedValue({
        ok: true,
        body: mockBody,
      });

      const chunks: Array<{ id: string }> = [];
      for await (const chunk of client.sendMessageStream([{ role: 'user', content: 'Hi' }])) {
        chunks.push(chunk);
      }

      expect(chunks).toHaveLength(1);
      expect(chunks[0].id).toBe('chatcmpl-456');
    });

     it('should include authorization header when token is set', async () => {
       client.updateConfig('http://api.com', 'my-token');

       mockReader.read.mockResolvedValue({ done: true });
       mockFetch.mockResolvedValue({
         ok: true,
         body: mockBody,
       });

        // eslint-disable-next-line @typescript-eslint/no-unused-vars, no-empty
        for await (const _chunk of client.sendMessageStream([{ role: 'user', content: 'Hi' }])) {
        }

        expect(mockFetch).toHaveBeenCalledWith(
          'http://api.com/chat/completions',
          expect.objectContaining({
            headers: expect.objectContaining({
              'Authorization': 'Bearer my-token',
            }),
          })
        );
      });

      it('should throw error on HTTP error response', async () => {
        mockFetch.mockResolvedValue({
          ok: false,
          status: 401,
          statusText: 'Unauthorized',
          text: async () => 'Invalid token',
        });

        const messages = [{ role: 'user' as const, content: 'Hi' }];

        await expect(async () => {
          // eslint-disable-next-line @typescript-eslint/no-unused-vars, no-empty
          for await (const _chunk of client.sendMessageStream(messages)) {
          }
       }).rejects.toThrow('HTTP 401: Invalid token');
     });

      it('should throw error when response body is null', async () => {
        mockFetch.mockResolvedValue({
          ok: true,
          body: null,
        });

        const messages = [{ role: 'user' as const, content: 'Hi' }];

        await expect(async () => {
          // eslint-disable-next-line @typescript-eslint/no-unused-vars, no-empty
          for await (const _chunk of client.sendMessageStream(messages)) {
          }
        }).rejects.toThrow('Response body is null');
      });

      it('should use default model when not specified', async () => {
        mockReader.read.mockResolvedValue({ done: true });
        mockFetch.mockResolvedValue({
          ok: true,
          body: mockBody,
        });

        // eslint-disable-next-line @typescript-eslint/no-unused-vars, no-empty
        for await (const _chunk of client.sendMessageStream([{ role: 'user', content: 'Hi' }])) {
        }

       const [, init] = mockFetch.mock.calls[0];
       const body = JSON.parse(init.body);
       expect(body.model).toBe('default');
     });

      it('should use custom model when specified', async () => {
        mockReader.read.mockResolvedValue({ done: true });
        mockFetch.mockResolvedValue({
          ok: true,
          body: mockBody,
        });

        // eslint-disable-next-line @typescript-eslint/no-unused-vars, no-empty
        for await (const _chunk of client.sendMessageStream([{ role: 'user', content: 'Hi' }], 'gpt-4')) {
        }

       const [, init] = mockFetch.mock.calls[0];
       const body = JSON.parse(init.body);
       expect(body.model).toBe('gpt-4');
     });

      it('should set stream flag to true', async () => {
        mockReader.read.mockResolvedValue({ done: true });
        mockFetch.mockResolvedValue({
          ok: true,
          body: mockBody,
        });

        // eslint-disable-next-line @typescript-eslint/no-unused-vars, no-empty
        for await (const _chunk of client.sendMessageStream([{ role: 'user', content: 'Hi' }])) {
        }

       const [, init] = mockFetch.mock.calls[0];
       const body = JSON.parse(init.body);
       expect(body.stream).toBe(true);
     });

    it('should handle invalid JSON gracefully', async () => {
      const consoleSpy = vi.spyOn(console, 'warn').mockImplementation(() => {});

      const encoder = new TextEncoder();
      const streamData = `data: invalid json here\n\ndata: [DONE]\n\n`;

      mockReader.read
        .mockResolvedValueOnce({ done: false, value: encoder.encode(streamData) })
        .mockResolvedValueOnce({ done: true });

      mockFetch.mockResolvedValue({
        ok: true,
        body: mockBody,
      });

      const chunks: Array<unknown> = [];
      for await (const chunk of client.sendMessageStream([{ role: 'user', content: 'Hi' }])) {
        chunks.push(chunk);
      }

      expect(chunks).toHaveLength(0);
      expect(consoleSpy).toHaveBeenCalled();
      consoleSpy.mockRestore();
    });

    it('should handle malformed SSE format', async () => {
      const chunk = {
        id: 'chatcmpl-789',
        object: 'chat.completion.chunk' as const,
        created: 1234567890,
        model: 'default',
        choices: [{ index: 0, delta: { content: 'Data' }, finish_reason: null }],
      };

      const encoder = new TextEncoder();
      const streamData = `event: message\ndata: ${JSON.stringify(chunk)}\n\ndata: [DONE]\n\n`;

      mockReader.read
        .mockResolvedValueOnce({ done: false, value: encoder.encode(streamData) })
        .mockResolvedValueOnce({ done: true });

      mockFetch.mockResolvedValue({
        ok: true,
        body: mockBody,
      });

      const chunks: Array<{ id: string }> = [];
      for await (const chunk of client.sendMessageStream([{ role: 'user', content: 'Hi' }])) {
        chunks.push(chunk);
      }

      expect(chunks).toHaveLength(1);
      expect(chunks[0].id).toBe('chatcmpl-789');
    });
  });
});
