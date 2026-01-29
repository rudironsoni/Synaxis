import AppShell from '@/components/layout/AppShell'
import ChatWindow from '@/features/chat/ChatWindow'
import { useSessionsStore } from '@/stores/sessions'
import { useEffect, useState } from 'react'
import './App.css'

function App(){
  const { sessions, loadSessions } = useSessionsStore()
  const [selected, setSelected] = useState<number|undefined>(undefined)

  useEffect(()=>{ loadSessions() }, [])

  useEffect(()=>{ if(sessions.length && selected === undefined) setSelected(sessions[0].id) },[sessions])

  return (
    <AppShell>
      {selected ? <ChatWindow sessionId={selected} /> : <div className="text-[var(--muted-foreground)]">Select a chat</div>}
    </AppShell>
  )
}

export default App
