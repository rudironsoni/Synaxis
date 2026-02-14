import { create } from 'zustand';

export interface Message {
  id: string;
  role: 'user' | 'assistant' | 'system';
  content: string;
  timestamp: Date;
}

export interface Conversation {
  id: string;
  title: string;
  messages: Message[];
  createdAt: Date;
  updatedAt: Date;
}

export interface ModelConfig {
  model: string;
  temperature: number;
  maxTokens: number;
}

export interface Settings {
  apiKey: string;
  theme: 'light' | 'dark';
  defaultModel: string;
}

interface ChatStore {
  conversations: Conversation[];
  currentConversationId: string | null;
  settings: Settings;
  modelConfig: ModelConfig;
  isAuthenticated: boolean;

  setCurrentConversationId: (id: string | null) => void;
  createConversation: () => void;
  deleteConversation: (id: string) => void;
  addMessage: (conversationId: string, message: Omit<Message, 'id' | 'timestamp'>) => void;
  updateConversationTitle: (id: string, title: string) => void;
  setSettings: (settings: Partial<Settings>) => void;
  setModelConfig: (config: Partial<ModelConfig>) => void;
  setAuthenticated: (authenticated: boolean) => void;
}

const defaultSettings: Settings = {
  apiKey: '',
  theme: 'light',
  defaultModel: 'gpt-4',
};

const defaultModelConfig: ModelConfig = {
  model: 'gpt-4',
  temperature: 0.7,
  maxTokens: 2048,
};

export const useChatStore = create<ChatStore>((set) => ({
  conversations: [],
  currentConversationId: null,
  settings: defaultSettings,
  modelConfig: defaultModelConfig,
  isAuthenticated: false,

  setCurrentConversationId: (id) => set({ currentConversationId: id }),

  createConversation: () => set((state) => {
    const newConversation: Conversation = {
      id: Date.now().toString(),
      title: 'New Conversation',
      messages: [],
      createdAt: new Date(),
      updatedAt: new Date(),
    };
    return {
      conversations: [newConversation, ...state.conversations],
      currentConversationId: newConversation.id,
    };
  }),

  deleteConversation: (id) => set((state) => ({
    conversations: state.conversations.filter((c) => c.id !== id),
    currentConversationId: state.currentConversationId === id ? null : state.currentConversationId,
  })),

  addMessage: (conversationId, message) => set((state) => ({
    conversations: state.conversations.map((conv) => {
      if (conv.id === conversationId) {
        const newMessage: Message = {
          ...message,
          id: Date.now().toString(),
          timestamp: new Date(),
        };
        const updatedMessages = [...conv.messages, newMessage];
        return {
          ...conv,
          messages: updatedMessages,
          title: conv.messages.length === 0 && message.role === 'user'
            ? message.content.slice(0, 50) + (message.content.length > 50 ? '...' : '')
            : conv.title,
          updatedAt: new Date(),
        };
      }
      return conv;
    }),
  })),

  updateConversationTitle: (id, title) => set((state) => ({
    conversations: state.conversations.map((conv) =>
      conv.id === id ? { ...conv, title, updatedAt: new Date() } : conv
    ),
  })),

  setSettings: (newSettings) => set((state) => ({
    settings: { ...state.settings, ...newSettings },
  })),

  setModelConfig: (newConfig) => set((state) => ({
    modelConfig: { ...state.modelConfig, ...newConfig },
  })),

  setAuthenticated: (authenticated) => set({ isAuthenticated: authenticated }),
}));
