import { useState } from 'react'
import { Outlet, NavLink } from 'react-router-dom'
import { LayoutDashboard, Server, BarChart3, Key, Settings, MessageSquare } from 'lucide-react'
import Badge from '@/components/ui/Badge'
import SettingsDialog from '@/features/settings/SettingsDialog'
import useSettingsStore from '@/stores/settings'

export default function DashboardLayout() {
  const [settingsOpen, setSettingsOpen] = useState(false)
  const [sidebarOpen] = useState(true)
  const costRate = useSettingsStore((s) => s.costRate)

  const navItems = [
    { path: '/dashboard', icon: LayoutDashboard, label: 'Overview' },
    { path: '/dashboard/providers', icon: Server, label: 'Providers' },
    { path: '/dashboard/analytics', icon: BarChart3, label: 'Analytics' },
    { path: '/dashboard/keys', icon: Key, label: 'API Keys' },
    { path: '/dashboard/models', icon: Settings, label: 'Models' },
    { path: '/dashboard/chat', icon: MessageSquare, label: 'Chat' },
  ]

  return (
    <div className="min-h-screen w-full flex">
      {sidebarOpen && (
        <aside className="w-[260px] border-r border-[var(--border)] p-4 flex flex-col">
          <div className="mb-6">
            <h1 className="text-xl font-bold">Synaxis</h1>
            <p className="text-sm text-[var(--muted-foreground)]">Dashboard</p>
          </div>
          <nav className="space-y-1 flex-1">
            {navItems.map((item) => (
              <NavLink
                key={item.path}
                to={item.path}
                className={({ isActive }) =>
                  `flex items-center gap-3 px-3 py-2 rounded-md transition-colors ${
                    isActive
                      ? 'bg-[var(--accent)] text-[var(--accent-foreground)]'
                      : 'hover:bg-[rgba(255,255,255,0.02)]'
                  }`
                }
              >
                <item.icon className="w-5 h-5" />
                <span>{item.label}</span>
              </NavLink>
            ))}
          </nav>
        </aside>
      )}

      <div className="flex-1 flex flex-col">
        <header className="flex items-center justify-between px-6 py-4 border-b border-[var(--border)]">
          <div className="flex items-center gap-4">
            <h2 className="text-xl font-semibold">Dashboard</h2>
          </div>
          <div className="flex items-center gap-3">
            <Badge>Miser: ${costRate.toFixed(2)}</Badge>
            <button
              type="button"
              onClick={() => setSettingsOpen(true)}
              title="Settings"
              className="p-2 rounded hover:bg-[rgba(255,255,255,0.02)]"
            >
              <Settings className="w-5 h-5" />
            </button>
          </div>
        </header>
        <main className="p-6 flex-1 overflow-auto">
          <Outlet />
        </main>
      </div>

      <SettingsDialog open={settingsOpen} onClose={() => setSettingsOpen(false)} />
    </div>
  )
}
