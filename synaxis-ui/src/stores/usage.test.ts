import { describe, it, expect, beforeEach } from 'vitest'
import useUsageStore from './usage'

describe('usage store', () => {
  beforeEach(() => {
    useUsageStore.setState({ totalTokens: 0 })
  })

  it('addUsage increases totalTokens', () => {
    useUsageStore.getState().addUsage(10)
    expect(useUsageStore.getState().totalTokens).toBe(10)
  })

  it('multiple addUsage accumulate', () => {
    useUsageStore.getState().addUsage(5)
    useUsageStore.getState().addUsage(3)
    expect(useUsageStore.getState().totalTokens).toBe(8)
  })
})
