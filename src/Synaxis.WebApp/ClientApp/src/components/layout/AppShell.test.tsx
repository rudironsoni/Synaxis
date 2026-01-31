import React from 'react'
import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import AppShell from './AppShell'

// Mock the dependencies
vi.mock('@/features/sessions/SessionList', () => ({
  default: () => <div data-testid="session-list">Session List</div>
}))

vi.mock('@/features/settings/SettingsDialog', () => ({
  default: ({ open, onClose }: { open: boolean; onClose: () => void }) => (
    open ? <div data-testid="settings-dialog">Settings Dialog <button type="button" onClick={onClose}>Close</button></div> : null
  )
}))

vi.mock('@/stores/settings', () => ({
  default: (selector: (s: any) => any) => selector({ costRate: 0.5 })
}))

describe('AppShell', () => {
  describe('layout structure', () => {
    it('renders main layout container', () => {
      render(<AppShell>Content</AppShell>)
      expect(screen.getByText('Content')).toBeInTheDocument()
    })

    it('renders header with title', () => {
      render(<AppShell>Content</AppShell>)
      expect(screen.getByText('Synaxis')).toBeInTheDocument()
    })

    it('renders session list in sidebar', () => {
      render(<AppShell>Content</AppShell>)
      expect(screen.getByTestId('session-list')).toBeInTheDocument()
    })

    it('renders children in main content area', () => {
      render(
        <AppShell>
          <div data-testid="main-content">Main Content</div>
        </AppShell>
      )
      expect(screen.getByTestId('main-content')).toBeInTheDocument()
    })

    it('renders settings button', () => {
      render(<AppShell>Content</AppShell>)
      const settingsButton = screen.getByTitle('Settings')
      expect(settingsButton).toBeInTheDocument()
    })
  })

  describe('header content', () => {
    it('displays cost rate badge', () => {
      render(<AppShell>Content</AppShell>)
      expect(screen.getByText(/Miser: \$0\.50/)).toBeInTheDocument()
    })

    it('formats cost rate to 2 decimal places', () => {
      render(<AppShell>Content</AppShell>)
      const badge = screen.getByText(/Miser:/)
      expect(badge.textContent).toMatch(/\$\d+\.\d{2}/)
    })
  })

  describe('settings dialog', () => {
    it('does not show settings dialog by default', () => {
      render(<AppShell>Content</AppShell>)
      expect(screen.queryByTestId('settings-dialog')).not.toBeInTheDocument()
    })

    it('opens settings dialog when settings button is clicked', async () => {
      const user = userEvent.setup()
      render(<AppShell>Content</AppShell>)
      
      const settingsButton = screen.getByTitle('Settings')
      await user.click(settingsButton)
      
      expect(screen.getByTestId('settings-dialog')).toBeInTheDocument()
    })

    it('closes settings dialog when onClose is called', async () => {
      const user = userEvent.setup()
      render(<AppShell>Content</AppShell>)
      
      // Open dialog
      const settingsButton = screen.getByTitle('Settings')
      await user.click(settingsButton)
      expect(screen.getByTestId('settings-dialog')).toBeInTheDocument()
      
      // Close dialog
      const closeButton = screen.getByText('Close')
      await user.click(closeButton)
      
      expect(screen.queryByTestId('settings-dialog')).not.toBeInTheDocument()
    })
  })

  describe('sidebar', () => {
    it('renders sidebar when sidebarOpen is true', () => {
      render(<AppShell>Content</AppShell>)
      expect(screen.getByTestId('session-list')).toBeInTheDocument()
    })

    it('sidebar contains SessionList component', () => {
      render(<AppShell>Content</AppShell>)
      const sessionList = screen.getByTestId('session-list')
      expect(sessionList).toHaveTextContent('Session List')
    })
  })

  describe('accessibility', () => {
    it('has header element', () => {
      const { container } = render(<AppShell>Content</AppShell>)
      const header = container.querySelector('header')
      expect(header).toBeInTheDocument()
    })

    it('has main element', () => {
      const { container } = render(<AppShell>Content</AppShell>)
      const main = container.querySelector('main')
      expect(main).toBeInTheDocument()
    })

    it('has aside element for sidebar', () => {
      const { container } = render(<AppShell>Content</AppShell>)
      const aside = container.querySelector('aside')
      expect(aside).toBeInTheDocument()
    })

    it('settings button has title attribute for accessibility', () => {
      render(<AppShell>Content</AppShell>)
      const settingsButton = screen.getByTitle('Settings')
      expect(settingsButton).toHaveAttribute('title', 'Settings')
    })
  })

  describe('children rendering', () => {
    it('renders string children', () => {
      render(<AppShell>Simple text content</AppShell>)
      expect(screen.getByText('Simple text content')).toBeInTheDocument()
    })

    it('renders React element children', () => {
      render(
        <AppShell>
          <div>
            <h1>Title</h1>
            <p>Description</p>
          </div>
        </AppShell>
      )
      expect(screen.getByText('Title')).toBeInTheDocument()
      expect(screen.getByText('Description')).toBeInTheDocument()
    })

    it('renders multiple children', () => {
      render(
        <AppShell>
          <div>Child 1</div>
          <div>Child 2</div>
          <div>Child 3</div>
        </AppShell>
      )
      expect(screen.getByText('Child 1')).toBeInTheDocument()
      expect(screen.getByText('Child 2')).toBeInTheDocument()
      expect(screen.getByText('Child 3')).toBeInTheDocument()
    })
  })

  describe('layout classes', () => {
    it('has min-height screen class', () => {
      const { container } = render(<AppShell>Content</AppShell>)
      const wrapper = container.firstChild
      expect(wrapper).toHaveClass('min-h-screen')
    })

    it('has flex layout class', () => {
      const { container } = render(<AppShell>Content</AppShell>)
      const wrapper = container.firstChild
      expect(wrapper).toHaveClass('flex')
    })

    it('has full width class', () => {
      const { container } = render(<AppShell>Content</AppShell>)
      const wrapper = container.firstChild
      expect(wrapper).toHaveClass('w-full')
    })
  })
})
