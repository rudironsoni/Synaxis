import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import ProviderConfig from './ProviderConfig';

const mockFetch = vi.fn();
global.fetch = mockFetch;

const mockJwtToken = 'test-jwt-token';

vi.mock('@/stores/settings', () => ({
  default: (selector: (s: { jwtToken: string }) => string) => selector({
    jwtToken: mockJwtToken,
  }),
}));

describe('ProviderConfig', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  const mockProviders = [
    {
      id: 'groq',
      name: 'Groq',
      type: 'groq',
      enabled: true,
      tier: 0,
      keyConfigured: true,
      models: [
        { id: 'llama-3.1-70b', name: 'Llama 3.1 70B', enabled: true },
      ],
      status: 'online',
      latency: 45,
    },
    {
      id: 'cohere',
      name: 'Cohere',
      type: 'cohere',
      enabled: false,
      tier: 1,
      keyConfigured: false,
      models: [
        { id: 'command-r', name: 'Command R', enabled: true },
      ],
      status: 'unknown',
    },
  ];

  it('should render provider configuration header', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => mockProviders,
    });

    render(<ProviderConfig />);

    await waitFor(() => {
      expect(screen.getByText('Provider Configuration')).toBeInTheDocument();
      expect(screen.getByText('Manage AI provider settings, API keys, and model availability.')).toBeInTheDocument();
    });
  });

  it('should fetch providers on mount', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => mockProviders,
    });

    render(<ProviderConfig />);

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledWith('/admin/providers', {
        headers: {
          'Authorization': 'Bearer test-jwt-token',
        },
      });
    });
  });

  it('should display provider list', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => mockProviders,
    });

    render(<ProviderConfig />);

    await waitFor(() => {
      expect(screen.getByText('Groq')).toBeInTheDocument();
      expect(screen.getByText('Cohere')).toBeInTheDocument();
    });
  });

  it('should show provider status indicators', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => mockProviders,
    });

    render(<ProviderConfig />);

    await waitFor(() => {
      expect(screen.getByText('Online')).toBeInTheDocument();
      expect(screen.getByText('Unknown')).toBeInTheDocument();
    });
  });

  it('should show key configuration status', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => mockProviders,
    });

    render(<ProviderConfig />);

    await waitFor(() => {
      expect(screen.getByText('Key set')).toBeInTheDocument();
      expect(screen.getByText('No key')).toBeInTheDocument();
    });
  });

  it('should show enabled/disabled status', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => mockProviders,
    });

    render(<ProviderConfig />);

    await waitFor(() => {
      expect(screen.getByText('Enabled')).toBeInTheDocument();
      expect(screen.getByText('Disabled')).toBeInTheDocument();
    });
  });

  it('should expand provider details on click', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => mockProviders,
    });

    render(<ProviderConfig />);

    await waitFor(() => {
      expect(screen.getByText('Groq')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByText('Groq'));

    await waitFor(() => {
      expect(screen.getByText('Provider ID')).toBeInTheDocument();
      expect(screen.getByText('Type')).toBeInTheDocument();
      expect(screen.getByText('Available Models')).toBeInTheDocument();
    });
  });

  it('should show API key edit form', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => mockProviders,
    });

    render(<ProviderConfig />);

    await waitFor(() => {
      expect(screen.getByText('Groq')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByText('Groq'));

    await waitFor(() => {
      const updateKeyButton = screen.getByText('Update Key');
      fireEvent.click(updateKeyButton);
    });

    await waitFor(() => {
      expect(screen.getByPlaceholderText('Enter API key...')).toBeInTheDocument();
      expect(screen.getByText('Save')).toBeInTheDocument();
      expect(screen.getByText('Cancel')).toBeInTheDocument();
    });
  });

  it('should toggle provider enabled state', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => mockProviders,
    });

    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ success: true }),
    });

    render(<ProviderConfig />);

    await waitFor(() => {
      expect(screen.getByText('Groq')).toBeInTheDocument();
    });

    const enabledButton = screen.getByText('Enabled');
    fireEvent.click(enabledButton);

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledWith(
        '/admin/providers/groq',
        expect.objectContaining({
          method: 'PUT',
          body: JSON.stringify({ enabled: false }),
        })
      );
    });
  });

  it('should save API key', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => mockProviders,
    });

    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ success: true }),
    });

    render(<ProviderConfig />);

    await waitFor(() => {
      expect(screen.getByText('Groq')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByText('Groq'));

    await waitFor(() => {
      const updateKeyButton = screen.getByText('Update Key');
      fireEvent.click(updateKeyButton);
    });

    const keyInput = screen.getByPlaceholderText('Enter API key...');
    fireEvent.change(keyInput, { target: { value: 'new-api-key' } });

    const saveButton = screen.getByText('Save');
    fireEvent.click(saveButton);

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledWith(
        '/admin/providers/groq',
        expect.objectContaining({
          method: 'PUT',
          body: JSON.stringify({ key: 'new-api-key' }),
        })
      );
    });
  });

  it('should refresh providers on button click', async () => {
    mockFetch.mockResolvedValue({
      ok: true,
      json: async () => mockProviders,
    });

    render(<ProviderConfig />);

    await waitFor(() => {
      expect(screen.getByText('Groq')).toBeInTheDocument();
    });

    const refreshButton = screen.getByText('Refresh');
    fireEvent.click(refreshButton);

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(2);
    });
  });

  it('should display models for provider', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => mockProviders,
    });

    render(<ProviderConfig />);

    await waitFor(() => {
      expect(screen.getByText('Groq')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByText('Groq'));

    await waitFor(() => {
      expect(screen.getByText('Llama 3.1 70B')).toBeInTheDocument();
      expect(screen.getByText('llama-3.1-70b')).toBeInTheDocument();
      expect(screen.getByText('Active')).toBeInTheDocument();
    });
  });

  it('should handle fetch error gracefully', async () => {
    mockFetch.mockRejectedValueOnce(new Error('Network error'));

    render(<ProviderConfig />);

    await waitFor(() => {
      expect(screen.getByText(/Network error/)).toBeInTheDocument();
    });

    expect(screen.getByText('Groq')).toBeInTheDocument();
    expect(screen.getByText('Cohere')).toBeInTheDocument();
  });

  it('should show loading state initially', () => {
    mockFetch.mockImplementation(() => new Promise(() => {}));

    render(<ProviderConfig />);

    expect(document.querySelector('.animate-spin')).toBeInTheDocument();
  });

  it('should show provider tier and type', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => mockProviders,
    });

    render(<ProviderConfig />);

    await waitFor(() => {
      expect(screen.getByText('Tier 0')).toBeInTheDocument();
      expect(screen.getByText('Tier 1')).toBeInTheDocument();
      expect(screen.getByText('groq')).toBeInTheDocument();
      expect(screen.getByText('cohere')).toBeInTheDocument();
    });
  });

  it('should display latency for online providers', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => mockProviders,
    });

    render(<ProviderConfig />);

    await waitFor(() => {
      expect(screen.getByText('45ms')).toBeInTheDocument();
    });
  });
});
