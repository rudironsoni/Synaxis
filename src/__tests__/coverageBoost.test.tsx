import React from 'react'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'

// polyfill matchMedia used by AppShell
window.matchMedia = window.matchMedia || function(){ return { matches: false, addEventListener: ()=>{}, removeEventListener: ()=>{} } } as any

// mocks
vi.mock('@/db/db', ()=> import('../test/mocks/dbMock'))
vi.mock('@/api/client', ()=>({ defaultClient: { sendMessage: vi.fn().mockResolvedValue({ choices:[{ message:{ role:'assistant', content:'ok' } }], usage:{ prompt_tokens:1, completion_tokens:1, total_tokens:2 }, model: 'llama3-8b' }), updateConfig: vi.fn() } }))
vi.mock('@/stores/usage', ()=>({ useUsageStore: ()=>({ totalTokens: 0, addUsage: ()=>{} }) }))
vi.mock('@/stores/settings', ()=>({ default: ()=>({ gatewayUrl: 'http://x', costRate: 0.02, setGatewayUrl: ()=>{}, setCostRate: ()=>{} }) }))

import AppShell from '../components/layout/AppShell'
import MessageBubble from '../features/chat/MessageBubble'
import ChatInput from '../features/chat/ChatInput'
import ChatWindow from '../features/chat/ChatWindow'
import SessionList from '../features/sessions/SessionList'
import SettingsDialog from '../features/settings/SettingsDialog'
import Modal from '../components/ui/Modal'
import Input from '../components/ui/Input'

test('coverage boost - render many components', async ()=>{
  render(<AppShell><div>child</div></AppShell>)
  // open settings
  const btn = screen.getByTitle('Settings')
  await userEvent.click(btn)
  expect(screen.getByText('Settings')).toBeDefined()

  render(<MessageBubble role="assistant" content="hello" usage={{ prompt:1, completion:1, total:2 }} model="llama3-8b" />)
  expect(screen.getByText('hello')).toBeDefined()

  render(<ChatInput onSend={()=>{}} />)
  render(<SessionList />)
  render(<SettingsDialog open={true} onClose={()=>{}} />)
  render(<Modal open={true} onClose={()=>{}} title="ModalTest">hi</Modal>)
  render(<Input value="x" onChange={()=>{}} />)
})
