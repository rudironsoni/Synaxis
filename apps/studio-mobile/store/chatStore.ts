import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import { Conversation, Message, ChatSettings } from '@/types';

interface ChatStore {
  conversations: Conversation[];
  currentConversation: Conversation | null;
  settings: ChatSettings;
  isStreaming: boolean;

  // Conversation actions
  setCurrentConversation: (conversation: Conversation | null) => void;
  createConversation: (title?: string) => Conversation;
  updateConversation: (id: string, updates: Partial<Conversation>) => void;
  deleteConversation: (id: string) => void;
  pinConversation: (id: string) => void;

  // Message actions
  addMessage: (conversationId: string, message: Message) => void;
  updateMessage: (conversationId: string, messageId: string, updates: Partial<Message>) => void;
  deleteMessage: (conversationId: string, messageId: string) => void;

  // Settings actions
  updateSettings: (updates: Partial<ChatSettings>) => void;

  // Streaming actions
  setStreaming: (isStreaming: boolean) => void;
}

const defaultSettings: ChatSettings = {
  apiKey: '',
  apiUrl: 'https://api.synaxis.ai/v1',
  model: 'gpt-4',
  temperature: 0.7,
  maxTokens: 2048,
  theme: 'system',
  biometricAuth: false,
  hapticFeedback: true,
};

export const useChatStore = create<ChatStore>()(
  persist(
    (set, get) => ({
      conversations: [],
      currentConversation: null,
      settings: defaultSettings,
      isStreaming: false,

      setCurrentConversation: (conversation) => set({ currentConversation: conversation }),

      createConversation: (title = 'New Chat') => {
        const newConversation: Conversation = {
          id: Date.now().toString(),
          title,
          messages: [],
          createdAt: new Date(),
          updatedAt: new Date(),
        };
        set((state) => ({
          conversations: [newConversation, ...state.conversations],
          currentConversation: newConversation,
        }));
        return newConversation;
      },

      updateConversation: (id, updates) =>
        set((state) => ({
          conversations: state.conversations.map((conv) =>
            conv.id === id ? { ...conv, ...updates, updatedAt: new Date() } : conv
          ),
          currentConversation:
            state.currentConversation?.id === id
              ? { ...state.currentConversation, ...updates, updatedAt: new Date() }
              : state.currentConversation,
        })),

      deleteConversation: (id) =>
        set((state) => ({
          conversations: state.conversations.filter((conv) => conv.id !== id),
          currentConversation:
            state.currentConversation?.id === id ? null : state.currentConversation,
        })),

      pinConversation: (id) =>
        set((state) => ({
          conversations: state.conversations.map((conv) =>
            conv.id === id ? { ...conv, isPinned: !conv.isPinned } : conv
          ),
        })),

      addMessage: (conversationId, message) =>
        set((state) => ({
          conversations: state.conversations.map((conv) =>
            conv.id === conversationId
              ? {
                  ...conv,
                  messages: [...conv.messages, message],
                  updatedAt: new Date(),
                }
              : conv
          ),
          currentConversation:
            state.currentConversation?.id === conversationId
              ? {
                  ...state.currentConversation,
                  messages: [...state.currentConversation.messages, message],
                  updatedAt: new Date(),
                }
              : state.currentConversation,
        })),

      updateMessage: (conversationId, messageId, updates) =>
        set((state) => ({
          conversations: state.conversations.map((conv) =>
            conv.id === conversationId
              ? {
                  ...conv,
                  messages: conv.messages.map((msg) =>
                    msg.id === messageId ? { ...msg, ...updates } : msg
                  ),
                }
              : conv
          ),
          currentConversation:
            state.currentConversation?.id === conversationId
              ? {
                  ...state.currentConversation,
                  messages: state.currentConversation.messages.map((msg) =>
                    msg.id === messageId ? { ...msg, ...updates } : msg
                  ),
                }
              : state.currentConversation,
        })),

      deleteMessage: (conversationId, messageId) =>
        set((state) => ({
          conversations: state.conversations.map((conv) =>
            conv.id === conversationId
              ? {
                  ...conv,
                  messages: conv.messages.filter((msg) => msg.id !== messageId),
                }
              : conv
          ),
          currentConversation:
            state.currentConversation?.id === conversationId
              ? {
                  ...state.currentConversation,
                  messages: state.currentConversation.messages.filter(
                    (msg) => msg.id !== messageId
                  ),
                }
              : state.currentConversation,
        })),

      updateSettings: (updates) =>
        set((state) => ({
          settings: { ...state.settings, ...updates },
        })),

      setStreaming: (isStreaming) => set({ isStreaming }),
    }),
    {
      name: 'synaxis-chat-storage',
      partialize: (state) => ({
        conversations: state.conversations,
        settings: state.settings,
      }),
    }
  )
);
