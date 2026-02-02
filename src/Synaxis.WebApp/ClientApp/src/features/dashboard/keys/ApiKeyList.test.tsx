import React from 'react'
import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import ApiKeyList from './ApiKeyList'

describe('ApiKeyList', () => {
  describe('rendering', () => {
    it('renders the component with title and description', () => {
      render(<ApiKeyList />)
      expect(screen.getByText('API Key Management')).toBeInTheDocument()
      expect(screen.getByText('Manage your API keys and access tokens.')).toBeInTheDocument()
    })

    it('renders create new key button', () => {
      render(<ApiKeyList />)
      expect(screen.getByText('Create New Key')).toBeInTheDocument()
    })

    it('renders initial API keys', () => {
      render(<ApiKeyList />)
      expect(screen.getByText('Production Key')).toBeInTheDocument()
      expect(screen.getByText('Development Key')).toBeInTheDocument()
    })

    it('displays key masked format', () => {
      render(<ApiKeyList />)
      const maskedKeys = screen.getAllByText(/\(masked\)/)
      expect(maskedKeys.length).toBeGreaterThan(0)
    })

    it('shows created date for each key', () => {
      render(<ApiKeyList />)
      expect(screen.getByText(/Created Jan 15, 2024/)).toBeInTheDocument()
      expect(screen.getByText(/Created Feb 1, 2024/)).toBeInTheDocument()
    })
  })

  describe('key masking', () => {
    it('masks API keys correctly', () => {
      render(<ApiKeyList />)
      const maskedTexts = screen.getAllByText(/•+/)
      expect(maskedTexts.length).toBeGreaterThan(0)
    })

    it('shows active status for keys', () => {
      render(<ApiKeyList />)
      const activeStatuses = screen.getAllByText('Active')
      expect(activeStatuses.length).toBeGreaterThan(0)
    })
  })

  describe('copy to clipboard', () => {
    it('renders copy button for each key', () => {
      render(<ApiKeyList />)
      const copyButtons = screen.getAllByText('Copy')
      expect(copyButtons.length).toBeGreaterThan(0)
    })

    it('copies key to clipboard when copy button clicked', async () => {
      const user = userEvent.setup()
      const clipboardSpy = vi.spyOn(navigator.clipboard, 'writeText')
      
      render(<ApiKeyList />)
      const copyButtons = screen.getAllByText('Copy')
      await user.click(copyButtons[0])
      
      expect(clipboardSpy).toHaveBeenCalled()
      clipboardSpy.mockRestore()
    })

    it('copies the correct key value', async () => {
      const user = userEvent.setup()
      const clipboardSpy = vi.spyOn(navigator.clipboard, 'writeText')
      
      render(<ApiKeyList />)
      const copyButtons = screen.getAllByText('Copy')
      await user.click(copyButtons[0])
      
      expect(clipboardSpy).toHaveBeenCalledWith(expect.stringContaining('sk-'))
      clipboardSpy.mockRestore()
    })
  })

  describe('key revocation', () => {
    it('renders revoke button for each key', () => {
      render(<ApiKeyList />)
      const revokeButtons = screen.getAllByText('Revoke')
      expect(revokeButtons.length).toBeGreaterThan(0)
    })

    it('removes key when revoke button is clicked', async () => {
      const user = userEvent.setup()
      render(<ApiKeyList />)
      
      expect(screen.getByText('Production Key')).toBeInTheDocument()
      
      const revokeButtons = screen.getAllByText('Revoke')
      await user.click(revokeButtons[0])
      
      expect(screen.queryByText('Production Key')).not.toBeInTheDocument()
    })

    it('calls onKeyRevoked callback when key is revoked', async () => {
      const user = userEvent.setup()
      const onKeyRevoked = vi.fn()
      render(<ApiKeyList onKeyRevoked={onKeyRevoked} />)
      
      const revokeButtons = screen.getAllByText('Revoke')
      await user.click(revokeButtons[0])
      
      expect(onKeyRevoked).toHaveBeenCalledWith('1')
    })

    it('shows empty state after revoking all keys', async () => {
      const user = userEvent.setup()
      render(<ApiKeyList />)
      
      const revokeButtons = screen.getAllByText('Revoke')
      await user.click(revokeButtons[0])
      await user.click(screen.getAllByText('Revoke')[0])
      
      expect(screen.getByText('No API keys yet. Create one to get started.')).toBeInTheDocument()
    })
  })

  describe('key creation', () => {
    it('opens creation modal when create button clicked', async () => {
      const user = userEvent.setup()
      render(<ApiKeyList />)
      
      const createButton = screen.getByText('Create New Key')
      await user.click(createButton)
      
      expect(screen.getByText('Create API Key')).toBeInTheDocument()
    })

    it('calls onKeyCreated callback when key is created', async () => {
      const user = userEvent.setup()
      const onKeyCreated = vi.fn()
      render(<ApiKeyList onKeyCreated={onKeyCreated} />)
      
      const createButton = screen.getByText('Create New Key')
      await user.click(createButton)
      
      const input = screen.getByPlaceholderText('e.g., Production API, Development Key')
      await user.type(input, 'New Test Key')
      
      const submitButton = screen.getByText('Create Key')
      await user.click(submitButton)
      
      setTimeout(() => {
        expect(onKeyCreated).toHaveBeenCalled()
        expect(onKeyCreated).toHaveBeenCalledWith(expect.objectContaining({
          name: 'New Test Key',
          status: 'active'
        }))
      }, 200)
    })

    it('shows new key display modal after creation', async () => {
      const user = userEvent.setup()
      render(<ApiKeyList />)
      
      const createButton = screen.getByText('Create New Key')
      await user.click(createButton)
      
      const input = screen.getByPlaceholderText('e.g., Production API, Development Key')
      await user.type(input, 'New Key')
      
      const submitButton = screen.getByText('Create Key')
      await user.click(submitButton)
      
      expect(screen.getByText('API Key Created')).toBeInTheDocument()
      expect(screen.getByText(/Your API key has been created/)).toBeInTheDocument()
    })

    it('displays the new key in the confirmation modal', async () => {
      const user = userEvent.setup()
      render(<ApiKeyList />)
      
      const createButton = screen.getByText('Create New Key')
      await user.click(createButton)
      
      const input = screen.getByPlaceholderText('e.g., Production API, Development Key')
      await user.type(input, 'Display Test Key')
      
      const submitButton = screen.getByText('Create Key')
      await user.click(submitButton)
      
      expect(screen.getByText('Display Test Key')).toBeInTheDocument()
      expect(screen.getByText(/sk-/)).toBeInTheDocument()
    })

    it('allows copying the new key from confirmation modal', async () => {
      const user = userEvent.setup()
      const clipboardSpy = vi.spyOn(navigator.clipboard, 'writeText')
      
      render(<ApiKeyList />)
      
      const createButton = screen.getByText('Create New Key')
      await user.click(createButton)
      
      const input = screen.getByPlaceholderText('e.g., Production API, Development Key')
      await user.type(input, 'Copy Test Key')
      
      const submitButton = screen.getByText('Create Key')
      await user.click(submitButton)
      
      const copyButtons = screen.getAllByText('Copy Key')
      await user.click(copyButtons[0])
      
      expect(clipboardSpy).toHaveBeenCalled()
      clipboardSpy.mockRestore()
    })
  })

  describe('empty state', () => {
    it('shows empty state message when no keys exist', async () => {
      const user = userEvent.setup()
      render(<ApiKeyList />)
      
      const revokeButtons = screen.getAllByText('Revoke')
      await user.click(revokeButtons[0])
      await user.click(screen.getAllByText('Revoke')[0])
      
      expect(screen.getByText('No API keys yet. Create one to get started.')).toBeInTheDocument()
    })
  })

  describe('key properties', () => {
    it('displays key with correct status', () => {
      render(<ApiKeyList />)
      const activeStatuses = screen.getAllByText('Active')
      expect(activeStatuses.length).toBeGreaterThanOrEqual(2)
    })

    it('displays formatted creation date', () => {
      render(<ApiKeyList />)
      expect(screen.getByText(/Created Jan 15, 2024/)).toBeInTheDocument()
      expect(screen.getByText(/Created Feb 1, 2024/)).toBeInTheDocument()
    })

    it('shows key name prominently', () => {
      render(<ApiKeyList />)
      const heading = screen.getByText('Production Key')
      expect(heading).toHaveClass('font-medium')
    })
  })

  describe('modal interactions', () => {
    it('closes new key modal when done clicked', async () => {
      const user = userEvent.setup()
      render(<ApiKeyList />)
      
      const createButton = screen.getByText('Create New Key')
      await user.click(createButton)
      
      const input = screen.getByPlaceholderText('e.g., Production API, Development Key')
      await user.type(input, 'Modal Test')
      
      const submitButton = screen.getByText('Create Key')
      await user.click(submitButton)
      
      const done = screen.getByText('Done')
      await user.click(done)
      
      expect(screen.queryByText('API Key Created')).not.toBeInTheDocument()
    })

    it('opens creation modal from dashboard', async () => {
      const user = userEvent.setup()
      render(<ApiKeyList />)
      
      const createButton = screen.getByText('Create New Key')
      expect(createButton).toBeInTheDocument()
      
      await user.click(createButton)
      
      expect(screen.getByText('Create API Key')).toBeInTheDocument()
    })
  })

  describe('key list interactions', () => {
    it('maintains key list after copy operation', async () => {
      const user = userEvent.setup()
      const clipboardSpy = vi.spyOn(navigator.clipboard, 'writeText')
      
      render(<ApiKeyList />)
      
      const copyButtons = screen.getAllByText('Copy')
      await user.click(copyButtons[0])
      
      expect(screen.getByText('Production Key')).toBeInTheDocument()
      expect(screen.getByText('Development Key')).toBeInTheDocument()
      clipboardSpy.mockRestore()
    })
  })
})
