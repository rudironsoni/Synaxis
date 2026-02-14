import React from 'react';
import { View, Text, Pressable, StyleSheet } from 'react-native';
import { XStack, YStack } from 'tamagui';
import { Message } from '@/types';
import { formatTimestamp, copyToClipboard, shareText, triggerHaptic } from '@/utils/helpers';

interface ChatMessageProps {
  message: Message;
  isStreaming?: boolean;
}

export const ChatMessage: React.FC<ChatMessageProps> = ({ message, isStreaming }) => {
  const isUser = message.role === 'user';

  const handleCopy = async () => {
    await copyToClipboard(message.content);
  };

  const handleShare = async () => {
    await shareText(message.content, 'Message from Synaxis Studio');
  };

  return (
    <YStack
      style={[
        styles.container,
        isUser ? styles.userContainer : styles.assistantContainer,
      ]}
    >
      <XStack
        style={[
          styles.messageBubble,
          isUser ? styles.userBubble : styles.assistantBubble,
        ]}
      >
        <YStack flex={1}>
          <Text
            style={[
              styles.roleText,
              isUser ? styles.userRoleText : styles.assistantRoleText,
            ]}
          >
            {isUser ? 'You' : 'Assistant'}
          </Text>
          <Text style={styles.contentText}>
            {message.content}
            {isStreaming && <Text style={styles.cursor}>|</Text>}
          </Text>
          <Text style={styles.timestampText}>
            {formatTimestamp(message.timestamp)}
          </Text>
        </YStack>
      </XStack>

      {!isUser && !isStreaming && (
        <XStack gap="$2" marginTop="$2">
          <Pressable onPress={handleCopy} style={styles.actionButton}>
            <Text style={styles.actionText}>Copy</Text>
          </Pressable>
          <Pressable onPress={handleShare} style={styles.actionButton}>
            <Text style={styles.actionText}>Share</Text>
          </Pressable>
        </XStack>
      )}
    </YStack>
  );
};

const styles = StyleSheet.create({
  container: {
    padding: 12,
    maxWidth: '85%',
  },
  userContainer: {
    alignSelf: 'flex-end',
  },
  assistantContainer: {
    alignSelf: 'flex-start',
  },
  messageBubble: {
    padding: 12,
    borderRadius: 16,
    minWidth: 60,
  },
  userBubble: {
    backgroundColor: '#007AFF',
  },
  assistantBubble: {
    backgroundColor: '#F2F2F7',
  },
  roleText: {
    fontSize: 12,
    fontWeight: '600',
    marginBottom: 4,
  },
  userRoleText: {
    color: '#FFFFFF',
  },
  assistantRoleText: {
    color: '#8E8E93',
  },
  contentText: {
    fontSize: 16,
    lineHeight: 22,
    color: '#000000',
  },
  timestampText: {
    fontSize: 11,
    marginTop: 4,
    opacity: 0.7,
  },
  cursor: {
    opacity: 0.7,
  },
  actionButton: {
    paddingHorizontal: 12,
    paddingVertical: 6,
    backgroundColor: '#E5E5EA',
    borderRadius: 8,
  },
  actionText: {
    fontSize: 12,
    color: '#007AFF',
    fontWeight: '500',
  },
});
