import React from 'react'
import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import Badge from './Badge'

describe('Badge', () => {
  describe('rendering', () => {
    it('renders children', () => {
      render(<Badge>New</Badge>)
      expect(screen.getByText('New')).toBeInTheDocument()
    })

    it('renders text children', () => {
      render(<Badge>Badge Text</Badge>)
      expect(screen.getByText('Badge Text')).toBeInTheDocument()
    })

    it('renders number children', () => {
      render(<Badge>42</Badge>)
      expect(screen.getByText('42')).toBeInTheDocument()
    })

    it('renders React nodes as children', () => {
      render(
        <Badge>
          <span data-testid="icon">★</span>
          <span>Featured</span>
        </Badge>
      )
      expect(screen.getByTestId('icon')).toBeInTheDocument()
      expect(screen.getByText('Featured')).toBeInTheDocument()
    })
  })

  describe('styling', () => {
    it('has default styling classes', () => {
      const { container } = render(<Badge>Test</Badge>)
      const badge = container.querySelector('span')
      expect(badge).toHaveClass('inline-flex')
      expect(badge).toHaveClass('rounded-full')
      expect(badge).toHaveClass('text-xs')
    })

    it('has muted background by default', () => {
      const { container } = render(<Badge>Test</Badge>)
      const badge = container.querySelector('span')
      expect(badge).toHaveClass('bg-[var(--muted)]')
    })

    it('accepts custom className', () => {
      const { container } = render(<Badge className="custom-badge">Test</Badge>)
      const badge = container.querySelector('span')
      expect(badge).toHaveClass('custom-badge')
    })

    it('combines default and custom classes', () => {
      const { container } = render(<Badge className="custom-badge">Test</Badge>)
      const badge = container.querySelector('span')
      expect(badge).toHaveClass('inline-flex')
      expect(badge).toHaveClass('custom-badge')
    })

    it('supports multiple custom classes', () => {
      const { container } = render(<Badge className="class1 class2 class3">Test</Badge>)
      const badge = container.querySelector('span')
      expect(badge).toHaveClass('class1')
      expect(badge).toHaveClass('class2')
      expect(badge).toHaveClass('class3')
    })
  })

  describe('variants via className', () => {
    it('can be styled as success badge', () => {
      const { container } = render(<Badge className="bg-green-500 text-white">Success</Badge>)
      const badge = container.querySelector('span')
      expect(badge).toHaveClass('bg-green-500')
      expect(badge).toHaveClass('text-white')
    })

    it('can be styled as error badge', () => {
      const { container } = render(<Badge className="bg-red-500 text-white">Error</Badge>)
      const badge = container.querySelector('span')
      expect(badge).toHaveClass('bg-red-500')
    })

    it('can be styled as warning badge', () => {
      const { container } = render(<Badge className="bg-yellow-500 text-black">Warning</Badge>)
      const badge = container.querySelector('span')
      expect(badge).toHaveClass('bg-yellow-500')
    })

    it('can be styled as info badge', () => {
      const { container } = render(<Badge className="bg-blue-500 text-white">Info</Badge>)
      const badge = container.querySelector('span')
      expect(badge).toHaveClass('bg-blue-500')
    })
  })

  describe('content variations', () => {
    it('handles long text', () => {
      const longText = 'This is a very long badge text that might overflow'
      render(<Badge>{longText}</Badge>)
      expect(screen.getByText(longText)).toBeInTheDocument()
    })

    it('handles special characters', () => {
      render(<Badge>!@#$%^&*()</Badge>)
      expect(screen.getByText('!@#$%^&*()')).toBeInTheDocument()
    })

    it('handles unicode characters', () => {
      render(<Badge>中文 🎉 émoji</Badge>)
      expect(screen.getByText('中文 🎉 émoji')).toBeInTheDocument()
    })

    it('handles empty string', () => {
      const { container } = render(<Badge>{''}</Badge>)
      const badge = container.querySelector('span')
      expect(badge).toBeInTheDocument()
      expect(badge?.textContent).toBe('')
    })
  })

  describe('HTML attributes', () => {
    it('renders as span element', () => {
      const { container } = render(<Badge>Test</Badge>)
      const badge = container.querySelector('span')
      expect(badge?.tagName.toLowerCase()).toBe('span')
    })

    it('renders as span element', () => {
      const { container } = render(<Badge>Test</Badge>)
      const badge = container.querySelector('span')
      expect(badge?.tagName.toLowerCase()).toBe('span')
    })
  })

  describe('use cases', () => {
    it('works as status indicator', () => {
      render(<Badge className="bg-green-500">Active</Badge>)
      expect(screen.getByText('Active')).toHaveClass('bg-green-500')
    })

    it('works as counter badge', () => {
      render(<Badge className="bg-red-500 text-white">99+</Badge>)
      expect(screen.getByText('99+')).toBeInTheDocument()
    })

    it('works as label/tag', () => {
      render(<Badge className="bg-blue-100 text-blue-800">Tag</Badge>)
      expect(screen.getByText('Tag')).toBeInTheDocument()
    })

    it('works with icons and text', () => {
      render(
        <Badge>
          <span>🔒</span>
          <span>Secure</span>
        </Badge>
      )
      expect(screen.getByText('🔒')).toBeInTheDocument()
      expect(screen.getByText('Secure')).toBeInTheDocument()
    })
  })
})
