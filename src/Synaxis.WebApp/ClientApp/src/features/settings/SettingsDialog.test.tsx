import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import useSettingsStore from '@/stores/settings'
import SettingsDialog from './SettingsDialog'

// Mock the settings store module
vi.mock('@/stores/settings')

describe('SettingsDialog', () => {
  const mockOnClose = vi.fn()
  const mockSetGatewayUrl = vi.fn()
  const mockSetCostRate = vi.fn()

  beforeEach(() => {
    vi.clearAllMocks()
    
    // Create a mock store implementation that supports both selector and getState calls
    const mockState = {
      gatewayUrl: 'http://localhost:5000',
      costRate: 0,
      setGatewayUrl: mockSetGatewayUrl,
      setCostRate: mockSetCostRate,
      setStreamingEnabled: vi.fn(),
      setJwtToken: vi.fn(),
      logout: vi.fn(),
    }
    
    // Mock the store to work with selectors
    vi.mocked(useSettingsStore).mockImplementation(((selector?: any) => {
      if (typeof selector === 'function') {
        return selector(mockState)
      }
      return mockState
    }) as any)
    
    // Also mock getState
    ;(useSettingsStore as any).getState = vi.fn(() => mockState)
  })

  describe('visibility', () => {
    it('renders when open is true', () => {
      render(<SettingsDialog open={true} onClose={mockOnClose} />)
      expect(screen.getByText('Settings')).toBeInTheDocument()
    })

    it('does not render when open is false', () => {
      const { container } = render(<SettingsDialog open={false} onClose={mockOnClose} />)
      expect(container.firstChild).toBeNull()
    })
  })

  describe('form fields', () => {
    it('renders gateway URL label', () => {
      render(<SettingsDialog open={true} onClose={mockOnClose} />)
      expect(screen.getByText('Gateway URL')).toBeInTheDocument()
    })

    it('renders cost rate label', () => {
      render(<SettingsDialog open={true} onClose={mockOnClose} />)
      expect(screen.getByText(/Cost Rate/)).toBeInTheDocument()
    })

    it('displays current gateway URL', () => {
      const mockState = {
        gatewayUrl: 'http://custom-api.com',
        costRate: 0,
        setGatewayUrl: mockSetGatewayUrl,
        setCostRate: mockSetCostRate,
        setStreamingEnabled: vi.fn(),
        setJwtToken: vi.fn(),
        logout: vi.fn(),
      }
      
      vi.mocked(useSettingsStore).mockImplementation(((selector?: any) => {
        if (typeof selector === 'function') {
          return selector(mockState)
        }
        return mockState
      }) as any)
      ;(useSettingsStore as any).getState = vi.fn(() => mockState)
      
      render(<SettingsDialog open={true} onClose={mockOnClose} />)
      const urlInput = screen.getByDisplayValue('http://custom-api.com')
      expect(urlInput).toBeInTheDocument()
    })

    it('displays current cost rate', () => {
      const mockState = {
        gatewayUrl: 'http://localhost:5000',
        costRate: 0.5,
        setGatewayUrl: mockSetGatewayUrl,
        setCostRate: mockSetCostRate,
        setStreamingEnabled: vi.fn(),
        setJwtToken: vi.fn(),
        logout: vi.fn(),
      }
      
      vi.mocked(useSettingsStore).mockImplementation(((selector?: any) => {
        if (typeof selector === 'function') {
          return selector(mockState)
        }
        return mockState
      }) as any)
      ;(useSettingsStore as any).getState = vi.fn(() => mockState)
      
      render(<SettingsDialog open={true} onClose={mockOnClose} />)
      const rateInput = screen.getByDisplayValue(0.5)
      expect(rateInput).toBeInTheDocument()
    })
  })

  describe('user interactions', () => {
    it('updates URL input when typing', () => {
      render(<SettingsDialog open={true} onClose={mockOnClose} />)
      const urlInput = screen.getByDisplayValue('http://localhost:5000')
      
      fireEvent.change(urlInput, { target: { value: 'http://new-url.com' } })
      expect(urlInput).toHaveValue('http://new-url.com')
    })

    it('updates rate input when typing', () => {
      render(<SettingsDialog open={true} onClose={mockOnClose} />)
      const rateInput = screen.getByDisplayValue(0)
      
      fireEvent.change(rateInput, { target: { value: '1.5' } })
      expect(rateInput).toHaveValue(1.5)
    })
  })

  describe('saving settings', () => {
    it('calls setGatewayUrl when saving', async () => {
      render(<SettingsDialog open={true} onClose={mockOnClose} />)
      
      const urlInput = screen.getByDisplayValue('http://localhost:5000')
      fireEvent.change(urlInput, { target: { value: 'http://saved-url.com' } })
      
      const saveButton = screen.getByText('Save')
      fireEvent.click(saveButton)
      
      await waitFor(() => {
        expect(mockSetGatewayUrl).toHaveBeenCalledWith('http://saved-url.com')
      })
    })

    it('calls setCostRate when saving', async () => {
      render(<SettingsDialog open={true} onClose={mockOnClose} />)
      
      const rateInput = screen.getByDisplayValue('0')
      fireEvent.change(rateInput, { target: { value: '2' } })
      
      const saveButton = screen.getByText('Save')
      fireEvent.click(saveButton)
      
      await waitFor(() => {
        expect(mockSetCostRate).toHaveBeenCalledWith(2)
      })
    })

    it('calls onClose after saving', async () => {
      render(<SettingsDialog open={true} onClose={mockOnClose} />)
      
      const saveButton = screen.getByText('Save')
      fireEvent.click(saveButton)
      
      await waitFor(() => {
        expect(mockOnClose).toHaveBeenCalled()
      })
    })
  })

  describe('edge cases', () => {
    it('handles empty URL', async () => {
      render(<SettingsDialog open={true} onClose={mockOnClose} />)
      
      const urlInput = screen.getByDisplayValue('http://localhost:5000')
      fireEvent.change(urlInput, { target: { value: '' } })
      
      const saveButton = screen.getByText('Save')
      fireEvent.click(saveButton)
      
      await waitFor(() => {
        expect(mockSetGatewayUrl).toHaveBeenCalledWith('')
      })
    })

    it('handles decimal cost rate', async () => {
      render(<SettingsDialog open={true} onClose={mockOnClose} />)
      
      const rateInput = screen.getByDisplayValue(0)
      fireEvent.change(rateInput, { target: { value: '1.25' } })
      
      const saveButton = screen.getByText('Save')
      fireEvent.click(saveButton)
      
      await waitFor(() => {
        expect(mockSetCostRate).toHaveBeenCalledWith(1.25)
      })
    })
  })
})
