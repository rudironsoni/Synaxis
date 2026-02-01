import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { useSessionsStore, type SessionsState } from '@/stores/sessions'
import SessionList from './SessionList'

// Mock the sessions store module
vi.mock('@/stores/sessions', () => ({
  useSessionsStore: vi.fn(),
}))

describe('SessionList', () => {
  const mockLoadSessions = vi.fn()
  const mockCreateSession = vi.fn()
  const mockDeleteSession = vi.fn()

  beforeEach(() => {
    vi.clearAllMocks()
    
    // Reset mock implementation
    vi.mocked(useSessionsStore).mockReturnValue({
      sessions: [],
      loadSessions: mockLoadSessions,
      createSession: mockCreateSession,
      deleteSession: mockDeleteSession,
    } as SessionsState)
  })

  describe('rendering', () => {
    it('renders the chats header', () => {
      render(<SessionList />)
      expect(screen.getByText('Chats')).toBeInTheDocument()
    })

    it('renders new chat button', () => {
      render(<SessionList />)
      expect(screen.getByLabelText('New chat')).toBeInTheDocument()
    })

    it('renders empty list when no sessions', () => {
      render(<SessionList />)
      const sessionItems = screen.queryAllByText(/Session/)
      expect(sessionItems).toHaveLength(0)
    })

     it('renders sessions from store', () => {
       vi.mocked(useSessionsStore).mockReturnValue({
         sessions: [
           { id: 1, title: 'Session 1' },
           { id: 2, title: 'Session 2' },
         ],
         loadSessions: mockLoadSessions,
         createSession: mockCreateSession,
         deleteSession: mockDeleteSession,
       } as SessionsState)
      
      render(<SessionList />)
      expect(screen.getByText('Session 1')).toBeInTheDocument()
      expect(screen.getByText('Session 2')).toBeInTheDocument()
    })

     it('renders multiple sessions', () => {
       vi.mocked(useSessionsStore).mockReturnValue({
         sessions: [
           { id: 1, title: 'Chat A' },
           { id: 2, title: 'Chat B' },
           { id: 3, title: 'Chat C' },
         ],
         loadSessions: mockLoadSessions,
         createSession: mockCreateSession,
         deleteSession: mockDeleteSession,
       } as SessionsState)
      
      render(<SessionList />)
      expect(screen.getByText('Chat A')).toBeInTheDocument()
      expect(screen.getByText('Chat B')).toBeInTheDocument()
      expect(screen.getByText('Chat C')).toBeInTheDocument()
    })
  })

  describe('loading sessions', () => {
    it('calls loadSessions on mount', () => {
      render(<SessionList />)
      expect(mockLoadSessions).toHaveBeenCalled()
    })
  })

  describe('creating sessions', () => {
    it('calls createSession when new chat button is clicked', async () => {
      mockCreateSession.mockResolvedValue({ id: 1, title: 'New Chat' })
      render(<SessionList />)
      
      const newChatButton = screen.getByLabelText('New chat')
      fireEvent.click(newChatButton)
      
      await waitFor(() => {
        expect(mockCreateSession).toHaveBeenCalledWith('New Chat')
      })
    })

    it('new chat button has correct aria-label', () => {
      render(<SessionList />)
      const newChatButton = screen.getByLabelText('New chat')
      expect(newChatButton).toHaveAttribute('aria-label', 'New chat')
    })

    it('new chat button has title tooltip', () => {
      render(<SessionList />)
      const newChatButton = screen.getByTitle('New chat')
      expect(newChatButton).toBeInTheDocument()
    })
  })

  describe('deleting sessions', () => {
     it('calls deleteSession when delete button is clicked', async () => {
       vi.mocked(useSessionsStore).mockReturnValue({
         sessions: [{ id: 1, title: 'Session to Delete' }],
         loadSessions: mockLoadSessions,
         createSession: mockCreateSession,
         deleteSession: mockDeleteSession,
       } as SessionsState)
      
      mockDeleteSession.mockResolvedValue(undefined)
      
      render(<SessionList />)
      
      const deleteButton = screen.getByTitle('Delete')
      fireEvent.click(deleteButton)
      
      await waitFor(() => {
        expect(mockDeleteSession).toHaveBeenCalledWith(1)
      })
    })

     it('shows delete button for each session', () => {
       vi.mocked(useSessionsStore).mockReturnValue({
         sessions: [
           { id: 1, title: 'Session 1' },
           { id: 2, title: 'Session 2' },
         ],
         loadSessions: mockLoadSessions,
         createSession: mockCreateSession,
         deleteSession: mockDeleteSession,
       } as SessionsState)
      
      render(<SessionList />)
      
      const deleteButtons = screen.getAllByTitle('Delete')
      expect(deleteButtons).toHaveLength(2)
    })

     it('delete button has correct title', () => {
       vi.mocked(useSessionsStore).mockReturnValue({
         sessions: [{ id: 1, title: 'Session 1' }],
         loadSessions: mockLoadSessions,
         createSession: mockCreateSession,
         deleteSession: mockDeleteSession,
       } as SessionsState)
      
      render(<SessionList />)
      
      const deleteButton = screen.getByTitle('Delete')
      expect(deleteButton).toHaveAttribute('title', 'Delete')
    })
  })

  describe('session display', () => {
     it('displays session title', () => {
       vi.mocked(useSessionsStore).mockReturnValue({
         sessions: [{ id: 1, title: 'My Chat' }],
         loadSessions: mockLoadSessions,
         createSession: mockCreateSession,
         deleteSession: mockDeleteSession,
       } as SessionsState)
      
      render(<SessionList />)
      expect(screen.getByText('My Chat')).toBeInTheDocument()
    })

     it('displays default session title', () => {
       vi.mocked(useSessionsStore).mockReturnValue({
         sessions: [{ id: 1, title: 'New Chat' }],
         loadSessions: mockLoadSessions,
         createSession: mockCreateSession,
         deleteSession: mockDeleteSession,
       } as SessionsState)
      
      render(<SessionList />)
      expect(screen.getByText('New Chat')).toBeInTheDocument()
    })
  })

  describe('layout and styling', () => {
     it('has scrollable container for many sessions', () => {
       vi.mocked(useSessionsStore).mockReturnValue({
         sessions: Array.from({ length: 20 }, (_, i) => ({
           id: i + 1,
           title: `Session ${i + 1}`,
         })),
         loadSessions: mockLoadSessions,
         createSession: mockCreateSession,
         deleteSession: mockDeleteSession,
       } as SessionsState)
      
      const { container } = render(<SessionList />)
      const scrollContainer = container.querySelector('.overflow-auto')
      expect(scrollContainer).toBeInTheDocument()
    })
  })
})
