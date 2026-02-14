import { config, createTamagui } from '@tamagui/core'
import { shorthands } from '@tamagui/shorthands'
import { tokens } from '@tamagui/config'

const tamaguiConfig = createTamagui({
  ...config,
  tokens,
  shorthands,
})

export type TamaguiConfig = typeof tamaguiConfig

declare module '@tamagui/core' {
  interface TamaguiCustomConfig extends TamaguiConfig {}
}

export { tamaguiConfig }

