import React from 'react'
import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import Input from './Input'

describe('Input', () => {
  describe('rendering', () => {
    it('renders with placeholder', () => {
      render(<Input placeholder="Enter text" />)
      expect(screen.getByPlaceholderText('Enter text')).toBeInTheDocument()
    })

    it('renders with value', () => {
      render(<Input value="Test value" onChange={() => {}} />)
      expect(screen.getByDisplayValue('Test value')).toBeInTheDocument()
    })

    it('renders empty by default', () => {
      render(<Input />)
      expect(screen.getByRole('textbox')).toHaveValue('')
    })
  })

  describe('user interactions', () => {
    it('calls onChange when typing', async () => {
      const user = userEvent.setup()
      const onChange = vi.fn()
      render(<Input onChange={onChange} />)
      const input = screen.getByRole('textbox')
      await user.type(input, 'Hello')
      expect(onChange).toHaveBeenCalled()
    })

    it('captures typed text', async () => {
      const user = userEvent.setup()
      render(<Input />)
      const input = screen.getByRole('textbox')
      await user.type(input, 'World')
      expect(input).toHaveValue('World')
    })

    it('clears when backspace is pressed', async () => {
      const user = userEvent.setup()
      render(<Input defaultValue="Hello" />)
      const input = screen.getByRole('textbox')
      await user.clear(input)
      expect(input).toHaveValue('')
    })

    it('accepts special characters', async () => {
      const user = userEvent.setup()
      render(<Input />)
      const input = screen.getByRole('textbox')
      await user.type(input, '!@#$%^&*()')
      expect(input).toHaveValue('!@#$%^&*()')
    })

    it('accepts unicode characters', async () => {
      const user = userEvent.setup()
      render(<Input />)
      const input = screen.getByRole('textbox')
      await user.type(input, '中文 тест 🎉')
      expect(input).toHaveValue('中文 тест 🎉')
    })
  })

  describe('disabled state', () => {
    it('is disabled when disabled prop is true', () => {
      render(<Input disabled />)
      expect(screen.getByRole('textbox')).toBeDisabled()
    })

    it('is not disabled by default', () => {
      render(<Input />)
      expect(screen.getByRole('textbox')).not.toBeDisabled()
    })

    it('does not accept input when disabled', async () => {
      const user = userEvent.setup()
      render(<Input disabled defaultValue="Initial" />)
      const input = screen.getByRole('textbox')
      await user.type(input, 'New text')
      expect(input).toHaveValue('Initial')
    })
  })

  describe('read only', () => {
    it('respects readOnly prop', () => {
      render(<Input readOnly value="Read only value" />)
      expect(screen.getByRole('textbox')).toHaveAttribute('readonly')
    })

    it('displays value in readOnly mode', () => {
      render(<Input readOnly value="Cannot change" />)
      expect(screen.getByDisplayValue('Cannot change')).toBeInTheDocument()
    })
  })

  describe('input types', () => {
    it('renders as text input by default', () => {
      const { container } = render(<Input />)
      const input = container.querySelector('input')
      expect(input).toBeInTheDocument()
      expect(input?.getAttribute('type')).toBeNull() // HTML default is text, but attribute is not set
    })

    it('supports password type', () => {
      const { container } = render(<Input type="password" />)
      const input = container.querySelector('input[type="password"]')
      expect(input).toBeInTheDocument()
    })

    it('supports email type', () => {
      render(<Input type="email" />)
      const input = document.querySelector('input[type="email"]')
      expect(input).toBeInTheDocument()
    })

    it('supports number type', () => {
      render(<Input type="number" />)
      const input = document.querySelector('input[type="number"]')
      expect(input).toBeInTheDocument()
    })
  })

  describe('HTML attributes', () => {
    it('accepts id attribute', () => {
      render(<Input id="unique-input" />)
      expect(screen.getByRole('textbox')).toHaveAttribute('id', 'unique-input')
    })

    it('accepts name attribute', () => {
      render(<Input name="username" />)
      expect(screen.getByRole('textbox')).toHaveAttribute('name', 'username')
    })

    it('accepts aria-label attribute', () => {
      render(<Input aria-label="Search input" />)
      expect(screen.getByLabelText('Search input')).toBeInTheDocument()
    })

    it('accepts aria-required attribute', () => {
      render(<Input required aria-required="true" />)
      expect(screen.getByRole('textbox')).toHaveAttribute('aria-required', 'true')
    })

    it('accepts maxLength attribute', () => {
      render(<Input maxLength={10} />)
      expect(screen.getByRole('textbox')).toHaveAttribute('maxlength', '10')
    })

    it('accepts minLength attribute', () => {
      render(<Input minLength={3} />)
      expect(screen.getByRole('textbox')).toHaveAttribute('minlength', '3')
    })

    it('accepts autoComplete attribute', () => {
      render(<Input autoComplete="email" />)
      expect(screen.getByRole('textbox')).toHaveAttribute('autocomplete', 'email')
    })

    it('accepts autoFocus attribute', () => {
      render(<Input autoFocus />)
      expect(screen.getByRole('textbox')).toHaveFocus()
    })
  })

  describe('custom classes', () => {
    it('applies custom className', () => {
      const { container } = render(<Input className="custom-input" />)
      const input = container.querySelector('input')
      expect(input).toHaveClass('custom-input')
    })

    it('combines default and custom classes', () => {
      const { container } = render(<Input className="custom-input" />)
      const input = container.querySelector('input')
      expect(input).toHaveClass('w-full')
      expect(input).toHaveClass('custom-input')
    })
  })

  describe('focus management', () => {
    it('can be focused', async () => {
      render(<Input />)
      const input = screen.getByRole('textbox')
      input.focus()
      expect(input).toHaveFocus()
    })

    it('can be blurred', async () => {
      render(<Input />)
      const input = screen.getByRole('textbox')
      input.focus()
      expect(input).toHaveFocus()
      input.blur()
      expect(input).not.toHaveFocus()
    })
  })

  describe('accessibility', () => {
    it('has textbox role', () => {
      render(<Input />)
      expect(screen.getByRole('textbox')).toBeInTheDocument()
    })

    it('supports aria-invalid', () => {
      render(<Input aria-invalid="true" />)
      expect(screen.getByRole('textbox')).toHaveAttribute('aria-invalid', 'true')
    })

    it('supports aria-describedby', () => {
      render(
        <>
          <Input aria-describedby="help-text" />
          <span id="help-text">Enter your name</span>
        </>
      )
      expect(screen.getByRole('textbox')).toHaveAttribute('aria-describedby', 'help-text')
    })
  })

  describe('event handling', () => {
    it('calls onFocus when focused', () => {
      const onFocus = vi.fn()
      render(<Input onFocus={onFocus} />)
      const input = screen.getByRole('textbox')
      input.focus()
      expect(onFocus).toHaveBeenCalled()
    })

    it('calls onBlur when blurred', () => {
      const onBlur = vi.fn()
      render(<Input onBlur={onBlur} />)
      const input = screen.getByRole('textbox')
      input.focus()
      input.blur()
      expect(onBlur).toHaveBeenCalled()
    })

    it('calls onKeyDown when key is pressed', async () => {
      const user = userEvent.setup()
      const onKeyDown = vi.fn()
      render(<Input onKeyDown={onKeyDown} />)
      const input = screen.getByRole('textbox')
      input.focus()
      await user.keyboard('a')
      expect(onKeyDown).toHaveBeenCalled()
    })
  })
})
