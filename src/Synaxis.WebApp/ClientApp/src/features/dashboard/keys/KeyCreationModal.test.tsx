import React from 'react'
import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import KeyCreationModal from './KeyCreationModal'

describe('KeyCreationModal', () => {
  describe('visibility', () => {
    it('renders when open is true', () => {
      render(<KeyCreationModal open={true} onClose={() => {}} onCreate={() => {}} />)
      expect(screen.getByText('Create API Key')).toBeInTheDocument()
    })

    it('does not render when open is false', () => {
      const { container } = render(
        <KeyCreationModal open={false} onClose={() => {}} onCreate={() => {}} />
      )
      expect(container.firstChild).toBeNull()
    })
  })

  describe('content', () => {
    it('displays the modal title', () => {
      render(<KeyCreationModal open={true} onClose={() => {}} onCreate={() => {}} />)
      expect(screen.getByText('Create API Key')).toBeInTheDocument()
    })

    it('displays the key name label', () => {
      render(<KeyCreationModal open={true} onClose={() => {}} onCreate={() => {}} />)
      expect(screen.getByText('Key Name')).toBeInTheDocument()
    })

    it('renders input field with placeholder', () => {
      render(<KeyCreationModal open={true} onClose={() => {}} onCreate={() => {}} />)
      expect(screen.getByPlaceholderText('e.g., Production API, Development Key')).toBeInTheDocument()
    })

    it('displays security notice', () => {
      render(<KeyCreationModal open={true} onClose={() => {}} onCreate={() => {}} />)
      expect(screen.getByText('Security Notice')).toBeInTheDocument()
      expect(screen.getByText(/Your API key will be displayed only once/)).toBeInTheDocument()
    })

    it('renders cancel button', () => {
      render(<KeyCreationModal open={true} onClose={() => {}} onCreate={() => {}} />)
      const buttons = screen.getAllByText('Cancel')
      expect(buttons.length).toBeGreaterThan(0)
    })

    it('renders create key button', () => {
      render(<KeyCreationModal open={true} onClose={() => {}} onCreate={() => {}} />)
      expect(screen.getByText('Create Key')).toBeInTheDocument()
    })
  })

  describe('input handling', () => {
    it('updates input value when user types', async () => {
      const user = userEvent.setup()
      render(<KeyCreationModal open={true} onClose={() => {}} onCreate={() => {}} />)
      
      const input = screen.getByPlaceholderText('e.g., Production API, Development Key')
      await user.type(input, 'My API Key')
      
      expect(input).toHaveValue('My API Key')
    })

    it('shows character count', () => {
      render(<KeyCreationModal open={true} onClose={() => {}} onCreate={() => {}} />)
      expect(screen.getByText(/\/100 characters/)).toBeInTheDocument()
    })

    it('updates character count as user types', async () => {
      const user = userEvent.setup()
      render(<KeyCreationModal open={true} onClose={() => {}} onCreate={() => {}} />)
      
      const input = screen.getByPlaceholderText('e.g., Production API, Development Key')
      await user.type(input, 'Test Key')
      
      expect(screen.getByText('8/100 characters')).toBeInTheDocument()
    })

    it('clears error message when user types after error', async () => {
      const user = userEvent.setup()
      render(<KeyCreationModal open={true} onClose={() => {}} onCreate={() => {}} />)
      
      const createButton = screen.getByText('Create Key') as HTMLButtonElement
      expect(createButton.disabled).toBe(true)
      
      const input = screen.getByPlaceholderText('e.g., Production API, Development Key')
      await user.type(input, 'A')
      
      await user.click(screen.getByText('Create Key'))
    })
  })

  describe('form validation', () => {
    it('shows error when creating with empty name', async () => {
      const user = userEvent.setup()
      render(<KeyCreationModal open={true} onClose={() => {}} onCreate={() => {}} />)
      
      const createButton = screen.getByText('Create Key') as HTMLButtonElement
      expect(createButton.disabled).toBe(true)
    })

    it('shows error when creating with only whitespace', async () => {
      const user = userEvent.setup()
      render(<KeyCreationModal open={true} onClose={() => {}} onCreate={() => {}} />)
      
      const input = screen.getByPlaceholderText('e.g., Production API, Development Key')
      await user.type(input, '   ')
      
      const createButton = screen.getByText('Create Key') as HTMLButtonElement
      expect(createButton.disabled).toBe(true)
    })

    it('shows error when name exceeds 100 characters', async () => {
      const user = userEvent.setup()
      const onCreate = vi.fn()
      render(<KeyCreationModal open={true} onClose={() => {}} onCreate={onCreate} />)
      
      const input = screen.getByPlaceholderText('e.g., Production API, Development Key') as HTMLInputElement
      const longName = 'a'.repeat(100)
      await user.type(input, longName)
      
      expect(input.value.length).toBeLessThanOrEqual(100)
    })

    it('does not show error for valid input', async () => {
      const user = userEvent.setup()
      render(<KeyCreationModal open={true} onClose={() => {}} onCreate={() => {}} />)
      
      const input = screen.getByPlaceholderText('e.g., Production API, Development Key')
      await user.type(input, 'Valid Key Name')
      
      expect(screen.queryByText(/error/i)).not.toBeInTheDocument()
    })

    it('prevents input longer than 100 characters', async () => {
      const user = userEvent.setup()
      render(<KeyCreationModal open={true} onClose={() => {}} onCreate={() => {}} />)
      
      const input = screen.getByPlaceholderText('e.g., Production API, Development Key') as HTMLInputElement
      const longName = 'a'.repeat(150)
      await user.type(input, longName)
      
      expect(input.value.length).toBeLessThanOrEqual(100)
    })
  })

  describe('button states', () => {
    it('disables create button when input is empty', () => {
      render(<KeyCreationModal open={true} onClose={() => {}} onCreate={() => {}} />)
      const createButton = screen.getByText('Create Key') as HTMLButtonElement
      expect(createButton.disabled).toBe(true)
    })

    it('enables create button when input has value', async () => {
      const user = userEvent.setup()
      render(<KeyCreationModal open={true} onClose={() => {}} onCreate={() => {}} />)
      
      const input = screen.getByPlaceholderText('e.g., Production API, Development Key')
      await user.type(input, 'Test Key')
      
      const createButton = screen.getByText('Create Key') as HTMLButtonElement
      expect(createButton.disabled).toBe(false)
    })

    it('disables create button when input is only whitespace', async () => {
      const user = userEvent.setup()
      render(<KeyCreationModal open={true} onClose={() => {}} onCreate={() => {}} />)
      
      const input = screen.getByPlaceholderText('e.g., Production API, Development Key')
      await user.type(input, '   ')
      
      const createButton = screen.getByText('Create Key') as HTMLButtonElement
      expect(createButton.disabled).toBe(true)
    })
  })

  describe('callbacks', () => {
    it('calls onCreate with key name when create button clicked', async () => {
      const user = userEvent.setup()
      const onCreate = vi.fn()
      render(<KeyCreationModal open={true} onClose={() => {}} onCreate={onCreate} />)
      
      const input = screen.getByPlaceholderText('e.g., Production API, Development Key')
      await user.type(input, 'New API Key')
      
      const createButton = screen.getByText('Create Key')
      await user.click(createButton)
      
      expect(onCreate).toHaveBeenCalledWith('New API Key')
    })

    it('calls onClose when cancel button clicked', async () => {
      const user = userEvent.setup()
      const onClose = vi.fn()
      render(<KeyCreationModal open={true} onClose={onClose} onCreate={() => {}} />)
      
      const cancelButton = screen.getByText('Cancel')
      await user.click(cancelButton)
      
      expect(onClose).toHaveBeenCalled()
    })

    it('resets form after successful creation', async () => {
      const user = userEvent.setup()
      const onCreate = vi.fn()
      const onClose = vi.fn()
      
      const { rerender } = render(
        <KeyCreationModal open={true} onClose={onClose} onCreate={onCreate} />
      )
      
      const input = screen.getByPlaceholderText('e.g., Production API, Development Key')
      await user.type(input, 'Test Key')
      
      const createButton = screen.getByText('Create Key')
      await user.click(createButton)
      
      rerender(<KeyCreationModal open={false} onClose={onClose} onCreate={onCreate} />)
      rerender(<KeyCreationModal open={true} onClose={onClose} onCreate={onCreate} />)
      
      const newInput = screen.getByPlaceholderText('e.g., Production API, Development Key') as HTMLInputElement
      expect(newInput.value).toBe('')
    })

    it('clears error after successful creation', async () => {
      const user = userEvent.setup()
      const onCreate = vi.fn()
      
      const { rerender } = render(
        <KeyCreationModal open={true} onClose={() => {}} onCreate={onCreate} />
      )
      
      const input = screen.getByPlaceholderText('e.g., Production API, Development Key')
      await user.type(input, 'Valid Key')
      await user.click(screen.getByText('Create Key'))
      
      expect(onCreate).toHaveBeenCalledWith('Valid Key')
      
      rerender(<KeyCreationModal open={false} onClose={() => {}} onCreate={onCreate} />)
      rerender(<KeyCreationModal open={true} onClose={() => {}} onCreate={onCreate} />)
      
      const newInput = screen.getByPlaceholderText('e.g., Production API, Development Key') as HTMLInputElement
      expect(newInput.value).toBe('')
    })
  })

  describe('keyboard interactions', () => {
    it('submits form when Enter key is pressed in input', async () => {
      const user = userEvent.setup()
      const onCreate = vi.fn()
      render(<KeyCreationModal open={true} onClose={() => {}} onCreate={onCreate} />)
      
      const input = screen.getByPlaceholderText('e.g., Production API, Development Key')
      await user.type(input, 'Enter Key Test')
      await user.keyboard('{Enter}')
      
      expect(onCreate).toHaveBeenCalledWith('Enter Key Test')
    })

    it('does not submit when Enter is pressed with empty input', async () => {
      const user = userEvent.setup()
      const onCreate = vi.fn()
      render(<KeyCreationModal open={true} onClose={() => {}} onCreate={onCreate} />)
      
      const input = screen.getByPlaceholderText('e.g., Production API, Development Key')
      input.focus()
      await user.keyboard('{Enter}')
      
      expect(onCreate).not.toHaveBeenCalled()
    })
  })

  describe('focus management', () => {
    it('input field is focusable', async () => {
      render(<KeyCreationModal open={true} onClose={() => {}} onCreate={() => {}} />)
      const input = screen.getByPlaceholderText('e.g., Production API, Development Key')
      input.focus()
      expect(input).toHaveFocus()
    })
  })

  describe('accessibility', () => {
    it('has associated label for input', () => {
      render(<KeyCreationModal open={true} onClose={() => {}} onCreate={() => {}} />)
      const input = screen.getByPlaceholderText('e.g., Production API, Development Key')
      expect(input).toHaveAttribute('id', 'key-name-input')
    })

    it('label references the input field', () => {
      render(<KeyCreationModal open={true} onClose={() => {}} onCreate={() => {}} />)
      const label = screen.getByText('Key Name')
      expect(label).toHaveAttribute('for', 'key-name-input')
    })

    it('error message is visible when validation fails', async () => {
      const user = userEvent.setup()
      const onCreate = vi.fn()
      render(<KeyCreationModal open={true} onClose={() => {}} onCreate={onCreate} />)
      
      const input = screen.getByPlaceholderText('e.g., Production API, Development Key')
      await user.type(input, 'Valid')
      
      const createButton = screen.getByText('Create Key')
      await user.click(createButton)
      
      expect(onCreate).toHaveBeenCalled()
    })
  })

  describe('edge cases', () => {
    it('handles rapid clicking of create button', async () => {
      const user = userEvent.setup()
      const onCreate = vi.fn()
      render(<KeyCreationModal open={true} onClose={() => {}} onCreate={onCreate} />)
      
      const input = screen.getByPlaceholderText('e.g., Production API, Development Key')
      await user.type(input, 'Multiple Clicks')
      
      const createButton = screen.getByText('Create Key')
      await user.click(createButton)
      await user.click(createButton)
      
      expect(onCreate).toHaveBeenCalledTimes(1)
    })

    it('handles special characters in key name', async () => {
      const user = userEvent.setup()
      const onCreate = vi.fn()
      render(<KeyCreationModal open={true} onClose={() => {}} onCreate={onCreate} />)
      
      const input = screen.getByPlaceholderText('e.g., Production API, Development Key')
      await user.type(input, 'Key-Name_123!@#')
      
      const createButton = screen.getByText('Create Key')
      await user.click(createButton)
      
      expect(onCreate).toHaveBeenCalledWith('Key-Name_123!@#')
    })
  })
})
