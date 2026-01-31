import React from 'react'
import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import Modal from './Modal'

describe('Modal', () => {
  describe('visibility', () => {
    it('renders when open is true', () => {
      render(<Modal open={true} onClose={() => {}}>Content</Modal>)
      expect(screen.getByText('Content')).toBeInTheDocument()
    })

    it('does not render when open is false', () => {
      const { container } = render(<Modal open={false} onClose={() => {}}>Content</Modal>)
      expect(container.firstChild).toBeNull()
    })

    it('renders nothing when open changes to false', () => {
      const { container, rerender } = render(<Modal open={true} onClose={() => {}}>Content</Modal>)
      expect(screen.getByText('Content')).toBeInTheDocument()
      
      rerender(<Modal open={false} onClose={() => {}}>Content</Modal>)
      expect(container.firstChild).toBeNull()
    })
  })

  describe('content rendering', () => {
    it('renders children', () => {
      render(
        <Modal open={true} onClose={() => {}}>
          <p>Modal content</p>
        </Modal>
      )
      expect(screen.getByText('Modal content')).toBeInTheDocument()
    })

    it('renders title when provided', () => {
      render(
        <Modal open={true} onClose={() => {}} title="Modal Title">
          Content
        </Modal>
      )
      expect(screen.getByText('Modal Title')).toBeInTheDocument()
    })

    it('does not render title when not provided', () => {
      const { container } = render(
        <Modal open={true} onClose={() => {}}>
          Content
        </Modal>
      )
      const heading = container.querySelector('h3')
      expect(heading).toBeNull()
    })

    it('renders complex children', () => {
      render(
        <Modal open={true} onClose={() => {}}>
          <div>
            <h4>Header</h4>
            <p>Paragraph</p>
            <button type="button">Action</button>
          </div>
        </Modal>
      )
      expect(screen.getByText('Header')).toBeInTheDocument()
      expect(screen.getByText('Paragraph')).toBeInTheDocument()
      expect(screen.getByText('Action')).toBeInTheDocument()
    })
  })

  describe('close interactions', () => {
    it('calls onClose when backdrop is clicked', async () => {
      const user = userEvent.setup()
      const onClose = vi.fn()
      render(<Modal open={true} onClose={onClose}>Content</Modal>)
      
      const backdrop = screen.getByText('Content').parentElement?.parentElement?.firstChild
      if (backdrop) await user.click(backdrop)
      
      expect(onClose).toHaveBeenCalled()
    })

    it('calls onClose when close button is clicked', async () => {
      const user = userEvent.setup()
      const onClose = vi.fn()
      render(<Modal open={true} onClose={onClose}>Content</Modal>)
      
      const closeButton = screen.getByText('Close')
      await user.click(closeButton)
      
      expect(onClose).toHaveBeenCalled()
    })

    it('calls onClose once per click', async () => {
      const user = userEvent.setup()
      const onClose = vi.fn()
      render(<Modal open={true} onClose={onClose}>Content</Modal>)
      
      const closeButton = screen.getByText('Close')
      await user.click(closeButton)
      
      expect(onClose).toHaveBeenCalledTimes(1)
    })
  })

  describe('modal structure', () => {
    it('renders with proper z-index container', () => {
      const { container } = render(<Modal open={true} onClose={() => {}}>Content</Modal>)
      const wrapper = container.querySelector('.fixed.inset-0')
      expect(wrapper).toBeInTheDocument()
    })

    it('renders backdrop with correct styling', () => {
      const { container } = render(<Modal open={true} onClose={() => {}}>Content</Modal>)
      const backdrop = container.querySelector('.absolute.inset-0')
      expect(backdrop).toHaveClass('bg-black/60')
    })

    it('renders modal content area', () => {
      const { container } = render(<Modal open={true} onClose={() => {}}>Content</Modal>)
      const modalContent = container.querySelector('.relative.z-10')
      expect(modalContent).toBeInTheDocument()
    })

    it('modal has rounded corners', () => {
      const { container } = render(<Modal open={true} onClose={() => {}}>Content</Modal>)
      const modalContent = container.querySelector('.relative.z-10')
      expect(modalContent).toHaveClass('rounded-lg')
    })
  })

  describe('accessibility', () => {
    it('renders close button', () => {
      render(<Modal open={true} onClose={() => {}}>Content</Modal>)
      expect(screen.getByText('Close')).toBeInTheDocument()
    })

    it('close button is clickable', async () => {
      const user = userEvent.setup()
      const onClose = vi.fn()
      render(<Modal open={true} onClose={onClose}>Content</Modal>)
      
      const closeButton = screen.getByText('Close')
      expect(closeButton.tagName.toLowerCase()).toBe('button')
      await user.click(closeButton)
      expect(onClose).toHaveBeenCalled()
    })
  })

  describe('state changes', () => {
    it('updates content when children change', () => {
      const { rerender } = render(<Modal open={true} onClose={() => {}}>Initial</Modal>)
      expect(screen.getByText('Initial')).toBeInTheDocument()
      
      rerender(<Modal open={true} onClose={() => {}}>Updated</Modal>)
      expect(screen.getByText('Updated')).toBeInTheDocument()
    })

    it('updates title when title prop changes', () => {
      const { rerender } = render(
        <Modal open={true} onClose={() => {}} title="First Title">Content</Modal>
      )
      expect(screen.getByText('First Title')).toBeInTheDocument()
      
      rerender(
        <Modal open={true} onClose={() => {}} title="Second Title">Content</Modal>
      )
      expect(screen.getByText('Second Title')).toBeInTheDocument()
    })
  })

  describe('edge cases', () => {
    it('handles empty children', () => {
      render(<Modal open={true} onClose={() => {}} />)
      expect(screen.getByText('Close')).toBeInTheDocument()
    })

    it('handles empty string children', () => {
      render(<Modal open={true} onClose={() => {}}>{}</Modal>)
      expect(screen.getByText('Close')).toBeInTheDocument()
    })

    it('handles null children', () => {
      render(<Modal open={true} onClose={() => {}}>{null}</Modal>)
      expect(screen.getByText('Close')).toBeInTheDocument()
    })
  })
})
