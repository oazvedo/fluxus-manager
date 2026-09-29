import { createBrowserRouter } from 'react-router'
import { AppLayout } from '@/app/layout/AppLayout'
import { dashboardRoutes } from '@/features/dashboard'
import { empresasRoutes } from '@/features/empresas'
import { filiaisRoutes } from '@/features/filiais'
import { usuariosRoutes } from '@/features/usuarios'
import { perfisRoutes } from '@/features/perfis'
import { aceiteConviteRoutes, convitesRoutes } from '@/features/convites'
import { NotFound } from '@/shared/components/common/NotFound'
import { authRoutes } from '@/features/auth'
import { solicitacaoCadastroRoutes } from '@/features/onboarding'
import { plataformaRoutes } from '@/features/plataforma'
import { RequireSession } from './RequireSession'
import { PageLoading } from '@/shared/components/common/PageLoading'

/** Título de cada rota, exibido no cabeçalho (lido via useMatches). */
export type RouteHandle = { title: string }

export const router = createBrowserRouter([
  ...authRoutes,
  ...aceiteConviteRoutes,
  ...solicitacaoCadastroRoutes,
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
          ...perfisRoutes,
          ...convitesRoutes,
          ...plataformaRoutes,
          { path: '*', Component: NotFound, handle: { title: 'Página não encontrada' } satisfies RouteHandle },
        ],
      },
    ],
  },
])
