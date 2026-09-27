import { createBrowserRouter } from 'react-router'
import { AppLayout } from '@/app/layout/AppLayout'
import { dashboardRoutes } from '@/features/dashboard'
import { empresasRoutes } from '@/features/empresas'
import { usuariosRoutes } from '@/features/usuarios'
import { ComingSoon } from '@/shared/components/common/ComingSoon'
import { NotFound } from '@/shared/components/common/NotFound'

/** Título de cada rota, exibido no cabeçalho (lido via useMatches). */
export type RouteHandle = { title: string }

// Módulos ainda não implementados: cada um vira uma feature com suas próprias rotas.
const comingSoon = (path: string, title: string) => ({
  path,
  element: <ComingSoon title={title} />,
  handle: { title } satisfies RouteHandle,
})

export const router = createBrowserRouter([
  {
    path: '/',
    Component: AppLayout,
    children: [
      ...dashboardRoutes,
      ...empresasRoutes,
      ...usuariosRoutes,
      comingSoon('filiais', 'Filiais'),
      comingSoon('perfis', 'Perfis e permissões'),
      { path: '*', Component: NotFound, handle: { title: 'Página não encontrada' } satisfies RouteHandle },
    ],
  },
])
