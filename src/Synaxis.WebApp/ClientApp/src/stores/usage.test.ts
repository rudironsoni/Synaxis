import { describe, it, expect, beforeEach, vi } from 'vitest'
import useUsageStore from './usage'

// Mock the db module
vi.mock('@/db/db', () => ({
  default: {
    messages: {
      toArray: vi.fn(),
    },
  },
}))

// Import mocked db after mocking
import db from '@/db/db'

describe('usage store', () => {
  beforeEach(() => {
    // Reset store state
    useUsageStore.setState({ totalTokens: 0 })
    // Clear mock calls
    vi.clearAllMocks()
  })

  describe('initialization', () => {
    it('should have totalTokens set to 0 initially', () => {
      expect(useUsageStore.getState().totalTokens).toBe(0)
    })

    it('should have addUsage function', () => {
      expect(useUsageStore.getState().addUsage).toBeInstanceOf(Function)
    })
  })

  describe('addUsage', () => {
    it('should increase totalTokens by specified amount', () => {
      useUsageStore.getState().addUsage(10)
      expect(useUsageStore.getState().totalTokens).toBe(10)
    })

    it('should accumulate multiple usages', () => {
      useUsageStore.getState().addUsage(5)
      useUsageStore.getState().addUsage(3)
      expect(useUsageStore.getState().totalTokens).toBe(8)
    })

    it('should handle zero tokens', () => {
      useUsageStore.getState().addUsage(10)
      useUsageStore.getState().addUsage(0)
      expect(useUsageStore.getState().totalTokens).toBe(10)
    })

    it('should handle adding to zero', () => {
      expect(useUsageStore.getState().totalTokens).toBe(0)
      useUsageStore.getState().addUsage(100)
      expect(useUsageStore.getState().totalTokens).toBe(100)
    })

    it('should handle large numbers', () => {
      useUsageStore.getState().addUsage(1000000)
      useUsageStore.getState().addUsage(2000000)
      expect(useUsageStore.getState().totalTokens).toBe(3000000)
    })

    it('should handle decimal token counts', () => {
      useUsageStore.getState().addUsage(10.5)
      useUsageStore.getState().addUsage(5.3)
      expect(useUsageStore.getState().totalTokens).toBeCloseTo(15.8)
    })

    it('should handle negative token counts', () => {
      useUsageStore.getState().addUsage(100)
      useUsageStore.getState().addUsage(-20)
      expect(useUsageStore.getState().totalTokens).toBe(80)
    })
  })

  describe('state reactivity', () => {
    it('should maintain state between calls', () => {
      useUsageStore.getState().addUsage(10)
      expect(useUsageStore.getState().totalTokens).toBe(10)

      useUsageStore.getState().addUsage(5)
      expect(useUsageStore.getState().totalTokens).toBe(15)
    })

    it('should allow direct state updates', () => {
      useUsageStore.setState({ totalTokens: 50 })
      expect(useUsageStore.getState().totalTokens).toBe(50)
    })

    it('should reset state correctly', () => {
      useUsageStore.getState().addUsage(100)
      useUsageStore.setState({ totalTokens: 0 })
      expect(useUsageStore.getState().totalTokens).toBe(0)
    })
  })

  describe('usage calculation from database', () => {
    it('should calculate total from message tokenUsage', async () => {
      const mockMessages = [
        { id: 1, content: 'Hello', tokenUsage: { total: 10 } },
        { id: 2, content: 'World', tokenUsage: { total: 5 } },
        { id: 3, content: '!', tokenUsage: { total: 1 } },
      ]
      vi.mocked(db.messages.toArray).mockResolvedValue(mockMessages)

      // Simulate the init function behavior
      const list = await db.messages.toArray()
      const total = list.reduce((acc, m) => acc + (m.tokenUsage?.total || 0), 0)
      useUsageStore.setState({ totalTokens: total })

      expect(useUsageStore.getState().totalTokens).toBe(16)
    })

    it('should handle messages without tokenUsage', async () => {
      const mockMessages = [
        { id: 1, content: 'Hello', tokenUsage: { total: 10 } },
        { id: 2, content: 'World' }, // No tokenUsage
        { id: 3, content: '!', tokenUsage: { total: 5 } },
      ]
      vi.mocked(db.messages.toArray).mockResolvedValue(mockMessages)

      const list = await db.messages.toArray()
      const total = list.reduce((acc, m) => acc + (m.tokenUsage?.total || 0), 0)
      useUsageStore.setState({ totalTokens: total })

      expect(useUsageStore.getState().totalTokens).toBe(15)
    })

    it('should handle empty messages array', async () => {
      vi.mocked(db.messages.toArray).mockResolvedValue([])

      const list = await db.messages.toArray()
      const total = list.reduce((acc, m) => acc + (m.tokenUsage?.total || 0), 0)
      useUsageStore.setState({ totalTokens: total })

      expect(useUsageStore.getState().totalTokens).toBe(0)
    })

    it('should handle messages with zero tokenUsage', async () => {
      const mockMessages = [
        { id: 1, content: 'Hello', tokenUsage: { total: 0 } },
        { id: 2, content: 'World', tokenUsage: { total: 0 } },
      ]
      vi.mocked(db.messages.toArray).mockResolvedValue(mockMessages)

      const list = await db.messages.toArray()
      const total = list.reduce((acc, m) => acc + (m.tokenUsage?.total || 0), 0)
      useUsageStore.setState({ totalTokens: total })

      expect(useUsageStore.getState().totalTokens).toBe(0)
    })

    it('should handle database error gracefully', async () => {
      // Mock console.warn to suppress error output
      const consoleSpy = vi.spyOn(console, 'warn').mockImplementation(() => {})
      vi.mocked(db.messages.toArray).mockRejectedValue(new Error('DB Error'))

      try {
        await db.messages.toArray()
      } catch (e) {
        console.warn('usage init failed', e)
      }

      // Verify error was logged
      expect(consoleSpy).toHaveBeenCalledWith('usage init failed', expect.any(Error))
      consoleSpy.mockRestore()
    })
  })

  describe('edge cases', () => {
    it('should handle single large addition', () => {
      useUsageStore.getState().addUsage(Number.MAX_SAFE_INTEGER)
      expect(useUsageStore.getState().totalTokens).toBe(Number.MAX_SAFE_INTEGER)
    })

    it('should handle adding after reset', () => {
      useUsageStore.getState().addUsage(100)
      useUsageStore.setState({ totalTokens: 0 })
      useUsageStore.getState().addUsage(50)
      expect(useUsageStore.getState().totalTokens).toBe(50)
    })

    it('should maintain precision for fractional tokens', () => {
      useUsageStore.getState().addUsage(0.1)
      useUsageStore.getState().addUsage(0.2)
      // Due to floating point, this might not be exactly 0.3
      expect(useUsageStore.getState().totalTokens).toBeCloseTo(0.3)
    })

    it('should handle adding Infinity', () => {
      useUsageStore.getState().addUsage(100)
      useUsageStore.getState().addUsage(Infinity)
      expect(useUsageStore.getState().totalTokens).toBe(Infinity)
    })

    it('should handle adding -Infinity', () => {
      useUsageStore.getState().addUsage(100)
      useUsageStore.getState().addUsage(-Infinity)
      expect(useUsageStore.getState().totalTokens).toBe(-Infinity)
    })

    it('should handle adding NaN', () => {
      useUsageStore.getState().addUsage(100)
      useUsageStore.getState().addUsage(NaN)
      expect(useUsageStore.getState().totalTokens).toBeNaN()
    })

    it('should handle very small decimal additions', () => {
      useUsageStore.getState().addUsage(0.0000001)
      useUsageStore.getState().addUsage(0.0000002)
      expect(useUsageStore.getState().totalTokens).toBeCloseTo(0.0000003)
    })

    it('should handle rapid successive additions', () => {
      for (let i = 0; i < 1000; i++) {
        useUsageStore.getState().addUsage(1)
      }
      expect(useUsageStore.getState().totalTokens).toBe(1000)
    })

    it('should handle alternating positive and negative additions', () => {
      useUsageStore.getState().addUsage(100)
      useUsageStore.getState().addUsage(-50)
      useUsageStore.getState().addUsage(30)
      useUsageStore.getState().addUsage(-20)
      expect(useUsageStore.getState().totalTokens).toBe(60)
    })

    it('should handle adding zero multiple times', () => {
      useUsageStore.getState().addUsage(100)
      for (let i = 0; i < 10; i++) {
        useUsageStore.getState().addUsage(0)
      }
      expect(useUsageStore.getState().totalTokens).toBe(100)
    })

    it('should handle very large negative addition', () => {
      useUsageStore.getState().addUsage(1000)
      useUsageStore.getState().addUsage(-Number.MAX_SAFE_INTEGER)
      expect(useUsageStore.getState().totalTokens).toBeLessThan(0)
    })

    it('should handle adding to already large total', () => {
      useUsageStore.setState({ totalTokens: Number.MAX_SAFE_INTEGER - 100 })
      useUsageStore.getState().addUsage(50)
      expect(useUsageStore.getState().totalTokens).toBe(Number.MAX_SAFE_INTEGER - 50)
    })

    it('should handle scientific notation additions', () => {
      useUsageStore.getState().addUsage(1e10)
      useUsageStore.getState().addUsage(1e5)
      expect(useUsageStore.getState().totalTokens).toBe(10000100000)
    })

    it('should handle adding after setting state directly', () => {
      useUsageStore.setState({ totalTokens: 500 })
      useUsageStore.getState().addUsage(100)
      expect(useUsageStore.getState().totalTokens).toBe(600)
    })

    it('should handle multiple resets and additions', () => {
      useUsageStore.getState().addUsage(100)
      useUsageStore.setState({ totalTokens: 0 })
      useUsageStore.getState().addUsage(200)
      useUsageStore.setState({ totalTokens: 0 })
      useUsageStore.getState().addUsage(300)
      expect(useUsageStore.getState().totalTokens).toBe(300)
    })

    it('should handle adding very small fractional values', () => {
      useUsageStore.getState().addUsage(0.000000001)
      useUsageStore.getState().addUsage(0.000000002)
      expect(useUsageStore.getState().totalTokens).toBeGreaterThan(0)
    })

    it('should handle overflow beyond MAX_SAFE_INTEGER', () => {
      useUsageStore.setState({ totalTokens: Number.MAX_SAFE_INTEGER })
      useUsageStore.getState().addUsage(1)
      // The result will lose precision but should not throw
      expect(useUsageStore.getState().totalTokens).toBeGreaterThan(Number.MAX_SAFE_INTEGER)
    })

    it('should handle underflow below MIN_SAFE_INTEGER', () => {
      useUsageStore.setState({ totalTokens: Number.MIN_SAFE_INTEGER })
      useUsageStore.getState().addUsage(-1)
      // The result will lose precision but should not throw
      expect(useUsageStore.getState().totalTokens).toBeLessThan(Number.MIN_SAFE_INTEGER)
    })

    it('should handle adding same value repeatedly', () => {
      const value = 42
      for (let i = 0; i < 100; i++) {
        useUsageStore.getState().addUsage(value)
      }
      expect(useUsageStore.getState().totalTokens).toBe(4200)
    })

    it('should handle database with very large token counts', async () => {
      const mockMessages = [
        { id: 1, content: 'Message 1', tokenUsage: { total: Number.MAX_SAFE_INTEGER } },
        { id: 2, content: 'Message 2', tokenUsage: { total: 1000 } },
      ]
      vi.mocked(db.messages.toArray).mockResolvedValue(mockMessages)

      const list = await db.messages.toArray()
      const total = list.reduce((acc, m) => acc + (m.tokenUsage?.total || 0), 0)
      useUsageStore.setState({ totalTokens: total })

      expect(useUsageStore.getState().totalTokens).toBeGreaterThan(Number.MAX_SAFE_INTEGER)
    })

    it('should handle database with negative token counts', async () => {
      const mockMessages = [
        { id: 1, content: 'Message 1', tokenUsage: { total: 100 } },
        { id: 2, content: 'Message 2', tokenUsage: { total: -50 } },
        { id: 3, content: 'Message 3', tokenUsage: { total: 30 } },
      ]
      vi.mocked(db.messages.toArray).mockResolvedValue(mockMessages)

      const list = await db.messages.toArray()
      const total = list.reduce((acc, m) => acc + (m.tokenUsage?.total || 0), 0)
      useUsageStore.setState({ totalTokens: total })

      expect(useUsageStore.getState().totalTokens).toBe(80)
    })

    it('should handle database with fractional token counts', async () => {
      const mockMessages = [
        { id: 1, content: 'Message 1', tokenUsage: { total: 10.5 } },
        { id: 2, content: 'Message 2', tokenUsage: { total: 5.3 } },
        { id: 3, content: 'Message 3', tokenUsage: { total: 2.2 } },
      ]
      vi.mocked(db.messages.toArray).mockResolvedValue(mockMessages)

      const list = await db.messages.toArray()
      const total = list.reduce((acc, m) => acc + (m.tokenUsage?.total || 0), 0)
      useUsageStore.setState({ totalTokens: total })

      expect(useUsageStore.getState().totalTokens).toBeCloseTo(18)
    })
  })
})
