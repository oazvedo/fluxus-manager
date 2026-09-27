import { createBrowserRouter } from 'react-router'
import { AppLayout } from '@/app/layout/AppLayout'
import { dashboardRoutes } from '@/features/dashboard'
import { empresasRoutes } from '@/features/empresas'
import { filiaisRoutes } from '@/features/filiais'
import { usuariosRoutes } from '@/features/usuarios'
import { ComingSoon } from '@/shared/components/common/ComingSoon'
import { NotFound } from '@/shared/components/common/NotFound'
import { authRoutes } from '@/features/auth'
import { RequireSession } from './RequireSession'
import { PageLoading } from '@/shared/components/common/PageLoading'

/** Título de cada rota, exibido no cabeçalho (lido via useMatches). */
export type RouteHandle = { title: string }

// Módulos ainda não implementados: cada um vira uma feature com suas próprias rotas.
const comingSoon = (path: string, title: string) => ({
  path,
  element: <ComingSoon title={title} />,
  handle: { title } satisfies RouteHandle,
})

export const router = createBrowserRouter([
  ...authRoutes,
  {
    Component: RequireSession,
    HydrateFallback: PageLoading,
    children: [
      {
        path: '/',
        Component: AppLayout,
        children: [
          ...dashboardRoutes,
          ...empresasRoutes,
          ...usuariosRoutes,
          ...filiaisRoutes,
          comingSoon('perfis', 'Perfis e permissões'),
          { path: '*', Component: NotFound, handle: { title: 'Página não encontrada' } satisfies RouteHandle },
        ],
      },
    ],
  },
])
