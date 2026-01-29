import { describe, it, expect, beforeEach } from 'vitest'
import useSessionsStore from './sessions'

describe('sessions store', () => {
  beforeEach(() => {
    useSessionsStore.setState({ sessions: [] as any[] })
  })

  it('initially empty', () => {
    expect(useSessionsStore.getState().sessions).toEqual([])
  })
})
