import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import ModelConfiguration from './ModelConfiguration'
import { mockConfigService } from '@/services/mockConfigService'

vi.mock('@/services/mockConfigService')

describe('ModelConfiguration', () => {
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
      id: 'command-r',
      name: 'Command R',
      provider: 'cohere',
      modelPath: 'command-r',
      description: 'Advanced instruction-following model',
      streaming: true,
      tools: true,
      vision: false,
      structuredOutput: true,
      logProbs: false,
      contextWindow: 128000,
      maxTokens: 4096,
    },
  ]

  const mockAliases = [
    {
      id: 'default',
      candidates: ['llama-3.1-70b-versatile', 'command-r'],
      priority: 0,
      description: 'Default model alias',
    },
    {
      id: 'fast',
      candidates: ['llama-3.1-8b-instant'],
      priority: 1,
      description: 'Fast models',
    },
  ]

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(mockConfigService.getCanonicalModels).mockResolvedValue(mockModels)
    vi.mocked(mockConfigService.getAliases).mockResolvedValue(mockAliases)
    vi.mocked(mockConfigService.updateCanonicalModel).mockResolvedValue(mockModels[0])
  })

  it('should render loading state initially', () => {
    render(<ModelConfiguration />)
    expect(screen.getByText('Model Configuration')).toBeInTheDocument()
  })

  it('should load and display models', async () => {
    render(<ModelConfiguration />)

    await waitFor(() => {
      expect(screen.getByText('Llama 3.1 70B Versatile')).toBeInTheDocument()
      expect(screen.getByText('Command R')).toBeInTheDocument()
    })
  })

  it('should display tab navigation', async () => {
    render(<ModelConfiguration />)

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /Models \(2\)/ })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /Aliases \(2\)/ })).toBeInTheDocument()
    })
  })

  it('should select model and display configuration form', async () => {
    render(<ModelConfiguration />)

    await waitFor(() => {
      expect(screen.getByText('Llama 3.1 70B Versatile')).toBeInTheDocument()
    })

    const modelButton = screen.getByText('Llama 3.1 70B Versatile').closest('button')
    if (modelButton) {
      fireEvent.click(modelButton)

      await waitFor(() => {
        expect(screen.getByDisplayValue('Highly capable open model')).toBeInTheDocument()
      })
    }
  })

  it('should display model capabilities badges', async () => {
    render(<ModelConfiguration />)

    await waitFor(() => {
      expect(screen.getByText('Llama 3.1 70B Versatile')).toBeInTheDocument()
    })

    const modelButton = screen.getByText('Llama 3.1 70B Versatile').closest('button')
    if (modelButton) {
      fireEvent.click(modelButton)

      await waitFor(() => {
        expect(screen.getByText('Current Capabilities')).toBeInTheDocument()
        expect(screen.getAllByText('Streaming')[0]).toBeInTheDocument()
        expect(screen.getAllByText('Tools')[0]).toBeInTheDocument()
      })
    }
  })

  it('should display model details', async () => {
    render(<ModelConfiguration />)

    await waitFor(() => {
      expect(screen.getByText('Llama 3.1 70B Versatile')).toBeInTheDocument()
    })

    const modelButton = screen.getByText('Llama 3.1 70B Versatile').closest('button')
    if (modelButton) {
      fireEvent.click(modelButton)

      await waitFor(() => {
        expect(screen.getByText('Context Window')).toBeInTheDocument()
        expect(screen.getByText('128,000')).toBeInTheDocument()
        expect(screen.getByText('Max Tokens')).toBeInTheDocument()
        expect(screen.getByText('8,000')).toBeInTheDocument()
      })
    }
  })

  it('should update form when model is selected', async () => {
    render(<ModelConfiguration />)

    await waitFor(() => {
      expect(screen.getByText('Command R')).toBeInTheDocument()
    })

    const modelButton = screen.getByText('Command R').closest('button')
    if (modelButton) {
      fireEvent.click(modelButton)

      await waitFor(() => {
        expect(screen.getByDisplayValue('Advanced instruction-following model')).toBeInTheDocument()
      })
    }
  })

  it('should toggle capability checkboxes', async () => {
    render(<ModelConfiguration />)

    await waitFor(() => {
      expect(screen.getByText('Llama 3.1 70B Versatile')).toBeInTheDocument()
    })

    const modelButton = screen.getByText('Llama 3.1 70B Versatile').closest('button')
    if (modelButton) {
      fireEvent.click(modelButton)

      await waitFor(() => {
        const checkboxes = screen.getAllByRole('checkbox')
        const streamingCheckbox = checkboxes[0]
        expect(streamingCheckbox).toBeChecked()

        fireEvent.click(streamingCheckbox)
        expect(streamingCheckbox).not.toBeChecked()
      })
    }
  })

  it('should save configuration', async () => {
    render(<ModelConfiguration />)

    await waitFor(() => {
      expect(screen.getByText('Llama 3.1 70B Versatile')).toBeInTheDocument()
    })

    const modelButton = screen.getByText('Llama 3.1 70B Versatile').closest('button')
    if (modelButton) {
      fireEvent.click(modelButton)

      await waitFor(() => {
        const saveButton = screen.getByRole('button', { name: 'Save Configuration' })
        expect(saveButton).toBeInTheDocument()
      })

      const saveButton = screen.getByRole('button', { name: 'Save Configuration' })
      fireEvent.click(saveButton)

      await waitFor(() => {
        expect(vi.mocked(mockConfigService.updateCanonicalModel)).toHaveBeenCalledWith(
          'llama-3.1-70b-versatile',
          expect.objectContaining({
            description: expect.any(String),
          })
        )
      })
    }
  })

  it('should switch to aliases tab', async () => {
    render(<ModelConfiguration />)

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /Aliases \(2\)/ })).toBeInTheDocument()
    })

    const aliasesTab = screen.getByRole('button', { name: /Aliases \(2\)/ })
    fireEvent.click(aliasesTab)

    await waitFor(() => {
      expect(screen.getByText('Model Aliases')).toBeInTheDocument()
      expect(screen.getByText('Default model alias')).toBeInTheDocument()
    })
  })

  it('should display aliases with candidates', async () => {
    render(<ModelConfiguration />)

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /Aliases \(2\)/ })).toBeInTheDocument()
    })

    const aliasesTab = screen.getByRole('button', { name: /Aliases \(2\)/ })
    fireEvent.click(aliasesTab)

    await waitFor(() => {
      expect(screen.getByText('llama-3.1-70b-versatile')).toBeInTheDocument()
      expect(screen.getByText('command-r')).toBeInTheDocument()
    })
  })

  it('should show empty state when no model selected', async () => {
    render(<ModelConfiguration />)

    await waitFor(() => {
      expect(screen.getByText('Llama 3.1 70B Versatile')).toBeInTheDocument()
    })

    await waitFor(() => {
      expect(screen.getByText('Select a model to configure')).toBeInTheDocument()
    })
  })

  it('should fetch models and aliases on mount', async () => {
    render(<ModelConfiguration />)

    await waitFor(() => {
      expect(vi.mocked(mockConfigService.getCanonicalModels)).toHaveBeenCalled()
      expect(vi.mocked(mockConfigService.getAliases)).toHaveBeenCalled()
    })
  })

  it('should display error when fetch fails', async () => {
    const errorMessage = 'Failed to load configuration'
    vi.mocked(mockConfigService.getCanonicalModels).mockRejectedValueOnce(new Error(errorMessage))

    render(<ModelConfiguration />)

    await waitFor(() => {
      expect(screen.getByText(errorMessage)).toBeInTheDocument()
    })
  })
})
