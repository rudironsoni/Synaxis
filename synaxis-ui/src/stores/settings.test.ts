import { describe, it, expect, beforeEach } from 'vitest'
import { act } from 'react-dom/test-utils'
import useSettingsStore from './settings'

describe('settings store', () => {
  beforeEach(() => {
    // reset to defaults
    useSettingsStore.setState({ gatewayUrl: 'http://localhost:5000', costRate: 0 })
  })

  it('has default values', () => {
    const s = useSettingsStore.getState()
    expect(s.gatewayUrl).toBe('http://localhost:5000')
    expect(s.costRate).toBe(0)
  })

  it('setGatewayUrl updates the url', () => {
    act(() => {
      useSettingsStore.getState().setGatewayUrl('http://example.com')
    })
    expect(useSettingsStore.getState().gatewayUrl).toBe('http://example.com')
  })

  it('setCostRate updates the rate', () => {
    act(() => {
      useSettingsStore.getState().setCostRate(5)
    })
    expect(useSettingsStore.getState().costRate).toBe(5)
  })
})
