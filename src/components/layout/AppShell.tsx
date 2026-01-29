import React, { useState, useEffect } from 'react'
import { Settings } from 'lucide-react'
import Badge from '@/components/ui/Badge'
import SettingsDialog from '@/features/settings/SettingsDialog'
import SessionList from '@/features/sessions/SessionList'
import useSettingsStore from '@/stores/settings'
import useUsageStore from '@/stores/usage'

export default function AppShell({ children }: { children: React.ReactNode }) {
  const [open, setOpen] = useState(false)
  const [sidebarOpen, setSidebarOpen] = useState(true)
  const costRate = useSettingsStore((s: any) => s.costRate)
  const totalTokens = useUsageStore((s) => s.totalTokens)
  const saved = (totalTokens / 1000) * (costRate || 0)

  useEffect(()=>{
    // hide sidebar on small screens
    const m = window.matchMedia('(max-width: 640px)')
    const fn = () => setSidebarOpen(!m.matches)
    fn()
    m.addEventListener('change', fn)
    return ()=> m.removeEventListener('change', fn)
  },[])

  return (
    <div className="min-h-screen w-full flex">
      {sidebarOpen && (
        <aside className="w-[260px] border-r border-[var(--border)] p-4">
          <SessionList />
        </aside>
      )}
      <div className="flex-1 flex flex-col">
        <header className="flex items-center justify-between px-6 py-4 border-b border-[var(--border)]">
          <div className="flex items-center gap-4">
            <h2 className="text-xl font-semibold">Synaxis</h2>
          </div>
          <div className="flex items-center gap-3">
            <Badge>Saved: ${saved.toFixed(4)}</Badge>
            <button onClick={()=>setOpen(true)} title="Settings" className="p-2 rounded hover:bg-[rgba(255,255,255,0.02)]">
              <Settings className="w-5 h-5" />
            </button>
          </div>
        </header>
        <main className="p-6 flex-1 overflow-hidden">{children}</main>
      </div>

      <SettingsDialog open={open} onClose={()=>setOpen(false)} />
    </div>
  )
}
