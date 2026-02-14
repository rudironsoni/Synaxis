import { createTamagui } from '@tamagui/core';
import { shorthands } from '@tamagui/shorthands';
import config from '@synaxis/ui/tamagui.config';

const tamaguiConfig = createTamagui({
  defaultFont: 'sans',
  shorthands,
  themes: config.themes,
  tokens: config.tokens,
  media: config.media,
  animations: {
    fast: '0.2s',
    medium: '0.3s',
    slow: '0.5s',
  },
});

type Conf = typeof tamaguiConfig;

declare module '@tamagui/core' {
  interface TamaguiCustomConfig extends Conf {}
}

export default tamaguiConfig;
