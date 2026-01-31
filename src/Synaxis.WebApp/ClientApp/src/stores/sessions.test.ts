import { describe, it, expect, beforeEach, vi } from 'vitest'
import { act } from 'react'
import useSessionsStore from './sessions'

// Mock the db module
vi.mock('../db/db', () => ({
  default: {
    sessions: {
      toArray: vi.fn(),
      add: vi.fn(),
      delete: vi.fn(),
    },
  },
}))

// Import mocked db after mocking
import db from '../db/db'

describe('sessions store', () => {
  beforeEach(() => {
    // Reset store state
    useSessionsStore.setState({ sessions: [], loading: false })
    // Clear mock calls
    vi.clearAllMocks()
  })

  describe('initialization', () => {
    it('should have empty sessions array initially', () => {
      expect(useSessionsStore.getState().sessions).toEqual([])
    })

    it('should have loading set to false initially', () => {
      expect(useSessionsStore.getState().loading).toBe(false)
    })
  })

  describe('loadSessions', () => {
    it('should load sessions from database', async () => {
      const mockSessions = [
        { id: 1, title: 'Session 1', createdAt: new Date(), updatedAt: new Date() },
        { id: 2, title: 'Session 2', createdAt: new Date(), updatedAt: new Date() },
      ]
      vi.mocked(db.sessions.toArray).mockResolvedValue(mockSessions)

      await act(async () => {
        await useSessionsStore.getState().loadSessions()
      })

      expect(useSessionsStore.getState().sessions).toEqual(mockSessions)
      expect(useSessionsStore.getState().loading).toBe(false)
    })

    it('should set loading to true while loading', async () => {
      vi.mocked(db.sessions.toArray).mockImplementation(() => new Promise(resolve => {
        // Check loading state before resolving
        expect(useSessionsStore.getState().loading).toBe(true)
        resolve([])
      }))

      await act(async () => {
        await useSessionsStore.getState().loadSessions()
      })
    })

    it('should handle empty database result', async () => {
      vi.mocked(db.sessions.toArray).mockResolvedValue([])

      await act(async () => {
        await useSessionsStore.getState().loadSessions()
      })

      expect(useSessionsStore.getState().sessions).toEqual([])
      expect(useSessionsStore.getState().loading).toBe(false)
    })

    it('should handle database errors gracefully', async () => {
      vi.mocked(db.sessions.toArray).mockRejectedValue(new Error('DB Error'))

      await act(async () => {
        await expect(useSessionsStore.getState().loadSessions()).rejects.toThrow('DB Error')
      })
    })
  })

  describe('createSession', () => {
    it('should create a new session and add to store', async () => {
      const newSessionId = 1
      vi.mocked(db.sessions.add).mockResolvedValue(newSessionId)

      let createdSession: any
      await act(async () => {
        createdSession = await useSessionsStore.getState().createSession('New Session')
      })

      expect(createdSession).toMatchObject({
        id: newSessionId,
        title: 'New Session',
      })
      expect(createdSession.createdAt).toBeInstanceOf(Date)
      expect(createdSession.updatedAt).toBeInstanceOf(Date)
      expect(useSessionsStore.getState().sessions).toHaveLength(1)
      expect(useSessionsStore.getState().sessions[0].title).toBe('New Session')
    })

    it('should add multiple sessions', async () => {
      vi.mocked(db.sessions.add)
        .mockResolvedValueOnce(1)
        .mockResolvedValueOnce(2)

      await act(async () => {
        await useSessionsStore.getState().createSession('Session 1')
        await useSessionsStore.getState().createSession('Session 2')
      })

      expect(useSessionsStore.getState().sessions).toHaveLength(2)
      expect(useSessionsStore.getState().sessions[0].title).toBe('Session 1')
      expect(useSessionsStore.getState().sessions[1].title).toBe('Session 2')
    })

    it('should handle empty title', async () => {
      const newSessionId = 1
      vi.mocked(db.sessions.add).mockResolvedValue(newSessionId)

      await act(async () => {
        await useSessionsStore.getState().createSession('')
      })

      expect(useSessionsStore.getState().sessions[0].title).toBe('')
    })

    it('should handle database errors during creation', async () => {
      vi.mocked(db.sessions.add).mockRejectedValue(new Error('DB Error'))

      await act(async () => {
        await expect(useSessionsStore.getState().createSession('Test')).rejects.toThrow('DB Error')
      })

      // Session should not be added to store on error
      expect(useSessionsStore.getState().sessions).toHaveLength(0)
    })
  })

  describe('deleteSession', () => {
    it('should delete a session from store', async () => {
      // Setup: Add a session first
      const sessionId = 1
      vi.mocked(db.sessions.add).mockResolvedValue(sessionId)
      vi.mocked(db.sessions.delete).mockResolvedValue(undefined)

      await act(async () => {
        await useSessionsStore.getState().createSession('Session to Delete')
      })

      expect(useSessionsStore.getState().sessions).toHaveLength(1)

      await act(async () => {
        await useSessionsStore.getState().deleteSession(sessionId)
      })

      expect(useSessionsStore.getState().sessions).toHaveLength(0)
      expect(db.sessions.delete).toHaveBeenCalledWith(sessionId)
    })

    it('should delete specific session by id', async () => {
      vi.mocked(db.sessions.add)
        .mockResolvedValueOnce(1)
        .mockResolvedValueOnce(2)
      vi.mocked(db.sessions.delete).mockResolvedValue(undefined)

      await act(async () => {
        await useSessionsStore.getState().createSession('Session 1')
        await useSessionsStore.getState().createSession('Session 2')
      })

      await act(async () => {
        await useSessionsStore.getState().deleteSession(1)
      })

      expect(useSessionsStore.getState().sessions).toHaveLength(1)
      expect(useSessionsStore.getState().sessions[0].id).toBe(2)
      expect(useSessionsStore.getState().sessions[0].title).toBe('Session 2')
    })

    it('should handle deleting non-existent session gracefully', async () => {
      vi.mocked(db.sessions.delete).mockResolvedValue(undefined)

      // Add a session first
      vi.mocked(db.sessions.add).mockResolvedValueOnce(1)
      await act(async () => {
        await useSessionsStore.getState().createSession('Session 1')
      })

      // Try to delete a different id
      await act(async () => {
        await useSessionsStore.getState().deleteSession(999)
      })

      // Original session should remain
      expect(useSessionsStore.getState().sessions).toHaveLength(1)
    })

    it('should handle database errors during deletion', async () => {
      vi.mocked(db.sessions.add).mockResolvedValueOnce(1)
      vi.mocked(db.sessions.delete).mockRejectedValue(new Error('DB Error'))

      await act(async () => {
        await useSessionsStore.getState().createSession('Session 1')
      })

      await act(async () => {
        await expect(useSessionsStore.getState().deleteSession(1)).rejects.toThrow('DB Error')
      })

      // Session should remain in store on error
      expect(useSessionsStore.getState().sessions).toHaveLength(1)
    })
  })

  describe('state reactivity', () => {
    it('should maintain state independence between tests', () => {
      useSessionsStore.setState({ sessions: [{ id: 1, title: 'Test', createdAt: new Date(), updatedAt: new Date() }] })
      expect(useSessionsStore.getState().sessions).toHaveLength(1)
    })

    it('should reset state correctly', () => {
      useSessionsStore.setState({ sessions: [{ id: 1, title: 'Test', createdAt: new Date(), updatedAt: new Date() }], loading: true })
      useSessionsStore.setState({ sessions: [], loading: false })
      expect(useSessionsStore.getState().sessions).toEqual([])
      expect(useSessionsStore.getState().loading).toBe(false)
    })
  })
})
