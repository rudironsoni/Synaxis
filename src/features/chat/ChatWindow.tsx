import { useEffect, useState, useRef } from 'react'
import MessageBubble from './MessageBubble'
import ChatInput from './ChatInput'
import db, { type Message } from '@/db/db'
import useSettingsStore from '@/stores/settings'
import { defaultClient, type ChatMessage } from '@/api/client'
import useUsageStore from '@/stores/usage'

// basic chat hook to interact with db and a mock gateway
function useChat(sessionId?: number){
  const [messages, setMessages] = useState<Message[]>([])
  const gatewayUrl = useSettingsStore((s)=>s.gatewayUrl)
  const addUsage = useUsageStore((s)=>s.addUsage)

  useEffect(()=>{
    if(!sessionId) return
    let cancelled = false
    db.messages.where('sessionId').equals(sessionId).toArray().then(list=>{ if(!cancelled) setMessages(list) })
    return ()=>{ cancelled = true }
  },[sessionId])

  useEffect(()=>{
    // update client base url when settings change
    defaultClient.updateConfig(gatewayUrl)
  },[gatewayUrl])

  const send = async (text:string)=>{
    if(!sessionId) return
    const now = new Date()
    const userMsg: Message = { sessionId, role: 'user', content: text, createdAt: now }
    const id = await db.messages.add(userMsg)
    setMessages((s)=>[...s, { ...userMsg, id }])

    try{
      const resp = await defaultClient.sendMessage([{ role: 'user', content: text } as ChatMessage])
      const assistantContent = (resp as any).choices?.[0]?.message?.content ?? 'No response'
      const usage = (resp as any).usage ? { prompt: (resp as any).usage.prompt_tokens || 0, completion: (resp as any).usage.completion_tokens || 0, total: (resp as any).usage.total_tokens || 0 } : undefined
      const reply: Message = { sessionId, role: 'assistant', content: assistantContent, createdAt: new Date(), tokenUsage: usage, model: (resp as any).model }
      const rid = await db.messages.add(reply)
      setMessages((s)=>[...s, { ...reply, id: rid }])
      if(usage?.total) addUsage(usage.total)
    }catch(e:any){
      console.error('send message failed', e)
      alert('Failed to send message: '+(e?.message ?? String(e)))
    }
  }

  return { messages, send }
}

export default function ChatWindow({ sessionId }:{ sessionId?:number }){
  const { messages, send } = useChat(sessionId)
  const listRef = useRef<HTMLDivElement|null>(null)
  const exportJson = async ()=>{
    if(!sessionId) return
    const msgs = await db.messages.where('sessionId').equals(sessionId).toArray()
    const blob = new Blob([JSON.stringify(msgs, null, 2)], { type: 'application/json' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `session-${sessionId}.json`
    document.body.appendChild(a)
    a.click()
    a.remove()
    URL.revokeObjectURL(url)
  }

  useEffect(()=>{ if(listRef.current) listRef.current.scrollTop = listRef.current.scrollHeight },[messages])

  if(!sessionId) return <div className="text-center text-[var(--muted-foreground)]">Select a chat</div>

  return (
    <div className="flex flex-col h-full">
      <div className="flex items-center justify-end p-2">
        <button onClick={exportJson} title="Download JSON" className="px-3 py-1 rounded bg-[var(--card)] text-[var(--card-foreground)]">Download JSON</button>
      </div>
      <div ref={listRef} className="flex-1 overflow-auto p-2">
        {messages.map(m=> (
          <MessageBubble key={m.id} role={m.role} content={m.content} usage={m.tokenUsage} model={(m as any).model} />
        ))}
      </div>
      <ChatInput onSend={send} />
    </div>
  )
}
