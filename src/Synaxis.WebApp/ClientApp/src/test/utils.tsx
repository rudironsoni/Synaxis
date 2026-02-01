import { ReactElement } from 'react'
import { render as rtlRender, RenderOptions, screen, waitFor } from '@testing-library/react'
import { AllTheProviders } from './AllTheProviders'

function render(ui: ReactElement, options?: Omit<RenderOptions, 'wrapper'>) {
  return rtlRender(ui, { wrapper: AllTheProviders, ...options })
}

// Re-export testing utilities
export { render, screen, waitFor }
export type { RenderOptions }
// Re-export specific testing utilities
export { fireEvent, waitFor as waitForElement } from '@testing-library/react'

