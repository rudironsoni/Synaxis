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
}

export const defaultClient = new GatewayClient();
