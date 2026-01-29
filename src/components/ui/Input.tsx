import type React from 'react'

export default function Input(props: React.InputHTMLAttributes<HTMLInputElement>) {
  return (
    <input
      {...props}
      className={`w-full rounded-md px-3 py-2 text-sm bg-[var(--input)] border border-[var(--border)] text-[var(--foreground)] placeholder:[var(--muted-foreground)] focus:outline-none focus:ring-2 focus:ring-[var(--ring)] ${props.className ?? ''}`}
    />
  )
}
