import React, { useState } from 'react';
import {
  View,
  TextInput,
  Pressable,
  StyleSheet,
  Keyboard,
  Platform,
} from 'react-native';
import { XStack, YStack } from 'tamagui';
import { Send, Mic } from 'lucide-react-native';
import { triggerHaptic } from '@/utils/helpers';

interface MessageInputProps {
  onSend: (message: string) => void;
  isStreaming?: boolean;
  disabled?: boolean;
  onVoiceInput?: () => void;
}

export const MessageInput: React.FC<MessageInputProps> = ({
  onSend,
  isStreaming = false,
  disabled = false,
  onVoiceInput,
}) => {
  const [text, setText] = useState('');

  const handleSend = () => {
    if (text.trim() && !disabled && !isStreaming) {
      onSend(text.trim());
      setText('');
      Keyboard.dismiss();
      triggerHaptic('light');
    }
  };

  const handleVoiceInput = () => {
    if (onVoiceInput && !disabled && !isStreaming) {
      onVoiceInput();
      triggerHaptic('medium');
    }
  };

  return (
    <View style={styles.container}>
      <XStack
        style={[
          styles.inputContainer,
          disabled && styles.inputContainerDisabled,
        ]}
        alignItems="center"
        gap="$2"
      >
        {onVoiceInput && (
          <Pressable
            onPress={handleVoiceInput}
            disabled={disabled || isStreaming}
            style={({ pressed }) => [
              styles.iconButton,
              pressed && styles.iconButtonPressed,
              (disabled || isStreaming) && styles.iconButtonDisabled,
            ]}
          >
            <Mic
              size={24}
              color={disabled || isStreaming ? '#8E8E93' : '#007AFF'}
            />
          </Pressable>
        )}

        <TextInput
          style={styles.input}
          value={text}
          onChangeText={setText}
          placeholder="Type a message..."
          placeholderTextColor="#8E8E93"
          multiline
          maxLength={4000}
          editable={!disabled && !isStreaming}
          onSubmitEditing={handleSend}
          blurOnSubmit={false}
        />

        <Pressable
          onPress={handleSend}
          disabled={!text.trim() || disabled || isStreaming}
          style={({ pressed }) => [
            styles.sendButton,
            pressed && styles.sendButtonPressed,
            (!text.trim() || disabled || isStreaming) && styles.sendButtonDisabled,
          ]}
        >
          <Send
            size={24}
            color={!text.trim() || disabled || isStreaming ? '#8E8E93' : '#FFFFFF'}
          />
        </Pressable>
      </XStack>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    padding: 12,
    paddingBottom: Platform.OS === 'ios' ? 28 : 12,
    backgroundColor: '#FFFFFF',
    borderTopWidth: 1,
    borderTopColor: '#E5E5EA',
  },
  inputContainer: {
    backgroundColor: '#F2F2F7',
    borderRadius: 20,
    paddingHorizontal: 12,
    paddingVertical: 8,
    minHeight: 44,
  },
  inputContainerDisabled: {
    opacity: 0.5,
  },
  input: {
    flex: 1,
    fontSize: 16,
    color: '#000000',
    maxHeight: 100,
    paddingVertical: 4,
  },
  iconButton: {
    padding: 8,
    borderRadius: 20,
  },
  iconButtonPressed: {
    backgroundColor: '#E5E5EA',
  },
  iconButtonDisabled: {
    opacity: 0.5,
  },
  sendButton: {
    backgroundColor: '#007AFF',
    padding: 8,
    borderRadius: 20,
  },
  sendButtonPressed: {
    backgroundColor: '#0056CC',
  },
  sendButtonDisabled: {
    backgroundColor: '#E5E5EA',
  },
});
