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
import { AdminRoute } from './components/AdminRoute.tsx'
import DashboardLayout from './features/dashboard/DashboardLayout.tsx'
import DashboardOverview from './features/dashboard/DashboardOverview.tsx'
import DashboardProviders from './features/dashboard/DashboardProviders.tsx'
import DashboardAnalytics from './features/dashboard/DashboardAnalytics.tsx'
import DashboardKeys from './features/dashboard/DashboardKeys.tsx'
import DashboardModels from './features/dashboard/DashboardModels.tsx'
import DashboardChat from './features/dashboard/DashboardChat.tsx'

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
        <Route path="/dashboard" element={<DashboardLayout />}>
          <Route index element={<DashboardOverview />} />
          <Route path="providers" element={<DashboardProviders />} />
          <Route path="analytics" element={<DashboardAnalytics />} />
          <Route path="keys" element={<DashboardKeys />} />
          <Route path="models" element={<DashboardModels />} />
          <Route path="chat" element={<DashboardChat />} />
        </Route>
      </Routes>
    </BrowserRouter>
  </StrictMode>,
)
