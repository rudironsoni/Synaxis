import React, { useState } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  TextInput,
  Switch,
  Pressable,
  Alert,
} from 'react-native';
import { XStack, YStack } from 'tamagui';
import {
  Settings as SettingsIcon,
  Key,
  Server,
  Zap,
  Moon,
  Sun,
  Fingerprint,
  Vibrate,
  Info,
  ChevronRight,
} from 'lucide-react-native';
import { useChatStore } from '@/store/chatStore';
import { createChatAPI } from '@/utils/api';
import { triggerHaptic } from '@/utils/helpers';

export const SettingsScreen: React.FC = () => {
  const { settings, updateSettings } = useChatStore();
  const [testingConnection, setTestingConnection] = useState(false);

  const handleTestConnection = async () => {
    if (!settings.apiKey || !settings.apiUrl) {
      Alert.alert('Error', 'Please enter API URL and API Key first');
      return;
    }

    setTestingConnection(true);
    try {
      const api = createChatAPI(settings.apiUrl, settings.apiKey);
      const success = await api.testConnection();
      if (success) {
        Alert.alert('Success', 'Connection successful!');
        triggerHaptic('success');
      } else {
        Alert.alert('Error', 'Connection failed. Please check your credentials.');
        triggerHaptic('heavy');
      }
    } catch (error) {
      Alert.alert('Error', 'Connection failed. Please check your credentials.');
      triggerHaptic('heavy');
    } finally {
      setTestingConnection(false);
    }
  };

  const handleClearData = () => {
    Alert.alert(
      'Clear All Data',
      'This will delete all conversations and settings. This action cannot be undone.',
      [
        { text: 'Cancel', style: 'cancel' },
        {
          text: 'Clear',
          style: 'destructive',
          onPress: () => {
            // TODO: Implement clear data functionality
            triggerHaptic('heavy');
          },
        },
      ]
    );
  };

  const renderSection = (title: string, children: React.ReactNode) => (
    <YStack style={styles.section}>
      <Text style={styles.sectionTitle}>{title}</Text>
      {children}
    </YStack>
  );

  const renderSettingItem = (
    icon: React.ReactNode,
    title: string,
    subtitle?: string,
    rightElement?: React.ReactNode,
    onPress?: () => void
  ) => (
    <Pressable
      onPress={onPress}
      style={({ pressed }) => [
        styles.settingItem,
        pressed && styles.settingItemPressed,
      ]}
    >
      <XStack alignItems="center" gap="$3" flex={1}>
        <View style={styles.iconContainer}>{icon}</View>
        <YStack flex={1}>
          <Text style={styles.settingTitle}>{title}</Text>
          {subtitle && <Text style={styles.settingSubtitle}>{subtitle}</Text>}
        </YStack>
        {rightElement}
      </XStack>
    </Pressable>
  );

  return (
    <ScrollView style={styles.container}>
      <YStack gap="$4" paddingBottom="$6">
        {/* API Configuration */}
        {renderSection('API Configuration', (
          <YStack gap="$2">
            <View style={styles.inputContainer}>
              <Server size={20} color="#8E8E93" style={styles.inputIcon} />
              <TextInput
                style={styles.input}
                placeholder="API URL"
                placeholderTextColor="#8E8E93"
                value={settings.apiUrl}
                onChangeText={(text) => updateSettings({ apiUrl: text })}
                autoCapitalize="none"
                autoCorrect={false}
              />
            </View>

            <View style={styles.inputContainer}>
              <Key size={20} color="#8E8E93" style={styles.inputIcon} />
              <TextInput
                style={styles.input}
                placeholder="API Key"
                placeholderTextColor="#8E8E93"
                value={settings.apiKey}
                onChangeText={(text) => updateSettings({ apiKey: text })}
                secureTextEntry
                autoCapitalize="none"
                autoCorrect={false}
              />
            </View>

            <Pressable
              onPress={handleTestConnection}
              disabled={testingConnection}
              style={({ pressed }) => [
                styles.testButton,
                pressed && styles.testButtonPressed,
                testingConnection && styles.testButtonDisabled,
              ]}
            >
              <Text style={styles.testButtonText}>
                {testingConnection ? 'Testing...' : 'Test Connection'}
              </Text>
            </Pressable>
          </YStack>
        ))}

        {/* Model Settings */}
        {renderSection('Model Settings', (
          <YStack gap="$2">
            <View style={styles.inputContainer}>
              <Zap size={20} color="#8E8E93" style={styles.inputIcon} />
              <TextInput
                style={styles.input}
                placeholder="Model (e.g., gpt-4)"
                placeholderTextColor="#8E8E93"
                value={settings.model}
                onChangeText={(text) => updateSettings({ model: text })}
                autoCapitalize="none"
                autoCorrect={false}
              />
            </View>

            {renderSettingItem(
              <Text style={styles.valueLabel}>Temperature</Text>,
              'Temperature',
              `${settings.temperature}`,
              <TextInput
                style={styles.numberInput}
                value={settings.temperature.toString()}
                onChangeText={(text) =>
                  updateSettings({
                    temperature: parseFloat(text) || 0.7,
                  })
                }
                keyboardType="decimal-pad"
              />
            )}

            {renderSettingItem(
              <Text style={styles.valueLabel}>Max Tokens</Text>,
              'Max Tokens',
              `${settings.maxTokens}`,
              <TextInput
                style={styles.numberInput}
                value={settings.maxTokens.toString()}
                onChangeText={(text) =>
                  updateSettings({
                    maxTokens: parseInt(text) || 2048,
                  })
                }
                keyboardType="number-pad"
              />
            )}
          </YStack>
        ))}

        {/* Appearance */}
        {renderSection('Appearance', (
          <YStack gap="$2">
            {renderSettingItem(
              settings.theme === 'dark' ? <Moon size={20} color="#007AFF" /> : <Sun size={20} color="#007AFF" />,
              'Theme',
              settings.theme === 'system' ? 'System' : settings.theme === 'dark' ? 'Dark' : 'Light',
              <ChevronRight size={20} color="#8E8E93" />,
              () => {
                const themes: Array<'light' | 'dark' | 'system'> = ['light', 'dark', 'system'];
                const currentIndex = themes.indexOf(settings.theme);
                const nextTheme = themes[(currentIndex + 1) % themes.length];
                updateSettings({ theme: nextTheme });
                triggerHaptic('light');
              }
            )}
          </YStack>
        ))}

        {/* Security & Privacy */}
        {renderSection('Security & Privacy', (
          <YStack gap="$2">
            {renderSettingItem(
              <Fingerprint size={20} color="#007AFF" />,
              'Biometric Authentication',
              'Require Face ID / Touch ID to send messages',
              <Switch
                value={settings.biometricAuth}
                onValueChange={(value) => {
                  updateSettings({ biometricAuth: value });
                  triggerHaptic('light');
                }}
                trackColor={{ false: '#E5E5EA', true: '#007AFF' }}
              />
            )}
          </YStack>
        ))}

        {/* Preferences */}
        {renderSection('Preferences', (
          <YStack gap="$2">
            {renderSettingItem(
              <Vibrate size={20} color="#007AFF" />,
              'Haptic Feedback',
              'Vibrate on interactions',
              <Switch
                value={settings.hapticFeedback}
                onValueChange={(value) => {
                  updateSettings({ hapticFeedback: value });
                  triggerHaptic('light');
                }}
                trackColor={{ false: '#E5E5EA', true: '#007AFF' }}
              />
            )}
          </YStack>
        ))}

        {/* About */}
        {renderSection('About', (
          <YStack gap="$2">
            {renderSettingItem(
              <Info size={20} color="#007AFF" />,
              'About Synaxis Studio',
              'Version 1.0.0',
              <ChevronRight size={20} color="#8E8E93" />,
              () => {
                Alert.alert(
                  'About Synaxis Studio',
                  'Synaxis Studio Mobile\nVersion 1.0.0\n\nA modern AI chat application built with React Native and Expo.',
                  [{ text: 'OK' }]
                );
              }
            )}
          </YStack>
        ))}

        {/* Danger Zone */}
        {renderSection('Danger Zone', (
          <Pressable
            onPress={handleClearData}
            style={({ pressed }) => [
              styles.dangerButton,
              pressed && styles.dangerButtonPressed,
            ]}
          >
            <Text style={styles.dangerButtonText}>Clear All Data</Text>
          </Pressable>
        ))}
      </YStack>
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#F2F2F7',
  },
  section: {
    backgroundColor: '#FFFFFF',
    paddingHorizontal: 16,
    paddingTop: 16,
    paddingBottom: 8,
  },
  sectionTitle: {
    fontSize: 13,
    fontWeight: '600',
    color: '#8E8E93',
    marginBottom: 8,
    textTransform: 'uppercase',
  },
  settingItem: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderBottomColor: '#E5E5EA',
  },
  settingItemPressed: {
    backgroundColor: '#F2F2F7',
  },
  iconContainer: {
    width: 32,
    height: 32,
    borderRadius: 8,
    backgroundColor: '#F2F2F7',
    alignItems: 'center',
    justifyContent: 'center',
  },
  settingTitle: {
    fontSize: 16,
    fontWeight: '500',
    color: '#000000',
  },
  settingSubtitle: {
    fontSize: 14,
    color: '#8E8E93',
    marginTop: 2,
  },
  inputContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#F2F2F7',
    borderRadius: 10,
    paddingHorizontal: 12,
    paddingVertical: 8,
    marginBottom: 8,
  },
  inputIcon: {
    marginRight: 8,
  },
  input: {
    flex: 1,
    fontSize: 16,
    color: '#000000',
  },
  numberInput: {
    width: 80,
    fontSize: 16,
    color: '#007AFF',
    textAlign: 'right',
    paddingVertical: 4,
  },
  valueLabel: {
    fontSize: 14,
    fontWeight: '600',
    color: '#007AFF',
  },
  testButton: {
    backgroundColor: '#007AFF',
    borderRadius: 10,
    paddingVertical: 12,
    alignItems: 'center',
    marginTop: 8,
  },
  testButtonPressed: {
    backgroundColor: '#0056CC',
  },
  testButtonDisabled: {
    backgroundColor: '#8E8E93',
  },
  testButtonText: {
    color: '#FFFFFF',
    fontSize: 16,
    fontWeight: '600',
  },
  dangerButton: {
    backgroundColor: '#FF3B30',
    borderRadius: 10,
    paddingVertical: 12,
    alignItems: 'center',
    marginTop: 8,
  },
  dangerButtonPressed: {
    backgroundColor: '#C42B1F',
  },
  dangerButtonText: {
    color: '#FFFFFF',
    fontSize: 16,
    fontWeight: '600',
  },
});
