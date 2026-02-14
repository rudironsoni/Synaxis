import React, { useState } from 'react';
import { Box, Flex, Input, Button, Text, Card, Switch, Select } from '@synaxis/ui';
import { useChatStore } from '../store/chatStore';

const Settings: React.FC = () => {
  const { settings, setSettings, modelConfig, setModelConfig } = useChatStore();
  const [apiKey, setApiKey] = useState(settings.apiKey);
  const [saved, setSaved] = useState(false);

  const handleSave = () => {
    setSettings({ apiKey });
    setSaved(true);
    setTimeout(() => setSaved(false), 2000);
  };

  const models = [
    { id: 'gpt-4', name: 'GPT-4' },
    { id: 'gpt-3.5-turbo', name: 'GPT-3.5 Turbo' },
    { id: 'claude-3-opus', name: 'Claude 3 Opus' },
    { id: 'claude-3-sonnet', name: 'Claude 3 Sonnet' },
  ];

  return (
    <Box padding="$6" backgroundColor="$background" minHeight="100%">
      <Text fontSize="$2xl" fontWeight="bold" marginBottom="$6">
        Settings
      </Text>

      <Flex flexDirection="column" gap="$6">
        <Card padding="$6">
          <Text fontSize="$lg" fontWeight="bold" marginBottom="$4">
            API Configuration
          </Text>

          <Flex flexDirection="column" gap="$4">
            <Box>
              <Text marginBottom="$2" display="block">
                API Key
              </Text>
              <Input
                type="password"
                value={apiKey}
                onChange={(e) => setApiKey(e.target.value)}
                placeholder="Enter your API key"
                width="100%"
              />
              <Text fontSize="$sm" color="$color11" marginTop="$1">
                Your API key is stored locally and never sent to our servers.
              </Text>
            </Box>

            <Button onPress={handleSave} width="fit-content">
              {saved ? 'Saved!' : 'Save API Key'}
            </Button>
          </Flex>
        </Card>

        <Card padding="$6">
          <Text fontSize="$lg" fontWeight="bold" marginBottom="$4">
            Appearance
          </Text>

          <Flex flexDirection="column" gap="$4">
            <Flex justifyContent="space-between" alignItems="center">
              <Box>
                <Text>Dark Mode</Text>
                <Text fontSize="$sm" color="$color11">
                  Toggle dark theme
                </Text>
              </Box>
              <Switch
                checked={settings.theme === 'dark'}
                onCheckedChange={(checked) =>
                  setSettings({ theme: checked ? 'dark' : 'light' })
                }
              />
            </Flex>
          </Flex>
        </Card>

        <Card padding="$6">
          <Text fontSize="$lg" fontWeight="bold" marginBottom="$4">
            Model Settings
          </Text>

          <Flex flexDirection="column" gap="$4">
            <Box>
              <Text marginBottom="$2" display="block">
                Default Model
              </Text>
              <Select
                value={settings.defaultModel}
                onValueChange={(value) => setSettings({ defaultModel: value })}
              >
                {models.map((model) => (
                  <Select.Item key={model.id} value={model.id}>
                    {model.name}
                  </Select.Item>
                ))}
              </Select>
            </Box>

            <Box>
              <Text marginBottom="$2" display="block">
                Default Temperature: {modelConfig.temperature}
              </Text>
              <Input
                type="range"
                min={0}
                max={2}
                step={0.1}
                value={modelConfig.temperature}
                onChange={(e) =>
                  setModelConfig({ temperature: parseFloat(e.target.value) })
                }
                width="100%"
              />
              <Text fontSize="$sm" color="$color11" marginTop="$1">
                Lower values make responses more focused, higher values more creative.
              </Text>
            </Box>

            <Box>
              <Text marginBottom="$2" display="block">
                Default Max Tokens: {modelConfig.maxTokens}
              </Text>
              <Input
                type="number"
                min={1}
                max={8192}
                value={modelConfig.maxTokens}
                onChange={(e) =>
                  setModelConfig({ maxTokens: parseInt(e.target.value) || 2048 })
                }
                width="100%"
              />
              <Text fontSize="$sm" color="$color11" marginTop="$1">
                Maximum number of tokens in the response.
              </Text>
            </Box>
          </Flex>
        </Card>

        <Card padding="$6">
          <Text fontSize="$lg" fontWeight="bold" marginBottom="$4">
            About
          </Text>

          <Flex flexDirection="column" gap="$2">
            <Text>Synaxis Studio v0.1.0</Text>
            <Text color="$color11">
              A modern AI chat interface for Synaxis platform.
            </Text>
          </Flex>
        </Card>
      </Flex>
    </Box>
  );
};

export default Settings;
