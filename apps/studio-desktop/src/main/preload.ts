import { contextBridge, ipcRenderer } from 'electron'

contextBridge.exposeInMainWorld('electronAPI', {
  getSettings: () => ipcRenderer.invoke('get-settings'),
  saveSettings: (settings: any) => ipcRenderer.invoke('save-settings', settings),
  getConversations: () => ipcRenderer.invoke('get-conversations'),
  saveConversation: (conversation: any) => ipcRenderer.invoke('save-conversation', conversation),
  deleteConversation: (id: string) => ipcRenderer.invoke('delete-conversation', id),
  showNotification: (options: { title: string; body: string }) => ipcRenderer.invoke('show-notification', options),
  onCommandPalette: (callback: () => void) => ipcRenderer.on('command-palette', callback),
  onNewConversation: (callback: () => void) => ipcRenderer.on('new-conversation', callback),
  onOpenSettings: (callback: () => void) => ipcRenderer.on('open-settings', callback),
  onToggleSidebar: (callback: () => void) => ipcRenderer.on('toggle-sidebar', callback),
  onToggleTheme: (callback: () => void) => ipcRenderer.on('toggle-theme', callback),
  onUpdateAvailable: (callback: () => void) => ipcRenderer.on('update-available', callback),
  onUpdateDownloaded: (callback: () => void) => ipcRenderer.on('update-downloaded', callback),
  installUpdate: () => ipcRenderer.send('install-update')
})
