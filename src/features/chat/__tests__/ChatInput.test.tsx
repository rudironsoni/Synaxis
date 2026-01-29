import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import ChatInput from '../../chat/ChatInput'

describe('ChatInput', ()=>{
  it('sends on enter and clears', async ()=>{
    const onSend = vi.fn()
    render(<ChatInput onSend={onSend} />)
    const ta = screen.getByPlaceholderText('Type a message...') as HTMLTextAreaElement
    await userEvent.type(ta, 'hello')
    await userEvent.keyboard('{Enter}')
    expect(onSend).toHaveBeenCalledWith('hello')
    expect(ta.value).toBe('')
  })
})
