import { describe, it, expect } from 'vitest'
import cn from './utils'

describe('cn (class name utility)', () => {
  describe('basic class merging', () => {
    it('joins class names', () => {
      const result = cn('class-a', 'class-b')
      expect(result).toContain('class-a')
      expect(result).toContain('class-b')
    })

    it('handles single class', () => {
      const result = cn('single')
      expect(result).toBe('single')
    })

    it('handles empty strings', () => {
      const result = cn('class-a', '', 'class-b')
      expect(result).toContain('class-a')
      expect(result).toContain('class-b')
      expect(result).not.toContain('  ')
    })

    it('handles whitespace in class names', () => {
      const result = cn('  padded  ', 'normal')
      expect(result).toContain('padded')
      expect(result).toContain('normal')
    })
  })

  describe('tailwind-merge deduplication', () => {
    it('deduplicates conflicting tailwind classes', () => {
      const result = cn('p-2', 'p-4')
      expect(result).toContain('p-4')
      expect(result).not.toContain('p-2')
    })

    it('keeps last conflicting class', () => {
      const result = cn('m-1', 'm-2', 'm-3')
      expect(result).toContain('m-3')
      expect(result).not.toContain('m-1')
      expect(result).not.toContain('m-2')
    })

    it('handles complex tailwind conflicts', () => {
      const result = cn('text-sm', 'text-lg', 'text-center')
      expect(result).toContain('text-lg')
      expect(result).toContain('text-center')
      expect(result).not.toContain('text-sm')
    })

    it('handles color class conflicts', () => {
      const result = cn('text-red-500', 'text-blue-500')
      expect(result).toContain('text-blue-500')
      expect(result).not.toContain('text-red-500')
    })

    it('handles background color conflicts', () => {
      const result = cn('bg-red-500', 'bg-green-500', 'bg-blue-500')
      expect(result).toContain('bg-blue-500')
      expect(result).not.toContain('bg-red-500')
      expect(result).not.toContain('bg-green-500')
    })

    it('handles padding conflicts with different directions', () => {
      const result = cn('p-2', 'px-4', 'py-6')
      expect(result).toContain('px-4')
      expect(result).toContain('py-6')
      // Current behavior includes all classes
      expect(result).toContain('p-2')
    })

    it('handles margin conflicts', () => {
      const result = cn('m-4', 'mt-8')
      expect(result).toContain('mt-8')
      // Current behavior includes all classes
      expect(result).toContain('m-4')
    })
  })

  describe('conditional classes', () => {
    it('includes classes from true conditions', () => {
      const result = cn('base', { 'conditional': true })
      expect(result).toContain('base')
      expect(result).toContain('conditional')
    })

    it('excludes classes from false conditions', () => {
      const result = cn('base', { 'conditional': false })
      expect(result).toContain('base')
      expect(result).not.toContain('conditional')
    })

    it('handles multiple conditions', () => {
      const result = cn('base', {
        'active': true,
        'disabled': false,
        'loading': true,
      })
      expect(result).toContain('base')
      expect(result).toContain('active')
      expect(result).toContain('loading')
      expect(result).not.toContain('disabled')
    })

    it('handles mixed strings and conditions', () => {
      const isActive = true
      const isDisabled = false
      const result = cn(
        'button',
        'rounded',
        { 'button-active': isActive },
        { 'button-disabled': isDisabled }
      )
      expect(result).toContain('button')
      expect(result).toContain('rounded')
      expect(result).toContain('button-active')
      expect(result).not.toContain('button-disabled')
    })
  })

  describe('array and object handling', () => {
    it('flattens arrays of classes', () => {
      const result = cn(['class-a', 'class-b'], 'class-c')
      expect(result).toContain('class-a')
      expect(result).toContain('class-b')
      expect(result).toContain('class-c')
    })

    it('handles nested arrays', () => {
      const result = cn(['a', ['b', 'c']], 'd')
      expect(result).toContain('a')
      expect(result).toContain('b')
      expect(result).toContain('c')
      expect(result).toContain('d')
    })

    it('handles null and undefined', () => {
      const result = cn('a', null, undefined, 'b')
      expect(result).toContain('a')
      expect(result).toContain('b')
    })

    it('handles falsy values', () => {
      const result = cn('a', false, 0, '', 'b')
      expect(result).toContain('a')
      expect(result).toContain('b')
    })
  })

  describe('real-world scenarios', () => {
    it('handles button styling', () => {
      const isPrimary = true
      const isLarge = false
      const result = cn(
        'px-4 py-2 rounded',
        { 'bg-blue-500 text-white': isPrimary },
        { 'bg-gray-500': !isPrimary },
        { 'text-lg': isLarge },
        'hover:opacity-90'
      )
      expect(result).toContain('px-4')
      expect(result).toContain('py-2')
      expect(result).toContain('rounded')
      expect(result).toContain('bg-blue-500')
      expect(result).toContain('text-white')
      expect(result).toContain('hover:opacity-90')
      expect(result).not.toContain('bg-gray-500')
      expect(result).not.toContain('text-lg')
    })

    it('handles card component styling', () => {
      const result = cn(
        'rounded-lg border p-4',
        'bg-white shadow-sm',
        'hover:shadow-md transition-shadow'
      )
      expect(result).toContain('rounded-lg')
      expect(result).toContain('border')
      expect(result).toContain('p-4')
      expect(result).toContain('bg-white')
      expect(result).toContain('shadow-sm')
      expect(result).toContain('hover:shadow-md')
      expect(result).toContain('transition-shadow')
    })

    it('handles responsive classes', () => {
      const result = cn(
        'w-full',
        'md:w-1/2',
        'lg:w-1/3'
      )
      expect(result).toContain('w-full')
      expect(result).toContain('md:w-1/2')
      expect(result).toContain('lg:w-1/3')
    })

    it('handles state-based styling', () => {
      const isOpen = true
      const hasError = false
      const result = cn(
        'modal',
        { 'modal-open': isOpen, 'modal-closed': !isOpen },
        { 'modal-error': hasError }
      )
      expect(result).toContain('modal')
      expect(result).toContain('modal-open')
      expect(result).not.toContain('modal-closed')
      expect(result).not.toContain('modal-error')
    })
  })

  describe('edge cases', () => {
    it('returns empty string for no arguments', () => {
      const result = cn()
      expect(result).toBe('')
    })

    it('handles only falsy values', () => {
      const result = cn(null, undefined, false, '', 0)
      expect(result).toBe('')
    })

    it('handles very long class strings', () => {
      const longClass = 'a'.repeat(1000)
      const result = cn(longClass, 'b')
      expect(result).toContain(longClass)
      expect(result).toContain('b')
    })

    it('handles special characters in class names', () => {
      const result = cn('class-[special]', 'class_with_underscore', 'class:with:colons')
      expect(result).toContain('class-[special]')
      expect(result).toContain('class_with_underscore')
      expect(result).toContain('class:with:colons')
    })

    it('preserves important modifier', () => {
      const result = cn('text-red-500', '!text-blue-500')
      expect(result).toContain('!text-blue-500')
      // Current behavior includes all classes
      expect(result).toContain('text-red-500')
    })
  })

  describe('tailwind-specific features', () => {
    it('handles arbitrary values', () => {
      const result = cn('w-[100px]', 'w-[200px]')
      expect(result).toContain('w-[200px]')
      expect(result).not.toContain('w-[100px]')
    })

    it('handles CSS variables', () => {
      const result = cn('bg-[var(--primary)]', 'text-[var(--foreground)]')
      expect(result).toContain('bg-[var(--primary)]')
      expect(result).toContain('text-[var(--foreground)]')
    })

    it('handles complex selectors', () => {
      const result = cn(
        'focus:outline-none',
        'focus:ring-2',
        'focus:ring-blue-500'
      )
      expect(result).toContain('focus:outline-none')
      expect(result).toContain('focus:ring-2')
      expect(result).toContain('focus:ring-blue-500')
    })
  })
})
