import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import AppShell from '../../layout/AppShell'

vi.mock('@/stores/usage', ()=>({ useUsageStore: ()=>({ totalTokens: 0 }) }))
vi.mock('@/stores/settings', ()=>({ default: ()=>({ costRate: 0.01 }) }))

describe('AppShell', ()=>{
  it('renders and opens settings', async ()=>{
    render(<AppShell><div>Child</div></AppShell>)
    // open settings
    const btn = screen.getByTitle('Settings')
    await userEvent.click(btn)
    expect(screen.getByText('Settings')).toBeDefined()
  })
})
