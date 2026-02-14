import { useEffect } from 'react'
import { create } from 'zustand'
import { Chat } from './pages/Chat'
import { Settings } from './pages/Settings'

interface AppSettings {
  apiKey: string
  defaultModel: string
  theme: 'light' | 'dark'
  temperature: number
  topP: number
  maxTokens: number
}

interface Conversation {
  id: string
  title: string
  messages: Message[]
  createdAt: Date
  updatedAt: Date
}

interface Message {
  id: string
  role: 'user' | 'assistant' | 'system'
  content: string
  timestamp: Date
}

interface AppState {
  currentPage: 'chat' | 'settings'
  settings: AppSettings
  conversations: Conversation[]
  currentConversation: Conversation | null
  setCurrentPage: (page: 'chat' | 'settings') => void
  setSettings: (settings: AppSettings) => void
  setConversations: (conversations: Conversation[]) => void
  setCurrentConversation: (conversation: Conversation | null) => void
  addMessage: (message: Message) => void
  createNewConversation: () => void
}

const useStore = create<AppState>((set) => ({
  currentPage: 'chat',
  settings: {
    apiKey: '',
    defaultModel: 'gpt-4',
    theme: 'dark',
    temperature: 0.7,
    topP: 0.9,
    maxTokens: 2048
  },
  conversations: [],
  currentConversation: null,
  setCurrentPage: (page) => set({ currentPage: page }),
  setSettings: (settings) => set({ settings }),
  setConversations: (conversations) => set({ conversations }),
  setCurrentConversation: (conversation) => set({ currentConversation: conversation }),
  addMessage: (message) => set((state) => {
    if (!state.currentConversation) return state
    const updatedConversation = {
      ...state.currentConversation,
      messages: [...state.currentConversation.messages, message],
      updatedAt: new Date()
    }
    const updatedConversations = state.conversations.map(c =>
      c.id === updatedConversation.id ? updatedConversation : c
    )
    return {
      currentConversation: updatedConversation,
      conversations: updatedConversations
    }
  }),
  createNewConversation: () => set((state) => {
    const newConversation: Conversation = {
      id: Date.now().toString(),
      title: 'New Conversation',
      messages: [],
      createdAt: new Date(),
      updatedAt: new Date()
    }
    return {
      currentConversation: newConversation,
      conversations: [newConversation, ...state.conversations]
    }
  })
}))

function App() {
  const { currentPage, setCurrentPage, settings, setSettings, setConversations, createNewConversation } = useStore()

  useEffect(() => {
    // Load settings from Electron
    window.electronAPI.getSettings().then((loadedSettings: AppSettings) => {
      setSettings(loadedSettings)
    })

    // Load conversations from Electron
    window.electronAPI.getConversations().then((loadedConversations: Conversation[]) => {
      setConversations(loadedConversations)
      if (loadedConversations.length > 0) {
        useStore.getState().setCurrentConversation(loadedConversations[0])
      }
    })

    // Listen for keyboard shortcuts
    window.electronAPI.onCommandPalette(() => {
      console.log('Command palette triggered')
    })

    window.electronAPI.onNewConversation(() => {
      createNewConversation()
    })

    window.electronAPI.onOpenSettings(() => {
      setCurrentPage('settings')
    })

    window.electronAPI.onToggleTheme(() => {
      setSettings({
        ...settings,
        theme: settings.theme === 'light' ? 'dark' : 'light'
      })
    })

    // Auto-updater notifications
    window.electronAPI.onUpdateAvailable(() => {
      window.electronAPI.showNotification({
        title: 'Update Available',
        body: 'A new version is available. Downloading...'
      })
    })

    window.electronAPI.onUpdateDownloaded(() => {
      window.electronAPI.showNotification({
        title: 'Update Ready',
        body: 'Restart to install the update'
      })
    })
  }, [])

  useEffect(() => {
    // Save settings when they change
    window.electronAPI.saveSettings(settings)
  }, [settings])

  return (
    <div style={{
      width: '100vw',
      height: '100vh',
      backgroundColor: settings.theme === 'dark' ? '#1a1a1a' : '#ffffff',
      color: settings.theme === 'dark' ? '#ffffff' : '#000000'
    }}>
      {currentPage === 'chat' && <Chat />}
      {currentPage === 'settings' && <Settings />}
    </div>
  )
}

export default App
export { useStore }
export type { AppSettings, Conversation, Message }
