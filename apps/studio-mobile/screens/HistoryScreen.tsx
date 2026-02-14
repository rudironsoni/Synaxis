import React, { useState } from 'react';
import {
  View,
  Text,
  FlatList,
  StyleSheet,
  TextInput,
  Pressable,
  Alert,
} from 'react-native';
import { XStack, YStack } from 'tamagui';
import { Search, Pin, Trash2, MessageSquare } from 'lucide-react-native';
import { useChatStore } from '@/store/chatStore';
import { Conversation } from '@/types';
import { formatTimestamp, triggerHaptic } from '@/utils/helpers';

export const HistoryScreen: React.FC = () => {
  const [searchQuery, setSearchQuery] = useState('');
  const {
    conversations,
    setCurrentConversation,
    deleteConversation,
    pinConversation,
  } = useChatStore();

  const filteredConversations = conversations.filter((conv) =>
    conv.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
    conv.messages.some((msg) =>
      msg.content.toLowerCase().includes(searchQuery.toLowerCase())
    )
  );

  const sortedConversations = [...filteredConversations].sort((a, b) => {
    // Pinned conversations first
    if (a.isPinned && !b.isPinned) return -1;
    if (!a.isPinned && b.isPinned) return 1;
    // Then by date
    return b.updatedAt.getTime() - a.updatedAt.getTime();
  });

  const handleSelectConversation = (conversation: Conversation) => {
    setCurrentConversation(conversation);
    triggerHaptic('light');
  };

  const handleDeleteConversation = (conversationId: string, title: string) => {
    Alert.alert(
      'Delete Conversation',
      `Are you sure you want to delete "${title}"?`,
      [
        { text: 'Cancel', style: 'cancel' },
        {
          text: 'Delete',
          style: 'destructive',
          onPress: () => {
            deleteConversation(conversationId);
            triggerHaptic('heavy');
          },
        },
      ]
    );
  };

  const handlePinConversation = (conversationId: string) => {
    pinConversation(conversationId);
    triggerHaptic('medium');
  };

  const renderConversation = ({ item }: { item: Conversation }) => (
    <Pressable
      onPress={() => handleSelectConversation(item)}
      style={({ pressed }) => [
        styles.conversationItem,
        pressed && styles.conversationItemPressed,
      ]}
    >
      <XStack alignItems="center" gap="$3" flex={1}>
        <View style={styles.iconContainer}>
          <MessageSquare size={20} color="#007AFF" />
        </View>

        <YStack flex={1}>
          <XStack alignItems="center" gap="$2">
            <Text style={styles.title} numberOfLines={1}>
              {item.title}
            </Text>
            {item.isPinned && <Pin size={14} color="#FF9500" />}
          </XStack>
          <Text style={styles.preview} numberOfLines={2}>
            {item.messages.length > 0
              ? item.messages[item.messages.length - 1].content
              : 'No messages yet'}
          </Text>
          <Text style={styles.timestamp}>
            {formatTimestamp(item.updatedAt)} · {item.messages.length} messages
          </Text>
        </YStack>

        <XStack gap="$2">
          <Pressable
            onPress={() => handlePinConversation(item.id)}
            style={styles.iconButton}
          >
            <Pin
              size={20}
              color={item.isPinned ? '#FF9500' : '#8E8E93'}
              fill={item.isPinned ? '#FF9500' : 'none'}
            />
          </Pressable>
          <Pressable
            onPress={() => handleDeleteConversation(item.id, item.title)}
            style={styles.iconButton}
          >
            <Trash2 size={20} color="#FF3B30" />
          </Pressable>
        </XStack>
      </XStack>
    </Pressable>
  );

  const renderEmptyState = () => (
    <View style={styles.emptyState}>
      <MessageSquare size={48} color="#C7C7CC" />
      <Text style={styles.emptyTitle}>No conversations yet</Text>
      <Text style={styles.emptyText}>
        Start a new chat to see it here
      </Text>
    </View>
  );

  return (
    <View style={styles.container}>
      <View style={styles.searchContainer}>
        <Search size={20} color="#8E8E93" style={styles.searchIcon} />
        <TextInput
          style={styles.searchInput}
          placeholder="Search conversations..."
          placeholderTextColor="#8E8E93"
          value={searchQuery}
          onChangeText={setSearchQuery}
        />
      </View>

      <FlatList
        data={sortedConversations}
        renderItem={renderConversation}
        keyExtractor={(item) => item.id}
        contentContainerStyle={styles.listContent}
        ListEmptyComponent={renderEmptyState}
      />
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#F2F2F7',
  },
  searchContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#FFFFFF',
    margin: 12,
    paddingHorizontal: 12,
    paddingVertical: 8,
    borderRadius: 10,
    borderWidth: 1,
    borderColor: '#E5E5EA',
  },
  searchIcon: {
    marginRight: 8,
  },
  searchInput: {
    flex: 1,
    fontSize: 16,
    color: '#000000',
  },
  listContent: {
    padding: 12,
    paddingTop: 0,
  },
  conversationItem: {
    backgroundColor: '#FFFFFF',
    padding: 12,
    borderRadius: 12,
    marginBottom: 8,
    borderWidth: 1,
    borderColor: '#E5E5EA',
  },
  conversationItemPressed: {
    backgroundColor: '#F2F2F7',
  },
  iconContainer: {
    width: 40,
    height: 40,
    borderRadius: 20,
    backgroundColor: '#E5F1FF',
    alignItems: 'center',
    justifyContent: 'center',
  },
  title: {
    fontSize: 16,
    fontWeight: '600',
    color: '#000000',
    flex: 1,
  },
  preview: {
    fontSize: 14,
    color: '#8E8E93',
    marginTop: 4,
  },
  timestamp: {
    fontSize: 12,
    color: '#8E8E93',
    marginTop: 4,
  },
  iconButton: {
    padding: 8,
  },
  emptyState: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: 60,
  },
  emptyTitle: {
    fontSize: 18,
    fontWeight: '600',
    color: '#000000',
    marginTop: 16,
  },
  emptyText: {
    fontSize: 14,
    color: '#8E8E93',
    marginTop: 8,
  },
});
