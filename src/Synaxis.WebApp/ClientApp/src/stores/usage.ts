import { create } from 'zustand'
import db from '@/db/db'

type UsageState = {
  totalTokens: number
  addUsage: (tokens: number) => void
}

export const useUsageStore = create<UsageState>((set) => ({
  totalTokens: 0,
  addUsage: (tokens: number) => set((s) => ({ totalTokens: s.totalTokens + tokens })),
}))

;(async function init(){
  try{
    const list = await db.messages.toArray()
    const total = list.reduce((acc,m)=>acc + (m.tokenUsage?.total || 0),0)
    useUsageStore.setState({ totalTokens: total })
  }catch(e){
    console.warn('usage init failed', e)
  }
})()

export default useUsageStore
