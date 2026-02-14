import { createRootRoute, Outlet } from '@tanstack/react-router'
import { AdminLayout } from '../components/layout/AdminLayout'

export const Route = createRootRoute({
  component: () => (
    <AdminLayout>
      <Outlet />
    </AdminLayout>
  ),
})
