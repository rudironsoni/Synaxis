import { useState, useRef, useEffect } from 'react'
import { Send, Zap } from 'lucide-react'
import useSettingsStore from '@/stores/settings'

type ChatInputProps = {
  onSend: (txt: string) => void
  disabled?: boolean
}

export default function ChatInput({ onSend, disabled }: ChatInputProps) {
  const [text, setText] = useState('')
  const taRef = useRef<HTMLTextAreaElement|null>(null)
  const streamingEnabled = useSettingsStore((s: { streamingEnabled: boolean }) => s.streamingEnabled)
  const setStreamingEnabled = useSettingsStore((s: { setStreamingEnabled: (v: boolean) => void }) => s.setStreamingEnabled)

  useEffect(() => {
    const ta = taRef.current
    if (!ta) return
    ta.style.height = 'auto'
    ta.style.height = Math.min(200, ta.scrollHeight) + 'px'
  })

  const submit = ()=>{
    if(!text.trim() || disabled) return
    onSend(text.trim())
    setText('')
  }

  const toggleStreaming = () => {
    setStreamingEnabled(!streamingEnabled)
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
          placeholder={disabled ? "Waiting for response..." : "Type a message..."}
          className="flex-1 resize-none rounded-md p-3 bg-[var(--input)] text-[var(--foreground)] border border-[var(--border)] disabled:opacity-50"
          rows={1}
          disabled={disabled}
        />
        <button
          type="button"
          aria-label="Send"
          onClick={submit}
          disabled={disabled}
          className="bg-[var(--primary)] text-[var(--primary-foreground)] px-3 py-2 rounded disabled:opacity-50 disabled:cursor-not-allowed"
        >
          <Send className="w-4 h-4" />
        </button>
      </div>
      <div className="flex items-center gap-2 mt-2 px-1">
        <button
          type="button"
          onClick={toggleStreaming}
          disabled={disabled}
          className={`flex items-center gap-1.5 text-xs px-2 py-1 rounded transition-colors ${
            streamingEnabled
              ? 'bg-[var(--primary)]/10 text-[var(--primary)]'
              : 'text-[var(--muted-foreground)] hover:text-[var(--foreground)]'
          } disabled:opacity-50 disabled:cursor-not-allowed`}
          aria-label={streamingEnabled ? 'Disable streaming' : 'Enable streaming'}
          title={streamingEnabled ? 'Streaming enabled - responses will appear word by word' : 'Streaming disabled - responses will appear all at once'}
        >
          <Zap className={`w-3 h-3 ${streamingEnabled ? 'fill-current' : ''}`} />
          <span>Streaming {streamingEnabled ? 'ON' : 'OFF'}</span>
        </button>
      </div>
    </div>
  )
}
