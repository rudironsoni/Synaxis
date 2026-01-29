import Dexie, { type Table } from 'dexie'

export interface Session {
  id?: number
  title: string
  createdAt: Date
  updatedAt: Date
}

export interface Message {
  id?: number
  sessionId: number
  role: 'user' | 'assistant' | 'system'
  content: string
  createdAt: Date
  tokenUsage?: { prompt: number; completion: number; total: number }
  cost?: number
}

export class SynaxisDB extends Dexie {
  sessions!: Table<Session, number>
  messages!: Table<Message, number>

  constructor() {
    super('synaxisDB')
    this.version(1).stores({
      sessions: '++id, title, createdAt, updatedAt',
      messages: '++id, sessionId, role, createdAt',
    })
  }
}

export const db = new SynaxisDB()

export default db
