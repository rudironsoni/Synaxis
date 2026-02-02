import axios from 'axios';
import type { Provider, ProviderDetail, ProviderStatus, ProviderUsage } from './mockProviderService';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:8080';

const client = axios.create({
  baseURL: API_BASE_URL,
  headers: { 'Content-Type': 'application/json' },
});

client.interceptors.request.use((config) => {
  const token = localStorage.getItem('jwtToken');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export const realProviderService = {
  async getProviders(): Promise<Provider[]> {
    const response = await client.get('/api/providers');
    return response.data.providers;
  },

  async getProviderDetail(id: string): Promise<ProviderDetail> {
    const response = await client.get(`/api/providers/${id}`);
    return response.data;
  },

  async getProviderStatus(id: string): Promise<ProviderStatus> {
    const response = await client.get(`/api/providers/${id}/status`);
    return response.data;
  },

  async getProviderUsage(id: string): Promise<ProviderUsage> {
    const response = await client.get(`/api/providers/${id}/usage`);
    return response.data;
  },

  async updateProvider(id: string, config: Partial<Provider>): Promise<{ success: boolean }> {
    const response = await client.put(`/api/providers/${id}/config`, config);
    return response.data;
  },

  async getAllModels(): Promise<Array<{ provider: string; models: string[] }>> {
    const response = await client.get('/api/models');
    return response.data.providers.map((p: { id: string; models: string[] }) => ({
      provider: p.id,
      models: p.models,
    }));
  },
};
