import AppShell from '@/components/layout/AppShell'
import ChatWindow from '@/features/chat/ChatWindow'
import { useSessionsStore } from '@/stores/sessions'
import { useEffect, useState, useCallback } from 'react'
import './App.css'

function App(){
  const { sessions, loadSessions } = useSessionsStore()
  const [selected] = useState<number|undefined>(() => {
    return sessions.length > 0 ? sessions[0].id : undefined
  })

  const memoizedLoadSessions = useCallback(() => {
    loadSessions()
  }, [loadSessions])

  useEffect(()=>{ memoizedLoadSessions() }, [memoizedLoadSessions])

  return (
    <AppShell>
      {selected ? <ChatWindow sessionId={selected} /> : <div className="text-[var(--muted-foreground)]">Select a chat</div>}
    </AppShell>
  )
}

export default App
