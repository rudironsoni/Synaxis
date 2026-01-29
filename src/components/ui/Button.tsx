import type React from 'react'
import clsx from 'clsx'

type Variant = 'primary' | 'ghost' | 'danger'

export default function Button({
  children,
  variant = 'primary',
  className,
  ...props
}: React.ButtonHTMLAttributes<HTMLButtonElement> & { variant?: Variant; className?: string }) {
  const base = 'inline-flex items-center gap-2 rounded-md px-3 py-2 text-sm font-medium'
  const variants: Record<Variant, string> = {
    primary: 'bg-[var(--primary)] text-[var(--primary-foreground)] hover:brightness-95',
    ghost: 'bg-transparent text-[var(--foreground)] border border-[var(--border)] hover:bg-[rgba(255,255,255,0.02)]',
    danger: 'bg-red-600 text-white hover:bg-red-700',
  }

  return (
    <button className={clsx(base, variants[variant], className)} {...props}>
      {children}
    </button>
  )
}
