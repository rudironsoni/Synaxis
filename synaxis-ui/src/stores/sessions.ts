import create from 'zustand'
import { devtools } from 'zustand/middleware'
import db, { type Session } from '../db/db'

type SessionsState = {
  sessions: Session[]
  loading: boolean
  loadSessions: () => Promise<void>
  createSession: (title: string) => Promise<Session>
  deleteSession: (id: number) => Promise<void>
}

export const useSessionsStore = create<SessionsState>()(
  devtools((set, get) => ({
    sessions: [],
    loading: false,
    loadSessions: async () => {
      set({ loading: true })
      const list = await db.sessions.toArray()
      set({ sessions: list, loading: false })
    },
    createSession: async (title: string) => {
      const now = new Date()
      const id = await db.sessions.add({ title, createdAt: now, updatedAt: now })
      const session = { id, title, createdAt: now, updatedAt: now }
      set({ sessions: [...get().sessions, session] })
      return session
    },
    deleteSession: async (id: number) => {
      await db.sessions.delete(id)
      set({ sessions: get().sessions.filter((s) => s.id !== id) })
    },
  }))
)

export default useSessionsStore
