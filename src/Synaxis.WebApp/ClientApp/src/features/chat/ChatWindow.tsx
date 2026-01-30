import { useEffect, useState, useRef, useCallback } from 'react'
import MessageBubble from './MessageBubble'
import ChatInput from './ChatInput'
import db, { type Message } from '@/db/db'
import useSettingsStore from '@/stores/settings'
import { defaultClient, type ChatMessage, type ChatResponse, type ChatStreamChunk } from '@/api/client'
import useUsageStore from '@/stores/usage'

interface StreamingState {
  isStreaming: boolean
  streamingContent: string
  error?: Error
}

// basic chat hook to interact with db and a mock gateway
function useChat(sessionId?: number){
  const [messages, setMessages] = useState<Message[]>([])
  const [streaming, setStreaming] = useState<StreamingState>({
    isStreaming: false,
    streamingContent: '',
  })
  const gatewayUrl = useSettingsStore((s:any)=>s.gatewayUrl)
  const streamingEnabled = useSettingsStore((s:any)=>s.streamingEnabled)
  const addUsage = useUsageStore((s:any)=>s.addUsage)
  const abortControllerRef = useRef<AbortController | null>(null)

  useEffect(()=>{
    if(!sessionId) return
    let cancelled = false
    db.messages.where('sessionId').equals(sessionId).toArray().then(list=>{ if(!cancelled) setMessages(list as Message[]) })
    return ()=>{ cancelled = true }
  },[sessionId])

  useEffect(()=>{
    defaultClient.updateConfig(gatewayUrl)
  },[gatewayUrl])

  useEffect(() => {
    return () => {
      if (abortControllerRef.current) {
        abortControllerRef.current.abort()
      }
    }
  }, [sessionId])

  const sendNonStreaming = async (text: string, sessionId: number) => {
    const resp = await defaultClient.sendMessage([{ role: 'user', content: text } as ChatMessage]) as ChatResponse
    const assistantContent = resp.choices?.[0]?.message?.content ?? 'No response'
    const usage = resp.usage ? { prompt: resp.usage.prompt_tokens || 0, completion: resp.usage.completion_tokens || 0, total: resp.usage.total_tokens || 0 } : undefined
    const reply: Message = { sessionId, role: 'assistant', content: assistantContent, createdAt: new Date(), tokenUsage: usage }
    const rid = await db.messages.add(reply)
    setMessages((s)=>[...s, { ...reply, id: rid }])
    if(usage?.total) addUsage(usage.total)
  }

  const sendStreaming = async (text: string, sessionId: number) => {
    setStreaming({ isStreaming: true, streamingContent: '' })

    try {
      let fullContent = ''
      const messageHistory: ChatMessage[] = [{ role: 'user', content: text }]

      for await (const chunk of defaultClient.sendMessageStream(messageHistory)) {
        const content = chunk.choices?.[0]?.delta?.content
        if (content) {
          fullContent += content
          setStreaming({ isStreaming: true, streamingContent: fullContent })
        }

        if (chunk.choices?.[0]?.finish_reason === 'stop') {
          break
        }
      }

      const reply: Message = {
        sessionId,
        role: 'assistant',
        content: fullContent,
        createdAt: new Date(),
      }
      const rid = await db.messages.add(reply)
      setMessages((s)=>[...s, { ...reply, id: rid }])
      setStreaming({ isStreaming: false, streamingContent: '' })
    } catch (error) {
      console.error('Streaming error:', error)
      setStreaming({ isStreaming: false, streamingContent: '', error: error as Error })
      throw error
    }
  }

  const send = useCallback(async (text:string)=>{
    if(!sessionId) return
    const now = new Date()
    const userMsg: Message = { sessionId, role: 'user', content: text, createdAt: now }
    const id = await db.messages.add(userMsg)
    setMessages((s)=>[...s, { ...userMsg, id }])

    try{
      if (streamingEnabled) {
        await sendStreaming(text, sessionId)
      } else {
        await sendNonStreaming(text, sessionId)
      }
    }catch(e:any){
      console.error('send message failed', e)
      alert('Failed to send message: '+(e?.message ?? String(e)))
    }
  }, [sessionId, streamingEnabled])

  return { messages, send, streaming }
}

export default function ChatWindow({ sessionId }:{ sessionId?:number }){
  const { messages, send, streaming } = useChat(sessionId)
  const listRef = useRef<HTMLDivElement|null>(null)

  useEffect(()=>{ if(listRef.current) listRef.current.scrollTop = listRef.current.scrollHeight },[messages, streaming.streamingContent])

  if(!sessionId) return <div className="text-center text-[var(--muted-foreground)]">Select a chat</div>

  return (
    <div className="flex flex-col h-full">
      <div ref={listRef} className="flex-1 overflow-auto p-2">
        {messages.map(m=> (
          <MessageBubble key={m.id} role={m.role} content={m.content} usage={m.tokenUsage} />
        ))}
        {streaming.isStreaming && (
          <MessageBubble
            role="assistant"
            content={streaming.streamingContent}
            isStreaming={true}
          />
        )}
      </div>
      <ChatInput onSend={send} disabled={streaming.isStreaming} />
    </div>
  )
}
