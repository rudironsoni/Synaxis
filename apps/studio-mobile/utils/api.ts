import axios, { AxiosInstance } from 'axios';
import { Message, StreamingResponse } from '@/types';

export class ChatAPI {
  private client: AxiosInstance;

  constructor(apiUrl: string, apiKey: string) {
    this.client = axios.create({
      baseURL: apiUrl,
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${apiKey}`,
      },
      timeout: 30000,
    });
  }

  async sendMessage(
    messages: Message[],
    model: string,
    temperature: number,
    maxTokens: number,
    onChunk?: (chunk: string) => void
  ): Promise<string> {
    try {
      if (onChunk) {
        return await this.streamMessage(messages, model, temperature, maxTokens, onChunk);
      } else {
        return await this.sendMessageNonStream(messages, model, temperature, maxTokens);
      }
    } catch (error) {
      console.error('API Error:', error);
      throw error;
    }
  }

  private async sendMessageNonStream(
    messages: Message[],
    model: string,
    temperature: number,
    maxTokens: number
  ): Promise<string> {
    const response = await this.client.post('/chat/completions', {
      model,
      messages: messages.map((m) => ({
        role: m.role,
        content: m.content,
      })),
      temperature,
      max_tokens: maxTokens,
    });

    return response.data.choices[0].message.content;
  }

  private async streamMessage(
    messages: Message[],
    model: string,
    temperature: number,
    maxTokens: number,
    onChunk: (chunk: string) => void
  ): Promise<string> {
    const response = await this.client.post('/chat/completions', {
      model,
      messages: messages.map((m) => ({
        role: m.role,
        content: m.content,
      })),
      temperature,
      max_tokens: maxTokens,
      stream: true,
    }, {
      responseType: 'stream',
    });

    let fullContent = '';

    return new Promise((resolve, reject) => {
      response.data.on('data', (chunk: Buffer) => {
        const lines = chunk.toString().split('\n').filter((line) => line.trim() !== '');

        for (const line of lines) {
          if (line.startsWith('data: ')) {
            const data = line.slice(6);

            if (data === '[DONE]') {
              resolve(fullContent);
              return;
            }

            try {
              const parsed: StreamingResponse = JSON.parse(data);
              const content = parsed.content;

              if (content) {
                fullContent += content;
                onChunk(content);
              }

              if (parsed.done) {
                resolve(fullContent);
              }
            } catch (e) {
              // Skip invalid JSON
            }
          }
        }
      });

      response.data.on('end', () => {
        resolve(fullContent);
      });

      response.data.on('error', (error: Error) => {
        reject(error);
      });
    });
  }

  async testConnection(): Promise<boolean> {
    try {
      await this.client.get('/models');
      return true;
    } catch (error) {
      console.error('Connection test failed:', error);
      return false;
    }
  }
}

export const createChatAPI = (apiUrl: string, apiKey: string): ChatAPI => {
  return new ChatAPI(apiUrl, apiKey);
};
