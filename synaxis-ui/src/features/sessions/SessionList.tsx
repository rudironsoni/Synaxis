import React, { useEffect } from 'react'
import { Plus, Trash } from 'lucide-react'
import { useSessionsStore } from '@/stores/sessions'

export default function SessionList(){
  const { sessions, loadSessions, createSession, deleteSession } = useSessionsStore()

  useEffect(()=>{ loadSessions() }, [])

  return (
    <div className="flex flex-col gap-3">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-semibold">Chats</h3>
        <button onClick={async ()=>{ await createSession('New Chat') }} title="New chat" className="p-1">
          <Plus className="w-4 h-4" />
        </button>
      </div>

      <div className="flex flex-col gap-2 overflow-auto max-h-[70vh]">
        {sessions.map(s=> (
          <div key={s.id} className="flex items-center justify-between p-2 rounded hover:bg-[rgba(255,255,255,0.02)]">
            <div className="text-sm">{s.title}</div>
            <div className="opacity-0 hover:opacity-100">
              <button onClick={()=>deleteSession(s.id!)} title="Delete" className="p-1 text-red-400">
                <Trash className="w-4 h-4" />
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}
