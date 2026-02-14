import React, { useState, useRef, useEffect } from 'react';
import { Box, Flex, Input, Button, Text, ScrollArea } from '@synaxis/ui';
import { ChatMessage, StreamingText, ModelSelector } from '@synaxis/ui';
import { useChatStore } from '../store/chatStore';

const Chat: React.FC = () => {
  const [input, setInput] = useState('');
  const [isStreaming, setIsStreaming] = useState(false);
  const [streamedContent, setStreamedContent] = useState('');
  const scrollRef = useRef<HTMLDivElement>(null);
  const messagesEndRef = useRef<HTMLDivElement>(null);

  const {
    currentConversationId,
    conversations,
    createConversation,
    addMessage,
    modelConfig,
    setModelConfig,
  } = useChatStore();

  const currentConversation = conversations.find((c) => c.id === currentConversationId);

  useEffect(() => {
    if (!currentConversationId) {
      createConversation();
    }
  }, [currentConversationId, createConversation]);

  useEffect(() => {
    scrollToBottom();
  }, [currentConversation?.messages, streamedContent]);

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  const handleSend = async () => {
    if (!input.trim() || !currentConversationId || isStreaming) return;

    const userMessage = input.trim();
    setInput('');

    addMessage(currentConversationId, {
      role: 'user',
      content: userMessage,
    });

    setIsStreaming(true);
    setStreamedContent('');

    // Simulate streaming response
    const response = 'This is a simulated streaming response from the AI assistant. In a real implementation, this would connect to the backend API and stream the response character by character or token by token.';
    let index = 0;

    const streamInterval = setInterval(() => {
      if (index < response.length) {
        setStreamedContent((prev) => prev + response[index]);
        index++;
      } else {
        clearInterval(streamInterval);
        setIsStreaming(false);
        addMessage(currentConversationId, {
          role: 'assistant',
          content: response,
        });
        setStreamedContent('');
      }
    }, 30);
  };

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  const models = [
    { id: 'gpt-4', name: 'GPT-4', provider: 'OpenAI' },
    { id: 'gpt-3.5-turbo', name: 'GPT-3.5 Turbo', provider: 'OpenAI' },
    { id: 'claude-3-opus', name: 'Claude 3 Opus', provider: 'Anthropic' },
    { id: 'claude-3-sonnet', name: 'Claude 3 Sonnet', provider: 'Anthropic' },
  ];

  if (!currentConversation) {
    return (
      <Flex
        height="100%"
        alignItems="center"
        justifyContent="center"
        backgroundColor="$background"
      >
        <Text>Loading...</Text>
      </Flex>
    );
  }

  return (
    <Flex height="100%" flexDirection="column" backgroundColor="$background">
      <Box
        height={60}
        borderBottom="$border"
        display="flex"
        alignItems="center"
        justifyContent="space-between"
        paddingHorizontal="$4"
        backgroundColor="$background"
      >
        <ModelSelector
          models={models}
          selectedModel={modelConfig.model}
          onModelChange={(model) => setModelConfig({ model })}
        />
      </Box>

      <Box flex={1} overflow="auto" padding="$4" ref={scrollRef}>
        {currentConversation.messages.length === 0 && !isStreaming ? (
          <Flex
            height="100%"
            alignItems="center"
            justifyContent="center"
            flexDirection="column"
            gap="$4"
          >
            <Text fontSize="$xl" color="$color11">
              Start a new conversation
            </Text>
            <Text color="$color11">
              Ask anything and get AI-powered responses
            </Text>
          </Flex>
        ) : (
          <Flex flexDirection="column" gap="$4">
            {currentConversation.messages.map((message) => (
              <ChatMessage
                key={message.id}
                role={message.role}
                content={message.content}
                timestamp={message.timestamp}
              />
            ))}
            {isStreaming && (
              <ChatMessage
                role="assistant"
                content={<StreamingText content={streamedContent} />}
                timestamp={new Date()}
              />
            )}
            <div ref={messagesEndRef} />
          </Flex>
        )}
      </Box>

      <Box
        borderTop="$border"
        padding="$4"
        backgroundColor="$background"
      >
        <Flex flexDirection="column" gap="$3">
          <Flex gap="$3" alignItems="center">
            <Text fontSize="$sm" color="$color11">
              Temperature: {modelConfig.temperature}
            </Text>
            <Input
              type="range"
              min={0}
              max={2}
              step={0.1}
              value={modelConfig.temperature}
              onChange={(e) => setModelConfig({ temperature: parseFloat(e.target.value) })}
              flex={1}
            />
          </Flex>
          <Flex gap="$3" alignItems="center">
            <Text fontSize="$sm" color="$color11">
              Max Tokens: {modelConfig.maxTokens}
            </Text>
            <Input
              type="number"
              min={1}
              max={8192}
              value={modelConfig.maxTokens}
              onChange={(e) => setModelConfig({ maxTokens: parseInt(e.target.value) || 2048 })}
              width={120}
            />
          </Flex>
          <Flex gap="$2">
            <Input
              value={input}
              onChange={(e) => setInput(e.target.value)}
              onKeyPress={handleKeyPress}
              placeholder="Type your message..."
              flex={1}
              multiline
              disabled={isStreaming}
            />
            <Button onPress={handleSend} disabled={!input.trim() || isStreaming}>
              {isStreaming ? '...' : 'Send'}
            </Button>
          </Flex>
        </Flex>
      </Box>
    </Flex>
  );
};

export default Chat;
