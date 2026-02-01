import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import ModelSelection from './ModelSelection';

const mocks = vi.hoisted(() => ({
  fetchModels: vi.fn().mockResolvedValue({
    object: 'list',
    data: [
      {
        id: 'model-1',
        object: 'model',
        created: 1234567890,
        owned_by: 'provider-a',
        provider: 'provider-a',
        model_path: 'model-1',
        capabilities: {
          streaming: true,
          tools: true,
          vision: false,
          structured_output: false,
          log_probs: false,
        },
      },
      {
        id: 'model-2',
        object: 'model',
        created: 1234567891,
        owned_by: 'provider-b',
        provider: 'provider-b',
        model_path: 'model-2',
        capabilities: {
          streaming: false,
          tools: false,
          vision: true,
          structured_output: false,
          log_probs: false,
        },
      },
    ],
    providers: [],
  }),
}));

vi.mock('@/api/client', () => ({
  defaultClient: {
    fetchModels: mocks.fetchModels,
  },
}));

const mockSetSelectedModel = vi.fn();
vi.mock('@/stores/settings', () => ({
  default: (selector: (s: { selectedModel: string; setSelectedModel: (model: string) => void }) => unknown) => {
    return selector({
      selectedModel: 'model-1',
      setSelectedModel: mockSetSelectedModel,
    });
  },
}));

describe('ModelSelection', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.fetchModels.mockResolvedValue({
      object: 'list',
      data: [
        {
          id: 'model-1',
          object: 'model',
          created: 1234567890,
          owned_by: 'provider-a',
          provider: 'provider-a',
          model_path: 'model-1',
          capabilities: {
            streaming: true,
            tools: true,
            vision: false,
            structured_output: false,
            log_probs: false,
          },
        },
        {
          id: 'model-2',
          object: 'model',
          created: 1234567891,
          owned_by: 'provider-b',
          provider: 'provider-b',
          model_path: 'model-2',
          capabilities: {
            streaming: false,
            tools: false,
            vision: true,
            structured_output: false,
            log_probs: false,
          },
        },
      ],
      providers: [],
    });
  });

  it('should render loading state initially', () => {
    render(<ModelSelection disabled={false} />);

    expect(screen.getByText('Loading models...')).toBeInTheDocument();
  });

  it('should fetch and display models', async () => {
    render(<ModelSelection disabled={false} />);

    await waitFor(() => {
      expect(mocks.fetchModels).toHaveBeenCalled();
    });

    await waitFor(() => {
      expect(screen.getByDisplayValue('model-1 (provider-a)')).toBeInTheDocument();
    });

    expect(screen.getByText('model-2 (provider-b)')).toBeInTheDocument();
  });

  it('should display error when models fetch fails', async () => {
    mocks.fetchModels.mockRejectedValueOnce(new Error('Network error'));

    render(<ModelSelection disabled={false} />);

    await waitFor(() => {
      expect(screen.getByText(/Error: Network error/)).toBeInTheDocument();
    });
  });

  it('should handle empty models list', async () => {
    mocks.fetchModels.mockResolvedValueOnce({
      object: 'list',
      data: [],
      providers: [],
    });

    render(<ModelSelection disabled={false} />);

    await waitFor(() => {
      expect(screen.getByText('No models available')).toBeInTheDocument();
    });
  });

  it('should call setSelectedModel when model selection changes', async () => {
    const user = userEvent.setup();

    render(<ModelSelection disabled={false} />);

    await waitFor(() => {
      expect(screen.getByDisplayValue('model-1 (provider-a)')).toBeInTheDocument();
    });

    const select = screen.getByDisplayValue('model-1 (provider-a)') as HTMLSelectElement;
    await user.selectOptions(select, 'model-2');

    expect(mockSetSelectedModel).toHaveBeenCalledWith('model-2');
  });

  it('should disable select when disabled prop is true', async () => {
    render(<ModelSelection disabled={true} />);

    await waitFor(() => {
      const select = screen.getByRole('combobox') as HTMLSelectElement;
      expect(select.disabled).toBe(true);
    });
  });

  it('should have proper accessibility attributes', async () => {
    render(<ModelSelection disabled={false} />);

    await waitFor(() => {
      const select = screen.getByLabelText('Select model');
      expect(select).toBeInTheDocument();
    });
  });

  it('should render model label', async () => {
    render(<ModelSelection disabled={false} />);

    await waitFor(() => {
      expect(screen.getByText('Model:')).toBeInTheDocument();
    });
  });
});
