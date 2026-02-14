import * as Haptics from 'expo-haptics';
import * as Clipboard from 'expo-clipboard';
import { Share } from 'react-native';

export const triggerHaptic = async (type: 'light' | 'medium' | 'heavy' | 'success' = 'light') => {
  try {
    switch (type) {
      case 'light':
        await Haptics.impactAsync(Haptics.ImpactFeedbackStyle.Light);
        break;
      case 'medium':
        await Haptics.impactAsync(Haptics.ImpactFeedbackStyle.Medium);
        break;
      case 'heavy':
        await Haptics.impactAsync(Haptics.ImpactFeedbackStyle.Heavy);
        break;
      case 'success':
        await Haptics.notificationAsync(Haptics.NotificationFeedbackType.Success);
        break;
    }
  } catch (error) {
    console.error('Haptic feedback error:', error);
  }
};

export const copyToClipboard = async (text: string) => {
  try {
    await Clipboard.setStringAsync(text);
    await triggerHaptic('success');
    return true;
  } catch (error) {
    console.error('Copy error:', error);
    return false;
  }
};

export const shareText = async (text: string, title?: string) => {
  try {
    await Share.share({
      message: text,
      title: title || 'Share from Synaxis Studio',
    });
    return true;
  } catch (error) {
    console.error('Share error:', error);
    return false;
  }
};

export const formatTimestamp = (date: Date): string => {
  const now = new Date();
  const diff = now.getTime() - date.getTime();
  const minutes = Math.floor(diff / 60000);
  const hours = Math.floor(diff / 3600000);
  const days = Math.floor(diff / 86400000);

  if (minutes < 1) return 'Just now';
  if (minutes < 60) return `${minutes}m ago`;
  if (hours < 24) return `${hours}h ago`;
  if (days < 7) return `${days}d ago`;

  return date.toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
  });
};

export const generateId = (): string => {
  return `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
};

export const truncateText = (text: string, maxLength: number): string => {
  if (text.length <= maxLength) return text;
  return text.substring(0, maxLength) + '...';
};
