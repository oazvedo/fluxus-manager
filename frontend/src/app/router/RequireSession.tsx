import { Navigate, Outlet, useLocation } from 'react-router'
import { useSession } from '@/core/auth/session'

export function RequireSession() {
  const session = useSession()
  const location = useLocation()
  if (!session) return <Navigate to="/login" replace state={{ from: location.pathname + location.search }} />
  return <Outlet />
}
