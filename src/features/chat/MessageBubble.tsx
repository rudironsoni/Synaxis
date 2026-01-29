import React from 'react'

type Props = { role: 'user'|'assistant'|'system'; content: string; usage?: { prompt:number; completion:number; total:number } }

export default function MessageBubble({ role, content, usage }: Props){
  const isUser = role === 'user'
  const base = 'max-w-[70%] p-3 rounded-md my-2 text-sm'
  const userCls = 'ml-auto bg-[var(--primary)] text-[var(--primary-foreground)]'
  const assistantCls = 'mr-auto bg-[var(--card)] text-[var(--card-foreground)] border border-[var(--border)]'

  return (
    <div className={isUser ? `flex justify-end` : `flex justify-start`}>
      <div className={`${base} ${isUser ? userCls : assistantCls}`}> 
        <div className="whitespace-pre-wrap">{content}</div>
        {usage && (
          <div className="mt-2 text-xs text-[var(--muted-foreground)]">Tokens: {usage.total} (p:{usage.prompt} c:{usage.completion})</div>
        )}
      </div>
    </div>
  )
}
