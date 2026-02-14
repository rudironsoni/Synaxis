import React, { useEffect, useRef, useState } from 'react';
import {
  View,
  FlatList,
  StyleSheet,
  KeyboardAvoidingView,
  Platform,
} from 'react-native';
import { YStack } from 'tamagui';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useChatStore } from '@/store/chatStore';
import { ChatMessage } from '@/components/ChatMessage';
import { MessageInput } from '@/components/MessageInput';
import { Message } from '@/types';
import { createChatAPI } from '@/utils/api';
import { generateId, triggerHaptic } from '@/utils/helpers';
import * as LocalAuthentication from 'expo-local-authentication';

export const ChatScreen: React.FC = () => {
  const flatListRef = useRef<FlatList>(null);
  const [isRecording, setIsRecording] = useState(false);

  const {
    currentConversation,
    createConversation,
    addMessage,
    updateMessage,
    settings,
    isStreaming,
    setStreaming,
  } = useChatStore();

  useEffect(() => {
    if (!currentConversation) {
      createConversation();
    }
  }, [currentConversation, createConversation]);

  useEffect(() => {
    if (flatListRef.current) {
      flatListRef.current.scrollToEnd({ animated: true });
    }
  }, [currentConversation?.messages]);

  const handleSendMessage = async (content: string) => {
    if (!currentConversation) return;

    // Check biometric auth if enabled
    if (settings.biometricAuth) {
      try {
        const result = await LocalAuthentication.authenticateAsync({
          promptMessage: 'Authenticate to send message',
          fallbackLabel: 'Use passcode',
        });
        if (!result.success) {
          return;
        }
      } catch (error) {
        console.error('Biometric auth error:', error);
      }
    }

    // Add user message
    const userMessage: Message = {
      id: generateId(),
      role: 'user',
      content,
      timestamp: new Date(),
    };
    addMessage(currentConversation.id, userMessage);

    // Create assistant message for streaming
    const assistantMessage: Message = {
      id: generateId(),
      role: 'assistant',
      content: '',
      timestamp: new Date(),
      isStreaming: true,
    };
    addMessage(currentConversation.id, assistantMessage);

    // Send to API
    try {
      setStreaming(true);
      const api = createChatAPI(settings.apiUrl, settings.apiKey);

      let fullContent = '';
      await api.sendMessage(
        [...currentConversation.messages, userMessage],
        settings.model,
        settings.temperature,
        settings.maxTokens,
        (chunk) => {
          fullContent += chunk;
          updateMessage(currentConversation.id, assistantMessage.id, {
            content: fullContent,
          });
        }
      );

      // Finalize message
      updateMessage(currentConversation.id, assistantMessage.id, {
        content: fullContent,
        isStreaming: false,
      });

      triggerHaptic('success');
    } catch (error) {
      console.error('Send message error:', error);
      updateMessage(currentConversation.id, assistantMessage.id, {
        content: 'Sorry, I encountered an error. Please try again.',
        isStreaming: false,
      });
      triggerHaptic('heavy');
    } finally {
      setStreaming(false);
    }
  };

  const handleVoiceInput = () => {
    setIsRecording(!isRecording);
    // TODO: Implement voice recording with expo-av
    triggerHaptic('medium');
  };

  const renderMessage = ({ item }: { item: Message }) => (
    <ChatMessage message={item} isStreaming={item.isStreaming} />
  );

  return (
    <SafeAreaView style={styles.container} edges={['bottom']}>
      <KeyboardAvoidingView
        style={styles.keyboardContainer}
        behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
        keyboardVerticalOffset={Platform.OS === 'ios' ? 0 : 20}
      >
        <YStack flex={1}>
          <FlatList
            ref={flatListRef}
            data={currentConversation?.messages || []}
            renderItem={renderMessage}
            keyExtractor={(item) => item.id}
            contentContainerStyle={styles.listContent}
            inverted={false}
            keyboardShouldPersistTaps="handled"
          />

          <MessageInput
            onSend={handleSendMessage}
            isStreaming={isStreaming}
            disabled={!settings.apiKey}
            onVoiceInput={handleVoiceInput}
          />
        </YStack>
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#FFFFFF',
  },
  keyboardContainer: {
    flex: 1,
  },
  listContent: {
    padding: 12,
    paddingBottom: 8,
  },
});
