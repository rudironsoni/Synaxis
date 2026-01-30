import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import './index.css'
import App from './App.tsx'
import AdminShell from './features/admin/AdminShell.tsx'
import AdminLogin from './features/admin/AdminLogin.tsx'
import HealthDashboard from './features/admin/HealthDashboard.tsx'
import ProviderConfig from './features/admin/ProviderConfig.tsx'
import AdminSettings from './features/admin/AdminSettings.tsx'
import useSettingsStore from './stores/settings.ts'

function AdminRoute({ children }: { children: React.ReactNode }) {
  const jwtToken = useSettingsStore((s: any) => s.jwtToken)
  return jwtToken ? <>{children}</> : <Navigate to="/admin/login" />
}

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<App />} />
        <Route path="/admin/login" element={<AdminLogin />} />
        <Route
          path="/admin/*"
          element={
            <AdminRoute>
              <AdminShell />
            </AdminRoute>
          }
        >
          <Route index element={<Navigate to="/admin/health" />} />
          <Route path="health" element={<HealthDashboard />} />
          <Route path="providers" element={<ProviderConfig />} />
          <Route path="settings" element={<AdminSettings />} />
        </Route>
      </Routes>
    </BrowserRouter>
  </StrictMode>,
)
