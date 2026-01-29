import { describe, it, expect, beforeEach, vi } from 'vitest'

vi.mock('@/db/db', ()=> import('../../test/mocks/dbMock'))

import useSessionsStore from '../../stores/sessions'

describe('sessions store async ops', ()=>{
  beforeEach(()=>{
    useSessionsStore.setState({ sessions: [] as any[] })
  })

  it('createSession adds a session and returns it', async ()=>{
    const s = await useSessionsStore.getState().createSession('Hello')
    expect(s.title).toBe('Hello')
    const list = useSessionsStore.getState().sessions
    expect(list.find((x:any)=>x.id === s.id)).toBeDefined()
  })

  it('deleteSession removes session', async ()=>{
    const s = await useSessionsStore.getState().createSession('ToDelete')
    await useSessionsStore.getState().deleteSession(s.id!)
    expect(useSessionsStore.getState().sessions.find((x:any)=>x.id === s.id)).toBeUndefined()
  })

  it('loadSessions populates from db', async ()=>{
    // add directly to mock db
    const db = (await import('@/db/db')).default
    await db.sessions.add({ title: 'FromDB', createdAt: new Date(), updatedAt: new Date() })
    await useSessionsStore.getState().loadSessions()
    expect(useSessionsStore.getState().sessions.find((s:any)=>s.title === 'FromDB')).toBeDefined()
  })
})
