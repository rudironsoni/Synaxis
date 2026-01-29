import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import MessageBubble from '../../chat/MessageBubble'
import { vi } from 'vitest'

vi.mock('@/stores/settings', ()=>({ default: ()=>({ costRate: 0.5 }) }))

describe('MessageBubble', ()=>{
  it('renders content and cost', ()=>{
    render(<MessageBubble role="assistant" content="Hi" usage={{ prompt:1, completion:1, total:2 }} />)
    expect(screen.getByText('Hi')).toBeDefined()
    expect(screen.getByText(/Tokens:/)).toBeDefined()
    // cost = (2/1000)*0.5 = 0.001 -> formatted
    expect(screen.getByText('Cost: $0.0010')).toBeDefined()
  })
})
