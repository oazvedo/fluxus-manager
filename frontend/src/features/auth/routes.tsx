import type { RouteObject } from 'react-router'
import { PageLoading } from '@/shared/components/common/PageLoading'

export const authRoutes: RouteObject[] = [{
  path: '/login',
  HydrateFallback: PageLoading,
  lazy: async () => ({ Component: (await import('./pages/LoginPage')).LoginPage }),
}, {
  path: '/esqueci-senha',
  HydrateFallback: PageLoading,
  lazy: async () => ({ Component: (await import('./pages/ForgotPasswordPage')).ForgotPasswordPage }),
}, {
  path: '/redefinir-senha',
  HydrateFallback: PageLoading,
  lazy: async () => ({ Component: (await import('./pages/ResetPasswordPage')).ResetPasswordPage }),
}]
