import create from 'zustand'
import { persist } from 'zustand/middleware'

type SettingsState = {
  gatewayUrl: string
  costRate: number
  setGatewayUrl: (url: string) => void
  setCostRate: (r: number) => void
}

export const useSettingsStore = create<SettingsState>()(
  persist(
    (set) => ({
      gatewayUrl: 'http://localhost:5000',
      costRate: 0,
      setGatewayUrl: (url: string) => set({ gatewayUrl: url }),
      setCostRate: (r: number) => set({ costRate: r }),
    }),
    { name: 'synaxis-settings' }
  )
)

export default useSettingsStore
