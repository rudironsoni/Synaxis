import React, { useState } from 'react';
import { Box, Flex, Input, Button, Text, Card } from '@synaxis/ui';
import { useChatStore } from '../store/chatStore';

const Login: React.FC = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const { setSettings, setAuthenticated } = useChatStore();

  const handleLogin = (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    if (!email || !password) {
      setError('Please fill in all fields');
      return;
    }

    // Simulate login - in production, this would call an API
    // Store JWT token in localStorage
    const fakeToken = 'fake-jwt-token-' + Date.now();
    localStorage.setItem('auth_token', fakeToken);

    setAuthenticated(true);
  };

  return (
    <Flex
      height="100vh"
      width="100vw"
      backgroundColor="$background"
      alignItems="center"
      justifyContent="center"
    >
      <Card width={400} padding="$6">
        <Text fontSize="$2xl" fontWeight="bold" marginBottom="$4" textAlign="center">
          Synaxis Studio
        </Text>

        {error && (
          <Box
            backgroundColor="$red9"
            color="$white"
            padding="$3"
            borderRadius="$2"
            marginBottom="$4"
          >
            <Text>{error}</Text>
          </Box>
        )}

        <form onSubmit={handleLogin}>
          <Flex flexDirection="column" gap="$4">
            <Box>
              <Text marginBottom="$2" display="block">
                Email
              </Text>
              <Input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="your@email.com"
                width="100%"
              />
            </Box>

            <Box>
              <Text marginBottom="$2" display="block">
                Password
              </Text>
              <Input
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="••••••••"
                width="100%"
              />
            </Box>

            <Button type="submit" width="100%">
              Sign In
            </Button>
          </Flex>
        </form>

        <Text marginTop="$4" textAlign="center" color="$color11">
          Don't have an account? <Text color="$blue10">Sign up</Text>
        </Text>
      </Card>
    </Flex>
  );
};

export default Login;
