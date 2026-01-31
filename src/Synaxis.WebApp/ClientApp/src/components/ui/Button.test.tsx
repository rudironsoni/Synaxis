import React from 'react'
import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import Button from './Button'

describe('Button', () => {
  describe('rendering', () => {
    it('renders children correctly', () => {
      render(<Button>Click me</Button>)
      expect(screen.getByRole('button', { name: /click me/i })).toBeInTheDocument()
    })

    it('renders with text content', () => {
      render(<Button>Submit</Button>)
      expect(screen.getByText('Submit')).toBeInTheDocument()
    })

    it('renders with React nodes as children', () => {
      render(
        <Button>
          <span>Icon</span>
          <span>Text</span>
        </Button>
      )
      expect(screen.getByText('Icon')).toBeInTheDocument()
      expect(screen.getByText('Text')).toBeInTheDocument()
    })
  })

  describe('variants', () => {
    it('renders primary variant by default', () => {
      const { container } = render(<Button>Primary</Button>)
      const button = container.querySelector('button')
      expect(button).toHaveClass('bg-[var(--primary)]')
    })

    it('renders ghost variant', () => {
      const { container } = render(<Button variant="ghost">Ghost</Button>)
      const button = container.querySelector('button')
      expect(button).toHaveClass('bg-transparent')
      expect(button).toHaveClass('border')
    })

    it('renders danger variant', () => {
      const { container } = render(<Button variant="danger">Danger</Button>)
      const button = container.querySelector('button')
      expect(button).toHaveClass('bg-red-600')
    })

    it('switches between variants correctly', () => {
      const { container, rerender } = render(<Button variant="primary">Button</Button>)
      let button = container.querySelector('button')
      expect(button).toHaveClass('bg-[var(--primary)]')

      rerender(<Button variant="ghost">Button</Button>)
      button = container.querySelector('button')
      expect(button).toHaveClass('bg-transparent')

      rerender(<Button variant="danger">Button</Button>)
      button = container.querySelector('button')
      expect(button).toHaveClass('bg-red-600')
    })
  })

  describe('interactions', () => {
    it('calls onClick when clicked', async () => {
      const user = userEvent.setup()
      const onClick = vi.fn()
      render(<Button onClick={onClick}>Click me</Button>)
      const btn = screen.getByRole('button', { name: /click me/i })
      await user.click(btn)
      expect(onClick).toHaveBeenCalled()
    })

    it('calls onClick once per click', async () => {
      const user = userEvent.setup()
      const onClick = vi.fn()
      render(<Button onClick={onClick}>Click me</Button>)
      const btn = screen.getByRole('button', { name: /click me/i })
      await user.click(btn)
      expect(onClick).toHaveBeenCalledTimes(1)
    })

    it('does not call onClick when disabled', async () => {
      const user = userEvent.setup()
      const onClick = vi.fn()
      render(<Button onClick={onClick} disabled>Click me</Button>)
      const btn = screen.getByRole('button', { name: /click me/i })
      await user.click(btn)
      expect(onClick).not.toHaveBeenCalled()
    })

    it('is focusable', async () => {
      render(<Button>Focusable</Button>)
      const btn = screen.getByRole('button', { name: /focusable/i })
      btn.focus()
      expect(btn).toHaveFocus()
    })

    it('supports keyboard activation with Enter', async () => {
      const user = userEvent.setup()
      const onClick = vi.fn()
      render(<Button onClick={onClick}>Press Enter</Button>)
      const btn = screen.getByRole('button', { name: /press enter/i })
      btn.focus()
      await user.keyboard('{Enter}')
      expect(onClick).toHaveBeenCalled()
    })

    it('supports keyboard activation with Space', async () => {
      const user = userEvent.setup()
      const onClick = vi.fn()
      render(<Button onClick={onClick}>Press Space</Button>)
      const btn = screen.getByRole('button', { name: /press space/i })
      btn.focus()
      await user.keyboard(' ')
      expect(onClick).toHaveBeenCalled()
    })
  })

  describe('disabled state', () => {
    it('has disabled attribute when disabled', () => {
      render(<Button disabled>Disabled</Button>)
      expect(screen.getByRole('button', { name: /disabled/i })).toBeDisabled()
    })

    it('does not have disabled attribute when not disabled', () => {
      render(<Button>Enabled</Button>)
      expect(screen.getByRole('button', { name: /enabled/i })).not.toBeDisabled()
    })

    it('is not focusable when disabled', () => {
      render(<Button disabled>Disabled</Button>)
      const btn = screen.getByRole('button', { name: /disabled/i })
      expect(btn).not.toHaveFocus()
    })
  })

  describe('custom classes', () => {
    it('applies custom className', () => {
      const { container } = render(<Button className="custom-class">Styled</Button>)
      const button = container.querySelector('button')
      expect(button).toHaveClass('custom-class')
    })

    it('applies multiple custom classes', () => {
      const { container } = render(<Button className="class1 class2">Styled</Button>)
      const button = container.querySelector('button')
      expect(button).toHaveClass('class1')
      expect(button).toHaveClass('class2')
    })

    it('combines default and custom classes', () => {
      const { container } = render(<Button className="custom-class">Styled</Button>)
      const button = container.querySelector('button')
      expect(button).toHaveClass('inline-flex')
      expect(button).toHaveClass('custom-class')
    })
  })

  describe('HTML attributes', () => {
    it('accepts type attribute', () => {
      render(<Button type="submit">Submit</Button>)
      expect(screen.getByRole('button', { name: /submit/i })).toHaveAttribute('type', 'submit')
    })

    it('accepts aria-label attribute', () => {
      render(<Button aria-label="Close dialog">X</Button>)
      expect(screen.getByLabelText('Close dialog')).toBeInTheDocument()
    })

    it('accepts data attributes', () => {
      render(<Button data-testid="test-button">Test</Button>)
      expect(screen.getByTestId('test-button')).toBeInTheDocument()
    })

    it('accepts id attribute', () => {
      render(<Button id="unique-button">Test</Button>)
      expect(screen.getByRole('button')).toHaveAttribute('id', 'unique-button')
    })

    it('accepts name attribute', () => {
      render(<Button name="submit-action">Submit</Button>)
      expect(screen.getByRole('button')).toHaveAttribute('name', 'submit-action')
    })
  })

  describe('accessibility', () => {
    it('has button role', () => {
      render(<Button>Accessible</Button>)
      expect(screen.getByRole('button')).toBeInTheDocument()
    })

    it('is accessible when focused', async () => {
      render(<Button>Focus Test</Button>)
      const btn = screen.getByRole('button', { name: /focus test/i })
      btn.focus()
      expect(btn).toHaveFocus()
    })

    it('maintains disabled state accessibility', () => {
      render(<Button disabled>Disabled</Button>)
      const btn = screen.getByRole('button', { name: /disabled/i })
      expect(btn).toHaveAttribute('disabled')
    })
  })
})
