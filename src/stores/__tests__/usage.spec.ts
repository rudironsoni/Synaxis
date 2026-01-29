import { describe, it, expect, beforeEach, vi } from 'vitest'

vi.mock('@/db/db', ()=> import('../../test/mocks/dbMock'))
import useUsageStore from '../../stores/usage'

describe('usage store init and add', ()=>{
  beforeEach(()=>{
    useUsageStore.setState({ totalTokens: 0 })
  })

  it('addUsage increases totalTokens', ()=>{
    useUsageStore.getState().addUsage(12)
    expect(useUsageStore.getState().totalTokens).toBe(12)
  })

  it('init aggregates existing messages', async ()=>{
    const db = (await import('@/db/db')).default
    // ensure messages have tokenUsage
    await db.messages.add({ sessionId:1, role:'assistant', content:'x', createdAt: new Date(), tokenUsage: { prompt:2, completion:3, total:5 } })
    // re-run init by importing module anew
    const mod = await import('../../stores/usage')
    // give async init a tick
    await new Promise(res=>setTimeout(res,10))
    expect(mod.default.getState().totalTokens).toBeGreaterThanOrEqual(5)
  })
})
