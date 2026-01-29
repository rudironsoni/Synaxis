import type React from 'react'

export default function Badge({ children, className }: { children: React.ReactNode; className?: string }) {
  return (
    <span className={`inline-flex items-center gap-2 rounded-full px-2 py-1 text-xs font-medium bg-[var(--muted)] text-[var(--muted-foreground)] ${className ?? ''}`}>
      {children}
    </span>
  )
}
