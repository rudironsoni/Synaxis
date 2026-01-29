import { describe, it, expect, beforeEach, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'

vi.mock('@/db/db', ()=> import('../../../test/mocks/dbMock'))
vi.mock('@/api/client', ()=>({ defaultClient: { sendMessage: vi.fn().mockResolvedValue({ choices:[{ message: { role:'assistant', content:'Reply' } }], usage: { prompt_tokens:1, completion_tokens:2, total_tokens:3 } }), updateConfig: vi.fn() } }))

import ChatWindow from '../../chat/ChatWindow'

describe('ChatWindow integration', ()=>{
  beforeEach(()=>{
    // ensure mock db is empty
    const db = require('@/db/db').default
    db.sessions.reset?.()
    db.messages.reset?.()
  })

  it('renders and sends a message and displays reply', async ()=>{
    // create a session entry
    const db = require('@/db/db').default
    const sid = await db.sessions.add({ title:'s', createdAt:new Date(), updatedAt:new Date() })
    render(<ChatWindow sessionId={sid} />)

    // type into textarea
    const ta = await screen.findByPlaceholderText('Type a message...')
    await userEvent.type(ta, 'hello')
    // press enter to send
    await userEvent.keyboard('{Enter}')

    // assistant reply should appear
    expect(await screen.findByText('Reply')).toBeDefined()
  })
})
