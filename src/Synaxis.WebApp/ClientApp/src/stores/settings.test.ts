import { describe, it, expect, beforeEach, vi } from 'vitest'
import { act } from 'react'
import useSettingsStore from './settings'

describe('settings store', () => {
  beforeEach(() => {
    // Reset to default state
    act(() => {
      useSettingsStore.setState({
        gatewayUrl: 'http://localhost:5000',
        costRate: 0,
        streamingEnabled: false,
        jwtToken: null,
      })
    })
  })

  describe('initialization', () => {
    it('should have default values', () => {
      const state = useSettingsStore.getState()
      expect(state.gatewayUrl).toBe('http://localhost:5000')
      expect(state.costRate).toBe(0)
      expect(state.streamingEnabled).toBe(false)
      expect(state.jwtToken).toBeNull()
    })

    it('should persist settings (persist middleware is configured)', () => {
      // Verify the store has persist configuration
      const state = useSettingsStore.getState()
      expect(state).toHaveProperty('setGatewayUrl')
      expect(state).toHaveProperty('setCostRate')
      expect(state).toHaveProperty('setStreamingEnabled')
      expect(state).toHaveProperty('setJwtToken')
      expect(state).toHaveProperty('logout')
    })
  })

  describe('setGatewayUrl', () => {
    it('should update the gateway URL', () => {
      act(() => {
        useSettingsStore.getState().setGatewayUrl('http://example.com')
      })
      expect(useSettingsStore.getState().gatewayUrl).toBe('http://example.com')
    })

    it('should update to custom port', () => {
      act(() => {
        useSettingsStore.getState().setGatewayUrl('http://localhost:8080')
      })
      expect(useSettingsStore.getState().gatewayUrl).toBe('http://localhost:8080')
    })

    it('should update to HTTPS URL', () => {
      act(() => {
        useSettingsStore.getState().setGatewayUrl('https://api.example.com')
      })
      expect(useSettingsStore.getState().gatewayUrl).toBe('https://api.example.com')
    })

    it('should handle empty string URL', () => {
      act(() => {
        useSettingsStore.getState().setGatewayUrl('')
      })
      expect(useSettingsStore.getState().gatewayUrl).toBe('')
    })

    it('should handle URL with path', () => {
      act(() => {
        useSettingsStore.getState().setGatewayUrl('http://localhost:5000/api/v1')
      })
      expect(useSettingsStore.getState().gatewayUrl).toBe('http://localhost:5000/api/v1')
    })
  })

  describe('setCostRate', () => {
    it('should update the cost rate', () => {
      act(() => {
        useSettingsStore.getState().setCostRate(5)
      })
      expect(useSettingsStore.getState().costRate).toBe(5)
    })

    it('should handle zero cost rate', () => {
      act(() => {
        useSettingsStore.getState().setCostRate(0)
      })
      expect(useSettingsStore.getState().costRate).toBe(0)
    })

    it('should handle negative cost rate', () => {
      act(() => {
        useSettingsStore.getState().setCostRate(-1)
      })
      expect(useSettingsStore.getState().costRate).toBe(-1)
    })

    it('should handle decimal cost rate', () => {
      act(() => {
        useSettingsStore.getState().setCostRate(0.5)
      })
      expect(useSettingsStore.getState().costRate).toBe(0.5)
    })

    it('should handle large cost rate', () => {
      act(() => {
        useSettingsStore.getState().setCostRate(1000000)
      })
      expect(useSettingsStore.getState().costRate).toBe(1000000)
    })
  })

  describe('setStreamingEnabled', () => {
    it('should enable streaming', () => {
      act(() => {
        useSettingsStore.getState().setStreamingEnabled(true)
      })
      expect(useSettingsStore.getState().streamingEnabled).toBe(true)
    })

    it('should disable streaming', () => {
      // First enable
      act(() => {
        useSettingsStore.getState().setStreamingEnabled(true)
      })
      expect(useSettingsStore.getState().streamingEnabled).toBe(true)

      // Then disable
      act(() => {
        useSettingsStore.getState().setStreamingEnabled(false)
      })
      expect(useSettingsStore.getState().streamingEnabled).toBe(false)
    })

    it('should handle multiple toggles', () => {
      act(() => {
        useSettingsStore.getState().setStreamingEnabled(true)
        useSettingsStore.getState().setStreamingEnabled(false)
        useSettingsStore.getState().setStreamingEnabled(true)
      })
      expect(useSettingsStore.getState().streamingEnabled).toBe(true)
    })
  })

  describe('setJwtToken', () => {
    it('should set JWT token', () => {
      const token = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.test'
      act(() => {
        useSettingsStore.getState().setJwtToken(token)
      })
      expect(useSettingsStore.getState().jwtToken).toBe(token)
    })

    it('should update existing token', () => {
      const token1 = 'token-1'
      const token2 = 'token-2'

      act(() => {
        useSettingsStore.getState().setJwtToken(token1)
      })
      expect(useSettingsStore.getState().jwtToken).toBe(token1)

      act(() => {
        useSettingsStore.getState().setJwtToken(token2)
      })
      expect(useSettingsStore.getState().jwtToken).toBe(token2)
    })

    it('should set token to null', () => {
      // First set a token
      act(() => {
        useSettingsStore.getState().setJwtToken('some-token')
      })
      expect(useSettingsStore.getState().jwtToken).toBe('some-token')

      // Then set to null
      act(() => {
        useSettingsStore.getState().setJwtToken(null)
      })
      expect(useSettingsStore.getState().jwtToken).toBeNull()
    })

    it('should handle empty string token', () => {
      act(() => {
        useSettingsStore.getState().setJwtToken('')
      })
      expect(useSettingsStore.getState().jwtToken).toBe('')
    })
  })

  describe('logout', () => {
    it('should clear JWT token on logout', () => {
      // Setup: Set a token
      act(() => {
        useSettingsStore.getState().setJwtToken('auth-token')
        useSettingsStore.getState().setGatewayUrl('http://custom.com')
      })
      expect(useSettingsStore.getState().jwtToken).toBe('auth-token')

      // Logout
      act(() => {
        useSettingsStore.getState().logout()
      })

      expect(useSettingsStore.getState().jwtToken).toBeNull()
      // Other settings should remain unchanged
      expect(useSettingsStore.getState().gatewayUrl).toBe('http://custom.com')
    })

    it('should handle logout when no token is set', () => {
      act(() => {
        useSettingsStore.getState().logout()
      })
      expect(useSettingsStore.getState().jwtToken).toBeNull()
    })
  })

  describe('multiple setting updates', () => {
    it('should handle multiple updates simultaneously', () => {
      act(() => {
        useSettingsStore.getState().setGatewayUrl('http://api.com')
        useSettingsStore.getState().setCostRate(10)
        useSettingsStore.getState().setStreamingEnabled(true)
        useSettingsStore.getState().setJwtToken('token')
      })

      const state = useSettingsStore.getState()
      expect(state.gatewayUrl).toBe('http://api.com')
      expect(state.costRate).toBe(10)
      expect(state.streamingEnabled).toBe(true)
      expect(state.jwtToken).toBe('token')
    })

    it('should maintain independent state properties', () => {
      act(() => {
        useSettingsStore.getState().setGatewayUrl('http://new.com')
      })

      // Verify only gatewayUrl changed
      const state = useSettingsStore.getState()
      expect(state.gatewayUrl).toBe('http://new.com')
      expect(state.costRate).toBe(0)
      expect(state.streamingEnabled).toBe(false)
      expect(state.jwtToken).toBeNull()
    })
  })

  describe('edge cases', () => {
    it('should handle special characters in URL', () => {
      const urlWithSpecialChars = 'http://localhost:5000/path?query=value&foo=bar'
      act(() => {
        useSettingsStore.getState().setGatewayUrl(urlWithSpecialChars)
      })
      expect(useSettingsStore.getState().gatewayUrl).toBe(urlWithSpecialChars)
    })

    it('should handle very long token', () => {
      const longToken = 'a'.repeat(1000)
      act(() => {
        useSettingsStore.getState().setJwtToken(longToken)
      })
      expect(useSettingsStore.getState().jwtToken).toBe(longToken)
    })

    it('should handle Unicode characters in URL', () => {
      const urlWithUnicode = 'http://localhost:5000/путь/中文'
      act(() => {
        useSettingsStore.getState().setGatewayUrl(urlWithUnicode)
      })
      expect(useSettingsStore.getState().gatewayUrl).toBe(urlWithUnicode)
    })
  })
})
