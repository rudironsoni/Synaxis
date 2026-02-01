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

      let createdSession: { id: number; title: string; createdAt: Date; updatedAt: Date } | undefined
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

  describe('edge cases', () => {
    it('should handle special characters in session title', async () => {
      const specialTitle = 'Session with <script>alert("xss")</script> & special chars: @#$%'
      const sessionId = 1
      vi.mocked(db.sessions.add).mockResolvedValue(sessionId)

      await act(async () => {
        await useSessionsStore.getState().createSession(specialTitle)
      })

      expect(useSessionsStore.getState().sessions[0].title).toBe(specialTitle)
    })

    it('should handle very long session title', async () => {
      const longTitle = 'A'.repeat(10000)
      const sessionId = 1
      vi.mocked(db.sessions.add).mockResolvedValue(sessionId)

      await act(async () => {
        await useSessionsStore.getState().createSession(longTitle)
      })

      expect(useSessionsStore.getState().sessions[0].title).toBe(longTitle)
    })

    it('should handle Unicode characters in session title', async () => {
      const unicodeTitle = 'Session with 中文, 한국어, 日本語, and emojis 🎉🚀'
      const sessionId = 1
      vi.mocked(db.sessions.add).mockResolvedValue(sessionId)

      await act(async () => {
        await useSessionsStore.getState().createSession(unicodeTitle)
      })

      expect(useSessionsStore.getState().sessions[0].title).toBe(unicodeTitle)
    })

    it('should handle rapid successive create operations', async () => {
      vi.mocked(db.sessions.add)
        .mockResolvedValueOnce(1)
        .mockResolvedValueOnce(2)
        .mockResolvedValueOnce(3)
        .mockResolvedValueOnce(4)
        .mockResolvedValueOnce(5)

      await act(async () => {
        await Promise.all([
          useSessionsStore.getState().createSession('Session 1'),
          useSessionsStore.getState().createSession('Session 2'),
          useSessionsStore.getState().createSession('Session 3'),
          useSessionsStore.getState().createSession('Session 4'),
          useSessionsStore.getState().createSession('Session 5'),
        ])
      })

      expect(useSessionsStore.getState().sessions).toHaveLength(5)
    })

     it('should handle session with null/undefined title gracefully', async () => {
       const sessionId = 1
       vi.mocked(db.sessions.add).mockResolvedValue(sessionId)

       await act(async () => {
         await useSessionsStore.getState().createSession(null as unknown as string)
       })

      expect(useSessionsStore.getState().sessions[0].title).toBeNull()
    })

    it('should handle session with whitespace-only title', async () => {
      const whitespaceTitle = '   '
      const sessionId = 1
      vi.mocked(db.sessions.add).mockResolvedValue(sessionId)

      await act(async () => {
        await useSessionsStore.getState().createSession(whitespaceTitle)
      })

      expect(useSessionsStore.getState().sessions[0].title).toBe(whitespaceTitle)
    })

    it('should handle deleting all sessions one by one', async () => {
      vi.mocked(db.sessions.add)
        .mockResolvedValueOnce(1)
        .mockResolvedValueOnce(2)
        .mockResolvedValueOnce(3)
      vi.mocked(db.sessions.delete).mockResolvedValue(undefined)

      await act(async () => {
        await useSessionsStore.getState().createSession('Session 1')
        await useSessionsStore.getState().createSession('Session 2')
        await useSessionsStore.getState().createSession('Session 3')
      })

      expect(useSessionsStore.getState().sessions).toHaveLength(3)

      await act(async () => {
        await useSessionsStore.getState().deleteSession(1)
        await useSessionsStore.getState().deleteSession(2)
        await useSessionsStore.getState().deleteSession(3)
      })

      expect(useSessionsStore.getState().sessions).toHaveLength(0)
    })

    it('should handle loadSessions with very large dataset', async () => {
      const largeDataset = Array.from({ length: 1000 }, (_, i) => ({
        id: i + 1,
        title: `Session ${i + 1}`,
        createdAt: new Date(),
        updatedAt: new Date(),
      }))
      vi.mocked(db.sessions.toArray).mockResolvedValue(largeDataset)

      await act(async () => {
        await useSessionsStore.getState().loadSessions()
      })

      expect(useSessionsStore.getState().sessions).toHaveLength(1000)
    })

    it('should maintain session order after multiple operations', async () => {
      vi.mocked(db.sessions.add)
        .mockResolvedValueOnce(1)
        .mockResolvedValueOnce(2)
        .mockResolvedValueOnce(3)
      vi.mocked(db.sessions.delete).mockResolvedValue(undefined)

      await act(async () => {
        await useSessionsStore.getState().createSession('First')
        await useSessionsStore.getState().createSession('Second')
        await useSessionsStore.getState().createSession('Third')
      })

      expect(useSessionsStore.getState().sessions[0].title).toBe('First')
      expect(useSessionsStore.getState().sessions[1].title).toBe('Second')
      expect(useSessionsStore.getState().sessions[2].title).toBe('Third')

      await act(async () => {
        await useSessionsStore.getState().deleteSession(2)
      })

      expect(useSessionsStore.getState().sessions[0].title).toBe('First')
      expect(useSessionsStore.getState().sessions[1].title).toBe('Third')
    })

    it('should handle concurrent load and create operations', async () => {
      const mockSessions = [
        { id: 1, title: 'Existing Session', createdAt: new Date(), updatedAt: new Date() },
      ]
      vi.mocked(db.sessions.toArray).mockResolvedValue(mockSessions)
      vi.mocked(db.sessions.add).mockResolvedValue(2)

      await act(async () => {
        await Promise.all([
          useSessionsStore.getState().loadSessions(),
          useSessionsStore.getState().createSession('New Session'),
        ])
      })

      expect(useSessionsStore.getState().sessions).toHaveLength(2)
    })
  })
})
