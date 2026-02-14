export interface Message {
  id: string;
  role: 'user' | 'assistant' | 'system';
  content: string;
  timestamp: Date;
  isStreaming?: boolean;
}

export interface Conversation {
  id: string;
  title: string;
  messages: Message[];
  createdAt: Date;
  updatedAt: Date;
  isPinned?: boolean;
  model?: string;
}

export interface ChatSettings {
  apiKey: string;
  apiUrl: string;
  model: string;
  temperature: number;
  maxTokens: number;
  theme: 'light' | 'dark' | 'system';
  biometricAuth: boolean;
  hapticFeedback: boolean;
}

export interface StreamingResponse {
  content: string;
  done: boolean;
}

export type RootStackParamList = {
  MainTabs: undefined;
  ChatDetail: { conversationId: string };
};

export type MainTabParamList = {
  Chat: undefined;
  History: undefined;
  Settings: undefined;
};
