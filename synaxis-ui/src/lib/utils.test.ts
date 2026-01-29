import { describe, it, expect } from 'vitest'
import cn from './utils'

describe('cn', () => {
  it('joins class names and deduplicates with tailwind-merge', () => {
    const res = cn('p-2', 'p-4', { 'text-center': true })
    expect(res).toContain('p-4')
    expect(res).toContain('text-center')
  })
})
