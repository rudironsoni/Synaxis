import { useState, useRef, useEffect } from 'react'
import { Send } from 'lucide-react'

export default function ChatInput({ onSend }:{ onSend:(txt:string)=>void }){
  const [text, setText] = useState('')
  const taRef = useRef<HTMLTextAreaElement|null>(null)

  useEffect(()=>{
    const ta = taRef.current
    if(!ta) return
    ta.style.height = 'auto'
    ta.style.height = Math.min(200, ta.scrollHeight) + 'px'
  },[text])

  const submit = ()=>{
    if(!text.trim()) return
    onSend(text.trim())
    setText('')
  }

  return (
    <div className="mt-3 border-t border-[var(--border)] pt-3">
      <div className="flex gap-2">
        <textarea
          ref={taRef}
          value={text}
          onChange={(e)=>setText(e.target.value)}
          onKeyDown={(e)=>{
            if(e.key === 'Enter' && !e.shiftKey){ e.preventDefault(); submit() }
          }}
          placeholder="Type a message..."
          className="flex-1 resize-none rounded-md p-3 bg-[var(--input)] text-[var(--foreground)] border border-[var(--border)]"
          rows={1}
        />
        <button onClick={submit} className="bg-[var(--primary)] text-[var(--primary-foreground)] px-3 py-2 rounded">
          <Send className="w-4 h-4" />
        </button>
      </div>
    </div>
  )
}
