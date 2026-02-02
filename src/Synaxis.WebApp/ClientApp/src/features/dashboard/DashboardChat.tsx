import ChatWindow from '@/features/chat/ChatWindow'
import { useSessionsStore } from '@/stores/sessions'
import { useState, useEffect, useCallback } from 'react'

export default function DashboardChat() {
  const { sessions, loadSessions } = useSessionsStore()
  const [selected] = useState<number | undefined>(() => {
    return sessions.length > 0 ? sessions[0].id : undefined
  })

  const memoizedLoadSessions = useCallback(() => {
    loadSessions()
  }, [loadSessions])

  useEffect(() => {
    memoizedLoadSessions()
  }, [memoizedLoadSessions])

  return (
    <div className="h-full">
      {selected ? (
        <ChatWindow sessionId={selected} />
      ) : (
        <div className="text-[var(--muted-foreground)]">Select a chat</div>
      )}
    </div>
  )
}
