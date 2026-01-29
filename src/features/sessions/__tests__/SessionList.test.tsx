import { describe, it, expect, beforeEach, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'

vi.mock('@/db/db', ()=> import('../../../test/mocks/dbMock'))

import SessionList from '../../sessions/SessionList'

describe('SessionList', ()=>{
  beforeEach(()=>{
    const db = require('@/db/db').default
    db.sessions.reset?.()
  })

  it('creates a new session when clicking new', async ()=>{
    render(<SessionList />)
    const btn = screen.getByTitle('New chat')
    await userEvent.click(btn)
    expect((await require('@/db/db').default.sessions.toArray()).length).toBeGreaterThan(0)
  })
})
