# Synaxis Studio Mobile

A modern AI chat application built with React Native and Expo for iOS and Android.

## Features

- **Chat Interface**: Real-time messaging with streaming responses
- **Conversation History**: Search, pin, and manage conversations
- **Settings**: Configure API, model preferences, and app settings
- **Biometric Authentication**: Secure access with Face ID / Touch ID
- **Haptic Feedback**: Tactile feedback for interactions
- **Voice Input**: Speech-to-text support (coming soon)
- **Dark Mode**: Automatic theme switching
- **Offline Support**: Local storage for conversations

## Tech Stack

- **Framework**: React Native 0.74+ with Expo SDK 51
- **Navigation**: React Navigation 6
- **UI Library**: Tamagui
- **State Management**: Zustand
- **Authentication**: Expo LocalAuthentication
- **Haptics**: Expo Haptics
- **Storage**: AsyncStorage (via Zustand persist)

## Getting Started

### Prerequisites

- Node.js 20+
- npm or yarn
- iOS: Xcode 15+ (for iOS development)
- Android: Android Studio (for Android development)

### Installation

```bash
# Install dependencies
npm install

# Start Expo development server
npm start

# Run on iOS simulator
npm run ios

# Run on Android emulator
npm run android
```

### Configuration

Before using the app, configure your API settings in the Settings screen:

1. Open the app and navigate to Settings
2. Enter your API URL (e.g., `https://api.synaxis.ai/v1`)
3. Enter your API Key
4. Test the connection
5. Configure model preferences (optional)

## Project Structure

```
apps/studio-mobile/
├── App.tsx                 # Entry point
├── app.json                # Expo configuration
├── package.json            # Dependencies
├── tsconfig.json           # TypeScript configuration
├── babel.config.js         # Babel configuration
├── tamagui.config.ts       # Tamagui UI configuration
├── components/             # Reusable components
│   ├── ChatMessage.tsx     # Message bubble component
│   └── MessageInput.tsx    # Input bar component
├── screens/                # Screen components
│   ├── ChatScreen.tsx      # Chat interface
│   ├── HistoryScreen.tsx   # Conversation history
│   └── SettingsScreen.tsx  # App settings
├── navigation/             # Navigation configuration
│   └── AppNavigator.tsx    # Tab and stack navigation
├── store/                  # State management
│   └── chatStore.ts        # Zustand store
├── types/                  # TypeScript types
│   └── index.ts            # Type definitions
└── utils/                  # Utility functions
    ├── api.ts              # API client
    └── helpers.ts          # Helper functions
```

## Development

### Running Tests

```bash
npm test
```

### Linting

```bash
npm run lint
```

### Building for Production

#### iOS

```bash
# Build for iOS
eas build --platform ios

# Submit to App Store
eas submit --platform ios
```

#### Android

```bash
# Build for Android
eas build --platform android

# Submit to Play Store
eas submit --platform android
```

## Features in Detail

### Chat Screen
- Real-time message streaming
- Message actions (copy, share)
- Voice input button
- Auto-scroll to latest message
- Keyboard avoiding view

### History Screen
- Search conversations
- Pin/star conversations
- Swipe to delete
- Sort by date and pinned status
- Message preview

### Settings Screen
- API configuration
- Model selection
- Temperature and max tokens
- Theme selection (light/dark/system)
- Biometric authentication toggle
- Haptic feedback toggle
- Clear all data

## Security

- API keys stored securely using Expo SecureStore
- Biometric authentication for sensitive actions
- No sensitive data logged
- HTTPS only for API calls

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Run tests and linting
5. Submit a pull request

## License

MIT License - see LICENSE file for details
