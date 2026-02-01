import { create } from 'zustand'
import { persist } from 'zustand/middleware'

export type SettingsState = {
  gatewayUrl: string
  costRate: number
  streamingEnabled: boolean
  jwtToken: string | null
  setGatewayUrl: (url: string) => void
  setCostRate: (r: number) => void
  setStreamingEnabled: (enabled: boolean) => void
  setJwtToken: (token: string | null) => void
  logout: () => void
}

export const useSettingsStore = create<SettingsState>()(
  persist(
    (set) => ({
      gatewayUrl: 'http://localhost:5000',
      costRate: 0,
      streamingEnabled: false,
      jwtToken: null,
      setGatewayUrl: (url: string) => set({ gatewayUrl: url }),
      setCostRate: (r: number) => set({ costRate: r }),
      setStreamingEnabled: (enabled: boolean) => set({ streamingEnabled: enabled }),
      setJwtToken: (token: string | null) => set({ jwtToken: token }),
      logout: () => set({ jwtToken: null }),
    }),
    { name: 'synaxis-settings' }
  )
)

export default useSettingsStore
