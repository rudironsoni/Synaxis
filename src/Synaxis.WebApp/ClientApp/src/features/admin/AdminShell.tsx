import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { Shield, Activity, Settings, LogOut, Cpu } from 'lucide-react'
import useSettingsStore from '@/stores/settings'
import { useAuth } from './useAuth'

function AdminNavLink({ to, icon: Icon, children }: { to: string; icon: React.ElementType; children: React.ReactNode }) {
  return (
    <NavLink
      to={to}
      className={({ isActive }) =>
        `flex items-center gap-3 px-4 py-3 rounded-lg transition-colors ${
          isActive
            ? 'bg-[var(--primary)]/10 text-[var(--primary)]'
            : 'text-[var(--muted-foreground)] hover:text-[var(--foreground)] hover:bg-[var(--muted)]/50'
        }`
      }
    >
      <Icon className="w-5 h-5" />
      <span className="font-medium">{children}</span>
    </NavLink>
  )
}

export default function AdminShell() {
  useAuth(true)
  const navigate = useNavigate()
  const logout = useSettingsStore((s: { logout: () => void }) => s.logout)

  const handleLogout = () => {
    logout()
    navigate('/admin/login')
  }

  return (
    <div className="min-h-screen w-full flex bg-[var(--background)]">
      <aside className="w-[260px] border-r border-[var(--border)] flex flex-col">
        <div className="p-6 border-b border-[var(--border)]">
          <div className="flex items-center gap-3">
            <Shield className="w-8 h-8 text-[var(--primary)]" />
            <div>
              <h1 className="text-xl font-bold text-[var(--foreground)]">Synaxis</h1>
              <p className="text-xs text-[var(--muted-foreground)]">Admin Panel</p>
            </div>
          </div>
        </div>

        <nav className="flex-1 p-4 space-y-1">
          <AdminNavLink to="/admin/health" icon={Activity}>
            Health Dashboard
          </AdminNavLink>
          <AdminNavLink to="/admin/providers" icon={Cpu}>
            Provider Config
          </AdminNavLink>
          <AdminNavLink to="/admin/settings" icon={Settings}>
            Settings
          </AdminNavLink>
        </nav>

        <div className="p-4 border-t border-[var(--border)]">
          <button
            onClick={handleLogout}
            className="flex items-center gap-3 px-4 py-3 w-full rounded-lg text-[var(--muted-foreground)] hover:text-[var(--foreground)] hover:bg-[var(--muted)]/50 transition-colors"
          >
            <LogOut className="w-5 h-5" />
            <span className="font-medium">Logout</span>
          </button>
        </div>
      </aside>

      <main className="flex-1 flex flex-col overflow-hidden">
        <header className="flex items-center justify-between px-6 py-4 border-b border-[var(--border)] bg-[var(--card)]">
          <h2 className="text-lg font-semibold text-[var(--foreground)]">Administration</h2>
          <div className="text-sm text-[var(--muted-foreground)]">
            Logged in as Administrator
          </div>
        </header>

        <div className="flex-1 overflow-auto p-6">
          <Outlet />
        </div>
      </main>
    </div>
  )
}
