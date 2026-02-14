import React from 'react';
import { Outlet, useNavigate, useLocation } from 'react-router-dom';
import { Box, Flex, Button, Text } from '@synaxis/ui';
import { useChatStore } from '../store/chatStore';

const Layout: React.FC<{ children?: React.ReactNode }> = ({ children }) => {
  const navigate = useNavigate();
  const location = useLocation();
  const { settings, setSettings, setAuthenticated } = useChatStore();

  const handleLogout = () => {
    setAuthenticated(false);
    navigate('/login');
  };

  const toggleTheme = () => {
    setSettings({ theme: settings.theme === 'light' ? 'dark' : 'light' });
  };

  return (
    <Flex height="100vh" width="100vw" backgroundColor="$background">
      <Sidebar />
      <Flex flex={1} flexDirection="column">
        <Header
          onLogout={handleLogout}
          onToggleTheme={toggleTheme}
          currentPath={location.pathname}
        />
        <Box flex={1} overflow="auto">
          {children || <Outlet />}
        </Box>
      </Flex>
    </Flex>
  );
};

const Sidebar: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const { conversations, currentConversationId, setCurrentConversationId, createConversation, deleteConversation } = useChatStore();

  return (
    <Box
      width={280}
      backgroundColor="$backgroundStrong"
      borderRight="$border"
      padding="$4"
      display="flex"
      flexDirection="column"
    >
      <Button onPress={createConversation} marginBottom="$4">
        + New Chat
      </Button>

      <Text fontSize="$sm" color="$color11" marginBottom="$2">
        Conversations
      </Text>

      <Box flex={1} overflow="auto">
        {conversations.map((conv) => (
          <Box
            key={conv.id}
            padding="$3"
            marginBottom="$2"
            borderRadius="$2"
            backgroundColor={currentConversationId === conv.id ? '$backgroundHover' : 'transparent'}
            hoverStyle={{ backgroundColor: '$backgroundHover' }}
            cursor="pointer"
            display="flex"
            justifyContent="space-between"
            alignItems="center"
            onPress={() => {
              setCurrentConversationId(conv.id);
              navigate('/chat');
            }}
          >
            <Text fontSize="$sm" color="$color12" numberOfLines={1}>
              {conv.title}
            </Text>
            <Button
              size="$1"
              onPress={(e) => {
                e.stopPropagation();
                deleteConversation(conv.id);
              }}
            >
              ×
            </Button>
          </Box>
        ))}
      </Box>

      <Box borderTop="$border" paddingTop="$4">
        <Button
          variant="ghost"
          onPress={() => navigate('/settings')}
          width="100%"
          justifyContent="flex-start"
        >
          Settings
        </Button>
      </Box>
    </Box>
  );
};

interface HeaderProps {
  onLogout: () => void;
  onToggleTheme: () => void;
  currentPath: string;
}

const Header: React.FC<HeaderProps> = ({ onLogout, onToggleTheme, currentPath }) => {
  const { settings } = useChatStore();

  return (
    <Box
      height={60}
      borderBottom="$border"
      display="flex"
      alignItems="center"
      justifyContent="space-between"
      paddingHorizontal="$4"
      backgroundColor="$background"
    >
      <Text fontSize="$lg" fontWeight="bold">
        Synaxis Studio
      </Text>

      <Flex gap="$2">
        <Button variant="ghost" size="$2" onPress={onToggleTheme}>
          {settings.theme === 'light' ? '🌙' : '☀️'}
        </Button>
        <Button variant="ghost" size="$2" onPress={onLogout}>
          Logout
        </Button>
      </Flex>
    </Box>
  );
};

export default Layout;
