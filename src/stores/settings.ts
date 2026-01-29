import { create } from 'zustand'

type SettingsState = {
  gatewayUrl: string
  costRate: number
  setGatewayUrl: (u:string)=>void
  setCostRate: (n:number)=>void
}

const useSettingsStore = create<SettingsState>((set)=>({
  gatewayUrl: 'http://localhost:3000',
  costRate: 0.02,
  setGatewayUrl: (u:string)=>set({ gatewayUrl: u }),
  setCostRate: (n:number)=>set({ costRate: n }),
}))

export default useSettingsStore
