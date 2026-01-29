import type React from 'react'

export default function Modal({ open, onClose, title, children }:{ open:boolean; onClose:()=>void; title?:string; children?:React.ReactNode }){
  if(!open) return null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-black/60" onClick={onClose} />
      <div className="relative z-10 w-full max-w-lg rounded-lg bg-[var(--card)] p-6">
        {title && <h3 className="text-lg font-semibold text-[var(--card-foreground)] mb-4">{title}</h3>}
        <div className="text-sm text-[var(--card-foreground)]">{children}</div>
        <div className="mt-4 flex justify-end">
          <button onClick={onClose} className="px-3 py-1 rounded bg-[var(--primary)] text-[var(--primary-foreground)]">Close</button>
        </div>
      </div>
    </div>
  )
}
