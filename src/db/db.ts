import Dexie from 'dexie'

export type TokenUsage = { prompt: number; completion: number; total: number }

export type Message = {
  id?: number
  sessionId: number
  role: 'user'|'assistant'|'system'
  content: string
  createdAt: Date
  tokenUsage?: TokenUsage
  model?: string
}

export type Session = {
  id?: number
  title: string
  createdAt: Date
  updatedAt: Date
}

class SynaxisDB extends Dexie {
  sessions!: Dexie.Table<Session, number>
  messages!: Dexie.Table<Message, number>

  constructor(){
    super('synaxis')
    // v1 schema kept for compatibility, v2 adds explicit indexes for role and sessionId
    this.version(1).stores({ sessions: '++id,updatedAt', messages: '++id,sessionId' })
    this.version(2).stores({ sessions: '++id,updatedAt', messages: '++id,sessionId,role' })

    this.sessions = this.table('sessions')
    this.messages = this.table('messages')
  }
}

const db = new SynaxisDB()

export default db
