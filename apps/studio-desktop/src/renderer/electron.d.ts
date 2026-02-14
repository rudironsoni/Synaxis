export interface ElectronAPI {
  getSettings: () => Promise<any>
  saveSettings: (settings: any) => Promise<{ success: boolean }>
  getConversations: () => Promise<any[]>
  saveConversation: (conversation: any) => Promise<{ success: boolean }>
  deleteConversation: (id: string) => Promise<{ success: boolean }>
  showNotification: (options: { title: string; body: string }) => Promise<void>
  onCommandPalette: (callback: () => void) => void
  onNewConversation: (callback: () => void) => void
  onOpenSettings: (callback: () => void) => void
  onToggleSidebar: (callback: () => void) => void
  onToggleTheme: (callback: () => void) => void
  onUpdateAvailable: (callback: () => void) => void
  onUpdateDownloaded: (callback: () => void) => void
  installUpdate: () => void
}

declare global {
  interface Window {
    electronAPI: ElectronAPI
  }
}

export {}
