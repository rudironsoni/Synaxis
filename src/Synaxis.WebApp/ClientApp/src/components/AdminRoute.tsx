import { Navigate } from 'react-router-dom'
import useSettingsStore, { type SettingsState } from '@/stores/settings'

export function AdminRoute({ children }: { children: React.ReactNode }) {
  const jwtToken = useSettingsStore((s: SettingsState) => s.jwtToken)
  return jwtToken ? <>{children}</> : <Navigate to="/admin/login" />
}
