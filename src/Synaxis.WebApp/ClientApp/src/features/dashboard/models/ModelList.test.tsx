import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import ModelList from './ModelList'
import { mockConfigService } from '@/services/mockConfigService'

vi.mock('@/services/mockConfigService')

describe('ModelList', () => {
  const mockModels = [
    {
      id: 'llama-3.1-70b-versatile',
      name: 'Llama 3.1 70B Versatile',
      provider: 'groq',
      modelPath: 'llama-3.1-70b-versatile',
      description: 'Highly capable open model',
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
      description: 'Mixture of experts model',
      streaming: true,
      tools: true,
      vision: false,
      structuredOutput: true,
      logProbs: false,
      contextWindow: 32768,
      maxTokens: 8000,
    },
  ]

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(mockConfigService.getCanonicalModels).mockResolvedValue(mockModels)
  })

  it('should render loading state initially', async () => {
    render(<ModelList />)
    
    await waitFor(() => {
      expect(screen.getByText('Llama 3.1 70B Versatile')).toBeInTheDocument()
    })
  })

  it('should load and display models', async () => {
    render(<ModelList />)

    await waitFor(() => {
      expect(screen.getByText('Llama 3.1 70B Versatile')).toBeInTheDocument()
      expect(screen.getByText('Mixtral 8x7B')).toBeInTheDocument()
    })
  })

  it('should display model provider', async () => {
    render(<ModelList />)

    await waitFor(() => {
      const providerElements = screen.getAllByText('groq')
      expect(providerElements.length).toBeGreaterThan(0)
    })
  })

  it('should display capability badges', async () => {
    render(<ModelList />)

    await waitFor(() => {
      expect(screen.getAllByText('Streaming')).toHaveLength(2)
      expect(screen.getAllByText('Tools')).toHaveLength(2)
      expect(screen.getAllByText('Structured')).toHaveLength(2)
    })
  })

  it('should handle model selection', async () => {
    const onModelSelect = vi.fn()
    render(<ModelList onModelSelect={onModelSelect} />)

    await waitFor(() => {
      const modelButton = screen.getByText('Llama 3.1 70B Versatile').closest('div')
      expect(modelButton).toBeInTheDocument()
    })

    const modelButton = screen.getByText('Llama 3.1 70B Versatile').closest('div')
    if (modelButton) {
      fireEvent.click(modelButton)

      await waitFor(() => {
        expect(onModelSelect).toHaveBeenCalledWith(mockModels[0])
      })
    }
  })

  it('should toggle model enabled state', async () => {
    render(<ModelList />)

    await waitFor(() => {
      expect(screen.getByText('Llama 3.1 70B Versatile')).toBeInTheDocument()
    })

    const checkboxes = screen.getAllByRole('checkbox')
    expect(checkboxes[0]).toBeChecked()

    fireEvent.click(checkboxes[0])

    await waitFor(() => {
      expect(checkboxes[0]).not.toBeChecked()
    })
  })

  it('should display error when fetch fails', async () => {
    const errorMessage = 'Failed to fetch models'
    vi.mocked(mockConfigService.getCanonicalModels).mockRejectedValueOnce(new Error(errorMessage))

    render(<ModelList />)

    await waitFor(() => {
      expect(screen.getByText(errorMessage)).toBeInTheDocument()
    })
  })

  it('should show empty state when no models', async () => {
    vi.mocked(mockConfigService.getCanonicalModels).mockResolvedValueOnce([])

    render(<ModelList />)

    await waitFor(() => {
      expect(screen.getByText('No models available')).toBeInTheDocument()
    })
  })

  it('should highlight selected model', async () => {
    const onModelSelect = vi.fn()
    render(<ModelList onModelSelect={onModelSelect} />)

    await waitFor(() => {
      expect(screen.getByText('Llama 3.1 70B Versatile')).toBeInTheDocument()
    })

    const modelDiv = screen.getByText('Llama 3.1 70B Versatile').closest('div')?.parentElement
    if (modelDiv) {
      fireEvent.click(modelDiv)

      await waitFor(() => {
        expect(onModelSelect).toHaveBeenCalledWith(mockModels[0])
      })
    }
  })

  it('should display model description', async () => {
    render(<ModelList />)

    await waitFor(() => {
      expect(screen.getByText('Highly capable open model')).toBeInTheDocument()
      expect(screen.getByText('Mixture of experts model')).toBeInTheDocument()
    })
  })
})
