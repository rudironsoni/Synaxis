import axios from 'axios';

export interface ChatMessage {
  role: 'user' | 'assistant' | 'system';
  content: string;
}

export interface Usage {
  prompt_tokens: number;
  completion_tokens: number;
  total_tokens: number;
}

export interface ChatResponse {
  id: string;
  choices: { message: ChatMessage }[];
  usage?: Usage;
}

export interface ChatStreamChunk {
  id: string;
  object: 'chat.completion.chunk';
  created: number;
  model: string;
  choices: {
    index: number;
    delta: {
      content?: string;
    };
    finish_reason: string | null;
  }[];
}

export interface ChatStreamState {
  content: string;
  isComplete: boolean;
  error?: Error;
}

// Configurable client that can be updated with new BaseURL/Token from settings
export class GatewayClient {
  private client = axios.create({
    baseURL: '/v1',
    headers: { 'Content-Type': 'application/json' },
  });

  constructor(baseURL?: string, token?: string) {
    if (baseURL) this.client.defaults.baseURL = baseURL;
    if (token) this.client.defaults.headers.common['Authorization'] = `Bearer ${token}`;
  }

  updateConfig(baseURL: string, token?: string) {
    this.client.defaults.baseURL = baseURL;
    if (token) {
      this.client.defaults.headers.common['Authorization'] = `Bearer ${token}`;
    } else {
      delete this.client.defaults.headers.common['Authorization'];
    }
  }

  async sendMessage(messages: ChatMessage[], model: string = 'default'): Promise<ChatResponse> {
    const response = await this.client.post<ChatResponse>('/chat/completions', {
      model,
      messages,
      stream: false, // Start with non-streaming for simplicity
    });
    return response.data;
  }

  async *sendMessageStream(
    messages: ChatMessage[],
    model: string = 'default'
  ): AsyncGenerator<ChatStreamChunk, void, unknown> {
    const baseURL = this.client.defaults.baseURL || '/v1';
    const token = this.client.defaults.headers.common['Authorization'] as string | undefined;

    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
    };
    if (token) {
      headers['Authorization'] = token;
    }

    const response = await fetch(`${baseURL}/chat/completions`, {
      method: 'POST',
      headers,
      body: JSON.stringify({
        model,
        messages,
        stream: true,
      }),
    });

    if (!response.ok) {
      const errorData = await response.text();
      throw new Error(`HTTP ${response.status}: ${errorData || response.statusText}`);
    }

    if (!response.body) {
      throw new Error('Response body is null');
    }

    const reader = response.body.getReader();
    const decoder = new TextDecoder();
    let buffer = '';

    try {
      while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        buffer += decoder.decode(value, { stream: true });

        const lines = buffer.split('\n');
        buffer = lines.pop() || '';

        for (const line of lines) {
          const trimmed = line.trim();
          if (trimmed.startsWith('data: ')) {
            const data = trimmed.slice('data: '.length);

            if (data === '[DONE]') {
              return;
            }

            try {
              const chunk: ChatStreamChunk = JSON.parse(data);
              yield chunk;
            } catch (parseError) {
              console.warn('Failed to parse chunk:', data, parseError);
            }
          }
        }
      }

      if (buffer.trim()) {
        const trimmed = buffer.trim();
        if (trimmed.startsWith('data: ')) {
          const data = trimmed.slice('data: '.length);
          if (data !== '[DONE]') {
            try {
              const chunk: ChatStreamChunk = JSON.parse(data);
              yield chunk;
            } catch (parseError) {
              console.warn('Failed to parse final chunk:', data, parseError);
            }
          }
        }
      }
    } finally {
      reader.releaseLock();
    }
  }
}

export const defaultClient = new GatewayClient();
